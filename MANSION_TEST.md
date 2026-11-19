# Mansion dialogue prototype

Open `Assets/Scenes/MansionTest/MansionTest.unity` in Unity 6000.3.19f1 and press Play. Click inside the Game view once if keyboard input is not being received.

## Controls

- WASD / arrow keys: move.
- E near a named character: talk.
- Click, E, Space, or Enter: advance one sentence at a time.
- Number keys or the on-screen buttons: choose a dialogue option.
- J: open or close the journal.
- F: accuse a suspect when accusations become available.
- R: return to the starting position while exploring.

The prototype runs for five days and begins each day with three time points. Introductions and ordinary conversation are free; asking a character for more information costs one point.

## Sample run

1. Advance the hunter's opening thought one sentence at a time.
2. Walk to any character and press E. Their close-up portrait and introduction appear. Each click advances exactly one sentence, then opens their conversation choices.
3. On Day I, **Ask for more information** always produces a random, still-unknown character trait about the speaker or one of their living relationship targets. No werewolf clue is active on Day I.
4. On Days II–V, three randomly chosen living characters initially know that day's clue. The first one questioned reveals it and creates a sparkle. From then on, the other two give random character information instead; clue testimony never repeats.
5. Walk to the sparkle and investigate it. A dismissible discovery card shows the clue image, physical description, and the trait connection before adding it to the journal. It closes by clicking, pressing Continue, or using E, Space, Enter, or Escape.
6. Spend the remaining points or end the day. One randomly selected non-werewolf character disappears overnight. Only living characters are shuffled among the available character spots for the next morning.
7. Repeat through Day V. Clues match the werewolf's trait categories and can be compared against the character information in the journal.
8. Open the accusation menu, select a living suspect, and confirm. The werewolf is randomly selected when the run begins, so the correct suspect changes between playthroughs.

Because the werewolf, character positions, clue-knowers, clue order, sparkle location, character information, and victims are randomized, names and locations in an individual run will differ. Each character always maps to the same five authored traits. The five matching werewolf clues are shuffled for the run, with the first four exposed on Days II–V; Day I intentionally has no werewolf clue.

## Runtime organization

| File/component | Responsibility |
| --- | --- |
| `PrototypeDialogue.json` | Authoring data for all 10 characters: introductions, five daily greetings, traits, facts, clue phrasing, and clue definitions. |
| `MansionPrototype` | The five-day session director. Owns day progression, dialogue, choices, random placement, clue selection, victims, accusation flow, and session-only state. |
| `NPCDialog` | Thin interaction component on each world character. It stores the character ID and forwards E interactions to `MansionPrototype`. |
| `MansionJournalData` | Records character profiles, learned facts, deaths, and collected clues for the current run. |
| `MansionJournal` | Renders the journal UI and clue images. |
| `GameManager` | Supplies the action-point counter and basic game-over/accusation state used by the prototype director. |
| `MansionPrototypeBuilder` | Editor-only scene wiring. It creates the 10 NPC objects, validates reachable placements, assigns portraits/clue art, and rebuilds the navigation surface. |

### Alive and investigation state

`MansionPrototype` uses ID-based sets so scene objects are presentation, not the source of truth:

- `deadIds`: everyone removed by a night. `IsAlive(id)` checks this set, and dead NPC GameObjects are disabled.
- `introduced`: profiles the player has unlocked.
- `learnedFactIds`: character facts already discovered, preventing duplicates.
- Learned character traits are deduplicated globally by `(character, trait)`. Different speakers cannot repeat the same underlying information with alternate wording.
- `clueKnowerIds`: the three living characters selected for the current day.
- `usedClueIds`: clues already selected on earlier days.
- `werewolfId`: the hidden character selected once when the run starts.
- `runClues`: the selected werewolf's five trait clues, shuffled once. Day I has no clue; Days II–V use the first four.
- `currentClue`, `clueRevealed`, and `clueSparkle`: the day's lead and its map-discovery state.

At a day transition, the director chooses a victim other than the werewolf, adds their ID to `deadIds`, disables that NPC, shuffles only the remaining living NPCs across the scene's validated spots, selects that day's clue, and selects up to three living non-werewolf clue-knowers. Restarting selects a new werewolf and clears all run-specific collections.

## Content and assets

- All 10 characters are present: Percy, Shizumi, Rowan, Daisy, Mataba, Sebes, Elara, Pell, Harold, and Marco.
- Normal character artwork is used for dialogue portraits with a close bust crop. Each character has a hand-authored face anchor so differently posed source art remains centered on the face.
- All 10 clue images from the updated main branch are wired into journal clue cards.
- Player-facing relationship labels use **Connected to Animal Side** and **Connected to Human Side** while retaining stable internal matching keys.
- Character profiles begin with no visible traits. Spending an action on character information reveals only the particular trait supported by that spoken line.
- Every character has both a bespoke innocent accusation response and a separate werewolf-reveal response based on the dialogue handoff.
- The happy/sad expression variants, the newer decorative UI images, and `mansion-2.fbx` have been reviewed but are not yet wired into this prototype scene.
- The existing invisible stair ramp is preserved during scene rebuilds. The builder no longer overwrites it with geometry extracted from the imported mansion model.
- Generated character positions exclude the stair footprint, require broad clearance in at least six of eight directions, and stay at least 2.2 world units from the nearest NavMesh boundary. This creates a roughly two-player-width buffer around doorways, narrow corridors, stair approaches, and similar access points during initial placement and daily shuffling.

## Editing and rebuilding

- Dialogue data: `Assets/MansionPrototype/Data/PrototypeDialogue.json`
- Source writing handoff: `SourceAssets/Dialogue/Dialogue Implementation Handoff.md`
- Portraits: `Assets/MansionPrototype/Portraits`
- Clue images: `Assets/MansionPrototype/Clues`
- Scene builder: Unity menu **Tools > Mansion > Build scripted prototype**

The builder intentionally rewrites `MansionTest.unity`; do not run it after hand-adjusting that scene without first saving a copy or updating the builder.

## Current prototype limits

- Night gameplay is not implemented here; advancing the day performs the victim and shuffle logic directly.
- Dialogue and investigation state resets when Play mode restarts and is not saved to disk.
- Expression changes and bespoke character-to-character ambient conversations are future content hooks, not part of this implementation.

## Verification

The Unity editor compiled the scripts and rebuilt the scene successfully. The builder found and validated NavMesh paths from the player to all 10 generated character positions. The data file contains 10 characters, five daily greetings per character, and 10 clue definitions. A full manual five-day balancing pass is still recommended because the randomized sequence can produce many combinations.
