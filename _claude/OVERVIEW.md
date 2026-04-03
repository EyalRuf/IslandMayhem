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

### Phase 3 — State Machine Design (Next)
Design a proper explicit state machine for the lobby/connection flow before touching code.
- Define states and transitions together
- Agree on design before implementation
→ See [state-machine.md](state-machine.md)

### Phase 4 — Architecture & General Cleanup
Broader codebase analysis and improvement planning.
→ See [architecture.md](architecture.md)

## Sub-Pages
| Page | Purpose |
|------|---------|
| [migration.md](migration.md) | Unity 6.4 migration error tracker |
| [networking.md](networking.md) | Steam P2P + Mirror analysis |
| [state-machine.md](state-machine.md) | Lobby/connection state machine design |
| [architecture.md](architecture.md) | Overall codebase map |
