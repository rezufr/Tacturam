# Tacturam – Premium Strategy Card Game

## Project Goal
Create a polished Unity card‑game featuring:
- **Fan‑hand layout** for cards in the player's hand.
- **Cinematic play animation** where cards highlight, stay in place, then vanish one‑by‑one.
- **Balatro‑style full‑deck view** showing thumbnails of every card.
- Smooth, tunable animation timings using **DOTween**.

## Core Scripts
| Script | Responsibility |
|--------|----------------|
| `HandLayoutManager.cs` | Manages the arc layout of hand cards. Keeps gaps open while a card is in the *dying* state, collapses hand only after destruction. |
| `CardController.cs` | Handles individual card behaviour. Introduces `IsDying` flag and splits play animation into `PlayShowPhase` (highlight) and `PlayVanishPhase` (shrink & destroy). |
| `GameManager.cs` | Orchestrates the play sequence. Uses `cardShowDuration` for the highlight phase, `playStaggerDelay` for sequential vanish, and guards against overlapping sequences. |
| `DeckViewManager.cs` | Implements the Balatro‑style deck view grid that updates in real‑time as cards are drawn/discarded. |

## Animation Flow
1. **Show Phase** – All selected cards execute `PlayShowPhase()` (pop‑up & slight scale). This gives the player a clear visual of the combo.
2. **Vanish Phase** – After a configurable delay (`cardShowDuration`), each card runs `PlayVanishPhase()` sequentially using `playStaggerDelay`. The hand layout only shifts after the card is fully destroyed (`IsDying`).
3. **Draw Phase** – New cards are drawn once the vanish sequence finishes.

## Key Settings (editable in the Unity Inspector)
- `playStaggerDelay` – Time between each card’s vanish.
- `drawStaggerDelay` – Time between each draw after play.
- `cardShowDuration` – Length of the highlight phase.

## Asset & GUID Management
During a recent merge, Unity `.meta` files contained Git conflict markers, causing GUID clashes (e.g., `GRS_B.asset`). The conflict was resolved by:
- Accepting the remote version for the conflicted assets.
- Manually cleaning the `.meta` files to retain the correct GUID block.
- Committing the fixes and confirming the repository is up‑to‑date.

## Dependencies
- **DOTween** – Used for all tweening/animation. Ensure the package is installed via the Unity Package Manager.

## Getting Started
1. Open the project in Unity (2021+ recommended).
2. Open the **Scene** you wish to test (e.g., `SampleScene.unity`).
3. Press **Play** and use the UI button to trigger the card‑play sequence.
4. Adjust the timing variables in `GameManager` to fine‑tune the feel.

---
*This README provides a quick orientation for anyone (including AI tools) reviewing the codebase to understand the current focus and architecture of the Tacturam project.*
