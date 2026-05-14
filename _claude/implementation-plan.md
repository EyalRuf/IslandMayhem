# Island Mayhem — State Machine Implementation Plan

## How to Use This Document
Each chunk is a self-contained unit of work. Complete and verify one chunk before starting the next. Each chunk specifies exactly which files to create, which to modify, what to change, and how to verify it worked. The game must be playable after every chunk.

Read `state-machine.md` and `networking.md` before starting any chunk. Read `architecture.md` for context on known issues.

## Library: CardboardCore
This project uses CardboardCore for state machines and dependency injection. Key patterns:
- State machines extend `StateMachine`. Transition graph is defined in the constructor via `SetInitialState<T>()`, `AddStaticTransition<From,To>()`, `AddFreeFlowTransition<From,To>()`. Call `.Start()` after setup.
- States extend plain `State` (not `State<T>`). Logic lives in `OnEnter()` and `OnExit()`. Access the owning machine via `owningStateMachine`.
- Managers are plain `MonoBehaviour` classes marked `[Injectable]`. States declare `[Inject] private SomeManager manager;` — injection is automatic on `Enter()`, release on `Exit()`.
- **States are the brain. Managers are dumb.** States call methods on managers; managers do not drive state transitions. UI panels, network calls, and Steam calls are all initiated by states.
- No `OnStateChanged` event is used. States call manager methods directly in `OnEnter()`.
- See the GradProj at `D:\GameDev\Y4\Graduation\GradProj` for real usage reference.

---

## Chunk 1 — CardboardCore State Machine Shells
**Risk: None. Additive only. No existing files touched.**

### Goal
Create all four state machine classes and all their state classes using CardboardCore. Establish the pattern all future chunks follow. Nothing is wired to anything yet — states log on enter/exit and do nothing else.

### File Structure to Create

```
Assets/Scripts/StateMachines/
  App/
    AppStateMachine.cs
    States/
      BootingState.cs
      MainMenuState.cs
      InGameState.cs
      BootFailedState.cs
  Lobby/
    LobbyStateMachine.cs
    States/
      IdleState.cs
      CreatingLobbyState.cs
      BrowsingLobbiesState.cs
      JoiningLobbyState.cs
      InLobbyState.cs
      LoadingGameState.cs
      SessionEstablishedState.cs
      ReconnectingState.cs
  Match/
    MatchLifecycleStateMachine.cs
    States/
      SetupPhaseState.cs
      CountdownState.cs
      InProgressState.cs
      GameOverState.cs
  Player/
    PlayerStateMachine.cs
    States/
      AliveState.cs
      DownedState.cs
```

### State Machine Patterns

**Machine class — defines the graph:**
```csharp
public class AppStateMachine : StateMachine
{
    public AppStateMachine() : base(enableDebugging: true)
    {
        SetInitialState<BootingState>();

        AddFreeFlowTransition<BootingState, MainMenuState>();
        AddFreeFlowTransition<BootingState, BootFailedState>();
        AddFreeFlowTransition<MainMenuState, InGameState>();
        AddStaticTransition<InGameState, MainMenuState>();
    }
}
```

**State shell — OnEnter/OnExit only:**
```csharp
public class BootingState : State
{
    protected override void OnEnter()
    {
        Debug.Log("[AppStateMachine] Entered BootingState");
    }

    protected override void OnExit()
    {
        Debug.Log("[AppStateMachine] Exited BootingState");
    }
}
```

### Transition Tables

**AppStateMachine:**
| From | To | Type |
|---|---|---|
| `BootingState` | `MainMenuState` | FreeFlow |
| `BootingState` | `BootFailedState` | FreeFlow |
| `MainMenuState` | `InGameState` | FreeFlow |
| `InGameState` | `MainMenuState` | Static |

**LobbyStateMachine:**
| From | To | Type |
|---|---|---|
| `IdleState` | `CreatingLobbyState` | FreeFlow |
| `IdleState` | `BrowsingLobbiesState` | FreeFlow |
| `IdleState` | `ReconnectingState` | FreeFlow |
| `CreatingLobbyState` | `InLobbyState` | FreeFlow |
| `CreatingLobbyState` | `IdleState` | Static (failure) |
| `BrowsingLobbiesState` | `JoiningLobbyState` | FreeFlow |
| `BrowsingLobbiesState` | `IdleState` | Static (back) |
| `JoiningLobbyState` | `InLobbyState` | FreeFlow |
| `JoiningLobbyState` | `IdleState` | Static (failure) |
| `InLobbyState` | `LoadingGameState` | FreeFlow |
| `InLobbyState` | `IdleState` | Static (leave) |
| `LoadingGameState` | `SessionEstablishedState` | FreeFlow |
| `LoadingGameState` | `IdleState` | Static (failure) |
| `SessionEstablishedState` | `ReconnectingState` | FreeFlow |
| `SessionEstablishedState` | `IdleState` | Static (match end) |
| `ReconnectingState` | `SessionEstablishedState` | FreeFlow |
| `ReconnectingState` | `IdleState` | Static (give up) |

