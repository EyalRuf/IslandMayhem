# Island Mayhem — Project Overview

## What It Is
A 3rd-person multiplayer brawler. Two teams (Natives vs Explorers) fight, collect totem pieces, and complete objectives on an island map. Peer-to-peer multiplayer via Steam lobbies (Steamworks). Networking layer built on Mirror.

## Current State (as of 2026-04-03)
- **Phase 1 (Migration) COMPLETE** — project compiles clean in Unity 6.4 (6000.4.1f1)
- External assets stubbed for Unity 6 (HBAO, FlatKit fog/outline/depth normals — URP API break)
- MCP connected and operational
- Codebase analysis complete — docs filled out below

## What We're Doing Together
Working in phases:

### Phase 1 — Migration ✅ DONE
Got the project compiling and running in Unity 6.4.
- Fixed KCP `out` param error (Mirror transport)
- Fixed `[SerializeField]` on static properties (HBAO)
- Stubbed HBAO URP render pass (URP 17 API break — pending RTHandle port)
- Stubbed FlatKit Fog, Outline, DepthNormals (same RenderTargetHandle issue)
- Fixed `hasAdvancedMode` override removed from URP 17
→ See [migration.md](migration.md)

### Phase 2 — Networking Analysis ✅ DONE
Deep-read all Steam + Mirror related scripts.
- Flow reconstructed — see [networking.md](networking.md)
- Fragile patterns identified
→ See [networking.md](networking.md)

### Phase 2.5 — Codebase Walkthrough ✅ DONE
All 5 layers complete. Full codebase understood. Issues logged in architecture.md throughout.

### Phase 3 — State Machine Design ✅ DONE
- ✅ Three state machines designed: AppStateMachine + LobbyStateMachine + MatchLifecycleStateMachine + PlayerStateMachine
- ✅ All states, transitions, architecture, and design decisions agreed
- ✅ Reconnect architecture: keep lobby alive, set non-joinable on game start, save lobby ID to PlayerPrefs
- ✅ Implementation chunked into 8 gradual, testable steps
→ See [state-machine.md](state-machine.md)
→ See [implementation-plan.md](implementation-plan.md) for chunk-by-chunk implementation guide

### Phase 4 — State Machine Implementation (Next)
Implement the state machines chunk by chunk. Each chunk is self-contained and leaves the game playable.
→ See [implementation-plan.md](implementation-plan.md)

### Phase 5 — Architecture & General Cleanup
Broader codebase analysis and improvement planning.
→ See [architecture.md](architecture.md)

## Sub-Pages
| Page | Purpose |
|------|---------|
| [migration.md](migration.md) | Unity 6.4 migration error tracker |
| [networking.md](networking.md) | Steam P2P + Mirror analysis |
| [state-machine.md](state-machine.md) | Lobby/connection state machine design |
| [architecture.md](architecture.md) | Overall codebase map |
| [scripts.md](scripts.md) | Full script index — every file, its base class, and its purpose |
| [walkthrough.md](walkthrough.md) | Layered codebase walkthrough plan (Layers 1–5) |
| [session-handoff.md](session-handoff.md) | Instructions for the next Claude session — read this if starting a walkthrough |
| [implementation-plan.md](implementation-plan.md) | Chunk-by-chunk state machine implementation guide |
