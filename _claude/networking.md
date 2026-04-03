# Island Mayhem — Networking Analysis

## Stack
- **Networking framework:** Mirror (vendored in `Assets/ExternalAssets/Mirror/` — no Package Manager, no version pinning)
- **Transport:** Steam P2P via Steamworks.NET (also vendored in ExternalAssets)
- **Lobby system:** Steam Matchmaking API (public lobbies, manual browse/join)
- **URP version:** 17.4.0

> Mirror being vendored means there is no easy upgrade path and no git history for the Mirror version. This is a risk for future Mirror API changes.

---

## How Steam Is Used

### What Steam provides here
Steam handles two separate concerns that are easy to conflate:
1. **Lobby/matchmaking** — a "room" that players can find and join, with arbitrary key/value metadata
2. **Transport address** — the host's Steam ID doubles as the network address (Steamworks P2P uses Steam IDs to route packets)

### Steam API calls made in this project

| Call | Where | What it does |
|------|-------|-------------|
| `SteamAPI.Init()` | `CustomNetworkManager.Start()` | Initialises the Steam API on startup |
| `SteamAPI.RunCallbacks()` | `CustomNetworkManager.LateUpdate()` | Must be called every frame for Steam callbacks to fire |
| `SteamMatchmaking.CreateLobby(Public, maxConnections)` | `SteamLobby.HostLobby()` | Creates a Steam lobby — result fires `OnLobbyCreated` callback |
| `SteamMatchmaking.SetLobbyData(id, "host", steamId)` | `SteamLobby.OnLobbyCreated()` | Stores host Steam ID in lobby metadata |
| `SteamMatchmaking.SetLobbyData(id, "hostName", name)` | `SteamLobby.OnLobbyCreated()` | Stores host display name for lobby list UI |
| `SteamMatchmaking.SetLobbyData(id, "HostAddress", steamId)` | `SteamLobby.OnLobbyCreated()` | **This is the Mirror transport address** — same value as "host" |
| `SteamMatchmaking.SetLobbyData(id, "game", "eyalgame")` | `SteamLobby.OnLobbyCreated()` | Filter tag so only this game's lobbies appear |
| `networkManager.StartHost()` | `SteamLobby.OnLobbyCreated()` | Starts Mirror host immediately after lobby created |
| `SteamMatchmaking.AddRequestLobbyListStringFilter(...)` | `SteamLobby.GetLobbies()` | Filters for `"game" == "eyalgame"` |
| `SteamMatchmaking.RequestLobbyList()` | `SteamLobby.GetLobbies()` | Fetches matching lobbies — result fires `OnLobbyMatchList` |
| `SteamMatchmaking.GetLobbyByIndex(i)` | `SteamLobby.OnLobbyMatchList()` | Iterates results |
| `SteamMatchmaking.GetLobbyData(id, ...)` | `SteamLobby.OnLobbyMatchList()` | Reads host name, player count for UI |
| `SteamMatchmaking.JoinLobby(id)` | `SteamLobby.JoinLobby()` | Joins a lobby — result fires `OnLobbyEntered` |
| `SteamMatchmaking.GetLobbyData(id, "HostAddress")` | `SteamLobby.OnLobbyEntered()` | Gets host Steam ID to use as Mirror's `networkAddress` |
| `networkManager.StartClient()` | `SteamLobby.OnLobbyEntered()` | Starts Mirror client with host's Steam ID as address |
| `SteamMatchmaking.GetNumLobbyMembers(id)` | `SteamLobby.OnLobbyEntered()` | Server-side: closes lobby when full |
| `SteamMatchmaking.SetLobbyJoinable(id, false)` | `SteamLobby.OnLobbyEntered()` | Prevents new joins when player count reached |
| `SteamMatchmaking.LeaveLobby(id)` | `SteamLobby.LeaveLobby()` | Called from `MatchManager.RpcStartGame()` when game starts |
| `SteamFriends.GetPersonaName()` | `SteamLobby.OnLobbyCreated()` | Host display name |
| `SteamFriends.GetFriendPersonaName(steamId)` | `NetworkPlayer.Start()` (local) | Sets player's display name via Cmd |
| `SteamFriends.SetRichPresence(...)` | `CustomNetworkManager.OnStartClient/OnStopClient` | Sets Steam status ("In Game" / "In Menu") and room ID |
| `SteamFriends.ClearRichPresence()` | `CustomNetworkManager.OnApplicationQuit()` | Clears on quit |