**MatchLifecycleStateMachine** (linear — all Static):
`SetupPhaseState` → `CountdownState` → `InProgressState` → `GameOverState`

**PlayerStateMachine:**
| From | To | Type |
|---|---|---|
| `AliveState` | `DownedState` | FreeFlow |
| `DownedState` | `AliveState` | FreeFlow |

### Verification
- Project compiles with no errors
- No existing behavior changed whatsoever

---

## Chunk 2 — AppManager + Boot Sequence
**Risk: Low. Moves SteamAPI.Init() — verify timing carefully.**

### Goal
Create `AppManager` as the persistent root MonoBehaviour. It owns all four state machine instances, starts `AppStateMachine`, and handles `SteamAPI` lifecycle. `BootingState` now contains the real boot logic: it attempts `SteamAPI.Init()` and transitions accordingly. Create `SessionData` as a shared injectable for reconnect data.

### Files to Create

**`Assets/Scripts/StateMachines/AppManager.cs`**
- `MonoBehaviour`, `[Injectable]`, `DontDestroyOnLoad`
- Fields: `AppStateMachine`, `LobbyStateMachine`, `MatchLifecycleStateMachine` — public, get-only
- `Awake()`: instantiate all four machines, `DontDestroyOnLoad(gameObject)`, call `appStateMachine.Start()`
- `LateUpdate()`: call `SteamAPI.RunCallbacks()`
- No boot logic here — that belongs in `BootingState`

**`Assets/Scripts/StateMachines/SessionData.cs`**
- Plain C# class, `[Injectable]`
- `CSteamID SavedLobbyId { get; set; }`
- `string SavedHostSteamId { get; set; }`
- `void Save()` — writes both values to `PlayerPrefs`
- `void Load()` — reads from `PlayerPrefs` into fields
- `void Clear()` — clears fields and `PlayerPrefs`
- `bool HasSavedSession` — returns true if `SavedLobbyId` is non-zero

**`Assets/Scripts/StateMachines/App/States/BootingState.cs`** (replaces shell from Chunk 1)
- `[Inject] private AppManager appManager` — to call `appStateMachine.ToState<>()`... but wait: states call `owningStateMachine.ToState<>()`. `BootingState` owns `appStateMachine` so this is direct.
- `[Inject] private SessionData sessionData`
- `OnEnter()`:
  - Attempt `SteamAPI.Init()`
  - On success: `sessionData.Load()`, then `owningStateMachine.ToState<MainMenuState>()`
  - On failure: `owningStateMachine.ToState<BootFailedState>()`
- No injection of SteamAPI itself — `SteamAPI.Init()` is a static call

**`Assets/Scripts/UI/BootFailedUI.cs`**
- Plain `MonoBehaviour`
- `[Injectable]`
- `Awake()`: panel hidden by default
- Exposes `void ShowError()` — called by `BootFailedState.OnEnter()`
- Message: "This game requires Steam. Please launch via Steam and restart."

**`Assets/Scripts/StateMachines/App/States/BootFailedState.cs`** (replaces shell from Chunk 1)
- `[Inject] private BootFailedUI bootFailedUI`
- `OnEnter()`: `bootFailedUI.ShowError()`
- `OnExit()`: nothing (terminal state — app must restart)

### Files to Modify

**`Assets/Scripts/Networking/CustomNetworkManager.cs`**
- Remove `SteamAPI.Init()` from `Start()` — `BootingState` owns this now
- Remove `SteamAPI.RunCallbacks()` from `LateUpdate()` — `AppManager` owns this now
- Leave everything else untouched

### Scene Changes
- Add empty GameObject named `AppManager` to the startup scene
- Attach `AppManager` component
- Add `BootFailedUI` canvas (hidden) to the startup scene

