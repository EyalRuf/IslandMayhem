# Island Mayhem — Codebase Architecture

## Status
✅ Phase 2 analysis complete — populated from codebase read

## Game Summary
3rd-person multiplayer island brawler. Two teams (Natives vs Explorers) fight, collect totem pieces, and complete objectives. Steam P2P lobbies, Mirror networking. One scene contains everything — menu, game, and lobby state all coexist.

---

## Principle: Scene Readiness Must Gate State Transitions

**Rule:** A state machine must never transition to a state that depends on scene-local objects until the target scene is fully loaded.

**Why this matters — the failure mode we hit:**
`InGameState` subscribed to `HostStoppedEvent` / `ClientStoppedEvent` and called `ToNextState()` → `MainMenuState` immediately when Mirror fired those events. But Mirror fires `OnStopHost` *before* unloading the match scene and loading the offline scene. `MainMenuState.OnEnter()` therefore ran while the match scene was still active — `MenuUIManager` (a scene-local object) was destroyed, causing a `MissingReferenceException`.

The band-aid instinct (null-check or scene-check in `OnEnter`) masks the symptom but leaves the flow semantically broken: the state thinks it's in the menu, but the scene disagrees.

**The correct pattern for leaving a networked scene:**
1. Transition to an intermediate "leaving" state (e.g. `ReturningToMenuState`) *before* stopping the network.
2. In that state's `OnEnter`: explicitly load the destination scene (`SceneManager.LoadScene`) and subscribe to `SceneManager.sceneLoaded`. The state machine *owns* the scene transition — do not rely on Mirror's StopHost as a side effect to trigger it.
3. Transition to the destination state only *inside* the scene-loaded callback, once confirmed. This triggers `OnExit`.
4. In `OnExit`: call `StopHost`/`StopClient` to clean up Mirror. By this point the destination scene exists, so Mirror's cleanup happens in the right context.
5. The destination state's `OnEnter` can now safely access scene-local objects — the scene is guaranteed to exist.

**Why not call `StopHost` in `OnEnter`:** Mirror fires `HostStoppedEvent` synchronously inside `StopHost`. If any downstream code (state transitions, event handlers) touches scene-local objects after that point, they will fail — the match scene may still be active. Calling `StopHost` in `OnExit` instead means it runs *after* the scene is loaded and *after* the new state has already taken over, so nothing in the old scene is touched at the wrong time.

**Applied in AppStateMachine:**
```
InGameState → ReturningToMenuState → MainMenuState
```
`ReturningToMenuState` owns the network-stop + scene-load-wait logic. `MainMenuState.OnEnter()` is unconditionally safe.

**General rule to remember:** If a state's `OnEnter` touches scene-local objects, there must be a state *before* it whose sole job is to wait for that scene to be ready. Never let a network event (e.g. `HostStopped`) drive a direct transition into a UI-dependent state.

---

---

## Pattern: DDOL Bridge for Scene-Local Managers

**Problem this solves:** The state machine lives in DDOL-land and needs to talk to scene-local objects (e.g. `MenuUIManager`). Injecting scene-local objects directly into states causes stale reference bugs after scene reloads — the DI system caches the first resolved instance, which becomes a destroyed fake-null on the next scene load.

**The pattern — two-layer manager split:**

