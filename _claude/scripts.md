# Island Mayhem — Script Index

> All paths relative to `Assets/Scripts/`.
> **Bold** = server-authoritative. *Italic* = client-local only. Plain = both / shared.
> "Dead" = exists but not wired into the active scene flow.

---

## Folder Tree

```
Scripts/
├── Audio/
│   ├── CollisionSound.cs
│   ├── Compressor.cs
│   └── Limiter.cs
├── Combat/
│   ├── HitInflictor.cs          ← base damage-dealer component
│   ├── Hittable.cs              ← receives hits from HitInflictor
│   ├── Lava.cs                  ← periodic damage zone
│   └── PlayerPunch.cs           ← punch hitbox (extends HitInflictor)
├── Events/
│   ├── RandomEvent.cs           ← base class
│   ├── CoconutEvent.cs
│   ├── FloorIsLavaEvent.cs
│   └── ZeroGravityEvent.cs
├── Hazards/
│   └── SwiperBehavior.cs        ← rotating obstacle
├── Items&Interactinos/
│   ├── ExplorationCamps/
│   │   ├── Camp.cs              ← camp controller (buffs + totem dispensing)
│   │   ├── CampManager.cs       ← rotates which camp is the "main" one
│   │   ├── CampStep.cs          ← base puzzle step
│   │   ├── CampStepButton.cs
│   │   ├── CampStepPressurePlate.cs
│   │   ├── CampStepTimerButton.cs
│   │   ├── ColorSequenceCampStep.cs
│   │   ├── ColorSequenceCampStepButton.cs
│   │   ├── SequenceCampStep.cs
│   │   ├── SequenceCampStepButton.cs
│   │   └── TimedCampStep.cs
│   ├── InteractionArea.cs       ← base interactable
│   ├── IA_DiggingSite.cs        ← dig → random item spawn
│   ├── IA_PlayerAmount.cs       ← multi-player trigger → item spawn
│   ├── IA_Totem.cs              ← totem-building station
│   ├── ItemSpawner.cs           ← spawns items at tagged positions
│   ├── TagItemSpawner.cs        ← like ItemSpawner, finds positions by tag
│   ├── PickupableItem.cs        ← networked pickup item
│   ├── ThrowableItem.cs         ← throwable physics item
│   ├── Totem.cs                 ← the totem object itself
│   └── TotemPiece.cs            ← item consumed to build a totem
├── MatchObjectives/
│   ├── MatchObjective.cs        ← interface
│   ├── SingleTotemObjective.cs
│   ├── TimerObjective.cs
│   └── TotemsObjective.cs
├── Minigames/                   ← DEAD CODE (superseded by Networking/Matches/)
│   ├── MinigameManager.cs
│   ├── MinigameManagerTotems.cs
│   ├── Team.cs                  ← still used by MatchManager
│   └── TeamPlayer.cs            ← empty stub, unused
├── Misc/
│   ├── CursorEnabler.cs
│   ├── DestroyAfter.cs
│   ├── EnabledForPlayersOfTeam.cs
│   ├── FloatTransform.cs
│   ├── GameobjectLimiter.cs
│   ├── NoRotation.cs
│   ├── OrbitAround.cs
│   ├── OutOfBounds.cs
│   ├── PalmTree.cs
│   ├── ScreenShot.cs
│   └── TimeFormatter.cs
├── Networking/
│   ├── CustomNetworkManager.cs  ← Mirror NetworkManager subclass (core)
│   ├── SteamLobby.cs            ← Steam matchmaking + transport setup
│   ├── Cheats.cs                ← F11 debug window
│   ├── CustomNetworkTransform.cs
│   ├── CustomNetworkTransformBase.cs
│   ├── Matches/
│   │   ├── MatchManager.cs      ← base match manager
│   │   ├── TTTMatchManager.cs   ← adds objectives + team colours
│   │   ├── SingelTotemMatchManager.cs  ← adds timer + win condition
│   │   └── MatchTimer.cs        ← networked countdown timer
│   ├── Misc/
│   │   ├── OnClientStartStop.cs
│   │   ├── OnClientStartStopEvents.cs
│   │   ├── OnMatchStartStop.cs
│   │   └── OnMatchStartStopEvents.cs
│   └── VoiceChat/
│       ├── SteamlessVoiceChat.cs
│       └── Spatializer.cs
├── Player/
│   ├── NetworkPlayer.cs         ← player identity + state hub
│   ├── ThirdPersonCharacterController.cs
│   ├── ThirdPersonCameraController.cs
│   ├── LocalPlayerInput.cs
│   ├── PlayerCombat.cs
│   ├── PlayerAnimations.cs
│   ├── PlayerBuffs.cs
│   ├── PlayerInteractionArea.cs
│   ├── PlayerItemInteractions.cs
│   ├── PlayerParticles.cs
│   ├── PlayerThrowTargetController.cs
│   └── LookAtCamera.cs
├── StateMachineBehaviours/
│   └── LandSMB.cs
├── TwitchChat/
│   ├── SimpleExample.cs
│   └── IRC/
│       ├── TwitchIRC.cs
│       ├── Chatter.cs
│       ├── IRCMessages.cs
│       ├── MainThread.cs
│       └── ParseHelper.cs
└── UI/
    ├── MenuUIManager.cs
    ├── DeathFade.cs
    ├── LobbyListItem.cs
    ├── ObjectiveUI.cs
    └── TeamAndObjectivesUI.cs
```