### Pre-Implementation Checklist
- [ ] Confirm which scene is the startup scene (check build settings)
- [ ] Confirm `SteamAPI.Init()` in `CustomNetworkManager.Start()` is the only init call — check `SteamManager` prefab (networking.md issue #5)
- [ ] Confirm `SteamAPI.RunCallbacks()` only exists in `CustomNetworkManager.LateUpdate()`

### Verification
1. Launch with Steam running → console logs `[AppStateMachine] Entered MainMenuState`
2. Launch without Steam → console logs `[AppStateMachine] Entered BootFailedState`, error panel visible
3. Full host/join flow still works exactly as before — existing `SteamLobby` / `MenuUIManager` behavior untouched

---

## Chunk 3 — Make Existing Managers Injectable, Wire Inputs into LobbyStateMachine
**Risk: Low. Additive only. Existing behavior completely unchanged.**

### Goal
Make `SteamLobby` and `CustomNetworkManager` injectable. Add C# `Action` events to both that surface Steam callbacks and Mirror lifecycle hooks. Implement the real logic in each LobbyStateMachine state — states inject these managers and subscribe to their events in `OnEnter`, triggering transitions when events fire. `MenuUIManager` is also made injectable but not yet wired to states (that's Chunk 4).

### Files to Modify

**`Assets/Scripts/Networking/SteamLobby.cs`**
Add `[Injectable]` attribute. Add C# events fired from existing Steam callbacks — purely additive, existing callback bodies unchanged:
```csharp
public event Action LobbyCreatedEvent;
public event Action LobbyCreateFailedEvent;
public event Action LobbyEnteredEvent;          // client joined a lobby
public event Action LobbyListReadyEvent;        // RequestLobbyList returned results
public event Action LobbyLeftEvent;
```
Fire each at the appropriate point inside the existing `OnLobbyCreated`, `OnLobbyEntered`, etc. callbacks.

**`Assets/Scripts/Networking/CustomNetworkManager.cs`**
Add `[Injectable]` attribute. Add C# events fired from existing Mirror override hooks — purely additive:
```csharp
public event Action HostStartedEvent;
public event Action HostStoppedEvent;
public event Action ClientStartedEvent;
public event Action ClientStoppedEvent;
public event Action<NetworkConnection> PlayerDisconnectedEvent;
```
Fire each at the top of the relevant existing override (`OnStartHost`, `OnStopHost`, `OnStartClient`, `OnStopClient`, `OnServerDisconnect`).

### LobbyStateMachine States — Real Logic

Replace the shells from Chunk 1 with real implementations. Pattern for every state:

**`IdleState.cs`**
- No injections needed for now (it's the resting state)
- `OnEnter()`: log only
- `OnExit()`: nothing

**`CreatingLobbyState.cs`**
```csharp
[Inject] private SteamLobby steamLobby;

protected override void OnEnter()
{
    steamLobby.LobbyCreatedEvent += OnLobbyCreated;
    steamLobby.LobbyCreateFailedEvent += OnFailed;
    SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
}

protected override void OnExit()
{
    steamLobby.LobbyCreatedEvent -= OnLobbyCreated;
    steamLobby.LobbyCreateFailedEvent -= OnFailed;
}

void OnLobbyCreated() => owningStateMachine.ToState<InLobbyState>();
void OnFailed() => owningStateMachine.ToNextState(); // static → Idle
```

**`BrowsingLobbiesState.cs`**
- `[Inject] private SteamLobby steamLobby`
- `OnEnter()`: subscribe to `LobbyListReadyEvent`, call `SteamMatchmaking.RequestLobbyList()`
- `OnExit()`: unsubscribe
- `OnLobbyListReady()`: transition to wait for user selection (stays in this state — user picks a lobby via UI button)
- Public method `SelectLobby(CSteamID id)` — called by UI button handler, transitions to `JoiningLobbyState`

**`JoiningLobbyState.cs`**
- `[Inject] private SteamLobby steamLobby`
- `OnEnter()`: subscribe to `LobbyEnteredEvent`, call `SteamMatchmaking.JoinLobby(id)`
- `OnExit()`: unsubscribe
- `OnLobbyEntered()` → `ToState<InLobbyState>()`
- On failure → `ToNextState()` (static → Idle)

**`InLobbyState.cs`**
- No network calls here — lobby is joined, waiting for host to start
- `OnEnter()`: log
- Transition to `LoadingGameState` is triggered externally when host presses Start (Chunk 4 wires this)

**`LoadingGameState.cs`**
- `[Inject] private CustomNetworkManager networkManager`
- `OnEnter()`: subscribe to `ClientStartedEvent`, subscribe to `HostStartedEvent`
- `OnExit()`: unsubscribe both
- `OnClientStarted()` or `OnHostStarted()` → `ToState<SessionEstablishedState>()`

**`SessionEstablishedState.cs`**
- `[Inject] private CustomNetworkManager networkManager`
- `OnEnter()`: subscribe to `ClientStoppedEvent`
- `OnExit()`: unsubscribe
- `OnClientStopped()` → `ToState<ReconnectingState>()` (unexpected disconnect)

**`ReconnectingState.cs`**
- `[Inject] private SessionData sessionData`
- `[Inject] private CustomNetworkManager networkManager`
- `OnEnter()`: check `sessionData.HasSavedSession`. If yes: attempt `networkManager.StartClient()`. Subscribe to `ClientStartedEvent` and set a timeout coroutine.
- On success → `ToState<SessionEstablishedState>()`
- On timeout/failure → `ToNextState()` (static → Idle)

### Pre-Implementation Checklist
- [ ] Confirm Steam callback method names in `SteamLobby.cs` — match event names to actual Steamworks callback types
- [ ] Confirm `OnLobbyCreated`, `OnLobbyEntered` etc. are registered callbacks (not just methods)

### Verification
1. Host a lobby → console shows `Idle → CreatingLobby → InLobby`
2. Join a lobby → console shows `Idle → BrowsingLobbies → JoiningLobby → InLobby`
3. Existing `MenuUIManager` / `SteamLobby` behavior completely unchanged — game still fully playable

---

## Chunk 4 — Wire UI: States Call MenuUIManager Directly
**Risk: Medium. First real behavior replacement — MenuUIManager flow changes.**

### Goal
`MenuUIManager` becomes injectable and dumb — it exposes panel show/hide methods and nothing else. States call these methods in `OnEnter()`/`OnExit()`. Remove `MenuUIManager`'s internal panel-switching logic. Remove `ClickedBackToMain()`. Button handlers now call state machine entry points instead of direct network calls.

### MenuUIManager Panel Methods to Expose
Each of these is called by the relevant state's `OnEnter()`/`OnExit()`:
```csharp
public void ShowMainMenu()
public void ShowCreatingLobbySpinner()
public void ShowLobbyList(List<CSteamID> lobbies)
public void ShowJoiningSpinner()
public void ShowLobbyWaitingRoom()
public void ShowLoadingScreen()
public void HideAllLobbyUI()
public void ShowReconnectingPanel()
public void ShowErrorToast(string message)
```

### State → UI Mapping
| State `OnEnter()` calls | State `OnExit()` calls |
|---|---|
| `IdleState` → `ShowMainMenu()` | nothing |
| `CreatingLobbyState` → `ShowCreatingLobbySpinner()` | nothing |
| `BrowsingLobbiesState` → waits for list, then `ShowLobbyList(lobbies)` | nothing |
| `JoiningLobbyState` → `ShowJoiningSpinner()` | nothing |
| `InLobbyState` → `ShowLobbyWaitingRoom()` | nothing |
| `LoadingGameState` → `ShowLoadingScreen()` | nothing |
| `SessionEstablishedState` → `HideAllLobbyUI()` | nothing |
| `ReconnectingState` → `ShowReconnectingPanel()` | nothing |

Each state that needs UI adds `[Inject] private MenuUIManager menuUIManager;` and calls the appropriate method in `OnEnter()`.

### Files to Modify

**`Assets/Scripts/UI/MenuUIManager.cs`**
- Add `[Injectable]` attribute
- Replace all panel-switching internal logic with the public methods listed above
- Remove `ClickedBackToMain()` — `IdleState.OnEnter()` shows main menu now
- Remove direct calls to `SteamLobby.HostLobby()`, `SteamLobby.GetLobbies()`, `SteamLobby.JoinLobby()` from button handlers
- Button handlers now call into the state machine via the LobbyStateMachine directly:
  - Host button → `lobbyStateMachine.ToState<CreatingLobbyState>()` ... but `MenuUIManager` shouldn't know about the state machine internals
  - Better: button handlers call `AppManager` public methods: `AppManager.Instance...` — wait, no static instance. Use `[Inject] private AppManager appManager` and call a method like `appManager.RequestCreateLobby()`
  - `AppManager` exposes thin public methods (`RequestCreateLobby`, `RequestBrowseLobbies`, `RequestJoinLobby`, `RequestLeave`) that call `lobbyStateMachine.ToState<X>()`. This keeps state machine internals out of the UI.

**`Assets/Scripts/Networking/CustomNetworkManager.cs`**
- Remove `menuUIManager.ClickedBackToMain()` call from `OnStopClient` — `IdleState.OnEnter()` handles UI now

### Verification
1. Launch → main menu visible (`IdleState.OnEnter()` called `ShowMainMenu()`)
2. Click Host → spinner shows, waiting room appears on success
3. Click Join → lobby list appears
4. Select lobby → spinner shows, waiting room appears on success
5. Leave lobby → main menu returns via `IdleState.OnEnter()` (not `ClickedBackToMain`)
6. Steam error during create → failure path fires, `IdleState` re-entered, main menu shown

---

## Chunk 5 — Wire Mirror/Steam Calls Through State Machine
**Risk: Medium-High. First chunk where actual connection behavior changes.**

### Goal
States now own the actual `StartHost`, `StartClient`, `LeaveLobby`, `SetLobbyJoinable`, and `PlayerPrefs`/`SessionData` save/clear calls. Remove these calls from `SteamLobby.OnLobbyCreated` and `SteamLobby.OnLobbyEntered`. The lobby stays alive when the game starts (reconnect architecture).

### Key State Changes

**`InLobbyState.cs`** (update)
- `[Inject] private CustomNetworkManager networkManager`
- `[Inject] private SteamLobby steamLobby`
- `[Inject] private SessionData sessionData`
- `OnEnter()` (as host): `networkManager.StartHost()`, store host steam ID in `sessionData`
- `OnEnter()` (as client): `networkManager.StartClient()` with host Steam ID as address
- How to distinguish host vs client: check `SteamMatchmaking.GetLobbyOwner(lobbyId) == SteamUser.GetSteamID()`

**`LoadingGameState.cs`** (update)
- `OnEnter()`: `SteamMatchmaking.SetLobbyJoinable(lobbyId, false)`, `sessionData.Save()`
- The lobby stays alive — do NOT call `LeaveLobby` here

**`IdleState.cs`** (update)
- `[Inject] private CustomNetworkManager networkManager`
- `[Inject] private SteamLobby steamLobby`
- `[Inject] private SessionData sessionData`
- `OnEnter()`: `SteamMatchmaking.LeaveLobby(sessionData.SavedLobbyId)`, `sessionData.Clear()`, `networkManager.StopHost()` or `StopClient()` as appropriate

**`ReconnectingState.cs`** (update — already partially written in Chunk 3)
- `OnEnter()`: `networkManager.StartClient()` with `sessionData.SavedHostSteamId` as address

### Files to Modify

**`Assets/Scripts/Networking/SteamLobby.cs`**
- Remove `networkManager.StartHost()` from `OnLobbyCreated` — `InLobbyState` owns this
- Remove `networkManager.StartClient()` from `OnLobbyEntered` — `InLobbyState` owns this

**`Assets/Scripts/Networking/Matches/MatchManager.cs`** (or `SingelTotemMatchManager.cs`)
- Remove `steamLobby.LeaveLobby()` from `RpcStartGame()` — lobby stays alive for reconnect

### Pre-Implementation Checklist
- [ ] Confirm `MatchManager.RpcStartGame()` calls `steamLobby.LeaveLobby()` — remove it
- [ ] Confirm no other code calls `StartHost` or `StartClient` outside `SteamLobby` and `CustomNetworkManager`
- [ ] Confirm how to distinguish host vs client in `InLobbyState` — test with SteamMatchmaking.GetLobbyOwner

### Verification
1. Full multiplayer test: host on machine A, join on machine B
2. Game starts: lobby set non-joinable, `SessionData` saved to `PlayerPrefs`
3. Client disconnects mid-game: `ReconnectingState` entered, `StartClient` fires with saved Steam ID, reconnects successfully
4. Crash + relaunch: `SessionData.Load()` finds saved lobby, `GetNumLobbyMembers > 0`, `ReconnectingState` entered, reconnects
5. Match ends normally: `LeaveLobby` called, `SessionData` cleared, `IdleState` entered, main menu shown

---

## Chunk 6 — Delete Old Coupling Triangle + Fix ResetManager
**Risk: High. Most dangerous chunk. Isolate carefully.**

### Goal
Remove the `MenuUIManager ↔ SteamLobby ↔ CustomNetworkManager` direct coupling that prior chunks made redundant. Fix the `ResetManager()` multi-fire problem. Replace `OnLevelWasLoaded` (Unity 4 API).

### ResetManager Fix
Current problem: `ResetManager()` called from 4 hooks simultaneously on disconnect.
Fix: Remove all four calls. `IdleState.OnEnter()` is now the single reset point — whatever `ResetManager()` did, `IdleState` initiates it by calling the appropriate manager methods.

### Files to Modify

**`Assets/Scripts/Networking/CustomNetworkManager.cs`**
- Remove `ResetManager()` calls from `OnServerDisconnect`, `OnStopHost`, `OnStopClient`
- Replace `OnLevelWasLoaded(int level)` with `SceneManager.sceneLoaded` subscription in `Start()`
- Remove `menuUIManager.ClickedBackToMain()` (should be gone from Chunk 4)
- Remove Vivox voice chat commented-out blocks
- Remove debug hotkeys if present

**`Assets/Scripts/Networking/SteamLobby.cs`**
- Remove any logic now owned by states (`StartHost`, `StartClient`, `LeaveLobby` calls)
- If nothing meaningful remains after Chunks 3–5, gut to a thin event-emitting class or delete if the component is no longer needed in scene

**`Assets/Scripts/UI/MenuUIManager.cs`**
- Remove any remaining direct references to `SteamLobby` or `CustomNetworkManager`
- UI driven exclusively by state calls to its public methods

### Verification (focus on disconnect scenarios)
1. Host disconnects → `IdleState.OnEnter()` fires once → reset runs once → clients return to main menu
2. Client disconnects mid-game → `ReconnectingState` → single reconnect attempt → on fail: `Idle`, on success: `SessionEstablished`
3. Multiple connect/disconnect cycles — no double-fire of reset behavior
4. Console clean after disconnect — no unexpected errors

---

## Chunk 6.9 — Eliminate FindObjectOfType: Registration Pattern + MatchSceneManager
**Status: 🔲 NEEDS PLANNING — do not implement until a full plan is written and approved.**

### Assignment
This chunk must be fully planned (files to create, files to modify, step-by-step order, verification) before any code is written. Read this section and the context below, then produce a plan for user approval.

### Problem
There are 15 `FindObjectOfType` / `FindAnyObjectByType` call sites across 11 files. These are implicit hidden dependencies — each class hunts for what it needs at runtime rather than being handed it explicitly. This must be eliminated.

### The Two Categories

**Category A — DDOL managers that are already `[Injectable]` but being found manually:**
- `CustomNetworkManager` — found in `MatchNetworkSync`, `Cheats`
- `MatchService` — found in `TeamAndObjectivesUI`, `Cheats`, `SingelTotemMatchService`, `PlayerCombat`, `Totem`, `Camp`
- `MenuManager` — found in `MenuUIManager`

Fix: states inject these managers via `[Inject]` and push the reference into the MonoBehaviour via a public setter when appropriate. MonoBehaviours cannot be injected into (they are not CardboardBehaviour), so states are the wiring layer.

**Category B — Scene-local objects (no `[Injectable]`, many are `NetworkBehaviour`) found by other scene-local objects:**
- `MatchNetworkSync` — found by `NetworkPlayer`, `Cheats`
- `CampManager` — found by `SingelTotemMatchService`, `PlayerCombat`, `Camp`
- `MatchTimer` — found by `SingelTotemMatchService`
- `RandomEventSystem` — found by `Cheats`
- `TeamAndObjectivesUI` — found by `NetworkPlayer`

Fix: the Registration Pattern (see below).

### The Registration Pattern
Already demonstrated in the codebase: `MenuUIManager` (scene-local) registers itself to `MenuManager` (DDOL injectable) on `Awake`. States inject `MenuManager` and receive the scene-local reference through it. This pattern must be generalised to the match scene.

**Three roles:**
1. **DDOL bridge manager** — injectable, always alive across scenes. Holds events (`OnXReady`) and exposes registered instances as properties once they arrive. A new `MatchSceneManager` must be created for this purpose.
2. **Scene-local object** — knows about the DDOL bridge (injects or finds it once on `Awake`, since it's guaranteed to exist). Calls `bridge.RegisterX(this)` on startup.
3. **State** — injects the DDOL bridge, subscribes to its ready events in `OnEnter`, maintains a checklist, and transitions forward only when all expected objects have registered.

**Multi-instance objects (NetworkPlayer, Camp):**
`MatchSceneManager` holds a `List<NetworkPlayer>` and `List<Camp>` etc. — not single references. Each instance registers itself on spawn. States that need to respond to individual player/camp readiness subscribe to a `OnPlayerRegistered(NetworkPlayer)` style event rather than waiting for a fixed count.

### New State: InGameLoadingState
The `AppStateMachine` is currently missing a state between "Mirror has loaded the match scene" and "game is running." Add `InGameLoadingState` between `MainMenuState` and `InGameState`:

```
MainMenuState → InGameLoadingState → InGameState → MainMenuState
```

`InGameLoadingState` subscribes to `MatchSceneManager` ready events and only transitions to `InGameState` once all expected match-scene objects have registered. This is the guarantee that everything is wired before gameplay begins.

### Scope of Files to Audit During Planning
Before writing the plan, read the current state of:
- All 11 files with `FindObjectOfType` calls (listed above)
- `Assets/Scripts/StateMachines/MenuManager.cs` — reference implementation of the DDOL bridge pattern
- `Assets/Scripts/UI/MenuUIManager.cs` — reference implementation of the scene-local registration side
- `Assets/Scripts/StateMachines/App/AppStateMachine.cs` — to understand current transition graph before adding `InGameLoadingState`
- `Assets/Scripts/StateMachines/App/States/InGameState.cs` — to understand what it currently does
- `Assets/Scripts/StateMachines/App/States/MainMenuState.cs` — to understand what triggers the `InGame` transition

### What the Plan Must Cover
1. `MatchSceneManager` — full design: what it holds, what events it exposes, which objects register to it
2. `AppStateMachine` transition graph change — adding `InGameLoadingState`, what it waits for
3. Category A fixes — for each call site, which state injects the manager and assigns it, and when
4. Category B fixes — for each call site, does it move to `MatchSceneManager` registration or can it be wired via `GetComponent` (same-GameObject refs like `PlayerCombat` → components on `NetworkPlayer` don't need a global find at all)
5. `Cheats.cs` — special case: it's a dev tool that finds 4 things. Decide whether to wire it properly or accept a single `Awake` find as a known exception
6. Order of operations — which files to touch in which order so the game stays playable throughout

---

## Chunk 7 — MatchLifecycleStateMachine
**Risk: Medium. Match flow changes but lobby/connection flow is now stable.**

### Goal
Wire `MatchLifecycleStateMachine` into existing match manager code. Collapse the `MatchManager → TTTMatchManager → SingelTotemMatchManager` 3-class chain to 2. Wire `AppStateMachine` `MainMenu ↔ InGame` transitions to match lifecycle.

### Collapse 3-Class Chain
See `architecture.md` — "Match Manager 3-Class Chain Should Be 2":
- Keep `MatchManager` as the base class with `[Injectable]`
- Merge `TTTMatchManager` into `SingelTotemMatchManager`, rename to `SingleTotemMatchManager` (fix typo)
- Delete `TTTMatchManager.cs`
- Update all scene references

### Cross-Machine Coordination
`AppStateMachine` must transition `MainMenu → InGame` when a session is established, and `InGame → MainMenu` when the match ends. Since states drive transitions themselves and there is no `OnStateChanged` event, this is handled as follows:
- `MainMenuState` (on AppStateMachine) injects `CustomNetworkManager`, subscribes to `ClientStartedEvent` and `HostStartedEvent` → `owningStateMachine.ToState<InGameState>()` when fired
- `InGameState` (on AppStateMachine) injects `MatchLifecycleStateMachine` reference via `AppManager`, listens for `GameOverState` entry signal via a `GameOverEvent` on `MatchManager` → `owningStateMachine.ToNextState()` (static → MainMenu)

### Files to Create

**`Assets/Scripts/StateMachines/App/States/MainMenuState.cs`** (replaces shell)
- `[Inject] private CustomNetworkManager networkManager`
- `OnEnter()`: subscribe to `HostStartedEvent` and `ClientStartedEvent`
- `OnExit()`: unsubscribe
- On either event → `owningStateMachine.ToState<InGameState>()`

**`Assets/Scripts/StateMachines/App/States/InGameState.cs`** (replaces shell)
- `[Inject] private MatchManager matchManager`
- `OnEnter()`: subscribe to `matchManager.GameOverEvent`
- `OnExit()`: unsubscribe
- `OnGameOver()` → `owningStateMachine.ToNextState()` (static → MainMenu)

**`Assets/Scripts/StateMachines/Match/States/SetupPhaseState.cs`** (replaces shell)
- `[Inject] private MatchManager matchManager`
- `OnEnter()`: subscribe to `matchManager.MatchStartRequestedEvent` (host presses Start)
- `OnExit()`: unsubscribe
- On event → `owningStateMachine.ToNextState()` (static → Countdown)

**`Assets/Scripts/StateMachines/Match/States/CountdownState.cs`** (replaces shell)
- `[Inject] private MatchManager matchManager`
- `OnEnter()`: call `matchManager.StartCountdown()`, subscribe to `CountdownCompleteEvent`
- `OnExit()`: unsubscribe
- On complete → `owningStateMachine.ToNextState()` (static → InProgress)

**`Assets/Scripts/StateMachines/Match/States/InProgressState.cs`** (replaces shell)
- `[Inject] private SingleTotemMatchManager matchManager`
- `OnEnter()`: subscribe to `WinConditionMetEvent`
- `OnExit()`: unsubscribe
- `OnWinConditionMet()` → `owningStateMachine.ToNextState()` (static → GameOver)

**`Assets/Scripts/StateMachines/Match/States/GameOverState.cs`** (replaces shell)
- `[Inject] private MatchManager matchManager`
- `OnEnter()`: call `matchManager.ShowResults()`, subscribe to `ReturnToMenuEvent` (after delay or player action)
- `OnExit()`: unsubscribe

### Files to Modify

**`Assets/Scripts/Networking/Matches/MatchManager.cs`**
- Add `[Injectable]`
- Add `Action MatchStartRequestedEvent` — fired when host presses Start (replaces `Input.GetKeyDown` debug key)
- Add `Action CountdownCompleteEvent`
- Add `Action GameOverEvent`
- Add `void StartCountdown()` and `void ShowResults()` public methods
- Remove `Input.GetKeyDown(KeyCode.Return)` debug force-start

**`Assets/Scripts/Networking/Matches/SingleTotemMatchManager.cs`** (renamed from SingelTotemMatchManager)
- Add `[Injectable]`
- Add `Action WinConditionMetEvent` — fired when win condition is satisfied
- Replace `Update()` state polling with event-driven win condition detection
- Absorb `TTTMatchManager` content

### Verification
1. Host starts game → `SetupPhase` → host presses start → `Countdown` → `InProgress`
2. Win condition met → `GameOver` → results shown → return to menu
3. `AppStateMachine` correctly transitions `MainMenu → InGame → MainMenu`
4. No errors after match ends and returning to menu

---

## Chunk 8 — PlayerStateMachine
**Risk: Low. Isolated to player components.**

### Goal
Wire `PlayerStateMachine` into `PlayerCombat` for the downed/respawn cycle. Input disabled when `DownedState` is active.

### Files to Modify

**`Assets/Scripts/Player/NetworkPlayer.cs`**
- Create and own the `PlayerStateMachine` instance (local player only, in `Start()` or `Awake()`)
- Call `playerStateMachine.Start()`
- Expose it as a public property for other player components

**`Assets/Scripts/Player/PlayerCombat.cs`**
- Get `PlayerStateMachine` from `NetworkPlayer`
- On HP reaching zero: `playerStateMachine.ToState<DownedState>()`
- On respawn complete: `playerStateMachine.ToState<AliveState>()`
- Remove inline death sequence component-disabling — `DownedState` drives this

**`Assets/Scripts/StateMachines/Player/States/DownedState.cs`** (replaces shell)
- Does NOT use DI — gets its dependencies from `NetworkPlayer` directly (passed in or accessed via component reference)
- `OnEnter()`: disable input, play downed animation, start respawn timer
- On respawn complete → `owningStateMachine.ToState<AliveState>()`

**`Assets/Scripts/StateMachines/Player/States/AliveState.cs`** (replaces shell)
- `OnEnter()`: enable input, restore player visuals
- `OnExit()`: nothing

### Note on PlayerStateMachine and DI
`PlayerStateMachine` is per-player, not global. `[Injectable]` is not appropriate here — there are multiple players. States should get their dependencies from `NetworkPlayer` (which creates the machine) rather than the DI system. Pass required references in via a constructor argument or property on the state before calling `Start()`, or have states access `NetworkPlayer` via `owningStateMachine` if the machine holds a reference.

### Verification
1. Take damage to zero HP → input disabled, downed visual plays
2. Respawn delay completes → input re-enabled
3. Remote players unaffected — state machine is local-only

---

## MCP Usage Guide

Unity MCP gives Claude direct access to the Unity Editor. Use it as follows:

### Scene Verification (before writing code)
Before any chunk that touches scene setup:
- Confirm which components are on which GameObjects
- Verify `AppManager` GameObject exists with the right components
- Check `DontDestroyOnLoad` is working

**Useful for:** Chunks 2, 3, 6

### Console Log Verification (after each chunk)
After implementing a chunk:
- Enter play mode
- Trigger the relevant flow (host, join, disconnect)
- Read console output to verify state transition logs
- Check for unexpected errors

**Useful for:** Every chunk's verification step

### What MCP Cannot Do
- Test multiplayer (requires two running instances)
- Verify Steam callbacks without a real Steam session
- Simulate a network disconnect cleanly

For multiplayer verification (Chunks 5 and 6), test manually with two machines or two Steam accounts.