### Active Steam Callbacks

| Callback | Handler | Notes |
|----------|---------|-------|
| `LobbyCreated_t` | `OnLobbyCreated` | Active ✅ |
| `LobbyEnter_t` | `OnLobbyEntered` | Active ✅ — fires for BOTH host and client when lobby is joined |
| `LobbyMatchList_t` | `OnLobbyMatchList` | Active ✅ |
| `GameLobbyJoinRequested_t` | `OnGameLobbyJoinRequested` | **COMMENTED OUT** ❌ — Steam "Join Game" from friend list/overlay won't work |

### Key design notes on Steam usage
- **The address IS the Steam ID**: The Steamworks transport routes packets using Steam user IDs. `networkManager.networkAddress = hostSteamId` — this string looks like a number (e.g. `"76561198012345678"`) but is the Steam64 ID.
- **`OnLobbyEntered` fires for both host AND client**: The early return `if (NetworkServer.active) return` is what separates host vs client behaviour in that callback. The host does nothing (already started), the client starts their Mirror connection.
- **Lobby is left when game starts**: `MatchManager.RpcStartGame()` calls `steamLobby.LeaveLobby()`. After this point there's no Steam lobby — the connection is purely Mirror/Steam transport P2P.
- **No re-joining**: If a player disconnects mid-game, there's no way back in. The lobby is gone.

---

## How Mirror Is Used

### CustomNetworkManager — What it actually does

Mirror's `NetworkManager` handles connection lifecycle, scene management, and player object spawning. `CustomNetworkManager` extends it and adds:

**Static player registry** (global access pattern):
```
playersDic: Dictionary<string, NetworkIdentity>  — "Player_1" → NetworkIdentity
localPlayerId: string
localPlayerInitialized: bool
```
`NetworkPlayer.OnStartClient()` calls `CustomNetworkManager.RegisterPlayer()`. Everything that needs to find a player by netId goes through `GetPlayerByNetId(netId)`. This is a singleton-like static access pattern.

**Overridden lifecycle hooks used:**
| Hook | What's done |
|------|------------|
| `Start()` | `SteamAPI.Init()` |
| `LateUpdate()` | `SteamAPI.RunCallbacks()` |
| `OnServerReady(conn)` | `matchManager.RpcSyncTeamInfo(...)` — syncs team data to newly ready client |
| `OnServerDisconnect(conn)` | `ResetManager()` |
| `OnStartClient()` | Steam rich presence, find+call `OnClientStartStop[]` |
| `OnStopClient()` | Steam rich presence, `ResetManager()`, call `OnClientStartStop[]` |
| `OnStopHost()` | `ResetManager()` |
| `OnApplicationQuit()` | `SteamFriends.ClearRichPresence()` |

**Overridden hooks that do nothing (just call base):**
`Awake`, `OnValidate`, `ConfigureServerFrameRate`, `OnServerConnect`, `OnServerAddPlayer`, `OnServerChangeScene`, `OnServerSceneChanged`, `OnClientSceneChanged`, `OnClientConnect`, `OnClientNotReady`, `OnStartServer`, `OnStartHost`, `OnStopServer`

Most of the mirror lifecycle is inherited default behaviour.

### Mirror authority model used
The game uses a **server-authoritative with client commands** pattern:

```
LocalPlayer detects input
    └─ Cmd(netId, data)  →  server looks up player  →  Rpc(data)  →  all clients apply
```

The netId is always sent explicitly as a parameter rather than using `[Command]` on the owning object. Every significant game action follows this exact pattern: jump, punch, hit, regen, revive, pickup, drop, throw.

**Why netId is passed explicitly:** Because `[Command]` only runs on the server AND requires the caller to be the owning client. By looking up the player via `CustomNetworkManager.GetPlayerByNetId()` on the server, any client in theory could call commands affecting other players — though in practice only the local player sends commands for itself. This pattern is fragile but works.

### Player spawning
Mirror's default `OnServerAddPlayer` spawns the `playerPrefab` assigned in the NetworkManager inspector. No custom spawning logic is added. Players are spawned via `ClientScene.AddPlayer` as part of Mirror's standard connection flow.

---

## Transform Synchronisation — The Full Story

This is the most architecturally confused part of the codebase. There are **three separate position sync implementations** across players and items:

