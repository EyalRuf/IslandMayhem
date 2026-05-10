# Island Mayhem — Codebase Walkthrough Plan

## Purpose
Before touching the refactor, we want to walk through the whole codebase together layer by layer so the user deeply understands how everything works, can contribute to design decisions, and isn't just watching Claude implement things blindly.

## Status
- **Layer 1** — ✅ DONE
- **Layer 2** — ✅ DONE
- **Layer 3** — ✅ DONE
- **Layer 4** — ✅ DONE
- **Layer 5** — ✅ DONE

---

## The Layers

### Layer 1 — Foundations
*Goal: understand the two frameworks we build on before looking at our code.*

Topics:
1. **Mirror's server/client model** — how does a Unity process become server, client, or both (host)? What is `NetworkBehaviour`, `isServer`, `isClient`, `isLocalPlayer`? What is a `NetworkIdentity`?
2. **SyncVar** — what is it, when does it sync, what are its limits?
3. **[Command] and [ClientRpc]** — the two arrows of networked communication. Direction, who can call them, gotchas.
4. **NetworkManager and NetworkServer.Spawn** — how objects enter the networked world. Spawn prefabs list.
5. **Steam transport** — how Mirror's KCP is replaced by the Steam P2P transport. What `networkAddress` means in this context (it's a Steam64 ID string).
6. **Listen server vs dedicated server** — this game uses listen server (the host is also a player). Implications: `isServer && isClient` is true on host.

Key files to look at: `CustomNetworkManager.cs`, `SteamLobby.cs` (transport setup only).

---

### Layer 2 — Connection & Lobby Flow
*Goal: trace exactly what happens step by step from "open game" to "playing".*

Topics:
1. **Menu → Create Lobby** — `MenuUIManager` calls `SteamLobby.CreateLobby()`. Steam callback `LobbyCreated_t` fires → `SteamMatchmaking.SetLobbyData`. Then `StartHost()`.
2. **Menu → Browse/Join Lobby** — `GetLobbiesList()` → `LobbyMatchList_t` → instantiate `LobbyListItem` rows → click → `JoinLobby(CSteamID)`.
3. **`LobbyEnter_t`** — fires on guest. Reads host Steam ID from lobby data → sets `networkAddress` → `StartClient()`.
4. **Mirror handshake** — `OnServerAddPlayer` on host → spawn player prefab → `connectedPlayers` dict populated.
5. **`localPlayerInitialized`** — how `CustomNetworkManager` signals that the local player object is ready.
6. **`OnClientStartStopEvents`** — what scripts get notified and when.
7. **Known fragile points** — no explicit states, `GameLobbyJoinRequested_t` commented out (no friend invites), `ResetManager` triple-fire.

Key files: `SteamLobby.cs`, `CustomNetworkManager.cs`, `MenuUIManager.cs`, `OnClientStartStopEvents.cs`.

---

### Layer 3 — Game Start & Match Flow
*Goal: understand how "lobby ready" turns into a running match.*

Topics:
1. **`MatchManager.CalculateAndAssignTeams()`** — `RandomizeComparer` shuffle, team distribution algorithm, `playerTeam` SyncVar set.
2. **`RpcSyncTeamInfo`** — JSON serialization of team data, why this exists (SyncVar lists can't hold complex types easily).
3. **`RpcStartGame`** — what fires: `OnMatchStartStopEvents`, objective list construction (TTT/Singel), `MatchTimer.StartTimer`.
4. **`TTTMatchManager` vs `SingelTotemMatchManager`** — the inheritance chain, what each layer adds.
5. **Match timer bug** — why `startTime = Time.time` gives 0 on client.
6. **Win condition check** — `SingelTotemMatchManager.Update` checks `SingleTotemObjective.IsCompleted`.
7. **Match reset** — what `ResetManager()` does, why it fires multiple times (multiple hooks).

Key files: `MatchManager.cs`, `TTTMatchManager.cs`, `SingelTotemMatchManager.cs`, `MatchTimer.cs`.

---

### Layer 4 — Player Systems
*Goal: understand all the moving parts on a player object.*

Topics:
1. **Player prefab component layout** — all the NetworkBehaviour scripts on one object and how they reference each other.
2. **`LocalPlayerInput`** — the input bus pattern. Why it exists (single place to swap input source).
3. **`ThirdPersonCharacterController`** — movement pipeline: input → velocity calculation → `CharacterController.Move`. Jump. Sprint. Buff multipliers.
4. **`ThirdPersonCameraController`** — local only. Camera arm, dolly collision, aim mode transition. Why it's separate from the character controller.
5. **`NetworkPlayer`** — the "network identity" component. What it syncs, death/respawn flow.
6. **`PlayerCombat`** — `CmdPlayerWasHit` → `RpcPlayerWasHit`. Invulnerability. The `Hittable` / `HitInflictor` pair.
7. **`PlayerAnimations`** — how animation state is driven locally and replicated. `NetworkAnimator` for triggers. `currentSkin` SyncVar.
8. **`PlayerBuffs`** — the 5 buffs, `RpcGiveBuff`, decay coroutines.
9. **`PlayerItemInteractions`** — pickup/hold/drop. Position sync while held.
10. **`PlayerInteractionArea`** — the Cmd→Rpc interaction pattern in detail.

Key files: Most of `Player/`.

---

### Layer 5 — World Systems
*Goal: understand everything that lives in the world and interacts with players.*

Topics:
1. **`Totem` + `IA_Totem` + `TotemPiece`** — the full totem-building loop: find piece → pick up → carry to station → interact → consumed → stage incremented → eventually `maxVisualStageReached`.
2. **`Camp` + `CampManager` + `CampStep`s** — the camp puzzle system. Steps, completion, buff rewards, totem dispenser mode, rotation via `SwapMainCamp`.
3. **`InteractionArea` hierarchy** — base → DiggingSite, PlayerAmount, Totem. The `Interact` coroutine pattern.
4. **`RandomEventSystem`** — currently disabled in scene. Would periodically call `ServerEvent` + `ClientRpc` to trigger `CoconutEvent`, `FloorIsLavaEvent`, `ZeroGravityEvent`.
5. **Item spawning** — `ItemSpawner`, `TagItemSpawner`, `GameobjectLimiter`. The LINQ shuffle bug.
6. **`PalmTree`** — small emergent hazard. Drops coconuts on players who walk near.
7. **`SwiperBehavior`** — server-only rotating obstacle.
8. **`OutOfBounds`** — fall-out-of-world recovery.
9. **Match objectives → UI** — `MatchObjective` interface → `TTTMatchManager` lists → `TeamAndObjectivesUI` → `ObjectiveUI`.

Key files: `Totem.cs`, `IA_Totem.cs`, `TotemPiece.cs`, `Camp.cs`, `CampManager.cs`, `RandomEventSystem.cs`, `TeamAndObjectivesUI.cs`.

---

## How to Run a Layer Session
1. Pick up from the layer marked NOT STARTED above.
2. For each topic: Claude explains the concept + points to the specific lines in the relevant file. User asks questions until it clicks.
3. After each topic: update your understanding of how it connects to adjacent systems.
4. After the layer is done: mark it DONE here, note any questions that became new issues to log in `architecture.md`.

## Cross-Reference
- Full script list with one-liner descriptions: [scripts.md](scripts.md)
- Networking deep-dive: [networking.md](networking.md)
- Architecture issues log: [architecture.md](architecture.md)
