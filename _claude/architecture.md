# Island Mayhem — Codebase Architecture

## Status
✅ Phase 2 analysis complete — populated from codebase read

## Game Summary
3rd-person multiplayer island brawler. Two teams (Natives vs Explorers) fight, collect totem pieces, and complete objectives. Steam P2P lobbies, Mirror networking. One scene contains everything — menu, game, and lobby state all coexist.

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

## Improvement Priorities (Draft — to confirm together)
1. Replace `OnLevelWasLoaded` with `SceneManager.sceneLoaded` (Unity 6 compat)
2. State machine for lobby/connection flow (Phase 3)
3. Re-enable and fix `RandomEventSystem`
4. Fix `GameLobbyJoinRequested` — Steam friend invites
5. Clean up duplicate position sync (NetworkPlayer vs CustomNetworkTransform)
6. Remove debug hotkeys (G, H, Return)