### 1. NetworkPlayer SyncVar sync (for players)
```
Local player FixedUpdate:
    CmdUpdatePlayerTransform(netId, pos, rot, vel, scale)
        → server sets SyncVars: netPlayerPos, netPlayerRot, netPlayerVel, netPlayerSize
            → Mirror auto-replicates SyncVars to all clients
                → remote clients FixedUpdate: Lerp toward SyncVar values
```
- **Problems**: Sends a Cmd every FixedUpdate regardless of whether anything moved. No sensitivity threshold. No proper interpolation (just Lerp, no timestamping). Scale is synced unnecessarily (changes only during buffs). Position sent as `Vector3` SyncVar (12 bytes each, uncompressed).

### 2. CustomNetworkTransformBase (also on players?)
A significantly better implementation:
- Sensitivity thresholds (only sends if moved > 0.01)
- Proper timestamped DataPoints with start/goal interpolation
- Compressed quaternion (4 bytes instead of 16)
- Teleport detection (if interpolation takes 5x expected time, teleport instead)
- Client authority mode (`clientAuthority = true`) — client sends to server, server broadcasts
- Server teleport API with authority restoration

**The problem:** It's unclear whether `CustomNetworkTransform` is actually on the player prefab. If BOTH systems are on the same player object, they would conflict directly — two systems writing to the same transform every frame.

> ⚠️ This needs to be verified in the Unity scene/prefab. If both are active on the player, one must be removed.

### 3. PickupableItem SyncVar sync (for items)
```
Server FixedUpdate:
    netPos = transform.position  (SyncVar — auto-replicated)
    netRot = transform.rotation
    netVel = rb.linearVelocity

Client FixedUpdate (if !isBeingHeld):
    Lerp/teleport toward netPos, netRot, netVel
```
- Same problems as NetworkPlayer sync
- When held: client doesn't lerp (item follows player transform), server still writes SyncVars
- Item physics (`rb.AddForce`, gravity) run on ALL clients but only server values are authoritative

---

## Match Lifecycle — Detailed

### Server-side state machine (implicit, in `MatchManager.Update()`)
```
State: Waiting
    Condition: players >= numberOfPlayersNeededToStart OR Enter key pressed
    → CalculateAndAssignTeams()
    → RpcSyncTeamInfo(json)
    → RpcStartGame()  [ClientRpc → all clients]

State: Starting (TTTMatchManager)
    PopulateTeamObjectives()  [runs on ALL clients]
    Countdown coroutine: gameStatus SyncVar → "3" → "2" → "1" → "GO!"
    base.StartGame():
        OnMatchStartStop[].OnMatchStart()  [via FindObjectsOfType]
        gameStarted = true  [server SyncVar]
        ItemSpawner.Spawn()  [server only]

State: In Game
    TTTMatchManager.Update() checks: didTeam0Win || didTeam1Win  [server only]
    SingelTotemMatchManager also checks: matchTimer.IsTimeOver  [server only]
    → RpcEndGame(winnerIndex)

State: Ending
    base.EndGame(): OnMatchStartStop[].OnMatchStop(), gameOver = true [server]
    Wait 1s → show winner text
    Wait 5s → StopClient/StopHost
    SceneManager.LoadScene(currentScene)  [reloads entire scene]
```