---

## Detailed Descriptions

### Audio

**CollisionSound** — Listens for `OnCollisionEnter`. If the collision impulse is above a threshold and the AudioSource isn't already playing, picks a random clip from an array, randomizes pitch, and plays it. Self-contained — no other script talks to it.

**Compressor** — DSP audio compressor that runs on the audio thread via `OnAudioFilterRead`. Each sample frame it checks if the signal exceeds a threshold, then smoothly drives a gain value down (attack) or back up (release) using `Mathf.Lerp`. Clamps output to prevent clipping. Sits on the same GameObject as whatever audio source needs compression (voice chat output).

**Limiter** — Minimal hard clipper, also via `OnAudioFilterRead`. Just clamps every sample to ±limit. No state, no parameters beyond the limit value. Sits after `Compressor` in the audio chain.

---

### Combat

**HitInflictor** — Base component placed on any collider that should deal damage. Holds `isActive`, `initiatorNetId`, `damage`, and `knockbackPower` as SyncVars or fields. `ActivateInflictorForDuration()` starts a coroutine that flips `isActive` off after N seconds. `HitInflicted()` is called by the Hittable it hits — if `singularDmgInstance` is set it deactivates itself immediately. Nothing actively polls this; `Hittable` detects it on trigger.

**Hittable** — Server-only hit receiver. `OnTriggerEnter` checks if the colliding object has an active `HitInflictor` whose `initiatorNetId` isn't the same object (no self-damage). If so, applies an impulse knockback force on the Rigidbody, calls `hit.HitInflicted()`, and starts a 1-second cooldown via an `Update` timer. Only runs on the server.

**Lava** — Client-side damage trigger. `FixedUpdate` increments a cooldown timer. `OnTriggerStay` checks that timer, then if the overlapping collider is the **local** player's `PlayerCombat` and they're not invulnerable, sends `CmdPlayerWasHit` to the server. The server does the actual damage — lava just fires the command.

**PlayerPunch** — Extends `HitInflictor`. A child collider on the player's arm. `OnEnable` resets the lifespan timer and activates the inflctor. `Update` counts down the lifespan and deactivates the GameObject when it expires. Stores `punchBaseDmg` / `punchBaseKP` at startup so `PlayerBuffs` can multiply and restore correctly.

---

### Events

**RandomEvent** — Abstract NetworkBehaviour base. Defines `ServerEvent()` and `ClientEvent()` as virtual methods with empty bodies. Subclasses override both. `RandomEventSystem` holds an array of these and calls both methods in sequence when triggering an event.

**CoconutEvent** — `ServerEvent` iterates over all connected players (`CustomNetworkManager.GetAllPlayers()`) and `NetworkServer.Spawn`s real coconut prefabs in random positions around each. `ClientEvent` emits a particle-only (fake) coconut system around the local player and instantiates a floating message prefab.

**FloorIsLavaEvent** — `ServerEvent` is empty. `ClientEvent` runs a coroutine (`MakeTheFloorLava`) that smoothly lerps the water mesh down and lava mesh up using `SmoothDamp`, holds that state for `duration`, then reverses. Fades lava audio in and out in sync. The lava GameObject itself (with `Lava.cs`) actually deals damage — this just animates the visual swap.