1. **DDOL bridge** (`MenuManager`) — `[Injectable]`, lives on a DDOL GameObject (e.g. AppManager's GO). States inject this. It exposes the same public API as the scene-local manager (`ShowMainScreen`, `HideAll`, etc.) but delegates internally to whoever has registered. Fires `UIReady` when the scene-local layer is available.

2. **Scene-local manager** (`MenuUIManager`) — owns all Unity UI references (panels, buttons, text). In `Awake()`, calls `FindObjectOfType<MenuManager>().RegisterUI(this)`. This is the only `FindObjectOfType` in the pattern, and it's acceptable: it's a one-time call in Awake, searching for a DDOL object that's guaranteed to exist.

**Why this works across scene reloads:**
- DDOL bridge persists — states keep a valid injection forever
- Each scene reload creates a fresh `MenuUIManager` → `Awake()` re-registers → DDOL bridge updates its internal reference and fires `UIReady`
- States that need to wait for the UI (e.g. `ReturningToMenuState`) subscribe to `menuManager.UIReady` as their scene-readiness gate — cleaner and more semantic than `SceneManager.sceneLoaded`

**Key insight about `FindObjectOfType` direction:**
The acceptable use is scene-local → DDOL (upward). The wrong direction is DDOL → scene-local (downward). A DDOL object searching for a scene-local object is fragile (may not exist yet, stale after reload). A scene-local object finding a DDOL object in `Awake()` is always safe — DDOL objects exist before any scene loads.

**Applied to:**
- `MenuManager` ↔ `MenuUIManager` — menu scene UI
- The same pattern should be applied to any future scene-local manager that needs to be visible to the state machine layer

---

## Design Gap: CardboardCore DI Does Not Support Scene-Scoped Objects

**Problem:** CardboardCore's `[Inject]` / `[Injectable]` system resolves instances once and caches them. It assumes all injectable objects are DDOL singletons. Scene-local MonoBehaviours cause stale reference bugs after scene reloads.

**Current workaround:** The DDOL bridge pattern above. Scene-local managers register themselves with a DDOL counterpart on `Awake()`. States inject the DDOL counterpart — always valid.

**The real fix:** A DI system with scene-scoped containers — where the scene container is rebuilt on each load and DDOL containers persist. Zenject and VContainer handle this natively.

**Ask coder friend about:** how to extend CardboardCore's Injector to support scene-scoped registration, or whether swapping to Zenject/VContainer is worth the migration cost at this stage. The DDOL bridge pattern is a solid interim solution and maps cleanly onto whatever scoped DI replaces it.

---

## Folder Structure (`Assets/Scripts/`)

```
Scripts/
├── Audio/              CollisionSound, Compressor, Limiter
├── Combat/             HitInflictor, Hittable, Lava, PlayerPunch
├── Events/             RandomEventSystem, CoconutEvent, FloorIsLavaEvent, ZeroGravityEvent, RandomEvent
├── Hazards/            SwiperBehavior
├── Items&Interactions/ ThrowableItem, PickupableItem, ItemSpawner, TagItemSpawner,
│                       InteractionArea, IA_DiggingSite, IA_PlayerAmount, IA_Totem,
│                       Totem, TotemPiece,
│                       ExplorationCamps/ (Camp, CampManager, CampStep, + variants)
├── MatchObjectives/    MatchObjective (base), TotemsObjective, SingleTotemObjective, TimerObjective
├── Minigames/          MinigameManager, MinigameManagerTotems, Team, TeamPlayer
├── Misc/               CursorEnabler, DestroyAfter, FloatTransform, NoRotation, OrbitAround,
│                       OutOfBounds, PalmTree, ScreenShot, TimeFormatter,
│                       EnabledForPlayersOfTeam, GameobjectLimiter
├── Networking/
│   ├── Cheats.cs
│   ├── CustomNetworkManager.cs
│   ├── CustomNetworkTransform.cs  (+ Base)
│   ├── SteamLobby.cs
│   ├── Matches/        MatchManager, MatchTimer, TTTMatchManager, SingelTotemMatchManager
│   ├── Misc/           OnClientStartStop(Events), OnMatchStartStop(Events)
│   └── VoiceChat/      Spatializer, SteamlessVoiceChat
├── Player/             NetworkPlayer, LocalPlayerInput, ThirdPersonCharacterController,
│                       PlayerCombat, PlayerAnimations, PlayerBuffs, PlayerItemInteractions,
│                       PlayerInteractionArea, PlayerParticles, PlayerThrowTargetController,
│                       ThirdPersonCameraController, LookAtCamera
├── StateMachineBehaviours/ LandSMB
├── TwitchChat/         IRC/ (Chatter, IRCMessages, MainThread, ParseHelper, TwitchIRC), SimpleExample
└── UI/                 DeathFade, LobbyListItem, MenuUIManager, ObjectiveUI, TeamAndObjectivesUI
```

## High-Level Systems

| System | Description | Key Scripts |
|--------|-------------|-------------|
| **Networking** | Mirror + Steam P2P transport | `CustomNetworkManager`, `SteamLobby`, `NetworkPlayer` |
| **Match / Game Flow** | Server-authoritative match lifecycle: waiting → countdown → in-game → end | `MatchManager` → `TTTMatchManager` → `SingelTotemMatchManager` |
| **Player** | Movement, combat, items, animations. Local player only runs physics; remotes lerp SyncVars | `ThirdPersonCharacterController`, `PlayerCombat`, `NetworkPlayer`, `LocalPlayerInput` |
| **Combat** | Collision-based hit detection. Local player detects hit → Cmd to server → Rpc to all | `HitInflictor`, `PlayerCombat`, `PlayerPunch`, `Hittable` |
| **Objectives** | Win conditions tracked per-team. Polled each frame on server | `MatchObjective`, `TotemsObjective`, `SingleTotemObjective` |
| **Totems** | Collectable pieces. Red/Blue groups map to team 0/1 objectives | `Totem`, `TotemPiece` |
| **Camps** | Spawn points + puzzle interactions. Main camp rotates via CampManager | `Camp`, `CampManager`, `CampStep` variants |
| **Events** | Random world events (lava, zero-g, coconut). Currently disabled (Update commented out) | `RandomEventSystem`, `CoconutEvent`, `FloorIsLavaEvent`, `ZeroGravityEvent` |
| **Items** | Throwable/pickupable items. Spawned server-side at match start | `ThrowableItem`, `PickupableItem`, `ItemSpawner` |
| **UI** | Main menu (pages: Main/Lobby/Load), in-game objectives, team info | `MenuUIManager`, `TeamAndObjectivesUI`, `ObjectiveUI` |
| **TwitchChat** | IRC-based Twitch chat integration (purpose unclear — spectator interaction?) | `TwitchIRC`, `Chatter` |
| **VoiceChat** | `SteamlessVoiceChat` + `Spatializer` — appears to be an alternate voice solution | — |
| **Minigames** | `MinigameManager` / `MinigameManagerTotems` — possible separate mini-game mode | `Team`, `TeamPlayer` |

## Match Manager Inheritance Chain

```
MatchManager (NetworkBehaviour)
  ├── Core state: gameStarted, gameOver, teams[], objectives[]
  ├── StartGame/EndGame as virtual coroutines
  └── TTTMatchManager : MatchManager
        ├── Win condition: didTeam0Win || didTeam1Win (all totems collected)
        ├── Countdown 3-2-1-GO
        └── SingelTotemMatchManager : TTTMatchManager
              ├── Timed match (MatchTimer)
              ├── Tiebreak on piece count
              └── CampManager.SwapMainCamp() on game start
```

## Player GameObject Component Layout (inferred)

```
PlayerPrefab
├── NetworkPlayer              (network identity, team color, SyncVar position)
├── NetworkIdentity            (Mirror)
├── ThirdPersonCharacterController  (movement, jump, audio)
├── PlayerCombat               (HP, hit response, death/respawn)
├── LocalPlayerInput           (input reading — disabled for non-local)
├── PlayerAnimations           (Animator wrapper)
├── PlayerItemInteractions     (hold/drop items)
├── PlayerBuffs                (speed/jump buffs)
├── PlayerThrowTargetController (aim for throw)
├── ThirdPersonCameraController (camera — disabled for non-local)
├── PlayerInteractionArea      (interaction trigger)
└── PlayerParticles            (VFX refs)
```

## Networking Pattern Used Throughout

The codebase follows a consistent Mirror pattern:
1. Local player detects input/event
2. `Cmd` sent to server with `netId` as lookup key
3. Server looks up player via `CustomNetworkManager.GetPlayerByNetId(netId)`
4. Server calls `Rpc` on that player to broadcast to all clients
5. All clients apply the effect locally

This is used for: jump, punch, movement clips, hit response, regen, revive.

**Notable:** position sync is manual — `NetworkPlayer.CmdUpdatePlayerTransform` every FixedUpdate. Not using Mirror's `NetworkTransform`.

## Dependency Map (Key)

```
MenuUIManager ──────────────────→ SteamLobby
MenuUIManager ──────────────────→ CustomNetworkManager
SteamLobby ─────────────────────→ CustomNetworkManager (networkManager)
SteamLobby ─────────────────────→ MatchManager (numberOfPlayersNeededToStart, LeaveLobby)
CustomNetworkManager ───────────→ MatchManager (ResetMatch)
CustomNetworkManager ───────────→ MenuUIManager (ClickedBackToMain)
MatchManager ───────────────────→ CustomNetworkManager (GetAllPlayers, static)
MatchManager ───────────────────→ SteamLobby (LeaveLobby on RpcStartGame)
NetworkPlayer ──────────────────→ CustomNetworkManager (RegisterPlayer, static)
PlayerCombat ───────────────────→ MatchManager (gameStarted check)
PlayerCombat ───────────────────→ CampManager (spawn point on death)
```

## Code Quality Notes

### Dead / Disabled Code
| File | Notes |
|------|-------|
| `RandomEventSystem.cs` | Entire Update() logic commented out — events never fire |
| `SteamLobby.cs` | `GameLobbyJoinRequested` callback commented out — Steam invites broken |
| `CustomNetworkManager.cs` | Large blocks of Vivox voice chat code commented out |
| `SteamLobby.Update()` | G/H hotkeys for host/join left in |
| `MatchManager.Update()` | `Input.GetKeyDown(KeyCode.Return)` force-start left in |

### Unclear Ownership
| Description | Files Involved |
|-------------|---------------|
| Dual position sync — SyncVar in NetworkPlayer AND CustomNetworkTransform exists | `NetworkPlayer.cs`, `CustomNetworkTransform.cs` |
| `ResetManager()` called from 4 different hooks | `CustomNetworkManager.cs` |
| TwitchChat system — unclear if active or experimental | `TwitchChat/` |
| Minigames system — unclear relationship to main match flow | `Minigames/` |

## Issue: Win Condition Double-Fire Risk (Low Priority)

`SingelTotemMatchManager.Update` has two win condition paths running every frame with no shared guard:
- `TTTMatchManager.Update` checks `didTeam0Win || didTeam1Win` and sets `endingGame = true` before calling `RpcEndGame`
- `SingelTotemMatchManager.Update` checks `matchTimer.IsTimeOver` but does NOT check `endingGame` first

If both conditions are true on the same frame, `RpcEndGame` fires twice.

**Fix:** Replace both polling checks with events:
- `Totem` fires an event when `maxVisualStageReached` → server calls `RpcEndGame` once
- `MatchTimer` fires an event when `timeRemaining <= 0` → server calls `RpcEndGame` once
- Both handlers set `endingGame = true` first, making any simultaneous second call a no-op

Eliminates the frame-timing race entirely and removes per-frame win condition polling from `Update`.

**Files to touch:** `SingelTotemMatchManager.cs`, `TTTMatchManager.cs`, `MatchTimer.cs`, `Totem.cs`

---

## Issue: MatchTimer Dead Code (Low Priority)

`MatchTimer.MatchStarted()` has two dead code issues:
- `startTime = Time.time` followed immediately by `timeRemaining = matchLength - (Time.time - startTime)` — the subtraction always evaluates to 0, making `startTime` pointless. `timeRemaining = matchLength` is all that's needed.
- `didStart` is set on all clients via the Rpc chain but does nothing on clients — the `if (isServer && didStart)` guard in `FixedUpdate` means the timer only ever ticks server-side. `timeRemaining` replicates to clients via SyncVar. `didStart` on clients is inert.

State machine note: the timer start/stop lifecycle belongs to state transitions, not manual method calls inside coroutines.

**Files to touch:** `MatchTimer.cs`

---

## Issue: Match Manager 3-Class Chain Should Be 2 (Medium Priority)

`MatchManager → TTTMatchManager → SingelTotemMatchManager` is over-engineered. `TTTMatchManager` is not a standalone game mode — it's just a behaviour layer that `SingelTotemMatchManager` inherits. There is only one game mode in the project.

**Fix:** Collapse into 2 classes:
- `MatchManager` — keep as-is, genuine shared base
- `SingleTotemMatchManager` (rename, fix typo) — absorb everything from `TTTMatchManager`: countdown, win condition check, end-game scene reload. Countdown can alternatively be delegated to a separate `CountdownManager` or similar if it needs to be reused.

**Also:** rename `SingelTotemMatchManager` → `SingleTotemMatchManager` (typo fix). This is a file rename + all references.

**Benefits:** Eliminates the `[ClientRpc]` inheritance footgun, removes `TTTMatchManager.CalculateAndAssignTeams` (does nothing), removes duplication, flattens the override chain.

**Files to touch:** `TTTMatchManager.cs` (delete), `SingelTotemMatchManager.cs` (rename + absorb), any scene references to the old class name.

---

## Issue: Redundant `teams` List + `RpcSyncTeamInfo` (Low Priority)

`MatchManager.teams` is a `List<Team>` maintained on all clients via a JSON Rpc workaround (`RpcSyncTeamInfo`). This exists because Mirror SyncVars can't handle complex nested objects. But the data is already fully replicated — every `NetworkPlayer` has a `playerTeam` SyncVar. The `teams` list is just a grouped view of information clients already have.

**Fix:** Make `teams` server-only (used only for team assignment logic). Clients derive team groupings on demand by querying `GetAllPlayers().Where(p => playerTeam == n)`. Remove `RpcSyncTeamInfo` and the JSON serialization entirely. Win condition checks already run server-side only so they're unaffected.

**Files to touch:** `MatchManager.cs`, `TTTMatchManager.cs`, `SingelTotemMatchManager.cs`, any client-side code reading `teams` directly.

---

## Issue: Redundant Player Registry (Low Priority)

`CustomNetworkManager.playersDic` is a manual dictionary mapping `netId → NetworkIdentity` maintained across all clients. It's used for two things:
- `GetLocalPlayer()` — find the local player object
- `GetPlayerByNetId(netId)` — resolve a netId uint from an Rpc back into a GameObject

Both are already provided by Mirror natively:
- `NetworkClient.spawned[netId]` is Mirror's own built-in spawn dictionary, always up to date, no manual registration needed
- `GetLocalPlayer()` can be replaced with a single cached reference set in `OnStartLocalPlayer`

**Fix:** Remove `playersDic`, `RegisterPlayer`, `UnregisterPlayer`, `localPlayerId`, `SetLocalPlayer`. Replace all `GetPlayerByNetId` calls with `NetworkClient.spawned[netId]`. Replace `GetLocalPlayer()` with a static `localPlayer` reference cached in `NetworkPlayer.OnStartLocalPlayer`. Simplifies `CustomNetworkManager` significantly.

**Files to touch:** `CustomNetworkManager.cs`, `NetworkPlayer.cs`, and all scripts currently calling `GetPlayerByNetId` or `GetLocalPlayer`.

---

## Issue: PlayerInteractionArea — No Server Validation (Medium Priority)

`CmdInteractWithArea` does zero validation — it receives playerNID and areaNID and immediately calls the Rpc. All checks (`canBeInteractedWith`, `!beingInteractedWith`, `restrictedToTeam`) happen client-side before the Cmd. Two players pressing interact simultaneously on the same area will both succeed. Server must re-run the same checks before acting.

**Also:**
- Per-frame `Physics.OverlapSphere` in `Update` is the wrong approach. Better: each `InteractionArea` owns a trigger collider. `OnTriggerEnter` (server-side only) registers the player; `OnTriggerExit` unregisters. When the relevant condition is met, fire against whoever is currently registered — no polling needed. This moves range definition to the interaction area itself (correct ownership) and removes per-frame casts entirely. If multiple areas are simultaneously in range, pick by distance as a reasonable default.

**This same pattern applies across multiple scripts — batch cleanup:**
- `PlayerInteractionArea.cs` — player interact detection (covered above)
- `PlayerItemInteractions.cs` — pickup proximity + highlight
- `IA_PlayerAmount.cs` — counts players in area (Update poll)
- `CampStepButton.cs`, `CampStepPressurePlate.cs`, `CampStepTimerButton.cs` — all FixedUpdate polls detecting players on buttons/plates — the strongest case for trigger colliders, these are literally designed to react to players stepping on them
- `RpcInteractWithArea` fires on all clients but `InteractWithArea` guards with `isLocalPlayer` — only one client does anything. Replace with `[TargetRpc]` — fires on the owner's connection only. Checked all other ClientRpcs in the codebase; this is the only instance of this specific pattern.
- The interaction coroutine (`area.Interact`) runs client-side only on the local player. Other clients get the animation via `NetworkAnimator` but have no awareness of the ongoing interaction. The world-side effects of completing an interaction must therefore be handled server-side inside `InteractionArea.Interact`, not at the end of the client coroutine — otherwise only one client triggers the consequence. Worth verifying this is the case when Layer 5 covers `InteractionArea`.
- Interact animation starts only after Cmd→Rpc round trip — same local prediction fix as pickup/jump.
- Commented-out `AssignClientAuthority` / `RemoveClientAuthority` (lines 49, 59) is dead code — remove it.

**Files to touch:** `PlayerInteractionArea.cs`

---

## Improvement Priorities (Draft — to confirm together)
1. Replace `OnLevelWasLoaded` with `SceneManager.sceneLoaded` (Unity 6 compat)
2. State machine for lobby/connection flow (Phase 3)
3. Re-enable and fix `RandomEventSystem`
4. Fix `GameLobbyJoinRequested` — Steam friend invites
5. Clean up duplicate position sync (NetworkPlayer vs CustomNetworkTransform)
6. Remove debug hotkeys (G, H, Return)
7. **Punch latency + hit detection overhaul** — see below

## Cleanup: ThirdPersonCameraController Aim State (Medium Priority)

`ToggleCameraAim` and `AimCamera` use three booleans (`isAiming`, `isInAimTransition`, `cameraBackToOrigin`) to track camera state. The `LateUpdate` conditionals depend on different combinations of all three, making it hard to add new camera modes without breaking something. **Throwing mechanics are expected to change significantly** — aim mode is directly involved, so this needs to be maintainable before that work starts. Clean fix: replace with a small enum — `Default`, `TransitionToAim`, `Aiming`, `TransitionToDefault` — and drive `LateUpdate` from a single switch.

**Files to touch:** `ThirdPersonCameraController.cs`

---

## Design Goal: Instant-Feel Player Actions with Server Authority (Medium Priority)

**Raised during Layer 4 walkthrough.** The user wants to deliberately design a consistent approach to this tradeoff across all player systems — not fix it piecemeal.

The core tension: players expect actions to feel instant (0ms feedback), but the server needs to be authoritative (who hit who, did the pickup succeed, did the interaction complete). These two goals pull in opposite directions.

Three zones to assess each player action:
1. **Movement** — should feel instant locally. Position is client-authoritative in most games; server corrects if needed.
2. **Combat** — needs server validation (anti-cheat), but visual feedback should be immediate locally.
3. **Interactions / Item pickups** — currently fully round-trip; feels laggy.

**After Layer 4 walkthrough:** design a unified pattern (or set of patterns) that covers all three zones consistently. Assess each system against it.

---

## Issue: Punch Latency & Split Hit Detection

**Symptom:** Players reported punches not feeling like they land. Likely root cause.

**Current flow:**
```
Input (local) → CmdPlayerPunch → server → RpcPunch → collider activates on all clients
                                                            ↓
                                              victim client detects collision
                                                            ↓
                                         CmdPlayerWasHit → server → RpcPlayerWasHit → reactions
```
4 network round trips before any hit feedback. At 100ms ping = ~400ms perceived delay.
Positions shift between steps, so the collider often misses entirely by the time it activates.

**Secondary problem:** Two independent hit detection paths exist simultaneously:
- `Hittable.OnTriggerEnter` (server) — handles knockback physics
- `PlayerCombat.OnTriggerEnter` (victim client) — handles damage + animations
These are uncoordinated and can produce inconsistent results.

**The fix — attacker-side prediction + server validation:**

Designed flow:
```
Attacker presses punch
  ↓
LOCAL (attacker): play punch animation immediately, activate collider locally
LOCAL (attacker): OnTriggerEnter fires → play cosmetic hit effects (particles, flinch anim) immediately
  ↓
CmdPlayerPunch(attackerNetId, victimNetId, hitDirection)
  ↓
SERVER validates:
  - Attacker punch CD was clear
  - Victim is within plausible distance (server positions + lag tolerance buffer)
  - Victim is not invulnerable (server-tracked)
  - Read damage + isStun from HitInflictor directly — never trust client-supplied values
  ↓ (if valid)
SERVER: writes currHp -= damage (SyncVar propagates HP bar update to all clients)
SERVER: applies cripple/knockdown (SyncVar)
SERVER: picks respawn point if death, sends to all clients
RpcPlayerWasHit(hitDirection): cosmetic-only — knockback force, death sequence trigger
```

**ApplyHitOnSelf needs to be split into two separate responsibilities:**
- **Server-only:** HP change, cripple/knockdown (SyncVar writes), death trigger, respawn position selection
- **All clients via Rpc (cosmetic):** hit particles, flinch animation, item drop visual, knockback force

Currently the whole function runs on every client via RpcPlayerWasHit, mixing authoritative state (SyncVar writes that silently fail on guest clients) with cosmetics. The authoritative parts must only execute on `isServer`.

**Additional bugs fixed by this architecture:**
- `isCrippled = false` / `isKnockedDown = false` on death: currently silent no-ops on guest clients — server owns these now
- Non-deterministic respawn position: server picks the spawn point, sends it as an Rpc parameter
- Client-supplied damage value: server reads from HitInflictor directly

**Also applies to:** lava and other environmental hazards — same pattern.

**isInvulnerable:** currently a plain client-side bool used as a gate before sending CmdPlayerWasHit. In the new flow (attacker sends the Cmd, not victim), invulnerability must be server-tracked state so the server can check it during validation.

**Regen:** currently the local player runs the regen timer and sends CmdRegen when it fires. This should move entirely server-side — server owns the timer, increments currHp directly as a SyncVar write, resets the timer when a hit is applied. No Cmd needed, no client involvement. The accelerating interval (currRegenInterval) also lives server-side. HP bar updates automatically via SyncVar on all clients.

**Files to touch when fixing:** `PlayerCombat.cs`, `PlayerPunch.cs`, `Hittable.cs`, `HitInflictor.cs`, `Lava.cs`

## Issue: Player Position Sync — Bandwidth Inefficiency (High Priority)

`NetworkPlayer.FixedUpdate` sends `CmdUpdatePlayerTransform` every FixedUpdate (50/s) unconditionally — even when the player is standing still. With 6 players this saturates the host's upload bandwidth.

**Problems:**
- No movement threshold — idle players still send 50 packets/s
- Cmd → SyncVar path adds an extra network hop vs Mirror's native sync
- Scale (`netPlayerSize`) travels in every packet despite only changing during size buffs
- Simple lerp interpolation vs proper timestamped interpolation means jittery remote players

**Fix — priority order:**
1. Replace manual `CmdUpdatePlayerTransform` with `CustomNetworkTransformBase` (already in project, already used on world objects). It has sensitivity thresholds and proper timestamped interpolation. Alternatively, add threshold + reduce send rate to ~20/s manually.
2. Split `netPlayerSize` (scale) into its own SyncVar — it only changes during size buffs. Removes it from 99% of packets at zero cost.
3. Compress `netPlayerRot` using Mirror's built-in `Compression.CompressQuaternion` — packs 16 bytes → 4 bytes using the "smallest three" algorithm. Change the SyncVar type to `uint`, compress on write, decompress on read. Zero quality loss.

Note: the problem is primarily **frequency** (50 packets/s unconditionally), not packet size. The threshold fix is the biggest win. Compression is a useful secondary improvement.

**Files to touch:** `NetworkPlayer.cs`, potentially add `CustomNetworkTransform` component to player prefab.

---

## Issue: Steam Lobby Lifecycle Gaps (High Priority)

**1. Lobby never left on disconnect** — `OnServerDisconnect` and `OnStopClient` call `ResetManager()` but never `SteamLobby.LeaveLobby()`. Stale lobbies persist in the browser after a crash or disconnect.

**2. `SetLobbyJoinable` logic wrong** — locks lobby when `playerCount >= numberOfPlayersNeededToStart`, not when at max capacity. If you need 2 players to start but the lobby holds 8, it locks out after 2 players join.

**3. `GameLobbyJoinRequested_t` commented out** — Steam overlay "Join Friend's Game" does nothing. Friend invites completely broken.

**4. No error handling on `GetLobbyData`** — if lobby metadata hasn't propagated yet, `hostAddress` returns empty string, `StartClient("")` fails silently with no user feedback.

**5. `SteamAPI.Init()` called twice** — once in `CustomNetworkManager.Start()` and once by the `SteamManager` prefab. Redundant, potential ordering issues.

**6. Redundant lobby data keys** — `"host"` and `"HostAddress"` both store the same Steam ID. `"host"` is never read.

**7. Mid-match reconnection system (Future Feature)** — `LeaveLobby` is intentionally not called at match start (`TTTMatchManager.RpcStartGame` line 67 — commented out). Design intent: keep the Steam lobby open throughout the match so disconnected players can find and rejoin. Currently the infrastructure for this doesn't exist — `OnServerDisconnect` calls `ResetManager` which tears everything down, so a reconnecting player would start a new match cycle rather than restore their state. Full solution requires: preserve player state on disconnect, restore on reconnect in Mirror, keep lobby open but non-joinable to new players (`SetLobbyJoinable(false)` at match start). This is a significant feature — scope separately.

**8. Lobby not reopened on disconnect** — if the lobby was closed (non-joinable) because it was full, and a player disconnects before the match starts, `OnServerDisconnect` calls `ResetManager()` but never calls `SetLobbyJoinable(true)`. The lobby stays closed and no one can fill the empty slot.

**8. Double-click lobby join (minor)** — lobby list row buttons are not disabled on click. Clicking two rows quickly could fire two `SteamMatchmaking.JoinLobby` calls. Fix: disable all row buttons immediately on any click, re-enable if join fails. Flag for polish pass.

**9. Double `"eyalgame"` filter in `OnLobbyMatchList`** — server-side filter already applied in `GetLobbies()`, the manual check at line 154 is redundant. Intentional while using app ID 480 (Spacewar). Remove when switching to real Steam app ID.

**10. Empty `hostAddress` on `LobbyEnter_t` (low priority, future-proofing)** — `GetLobbyData` can return empty string if metadata hasn't propagated. In the current browse-and-click flow this is essentially impossible (metadata already cached). The real trigger is `GameLobbyJoinRequested_t` (friend invites) which is currently disabled. When friend invites are re-enabled, add Option B retry coroutine in `OnLobbyEntered` before calling `StartClient`. Files: `SteamLobby.cs`.

**Files to touch:** `SteamLobby.cs`, `CustomNetworkManager.cs`

---

## Issue: PlayerAnimations — Animation Speed Sync Inefficiency (Low Priority)

`CmdUpdatePlayerAnimSpeed` sends the animator speed value every `FixedUpdate` (50/s) unconditionally — same bandwidth problem as position sync. Fix: only send when the value changes beyond a small threshold. Better alternative: remote clients derive animation speed from the already-synced `netPlayerVel` SyncVar on `NetworkPlayer` rather than receiving a separate dedicated sync.

**Also:** `isInvulnerable` is currently a local bool per-client. When it moves to server-tracked state (SyncVar) as part of the combat redesign, the invulnerability blink in `PlayerAnimations.Update` will work correctly across all clients automatically — no additional change needed there beyond the combat fix.

**Files to touch:** `PlayerAnimations.cs`

---

## Issue: PlayerBuffs — Client-Side Decay and SyncVar-in-Rpc (Medium Priority)

**Problems:**
1. `BuffDecay` coroutine runs independently on every client via `RpcGiveBuff`. Each client counts down from whenever they received the Rpc — timing diverges across clients. Only the server's SyncVar writes propagate; guest client decay resets (on `pp.damage`, `transform.localScale`, `pCombat.maxHp`) run on stale, unsynchronised state.
2. `msBuffActive` and `jumpBuffActive` are SyncVars written inside a `[ClientRpc]`. Guest client writes are silently ignored. Works by coincidence (host also runs the Rpc and its write propagates).
3. Scale buff sets `transform.localScale` directly on all clients, bypassing `netPlayerSize` position sync. The two paths can conflict.
4. `PurgeBuffs` called from `DeathSequence` (which runs on all clients) — same SyncVar-in-Rpc problem.

**Fix:** Move buff state entirely server-side. `GiveBuff` and `BuffDecay` run on server only, write SyncVars directly. SyncVar hooks on clients handle cosmetics (particles, UI icons). Scale buff writes `netPlayerSize` on the server rather than setting `localScale` directly.

**Files to touch:** `PlayerBuffs.cs`, `PlayerCombat.cs` (DeathSequence → PurgeBuffs call)

---

## Issue: PlayerItemInteractions — Round Trip Latency on Pickup/Drop/Throw (Medium Priority)

Pickup, drop, and throw all go through a full Cmd→Rpc round trip before the local player sees the result. Same client-side prediction fix applies as jump and punch: apply locally immediately on input, send Cmd simultaneously, skip the local player in the Rpc.

Pickup validation on the server (`!pickupable.isBeingHeld` check in `CmdPickupItem`) is correct and should be kept — two simultaneous pickups are handled properly. Local prediction just means showing the pickup visually before the server confirms; if the server rejects it (race condition), snap back.

**Throw vector trusted without validation:** `CmdUseItem` passes client-supplied `pos`, `rot`, `vel`, `throwVec` directly to `RpcUseItem`. Server should compute or validate the throw vector rather than trusting client values.

**`DropItemIfHeld` adds a Cmd→Rpc round trip to the hit response chain** — already 2 round trips deep. With the combat redesign, the drop on hit should be part of the server-authoritative hit response, not a separate Cmd from the client.

**Dead code:** Non-throwable item use (lines 53-61 in `PlayerItemInteractions.cs`) is commented out. Non-throwable items cannot be used. Either implement or remove.

**Files to touch:** `PlayerItemInteractions.cs`, `PickupableItem.cs`

---

## Issue: Item State Not Fully Networked (Medium Priority)

**Problem:** Pickup/drop/throw rely on RPCs to set physics state (isKinematic, collider enabled, parent transform) on each client independently. Any client that misses an RPC — late joiner, brief disconnect — ends up with an incorrect view of item state that can never self-correct.

**Specific gaps:**
- `rb.isKinematic` and `col.enabled` are set locally in `Pickup()`/`Drop()` via RPC, not driven by SyncVars
- `currUsingPlayer` is a local object reference — never synced, not recoverable after a missed RPC
- `RpcUseItem` applies throw force independently on every client from slightly different positions — trajectory can briefly disagree across clients

**Fix:** Promote item physics state to SyncVars so any client can reconstruct correct state at any time without relying on RPC history:
- `isKinematic` (or drive it from existing `isBeingHeld` SyncVar in `OnSyncVarChanged` hook)
- `holderNetId` (uint SyncVar replacing `currUsingPlayer` reference)
- Throw force applied server-side only, position replicated via existing netPos/netVel SyncVars

**Files to touch:** `PickupableItem.cs`, `PlayerItemInteractions.cs`

---

## Issue: `ignoreAuthority` Shortcuts on World Objects (Low Priority)

`IA_DiggingSite.CmdSpawnObj` and `IA_Totem.CmdBuildTotem` use `[Command(ignoreAuthority = true)]`, allowing any client to call Commands on world objects they don't own. This works but means a malicious client could trigger these without being near or legitimately interacting.

**Correct pattern** (already used in `PlayerInteractionArea`): player calls a Command on themselves passing the world object's `netId` — server looks it up and acts on it. Authority stays on the player object which the client legitimately owns.

**Fix:** Replace `ignoreAuthority` Commands on `IA_DiggingSite` and `IA_Totem` with the player-owned Cmd pattern. Low priority — no real exploit risk in this game's context, but worth cleaning up when those systems are touched.

---

## Issue: Two Parallel Totem Station Implementations (Medium Priority)

`Totem.cs` (trigger-based) and `IA_Totem.cs` (interaction-based) both implement the totem deposit mechanic, but only `Totem.cs` is wired to the win condition. `SingleTotemObjective.IsCompleted` has a hard-typed `public Totem totem` field — it reads `Totem.maxVisualStageReached`, not `IA_Totem`. If `IA_Totem` is used in the scene, the totem can be fully built visually but the win condition never fires.

**Context:** `IA_Totem` was likely built as a design exploration — the idea that a totem deposit station could live inside a camp as one of the interaction-area puzzle steps (hence it inheriting `InteractionArea` rather than `NetworkBehaviour`). That camp integration was never implemented, so `IA_Totem` is probably dead/unused code. Confirm in scene before touching.

**Also:** `IA_Totem.Interact` calls `player.DestroyItem()` on the local client before `CmdBuildTotem` reaches the server — no server validation that the player actually held a valid piece. Combined with `ignoreAuthority = true` on `CmdBuildTotem`, any client can spam the Cmd.

**Also:** `IA_Totem.RpcBuildTotem` has no bounds check — `visuals[stage]` throws if stage >= visuals.Length. `Totem.cs` uses `Mathf.Clamp`; `IA_Totem` does not.

**Also:** `IA_Totem` is missing the `matchManager.UpdateTeamAndObjectiveUI()` call on completion that `Totem.cs` has in its `RpcBuildTotem`.

**Design decision (agreed):** The trigger-based auto-consume is the right mechanic, but the `!item.isBeingHeld` check should be removed. A player carrying a totem piece should have it deposited automatically when they walk into the totem station's trigger — no drop required. This means the server needs to force-drop the item from the player as part of the deposit sequence: call the equivalent of `DropItem` on the player before destroying the piece, so hold state is cleaned up correctly. The extra code cost is worth the UX improvement.

**Action:** Confirm which implementation is actually placed in the game scene. If `IA_Totem` is the active one, wire it to `SingleTotemObjective` and add the missing bounds check and UI call. If `Totem.cs` is active, assess whether `IA_Totem` is dead code to be removed.

**Files to touch:** `IA_Totem.cs`, `SingleTotemObjective.cs`, `Totem.cs`, `PlayerItemInteractions.cs` (force-drop on deposit)

---

## Task: Scene Audit via MCP (Low Priority)

Several scripts are suspected unused/unplaced: `IA_Totem`, `IA_DiggingSite`, `SwiperBehavior`, and potentially others. Use MCP Unity tools to inspect the active scene and confirm what's actually placed vs what's dead code sitting in the scripts folder. Delete confirmed unused scripts.

---

## Cleanup: `SwiperBehavior` — Never Used, Delete (Low Priority)

Never placed in the scene. Safe to delete.

**Files to touch:** `SwiperBehavior.cs`

---

## Cleanup: `rect.Set()` Dead Code in UI (Low Priority)

`TeamAndObjectivesUI.RecreateObjectiveList` and `ObjectiveUI.Start` both call `rectTransform.rect.Set(...)`. `RectTransform.rect` returns a copy — `.Set()` on it does nothing. These lines are dead code. UI layout is handled by Unity's layout system in the inspector (Layout Groups, Content Size Fitters), so no code-side resizing is needed. Just remove the calls.

**Also:** `ObjectiveUI.Update` sets `objNameText.fontStyle` twice in a row identically — duplicate line, remove one.

**Files to touch:** `TeamAndObjectivesUI.cs`, `ObjectiveUI.cs`

---

## Issue: LINQ Shuffle Bug in ItemSpawner and TagItemSpawner (Low Priority)

`spawnPositions.OrderBy(s => Random.value)` discards the result — `OrderBy` returns a new IEnumerable, it doesn't sort in place. Spawn points are never actually shuffled; items always spawn at points in original hierarchy/tag-query order.

**Fix:** `spawnPositions = spawnPositions.OrderBy(s => Random.value).ToList();`

**Files to touch:** `ItemSpawner.cs`, `TagItemSpawner.cs`

---

## Issue: `GameobjectLimiter` — `FindGameObjectsWithTag` Every FixedUpdate (Low Priority)

Scans the entire scene graph with `FindGameObjectsWithTag` every physics tick (50/s) for each limit entry. Should instead track counts via spawn/destroy events rather than polling.

**Files to touch:** `GameobjectLimiter.cs`

---

## Issue: `InteractionArea.beingInteractedWith` Never Sets on Server (Medium Priority)

`InteractionArea.Interact()` sets `beingInteractedWith = true` — but the coroutine only runs on the local player's client (via the `RpcInteractWithArea → isLocalPlayer` path in `PlayerInteractionArea`). If that player is a guest, the SyncVar write is a silent no-op. The server never sees `beingInteractedWith = true`, so the guard in `PlayerInteractionArea` (`!area.beingInteractedWith`) never actually prevents two players from interacting simultaneously with the same area from the server's perspective.

**Fix:** Server should set `beingInteractedWith = true` when the `CmdInteractWithArea` arrives (before firing the Rpc), and clear it when the consequence completes server-side. The client coroutine is cosmetic only — authority over interaction state must be server-owned.

**Files to touch:** `InteractionArea.cs`, `PlayerInteractionArea.cs`

---

## Issue: `IA_DiggingSite` — Likely Unused (Low Priority)

`IA_DiggingSite` is a "hold interact to dig up a random item" mechanic. The user doesn't recall it ever being placed in the scene — likely another abandoned prototype like `IA_Totem`. Confirm in scene and remove if unused.

Additionally, `spawnPercentage` is a serialized field that implies a spawn chance, but `CmdSpawnObj` never reads it — always spawns unconditionally. Dead field regardless.

**Files to touch:** `IA_DiggingSite.cs` (remove if confirmed unused)

---

## Issue: CoconutEvent — Lag, Bandwidth Flood, and Crash Risk (High Priority)

**Root cause:** Each coconut is a full `PickupableItem` with 3 SyncVars (`netPos`, `netRot`, `netVel`) written server-side every FixedUpdate (50/s). The event spawns N coconuts per player simultaneously. At 6 players with a generous spawn range, 30+ networked objects appear in one frame, each immediately flooding Mirror's send queue with SyncVar updates. Combined with existing player position sync this saturates the host's bandwidth. The simultaneous spawn burst itself can overflow Mirror's internal message buffer → crash.

PalmTrees compound this — they independently spawn the same coconut prefab continuously, so by the time the event fires there's already an accumulated coconut population.

Per-coconut CPU cost also adds up: `Physics.Raycast` + `Vector3.Distance` every `Update` per coconut.

**Coconuts are intentionally pickupable/throwable** — so they must remain `PickupableItem`. The fixes target volume and burst, not the networking model:

1. **Stagger spawns** — don't spawn all coconuts in one frame. Spread them over 0.5–1s via a coroutine. Eliminates the burst that crashes Mirror's buffer.
2. **Hard global cap** — check total coconut count before spawning each one, skip if over limit. Replace `GameobjectLimiter` polling with a static counter incremented on spawn, decremented on destroy.
3. **Threshold-based SyncVar updates** — only write `netPos`/`netRot`/`netVel` when they've changed beyond a small epsilon. Same fix as player position sync. Eliminates idle-coconut bandwidth waste.
4. **Tune spawn range down** — fewer coconuts per player reduces baseline cost. Event should feel like a shower, not a storm.

**Also:** `PickupableItem.FixedUpdate` applies extra gravity using `Physics2D.gravity.y` on top of Unity's own 3D physics gravity — double gravity on every coconut (and all other PickupableItems). Use `Physics.gravity.y` or remove the manual application entirely and let Unity's physics handle it.

**Files to touch:** `CoconutEvent.cs`, `PickupableItem.cs`, `GameobjectLimiter.cs`

---

## Issue: `RandomEventSystem` — Three Bugs Blocking Re-enablement (Medium Priority)

The system is fully commented out and disabled. Before re-enabling, three issues need resolving:

**1. `FloorIsLavaEvent.ServerEvent()` is empty** — confirmed working. `Lava.cs` uses `isClient` + `isLocalPlayer` to detect damage (same victim-reports-hit pattern as `PlayerCombat`), so the lava collider only needs to be active client-side. No server-side activation needed.

**2. `ZeroGravityEvent` physics runs client-side only** — confirmed working on listen server. Players use `CharacterController` (not Rigidbody) so they're unaffected; the effect hits world items. Host is also server so server physics changes too via `ClientEvent`. Would break on a dedicated server but that's not the target.

**3. Event dispatch by `GameObject.name` is fragile** — `RpcStartClientEvent(events[index].name)` then matches by name on clients. Rename a GameObject in the hierarchy and the Rpc silently does nothing. Fix: pass the index instead.

**Also:** `FloorIsLavaEvent` lava/water position runs independent `SmoothDamp` per client — visual position will diverge slightly across machines. Minor for a cosmetic effect but worth noting.

**Files to touch:** `RandomEventSystem.cs`, `FloorIsLavaEvent.cs`, `ZeroGravityEvent.cs`

---

## Issue: `IA_PlayerAmount` Misleading Inheritance (Low Priority)

`IA_PlayerAmount` extends `InteractionArea` but never uses its coroutine system. It runs a fully independent `OverlapSphere` poll in `Update`. The inheritance picks up `canBeInteractedWith` / `restrictedToTeam` fields but the is-a relationship is misleading — it behaves nothing like the other `InteractionArea` subclasses. Worth noting when refactoring the interaction system.

**Files to touch:** `IA_PlayerAmount.cs`

---

## Issue: `RpcSwapMainCamp` Writes SyncVars on Clients (Medium Priority)

`CampManager.RpcSwapMainCamp` calls `camps[c].isTotemDispenser = false` and `camps[newMainCamp].SetMainCamp(swapDelay)` (which writes `isUnlocking`, `isCoolingDown`, `cooldownTimer`, `isTotemDispenser`, and `step.isEnabled` on each child CampStep) — all inside a `[ClientRpc]`. Same pattern as `PlayerBuffs`: SyncVar writes on guest clients are silent no-ops. It works by coincidence because the host (who is also server) runs the Rpc and those writes are legitimate. Guest clients receive the SyncVar propagation. But the logic is fragile and wrong — the intent is server-authoritative state, not Rpc-distributed writes.

**Fix:** Move all SyncVar writes in `SetMainCamp` and the `isTotemDispenser = false` reset to server-only code paths. `RpcSwapMainCamp` should only carry cosmetic signals if needed. The SyncVar updates will propagate automatically.

**Files to touch:** `CampManager.cs`, `Camp.cs`

---

## Issue: `CampManager.SwapMainCamp` Infinite Loop If Only One Camp (Low Priority)

`SwapMainCamp()` uses `while (newMainCamp == currentMainCamp)` to pick a different camp. If `camps.Length == 1`, this loops forever. Add a guard: if only one camp exists, skip the swap or return early.

**Files to touch:** `CampManager.cs`

---

## Issue: `SequenceCampStep.sequenceFiguresIndexes` — SyncVar on List Doesn't Work (High Priority)

`[SyncVar] public List<int> sequenceFiguresIndexes` — Mirror's SyncVar system does not support `List<T>`. SyncVar only works on value types and a handful of supported types. The list is assigned wholesale in `NewSequence()` (which runs server-side in `Start`), so the initial value may sync on spawn, but any subsequent reassignment of the list won't reliably propagate. Should be `SyncList<int>` — Mirror's purpose-built networked list type that tracks add/remove/clear operations and syncs them properly.

**Files to touch:** `SequenceCampStep.cs`

---

## Issue: `Totem.RpcBuildTotem` UI Update Is Dead Code (Low Priority)

`Totem.BuildTotem()` calls `RpcBuildTotem(currentVisualStage)` **before** incrementing. So when the last piece is inserted (e.g. stage = 3, visuals.Length = 4), the Rpc fires with stage = 3. The check `if (stage >= visuals.Length)` → `3 >= 4` is false — `matchManager.UpdateTeamAndObjectiveUI()` is never called. The win condition is detected separately by polling in `SingelTotemMatchManager.Update`, so this doesn't break anything, but the UI refresh call inside `RpcBuildTotem` is logically unreachable under normal gameplay.

**Fix:** Either remove the dead UI call, or pass `currentVisualStage + 1` to the Rpc so the check works correctly.

**Files to touch:** `Totem.cs`

---

## Improvement: EyalSandbox Local Test Scene — Clarity Pass (Low Priority)

EyalSandbox uses `isSteam: false` and `TelepathyTransport` for local Mirror testing, bypassing the entire Steam lobby path. The `isSteam` branch in `MenuUIManager` is implicit and easy to miss. Suggested improvements:

- Rename scene to something explicit like `LocalTestScene` or `SandboxLocal`
- Rename the `NetworkManager` GameObject to something clearer (e.g. `NetworkManager_Local`)
- Add a visible in-scene label or disabled GameObject named `[LOCAL MODE - no Steam]` so it's immediately obvious when opening the scene
- Consider extracting the local connection path in `MenuUIManager` into its own clearly named methods (`HostLocal`, `JoinLocal`) rather than sharing `ClickedHost`/`ClickedLobbies` with a branch
- The `isSteam` flag itself could be renamed to something more descriptive (e.g. `useSteamLobby`)

**Do after Phase 4 state machine work is stable.**

---

## Scene Changes Tracker — Phase 4 (State Machine Implementation)

Reference: MCP scan of EyalSandbox confirmed the following scene structure:
- **NetworkManager** GameObject: `CustomNetworkManager` + `FizzySteamworks` + `SteamLobby` + `MenuUIManager` + `OnClientStartStopEvents` + `TelepathyTransport` — all on one object
- **MatchManager** GameObject: `SingelTotemMatchManager` + `NetworkIdentity` + `GameobjectLimiter` + `MatchTimer`
- **AppManager** GameObject: `AppManager` ✅ added Chunk 2
- **UI&SHIT/BootFailedUI** Canvas: `BootFailedUI` ✅ added Chunk 2
- **SteamManager** component: not visible in hierarchy — likely instantiated at runtime by FizzySteamworks or exists as DontDestroyOnLoad

### Already Done
| Chunk | Change | Status |
|---|---|---|
| 2 | Add `AppManager` GameObject + component | ✅ Done |
| 2 | Add `BootFailedUI` Canvas + component | ✅ Done |

### Upcoming Scene Changes by Chunk

**Chunk 3 — No scene changes.**
Purely code: events added to `SteamLobby` and `CustomNetworkManager`, state shells replaced. No new GameObjects.

**Chunk 4 — Button handler rewiring.**
`MenuUIManager` UI buttons currently call `SteamLobby.HostLobby()` and `SteamLobby.GetLobbies()` via OnClick. These need to be rewired in the Inspector to call `AppManager` public methods instead (`RequestCreateLobby`, `RequestBrowseLobbies`, etc.). The `LobbyListItem` prefab's join button also calls `SteamLobby.JoinLobby()` — that callback needs updating too.
No new GameObjects needed.

**Chunk 5 — No scene changes.**
Purely code: Steam/Mirror calls move from `SteamLobby` callbacks into states.

**Chunk 6 — Inspector reference cleanup.**
After coupling is removed, some serialized field references on the `NetworkManager` GameObject may become stale (e.g. `menuUI` on `CustomNetworkManager`, `networkManager` on `SteamLobby`). These should be cleared or removed from the Inspector to avoid orphan references. `SteamLobby` may become a thin class or be removed — its entry in the scene component list would change accordingly.

**Chunk 7 — MatchLifecycleStateMachine wiring.**
`AppManager` creates but does not start `MatchLifecycleStateMachine` yet. Starting it moves to `InGameState.OnEnter()` in Chunk 7 — no new scene objects, but `AppManager` gets updated code. `MatchManager` on the **MatchManager** GameObject needs `[Injectable]` added — no scene change, just a code attribute.

**Chunk 8 — Player prefab.**
`PlayerStateMachine` is per-player, created by `NetworkPlayer` in code. The **player prefab** (not scene) needs `NetworkPlayer` to instantiate and own it. No global scene GameObjects needed.

---

## Issue: `CC_DI` Scripting Define Missing — Required Before Chunk 3 (Phase 4 Blocker)

`CardboardCore`'s dependency injection system is conditionally compiled under `#if CC_DI`. The `[Inject]` fields in states are only populated (via `Injector.Inject(this)` in `State.Enter()`) when this symbol is defined. Without it, all `[Inject]` fields silently remain `null` — no error, just broken behaviour at runtime.

Currently `CC_DI` is **not** in the project's Scripting Define Symbols (checked `ProjectSettings.asset` — only MIRROR defines are present).

**Chunk 1 is unaffected** — shells have no `[Inject]` fields.  
**Chunk 3 will break silently if this is not added first.**

**Fix:** Before starting Chunk 3, add `CC_DI` to Scripting Define Symbols for the Standalone platform via **Edit → Project Settings → Player → Other Settings → Scripting Define Symbols**.

**Files to touch:** `ProjectSettings/ProjectSettings.asset` (via Unity Editor UI)