### Team sync approach — why it's complicated
Mirror's `SyncList<Team>` wasn't working reliably (probably because `Team` contains a `List<NetworkPlayer>` which Mirror can't auto-serialize). The workaround:
- Server serializes teams to JSON via `JsonUtility.ToJson(new TeamInfo(teams))`
- Sends via `RpcSyncTeamInfo(jsonString)` 
- Clients deserialize, but only if `teams.Count <= 0` (won't overwrite if already populated)
- Also called from `CustomNetworkManager.OnServerReady()` for late-joining clients

This is fragile: if a client already has some teams data, it silently ignores the update.

### MatchTimer sync
`timeRemaining` is a `SyncVar` on a `NetworkBehaviour`. Server ticks it down every `FixedUpdate`. Clients receive automatic updates. Clean design.

Minor bug in `MatchStarted()`:
```csharp
startTime = Time.time;
timeRemaining = matchLength - (Time.time - startTime); // always = matchLength - 0 = matchLength
```
`startTime` is set and immediately used in the same line — the subtraction always equals 0. Harmless, but the code is confused about its own intent.

---

## Item Networking — Pickup/Drop/Throw

### Authority model for items
Items are `NetworkBehaviour` objects — spawned server-side via `ItemSpawner`. The server is always authoritative over item position (via SyncVars). Clients display items by lerping to server-broadcast position.

### Pickup flow
```
Local player presses E near item
    CmdPickupItem(playerNetId, itemNetId)
        → server validates: item exists in NetworkIdentity.spawned, !isBeingHeld
        → pp.RpcPickup(itemNetId)  [all clients]
            → Pickup(item): parent item under player hand, rb.isKinematic = true
```

### Throw flow  
```
Local player presses Mouse0 while holding throwable
    ThrowVec calculated from camera aim + player velocity
    CmdUseItem(playerNetId, pos, rot, vel, throwVec, true)
        → RpcUseItem(...)  [all clients]
            → ThrowableItem.UseMain(): Drop(), rb.AddForce(throwVec)
                [OR if autoAim active]: rb.isKinematic = true, homing starts
```

### Auto-aim (recent feature)
`PlayerThrowTargetController` is a local-only UI component. When aiming (Mouse1 held), trajectory `DrawThrowTrajectory()` raycasts. If the trajectory hits a `PlayerThrowTargetController` collider:
- `Target(player)` → `player.ShowTarget()` → starts a 1.5s coroutine
- After 0.75s: aim sprite changes to aim2
- After 1.5s: aim sprite changes to aim3, `isAutoAim = true`

When thrown with autoAim active, `ThrowableItem.UseMain()` sets `rb.isKinematic = true` and starts homing:
```csharp
rb.position = Vector3.Lerp(posWhenThrown, targetedTransform.position, timeElapsed * targetedThrowSpeed)
```
⚠️ This homing runs in `Update()` with no authority guard. All clients run this code. Since `PickupableItem.FixedUpdate()` on server overwrites `netPos` with `rb.position`, and server/throwing-client both run homing, this may work — but it's untested and could diverge on non-throwing clients.

---

## Event Systems

### OnClientStartStop / OnMatchStartStop pattern
Two parallel callback patterns:
- `OnClientStartStop` (base) + `OnClientStartStopEvents` (UnityEvent wrapper)
- `OnMatchStartStop` (base) + `OnMatchStartStopEvents` (UnityEvent wrapper)

Used as an observer pattern: `CustomNetworkManager` and `MatchManager` do `FindObjectsOfType<T>()` and call the methods. Objects in the scene that need to react to connection/match events inherit from these classes or use the Events variant with inspector-assigned callbacks.

⚠️ `FindObjectsOfType<T>()` is called at match start/stop. In a large scene this is a frame hitch. The list should be cached once.

### RandomEventSystem (currently disabled)
A server-driven random event loop:
- Server picks a random event from `events[]` array after a random delay (30-120 seconds)
- Calls `event.ServerEvent()` on server
- Calls `RpcStartClientEvent(eventName)` on all clients

Events: `CoconutEvent`, `FloorIsLavaEvent`, `ZeroGravityEvent` (all extend `RandomEvent`).
**The entire Update() logic that starts this is commented out.** Events never fire.

---

## Voice Chat

`SteamlessVoiceChat` is a custom push-to-talk implementation built on top of Mirror:
- Records mic audio via `Microphone.Start()` on local player
- Reads mic in chunks (`chunkSize = 256` samples)
- On V key held: serializes chunk as `VoicePacket` → `CmdSendData(bytes)` → `ClientReceiveData(bytes)` Rpc
- Remote clients queue packets and play them back via a continuously running `AudioClip` callback

⚠️ **Uses `BinaryFormatter` for serialization** — deprecated in .NET 5+, generates security warnings in modern .NET. Should be replaced with `System.Text.Json` or direct float array serialization.

⚠️ **Proximity filtering is commented out** — voice is broadcast to ALL players regardless of distance. The commented code had the right idea.

⚠️ **No spatial audio** — `source.spatialize = true` is set, but the comment says "not working for some reason". Voice comes from the remote player's AudioSource position in world space but spatialisation isn't actually working.

---

## Known Issues — Full List

### 🔴 High — correctness/stability

| # | Issue | Location |
|---|-------|----------|
| 1 | `OnLevelWasLoaded(int level)` is Unity 4-era API, unreliable in Unity 6 | `CustomNetworkManager.cs:31`, `TTTMatchManager.cs:133` |
| 2 | `ResetManager()` called from 4 hooks that can all fire together on host disconnect: `OnServerDisconnect`, `OnStopHost`, `OnStopClient`, and `OnLevelWasLoaded` | `CustomNetworkManager.cs` |
| 3 | `GameLobbyJoinRequested_t` callback commented out — Steam friend invites and Steam overlay "Join Game" are broken | `SteamLobby.cs:23,44,90-93` |
| 4 | Player disconnecting mid-game triggers `OnServerDisconnect` → `ResetManager()` → entire game resets for everyone | `CustomNetworkManager.cs:239` |

### 🟡 Medium — behaviour/data issues

| # | Issue | Location |
|---|-------|----------|
| 5 | Dual (possibly triple) transform sync — `NetworkPlayer.CmdUpdatePlayerTransform` AND `CustomNetworkTransformBase` may both be active on players | `NetworkPlayer.cs`, `CustomNetworkTransformBase.cs` |
| 6 | `BinaryFormatter` used for voice chat serialization — deprecated, security flagged | `SteamlessVoiceChat.cs:285-315` |
| 7 | `RpcSyncTeamInfo` won't update clients that already have team data (the `teams.Count <= 0` guard) | `MatchManager.cs:245` |
| 8 | `MatchStarted()` timer bug — `startTime` subtraction always yields 0 | `MatchTimer.cs:30-32` |
| 9 | Auto-aim homing in `ThrowableItem.Update()` has no authority guard — runs on all clients | `ThrowableItem.cs:52-63` |
| 10 | Voice chat broadcast to all players (proximity filter commented out) | `SteamlessVoiceChat.cs:139-153` |
| 11 | `FindObjectsOfType` called at match start/stop for event broadcasting (frame hitch) | `MatchManager.cs:165`, `CustomNetworkManager.cs:360` |

### 🟠 Low — debug leftovers / code smell

| # | Issue | Location |
|---|-------|----------|
| 12 | `SteamLobby.Update()` has G/H hotkeys for host/join | `SteamLobby.cs:53-62` |
| 13 | `MatchManager.Update()` has `Input.GetKeyDown(KeyCode.Return)` to force-start match | `MatchManager.cs:83` |
| 14 | `LocalPlayerInput` also maps Return to `interactInputDown` — same key, different systems | `LocalPlayerInput.cs:34` |
| 15 | `RandomEventSystem.Update()` entirely commented out — events never fire | `RandomEventSystem.cs:29-55` |
| 16 | Mirror vendored in ExternalAssets — no version, no upgrade path | `Assets/ExternalAssets/Mirror/` |
| 17 | Large blocks of Vivox voice code commented out in `CustomNetworkManager` | `CustomNetworkManager.cs:262-284` |
| 18 | `SteamLobby.lobbyIds` list is populated but never cleared on re-browse | `SteamLobby.cs:28,166` |

---

## Open Questions — Resolved

1. ✅ **`CustomNetworkTransform` is NOT on the player prefab** (`NetworkPlayer_Steam.prefab` confirmed). The custom transform system was built but never wired to players. Only the SyncVar sync in `NetworkPlayer` is active. `CustomNetworkTransformBase` is effectively unused dead code on players (may be on other objects — unconfirmed).

2. ⚠️ **Scene structure needs clarification.** Active scenes found: `Assets/Scenes/IslnadCasper.unity` (current main), `Assets/Scenes/EyalSandbox.unity` (Eyal's dev scene). Build settings only reference `OLD SCENES/IslnadCasperEyal.unity` — **build settings are stale and pointing at the wrong scene.** Which MatchManager variant (`TTTMatchManager` vs `SingelTotemMatchManager`) is in `IslnadCasper.unity` needs to be confirmed by opening that scene.

3. ✅ **Items are spawned correctly** via `NetworkServer.Spawn(Instantiate(...))`. Item prefabs must be registered in NetworkManager's spawnable prefabs list. `ItemSpawner` self-destructs after spawning (`Destroy(this)`).

4. ✅ **`PickupableItem` gets a NetworkIdentity via `NetworkServer.Spawn()`** — standard Mirror network object lifecycle.

5. ✅ **`Minigames/`** — `MinigameManager` appears in zero active scenes. Likely dead code or an unused experiment.

**Additional finding from ItemSpawner:** `spawnPositions.OrderBy(s => Random.value)` result is discarded — LINQ `OrderBy` is not in-place. Spawn positions are never actually shuffled despite the intent. Items always spawn in child-order sequence.