**ZeroGravityEvent** — `ServerEvent` runs the zero-G routine only if not also a client (avoids double-run on listen server). `ClientEvent` always runs it. The coroutine flips `useGravity` on every `Rigidbody` in the scene, fires them upward, then applies a small downforce each `FixedUpdate` for `duration`, then restores gravity.

---

### Hazards

**SwiperBehavior** — Server-only rotating obstacle. Destroys itself on clients in `Start`. In `FixedUpdate` uses `MoveRotation` with `Quaternion.Slerp` to spin continuously based on the `torque` vector and `speedMultiplier`. The physics engine on the server handles resulting collisions with players/items; Mirror replicates those transforms.

---

### Items & Interactions — ExplorationCamps

**Camp** — The main camp controller. `Update` drives all visual state: overhead text, timer display, totem dispenser icon visibility. On the server, it counts completed `CampStep` children and when all are done calls `CampDone()`. `CampDone()` either spawns a totem piece (if it's the "main" camp) and tells `CampManager` to rotate to the next camp, or does a proximity check and gives a random `Buff` to nearby players via `RpcGiveBuff`. Has a finite `spawnsLeft` count. Interacts with `MatchManager` (to check `gameStarted`), `CampManager`, `PlayerBuffs`, and `NetworkServer.Spawn`.

**CampManager** — Owns all `Camp` children. Tracks `currentMainCamp` as a SyncVar. When a camp finishes, `SwapMainCamp()` picks a random different index and broadcasts via `RpcSwapMainCamp()`, which clears all camps' `isTotemDispenser` flag and calls `SetMainCamp(swapDelay)` on the new one. The delay gives a countdown before it becomes active.

**CampStep** — Base networked puzzle step. SyncVars `isCompleted` and `isEnabled`. `Update` drives idle particle visibility. `CompleteStep()` sets the flag and fires `RpcCompleteStep()` to play finish particles on all clients. `ResetStep()` re-enables it. Subclasses override the completion trigger.

**CampStepButton** — Completes when a player presses it. Button press detected via trigger/interaction.

**CampStepPressurePlate** — Completes when weight (player or object) is on it.

**CampStepTimerButton** — Button that must be held for a set duration to complete.

**ColorSequenceCampStep** — Sequence puzzle where buttons must be pressed in a specific colour order. Tracks player input and resets on wrong press.

**ColorSequenceCampStepButton** — One coloured button in a `ColorSequenceCampStep`.

**SequenceCampStep** — Order-based sequence puzzle (non-colour variant). Same logic, different visual presentation.

**SequenceCampStepButton** — One button in a `SequenceCampStep`.

**TimedCampStep** — Completes automatically (or requires action) within a countdown.

---

### Items & Interactions

**InteractionArea** — Base class for all interactable world objects. SyncVars `canBeInteractedWith` and `beingInteractedWith` let any client check state. `restrictedToTeam` gates interaction by team. `Interact()` is a coroutine — it sets `beingInteractedWith`, waits for `interactionDuration`, then subclasses do their work. `EndInteraction()` resets state and fires the callback. Nothing calls this directly — `PlayerInteractionArea` drives it.

**IA_DiggingSite** — When the player finishes the interaction wait, calls `CmdSpawnObj()` which picks a random prefab from `spawnableObjects[]` and `NetworkServer.Spawn`s it above the site. Single-use is controlled by `isDisabledAfterInteraction` from the base.

**IA_PlayerAmount** — Doesn't use the `Interact()` pattern. Instead, `Update` (server-only path) does a `Physics.OverlapSphere` every frame to count players nearby. When threshold met, spawns an object, decrements `currSpawnLimit`, starts a cooldown. A `ClientRpc` starts the visual countdown timer on all clients. Material swaps to indicate state.

**IA_Totem** — Uses the full `Interact()` coroutine. After the wait, checks if the local player has a `TotemPiece` with a matching group. If so, calls `player.DestroyItem()` to consume it and sends `CmdBuildTotem()` to the server, which increments `currentVisualStage` on the `Totem` and fires `RpcBuildTotem()` for smoke VFX. Interacts with `PlayerItemInteractions`, `TotemPiece`, `CustomNetworkManager.GetLocalPlayer()`.

**ItemSpawner** — Called externally (e.g. by `MatchManager` on game start) via its `Spawn()` method. Finds tagged spawn point GameObjects, attempts to shuffle them (LINQ bug: shuffle discards result), then distributes random items from `spawnableObjects[]` across those points via `NetworkServer.Spawn`. Note: has a `destroyAfterSpawn` option.

**TagItemSpawner** — Same as `ItemSpawner` but finds positions at runtime by tag rather than pre-assigned references. Same LINQ shuffle bug.

**PickupableItem** — Networked item that can be held. SyncVar `isBeingHeld`. When held, its position is synced each frame to follow the player's hold position (manual position sync — one of three in the project). `CmdPickUp` / `CmdDrop` change authority and `isBeingHeld`. `GameobjectLimiter` reads `isBeingHeld` to avoid destroying held items.

**ThrowableItem** — Physics-based throwable. `UseMain()` applies a throw force vector to the Rigidbody. Trajectory is shaped by `aimMultiplyerVec`, `aimAdditionVec`, and `throwForce` — `PlayerItemInteractions` and `Cheats` both use it. Has `HitInflictor` sibling component for damage on impact.

**Totem** — The networked totem object. SyncVar `currentVisualStage` drives which mesh pieces are visible in `Update`. `maxVisualStageReached` flips true when the stage hits `visuals.Length`. Used by `SingleTotemObjective` and `TotemsObjective` to check win state.

**TotemPiece** — Simple MonoBehaviour on a pickup item. `Update` shows/hides a light beam based on `insertedToTotem`. Has a `group` enum (None/Any/Red/Blue) that `IA_Totem` checks for team-colour matching.

---

### Match Objectives

These are plain C# classes (not MonoBehaviour) used as data objects — no Unity lifecycle, just properties.

**MatchObjective** — Interface defining `IsCompleted`, progress counters (`MultiObjRequirement`, `MultiObjCurrIndex`), `ObjectiveName`, and `ProgressIndication`. `ObjectiveUI` displays these; `SingelTotemMatchManager` checks `IsCompleted`.

**SingleTotemObjective** — Wraps one `Totem` reference. `IsCompleted` returns `totem.maxVisualStageReached`. Progress shows current vs total visual stages. Created and held by `TTTMatchManager`.

**TotemsObjective** — Wraps a list of `Totem`s. Completed only when every totem reaches max stage. Tracks partial progress with `FindAll`.

**TimerObjective** — Wraps a `MatchTimer`. Completed when `timer.IsTimeOver`. Represents the defensive "prevent building" goal for one team. Progress indication is empty (timer is shown separately in the HUD).

---

### Minigames (Dead Code)

**MinigameManager** — Old match manager with its own player tracking, team assignment, and timer. `Update` waits for enough players then calls `CalculateAndAssignTeams()`, which shuffles via `RandomizeComparer` and distributes players round-robin across teams. Superseded by `Networking/Matches/MatchManager`.

**MinigameManagerTotems** — Subclass of `MinigameManager` with all overrides delegating straight to `base`. Has commented-out totem list. Completely inert.

**Team** — Simple data class still used by `MatchManager`. Holds `teamNumber` and `List<NetworkPlayer> playersInTeam`. `IsInTeam()` searches by `netId`.

**TeamPlayer** — Empty MonoBehaviour stub. Start and Update are empty. Nothing uses it.

---

### Misc

**CursorEnabler** — `Update` toggles cursor lock/visibility on Tab press. Used in the game scene so players can access the OS cursor.

**DestroyAfter** — `Start` calls `Destroy(gameObject, seconds)` and immediately destroys itself. Drop on any temporary GameObject (particles, VFX, messages) to auto-clean it.

**EnabledForPlayersOfTeam** — `Update` lazily gets the local `NetworkPlayer` once `localPlayerInitialized` is true, then calls `gameObject.SetActive(team == localPlayer.playerTeam)` each frame. Used to show team-specific world markers or UI.

**FloatTransform** — `Update` uses Perlin noise to continuously animate local position and rotation within configurable ranges. Seeds are randomized on start so multiple instances on screen don't move in sync. Used on floating pickup items.

**GameobjectLimiter** — Server-only. `FixedUpdate` uses `FindGameObjectsWithTag` for each configured tag/limit pair, counts them, and `NetworkServer.Destroy`s the excess oldest objects. Skips objects whose `PickupableItem.isBeingHeld` is true. Fires `RpcSpawnSmokePoof` before each destroy for visual feedback.

**NoRotation** — `LateUpdate` locks the object to its parent's world position + a fixed offset while forcing world rotation to zero. Used on overhead text/icons that should float above a rotating parent without rotating themselves.

**OrbitAround** — `Update` calls `RotateAround` on a target Transform at constant `speed` on the configured `axis`. Used on orbiting visual effects.

**OutOfBounds** — `OnTriggerEnter` snaps any Rigidbody on the matching layer back to a minimum Y. Acts as a fall-death recovery — teleports the object back up rather than destroying it.

**PalmTree** — Server-only. `FixedUpdate` increments a cooldown timer. When it fires, does a `Physics.OverlapBox` in front of the tree. If a player is detected, `NetworkServer.Spawn`s a coconut and calls `RpcPunchImpact` to play visual feedback on all clients.

**ScreenShot** — `Update` captures a full-resolution screenshot to `Assets/Screenshots/` on P press, but only in the editor. No gameplay effect.

**TimeFormatter** — Static utility class with one method: converts a float (seconds) to a `"MM:SS"` string. Used by camp timers, `IA_PlayerAmount`, and the match timer HUD.

---

### Networking

**CustomNetworkManager** — The central Mirror NetworkManager subclass. Overrides `OnServerAddPlayer` to spawn the player prefab and register it in the `connectedPlayers` dictionary (`uint netId → NetworkIdentity`). `OnServerDisconnect` removes entries. Sets `localPlayerInitialized = true` when `OnStartLocalPlayer` fires. Provides static helpers (`GetAllPlayers`, `GetLocalPlayer`, `GetPlayerByNetId`) used by almost every other networked script. Also fires `OnClientStartStopEvents` lifecycle events on connect/disconnect.

**SteamLobby** — Manages the full Steam matchmaking flow. On `CreateLobby()` calls `SteamMatchmaking.CreateLobby()` and waits for `LobbyCreated_t` callback, then sets lobby metadata and calls `StartHost()`. On `GetLobbiesList()` calls `RequestLobbyList()` and waits for `LobbyMatchList_t` to populate the `MenuUIManager` lobby browser. On `JoinLobby(CSteamID)` reads the host's Steam ID from lobby data, sets it as `CustomNetworkManager.networkAddress`, and calls `StartClient()`. `LobbyEnter_t` fires on the joining client and is the trigger for that sequence. Also has debug hotkeys (G/H) in `Update` for quick host/join without the UI. `GameLobbyJoinRequested_t` (Steam friend invite) is present but commented out.

**Cheats** — F11 debug/cheat window, only active in editor or debug builds. Password-gated in non-editor. `Update` lazily resolves references to the local player components once connected. The IMGUI window exposes: force-start game (server only), trigger random events, grow/shrink player, move/jump sliders, coconut machine gun (server only), spawn any networked prefab from the Mirror spawn list. Interacts with `MatchManager`, `RandomEventSystem`, `ThirdPersonCharacterController`, `PlayerItemInteractions`.

**CustomNetworkTransformBase** — Proper interpolated network transform. Owner sends position + compressed quaternion timestamps as `DataPoint`s. Non-owners interpolate between the two most recent data points. Large position delta triggers a teleport (snap instead of interpolate). **Not on the player prefab** — used on world objects that need smooth movement (e.g. rolling items).

**CustomNetworkTransform** — Concrete empty subclass of `CustomNetworkTransformBase`. Exists so prefabs can reference a concrete type.

---

#### Networking/Matches

**MatchManager** — Base networked match manager. Holds `teams` (List\<Team\>), `gameStarted` SyncVar, reference to `networkManager`. `CalculateAndAssignTeams()` shuffles all connected players with `RandomizeComparer` and distributes them round-robin, setting each `NetworkPlayer.playerTeam`. `RpcSyncTeamInfo(json)` ships team data to clients as JSON. `RpcStartGame()` tells all clients the match has begun and fires `OnMatchStartStopEvents`. `Update` on the server checks for debug force-start (Enter key). `ResetManager()` fires on `OnClientStartStop` hooks — known to fire multiple times.

**TTTMatchManager** — Extends `MatchManager`. Adds `teamColors[]`, `teamNames[]`, and per-team objective lists (`team0Objectives`, `team1Objectives`). Overrides `RpcStartGame` to populate those objective lists (constructing `SingleTotemObjective`, `TotemsObjective`, `TimerObjective` instances from scene refs). Used directly by `TeamAndObjectivesUI` for team colour and objective display.

**SingelTotemMatchManager** — Extends `TTTMatchManager`. Adds a `MatchTimer` reference and win-condition checking. `Update` calls `matchTimer.StartTimer` once on game start, then each frame checks `team0Objectives` for completion to call `EndMatch()`. The name is a typo ("Singel"). This is the match manager actually active in the main scene.

**MatchTimer** — Networked countdown. SyncVar `startTime` is set to `Time.time` when `StartTimer(duration)` is called on the server. `GetMatchTimeString` property computes remaining time as `duration - (Time.time - startTime)`. **Bug**: because `Time.time` is not synchronized across server and client, clients compute a different elapsed time. `IsTimeOver` property used by `SingelTotemMatchManager`.

---

#### Networking/Misc

**OnClientStartStop** — Interface. Two methods: `OnClientStart()` and `OnClientStop()`. Implement on any MonoBehaviour that needs to know when the Mirror client connects or disconnects.

**OnClientStartStopEvents** — NetworkBehaviour that sits in the scene. Mirror calls `OnStartClient` / `OnStopClient` on it directly. It then does `FindObjectsOfType<OnClientStartStop>()` and broadcasts to all implementors. `MatchManager.ResetManager` is called through this path.

**OnMatchStartStop** — Interface. `OnMatchStart()` and `OnMatchStop()`. Implement on anything that activates/deactivates with a match.

**OnMatchStartStopEvents** — Same broadcast pattern as `OnClientStartStopEvents` but for match start/stop. Called via `ClientRpc` from `MatchManager.RpcStartGame()`.

---

#### Networking/VoiceChat

**SteamlessVoiceChat** — Records from the default microphone via `Microphone.Start`. Each frame, encodes the captured audio data using `BinaryFormatter` (deprecated, broken in Unity 6 IL2CPP), and sends it via `CmdSendVoice` which broadcasts to all clients via `RpcReceiveVoice`. No proximity filtering — all players hear everyone at equal volume. The `Spatializer` component on the same object handles the 3D falloff.

**Spatializer** — Manual 3D audio spatialization via `OnAudioFilterRead`. `Update` lazily finds the local player's `AudioListener`. Each audio frame, evaluates an `AnimationCurve` against normalized distance to that listener, and multiplies all audio samples by the resulting volume. Works around Unity's built-in 3D audio not being suitable for the project's voice chat setup.

---

### Player

**NetworkPlayer** — The networked identity hub for each player. SyncVars include `playerName` (read from Steam persona name), `playerTeam`, `currHp`, `maxHp`. Holds a reference to the local camera. On death: calls `DeathFade.FadeOut`, disables a list of components, then starts a respawn coroutine that waits, re-enables components, teleports to a spawn point, and calls `DeathFade.FadeIn`. Other systems (UI, camps, objectives) look at `playerTeam` here.

**ThirdPersonCharacterController** — Networked movement. `Update` on the local player reads from `LocalPlayerInput` and computes a velocity vector (walk/sprint with `PlayerBuffs` multipliers, gravity accumulation, jump). Applies it via `CharacterController.Move`. `isGrounded`, `isMoving`, `isSprinting`, `isAiming`, `isCrippled`, `isDead` are read by `PlayerAnimations`. Calls `CmdPlayLandClip()` for networked audio when landing. Interacts with `PlayerBuffs`, `PlayerAnimations`, `LocalPlayerInput`, `ThirdPersonCameraController` (for aim direction).

**ThirdPersonCameraController** — Local-only, not a NetworkBehaviour. `LateUpdate` accumulates mouse input and rotates a camera arm pivot. Does a `Physics.Linecast` dolly test every frame to push the camera closer when geometry is in the way. On aim mode toggle, smoothly lerps camera position, rotation, and FOV to a configured aim position over `cameraTransitionDuration`. Reads `characterController.isLookingAround` to decide between free-look and locked rotation modes.

**LocalPlayerInput** — Local-only input bus. `Update` polls all `Input` axes and keys and writes them into plain fields (`moveInput`, `jumpInputDown`, `interactInputDown`, etc.). All other player scripts read from this instead of calling `Input` directly. Makes it easy to swap or mock input.

**PlayerCombat** — Handles HP, damage intake, invulnerability, and death. `CmdPlayerWasHit(netId, hitVec, dmg, isStunning)` validates on the server, decrements HP, applies knockback force, and calls `RpcPlayerWasHit` to run hit reactions on all clients (animation, invuln flash, cripple). On HP reaching zero, triggers death sequence via `NetworkPlayer`. `isInvulnerable` is read by `Lava` and `Hittable` to skip damage.

**PlayerAnimations** — Networked animation driver. `Update` (local player only) reads state from `ThirdPersonCharacterController` and `PlayerCombat` and sets Animator bool parameters. `FixedUpdate` syncs animation playback speed to movement speed via a `[Command]` that sets the `netPlayerAnimatorSpeed` SyncVar. `NetworkAnimator` handles trigger replication (jump, throw, punch, hit, death). SyncVar `currentSkin` picks which skin Animator is active — set randomly on the server in `Start`, then all clients activate that skin index.

**PlayerBuffs** — Five temporary stat boosts (MoveSpeed, SizeUp, JumpForce, Health, PunchPower). Server calls `RpcGiveBuff(Buff)` to apply on all clients. Each buff starts a `BuffDecay` coroutine that reverts the stat after `buffDecayTime` seconds. If the same buff is applied again before decay, the old coroutine is replaced. `PurgeBuffs()` instantly clears everything, called on death. Directly mutates `PlayerCombat.maxHp`, `ThirdPersonCharacterController` speed, `PlayerPunch` damage — no clean interface.

**PlayerInteractionArea** — Handles detecting and entering interactions. `Update` (local player only) checks `LocalPlayerInput.interactInputDown`, does a `Physics.OverlapSphere` around the player, finds an `InteractionArea`, validates team restriction, then sends `CmdInteractWithArea(playerNID, areaNID)` to the server. Server routes back via `RpcInteractWithArea(areaNID)` which calls `InteractWithArea()` on the local client — starting the area's `Interact()` coroutine and disabling configured behaviours while busy.

**PlayerItemInteractions** — Manages the `heldItem` reference. `Update` syncs held item position to `itemHoldPos` each frame (manual position sync). Exposes `CmdPickUpItem` / `CmdDropItem` for pick-up/drop. `DestroyItem()` used by `IA_Totem` to consume a totem piece. `PlayerThrowTargetController` reads `heldItem` to decide if a throw is possible. `PlayerAnimations` reads `heldItem != null` for the hold animation.

**PlayerParticles** — Local-only. `Update` checks the horizontal component of `ThirdPersonCharacterController.playerVelocity` against a threshold and enables/disables two `ParticleSystem.EmissionModule`s. No networking.

**PlayerThrowTargetController** — Local-only throw aim cursor. When the player is aiming and holding an item, projects a trajectory arc and positions a world-space target indicator. Feeds throw direction into `PlayerItemInteractions` when the throw input fires.

**LookAtCamera** — Billboard utility. `FixedUpdate` lazily resolves the local player's camera transform once (via `CustomNetworkManager.GetLocalPlayer()`). Then calls `transform.LookAt(cam)` every frame. Used on overhead UI GameObjects and item icons so they always face the local player's view.

---

### StateMachineBehaviours

**LandSMB** — Attached to the "Land" state in the player Animator. `OnStateEnter` fires when the landing animation begins. Lazily resolves the local player's `ThirdPersonCharacterController` via `CustomNetworkManager.GetLocalPlayer()` and calls `CmdPlayLandClip()` to play the landing sound across the network. Nothing else uses this — it's purely animator-driven.

---

### TwitchChat

**TwitchIRC** — Full Twitch IRC client. `Awake` connects if `connectOnStart` is set. Two background threads (input / output) are started. The input thread reads lines from the TCP stream in a blocking loop, parses PRIVMSG/USERSTATE/join confirmations, and dispatches `NewChatMessageEvent` back to the Unity main thread via `MainThread`. The output thread drains a queue with a 1750ms rate limit. Exposes `SendChatMessage()` and `SendCommand()`. Not connected to any gameplay system — standalone.

**SimpleExample** — Demo listener for `TwitchIRC`. Subscribes to `newChatMessageEvent` and appends `displayName: message` to a UI Text element. All meaningful example code is commented out. Not used in gameplay.

**Chatter** — Data class. Holds parsed IRC message data: login name, display name, message text, badges, emotes, colour. Constructed from `IRCPrivmsg` + `IRCTags`. Passed to `NewChatMessageEvent` listeners.

**IRCMessages** — Structs for raw IRC message types: `IRCPrivmsg` (channel, login, message) and `IRCUserstate` (channel). Used internally by `TwitchIRC`.

**MainThread** — Singleton dispatcher. Other threads call `MainThread.Instance.Enqueue(action)` to schedule Unity callbacks. Drains the queue in `Update` on the main thread. Used by `TwitchIRC`'s receive thread to safely invoke Unity events.

**ParseHelper** — Static helpers for pulling fields out of IRC protocol strings: login name, channel, message body, and full tag parsing (badges, emotes, display name, colour). Called by `TwitchIRC`.

---

### UI

**MenuUIManager** — Drives the main menu flow. Manages multiple UI panels (main, create lobby, browse lobbies, in-lobby). Calls `SteamLobby` methods for create/browse/join. `Update` refreshes the player list display in the lobby panel by reading member count and names from `SteamMatchmaking`. Starts/stops the lobby list scroll (browsing). Handles the transition from lobby panel to game start.

**DeathFade** — Thin wrapper around a full-screen UI `Image`. `FadeOut(duration)` fades the image to opaque (black screen). `FadeIn(duration)` fades back to transparent. Called by `NetworkPlayer` at the start and end of the respawn sequence via component reference. No `Update`.

**LobbyListItem** — A single row in the lobby browser list. Populated by `MenuUIManager` via `updateLobbyUI()` which sets owner name, player count text, stores the `CSteamID`, and registers a click listener. Click fires the `onLobbyClicked` callback with the stored `CSteamID` back to `MenuUIManager`.

**ObjectiveUI** — Displays one `MatchObjective`. `Start` sets the objective name text and positions itself in the list. `Update` writes `ProgressIndication` and applies strikethrough font style when `IsCompleted` is true. Instantiated dynamically by `TeamAndObjectivesUI`.

**TeamAndObjectivesUI** — In-game HUD panel. `Update` lazily resolves the local `NetworkPlayer`. Once the player's `playerTeam` is assigned (not -1), calls `RefreshUI()` once — which sets the background colour and team name from `TTTMatchManager` and rebuilds the objective list by instantiating one `ObjectiveUI` prefab per objective. Also updates the timer text each frame from `SingelTotemMatchManager.matchTimer`. Directly references `TTTMatchManager` by type.

---

## External Systems We Interface With

| System | Where we touch it |
|--------|------------------|
| **Mirror — NetworkManager** | `CustomNetworkManager` extends it. We override `OnServerAddPlayer`, `OnClientConnect`, `OnServerDisconnect` etc. |
| **Mirror — NetworkBehaviour** | ~25 of our scripts. `isServer`, `isClient`, `isLocalPlayer`, `netId`, `[SyncVar]`, `[Command]`, `[ClientRpc]` throughout. |
| **Mirror — NetworkServer** | `NetworkServer.Spawn()` / `NetworkServer.Destroy()` called from MatchManager, ItemSpawner, events, palms, camps. |
| **Mirror — NetworkAnimator** | Used in `PlayerAnimations` to sync animation triggers across the network. |
| **Steamworks.NET — SteamMatchmaking** | `CreateLobby`, `RequestLobbyList`, `JoinLobby`, `GetLobbyOwner`, `GetLobbyMemberCount`, `SetLobbyData` / `GetLobbyData`. All in `SteamLobby.cs`. |
| **Steamworks.NET — SteamFriends** | `GetFriendPersonaName(SteamID)` used in `SteamLobby` for player list display. |
| **Steamworks.NET — SteamUser** | `GetSteamID()` to identify the local user. |
| **Unity CharacterController** | `ThirdPersonCharacterController` uses it for movement + `isGrounded`. |
| **Unity Animator / NetworkAnimator** | `PlayerAnimations` drives bool/trigger params. `LandSMB` is a `StateMachineBehaviour`. |
| **Unity Microphone** | `SteamlessVoiceChat` captures audio. |
| **Unity Physics** (OverlapSphere/Box, Linecast) | `PlayerInteractionArea`, `IA_PlayerAmount`, `PalmTree`, `ThirdPersonCameraController` (dolly collision). |
