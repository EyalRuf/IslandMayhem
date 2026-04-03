# Island Mayhem — Lobby/Connection State Machine

## Status
[ ] Pending networking analysis — design happens after we understand current flow

## Goal
Replace the implicit, scattered connection state with an explicit state machine that:
- Makes the full lobby/connection flow easy to read and reason about
- Centralizes state transition logic
- Makes it easy to add/modify behavior at any stage (e.g. error handling, loading screens)

## Proposed States (Draft — to be revised after analysis)

```
IDLE
  └─[host]──→ CREATING_LOBBY
  └─[join]──→ BROWSING_LOBBIES

CREATING_LOBBY
  └─[success]──→ IN_LOBBY (as host)
  └─[fail]──→ IDLE

BROWSING_LOBBIES
  └─[selected]──→ JOINING_LOBBY
  └─[cancel]──→ IDLE

JOINING_LOBBY
  └─[success]──→ IN_LOBBY (as client)
  └─[fail]──→ IDLE

IN_LOBBY
  └─[host starts]──→ LOADING_GAME
  └─[leave]──→ IDLE
  └─[host leaves / kicked]──→ IDLE

LOADING_GAME
  └─[all ready]──→ IN_GAME
  └─[fail/timeout]──→ IDLE

IN_GAME
  └─[game over / leave]──→ POST_GAME or IDLE
  └─[disconnect]──→ IDLE

POST_GAME (optional)
  └─[return to lobby]──→ IN_LOBBY
  └─[quit]──→ IDLE
```

## Design Decisions
_To be made together after networking analysis._

- [ ] Where does the state machine live? (dedicated MonoBehaviour, ScriptableObject, static class?)
- [ ] How does it communicate with Mirror? (events, callbacks, direct calls?)
- [ ] How does it communicate with UI?
- [ ] Error handling strategy per state

## Implementation Notes
_To be filled during implementation phase._
