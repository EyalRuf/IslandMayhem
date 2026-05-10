# Session Handoff — Codebase Walkthrough

## Who This Is For
A fresh Claude instance picking up this project for the first time. Read this file first, then OVERVIEW.md, then start the walkthrough session below.

---

## What's Already Done
- Project compiles clean in Unity 6.4 (6000.4.1f1) — all migration errors fixed
- Full codebase analysis complete — all `_claude/` docs are filled out
- A complete script index exists at [scripts.md](scripts.md) with descriptions of every file
- A networking deep-dive exists at [networking.md](networking.md)
- The walkthrough plan (Layers 1–5) exists at [walkthrough.md](walkthrough.md)

**All layers (1–5) are complete. The walkthrough is done.**

Layer 1 covered: Mirror server/client/host model, SyncVar, Command/Rpc, NetworkServer.Spawn, Steam transport. Several issues were discovered and logged in `architecture.md` during Layer 1 — notably punch latency (4 network round trips), position sync bandwidth waste, Steam lobby lifecycle gaps, and item state not fully networked. Do not re-cover these — they are logged and the user is aware.

Layer 4 covered: all player systems. Key issues logged: unified design goal for instant-feel + server authority documented in architecture.md — split effects into cosmetic (immediate, local) vs authoritative state (server-owned), no rollback needed for a party brawler. `PlayerCombat` has the most systemic issues: no server-side hit validation, `CmdPlayerWasHit` trusts client-supplied damage values, SyncVar writes from client code (silent no-ops on guests), `DeathSequence` runs on all clients with non-deterministic respawn position, regen timer should be server-side entirely. Jump has unnecessary Cmd→Rpc round trip before force applies — same fix as combat: predict locally, send Cmd simultaneously. Same round trip latency pattern applies to pickup, drop, throw, and interaction — all fixable with local prediction. `PlayerBuffs` decay coroutine runs independently per client — genuine divergence bug, buff state must be server-owned. Animation speed sync sends unconditionally 50/s — needs threshold. `PlayerInteractionArea` has no server-side validation in `CmdInteractWithArea` and should use `TargetRpc` instead of ClientRpc+isLocalPlayer guard. Per-frame `OverlapSphere` in Update should be replaced with trigger collider `OnTriggerEnter`/`OnTriggerExit` pattern — affects `PlayerInteractionArea`, `PlayerItemInteractions`, `IA_PlayerAmount`, `CampStepButton`, `CampStepPressurePlate`, `CampStepTimerButton`. Camera aim state uses three booleans where a state enum would be cleaner — medium priority given throwing mechanics changes planned.

Layer 3 covered: match start and end flow. Key issues logged: `RpcSyncTeamInfo` and the `teams` list are redundant with `playerTeam` SyncVars — should be removed. 3-class inheritance chain (`MatchManager → TTTMatchManager → SingelTotemMatchManager`) should collapse to 2 — `TTTMatchManager` absorbed into `SingleTotemMatchManager` (fix typo). `[ClientRpc]` inheritance footgun documented. `MatchTimer` has dead code (`startTime`). Win condition double-fire risk — fix with events on `Totem` and `MatchTimer`. `ResetMatch` fires up to 5 times per disconnect — state machine is the fix. Mid-match reconnection flagged as a future feature (keep lobby open, needs Mirror rejoin system).

Layer 2 covered: the full connection and lobby flow from menu click to Mirror handshake complete. Key issues logged: redundant player registry (`playersDic` duplicates `NetworkClient.spawned`), `localPlayerInitialized` flag as a state machine concern, `ResetManager` multi-fire, `OnClientStartStop` null risk, lobby not reopened on disconnect, double-click lobby join, empty `hostAddress` guard needed when friend invites re-enabled, `GameLobbyJoinRequested_t` commented out. The user understands that most fragile patterns trace back to absence of explicit state — the state machine (Phase 3) is the root fix for most of them.

---

## What The User Wants This Session

The user wants to go through the entire codebase together, layer by layer, so they deeply understand how everything works before any refactoring begins. The goal is **their understanding**, not implementation.

Key things to know about how they work:
- They want to understand the *why*, not just the *what*
- They will ask follow-up questions — that's the point, don't rush past it
- If something clicks quickly, move on; if it doesn't, stay on it
- They are comfortable with code but may not know Unity/Mirror/Steam internals deeply
- They pushed back on shallow explanations before — go deep, don't skim

**Do not start any refactoring or code changes during this session.** Walkthrough only.

---

## How to Run the Session

**Start by saying:** "Ready to start Layer 1 — want me to kick off with how Mirror's server/client model works?"

Then work through each layer in [walkthrough.md](walkthrough.md) topic by topic:

1. **Explain the concept in plain language first** — no code dumps. One paragraph max.
2. **Point to where it lives in our actual code** — specific file + what to look at. E.g. "You can see this in `CustomNetworkManager.cs:45` where..."
3. **Pause and invite questions** — don't barrel through. End each topic with something like "Does that track? Any questions before we move on?"
4. **Connect it to the bigger picture** — once a topic clicks, say how it relates to the next one or why it matters for the game.
5. **Log issues immediately** — any bug, design flaw, or improvement identified during a topic gets added to [architecture.md](architecture.md) before moving to the next topic. Do not defer logging to end of layer or wait to be reminded. This is non-negotiable.

After each full layer is complete:
- Mark it DONE in [walkthrough.md](walkthrough.md)
- Ask if the user wants to note anything that confused them into [architecture.md](architecture.md)

---

## Layer Order & What Each One Covers

Full detail in [walkthrough.md](walkthrough.md). Summary:

| Layer | Goal | Key Files |
|-------|------|-----------|
| 1 — Foundations | Mirror + Steam concepts before looking at our code | `CustomNetworkManager.cs`, `SteamLobby.cs` |
| 2 — Connection & Lobby | Step-by-step from menu open to game joined | `SteamLobby.cs`, `CustomNetworkManager.cs`, `MenuUIManager.cs` |
| 3 — Match Flow | From lobby ready to match running and ending | `MatchManager.cs`, `TTTMatchManager.cs`, `SingelTotemMatchManager.cs`, `MatchTimer.cs` |
| 4 — Player Systems | Every component on the player prefab | All of `Player/` |
| 5 — World Systems | Totems, camps, events, items, objectives | `Totem.cs`, `Camp.cs`, `CampManager.cs`, `RandomEventSystem.cs`, etc. |

---

## Important Context to Keep In Mind

- This is a **listen-server** game (host is also a player). `isServer && isClient` is true on host. This creates several subtle bugs.
- Steam ID is used as the Mirror `networkAddress` — not an IP. The transport is Steamworks P2P, not KCP.
- `CustomNetworkManager.GetLocalPlayer()` is the central way to get the local player — used everywhere.
- There are **known bugs** documented in [networking.md](networking.md) — you'll naturally touch on these as they come up in the relevant layer. Don't surface all 18 at once; mention each when it's relevant.
- The `Minigames/` folder is dead code — superseded by `Networking/Matches/`. Mention it briefly in Layer 3 and move on.
- `SingelTotemMatchManager` has a typo in the name (missing 'e'). It is the active match manager in the main scene.

---

## Files to Read Before Starting (if you haven't already)

In order of priority:
1. [OVERVIEW.md](OVERVIEW.md) — project state and phase summary
2. [scripts.md](scripts.md) — what every script does
3. [networking.md](networking.md) — deep Steam + Mirror analysis
4. [walkthrough.md](walkthrough.md) — the layer plan

You don't need to re-read every source file before starting — the `_claude/` docs cover it. Read source files on demand as you walk through topics with the user.
