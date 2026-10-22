# Mansion prototype

Open `Assets/Scenes/MansionTest/MansionTest.unity` in Unity 6000.3.19f1 and press Play. If this scene is already open, reopen it from the Project panel to load the saved changes. Click the Game view for keyboard focus.

## Controls

- WASD / arrow keys: move the red player on the X/Z plane.
- E near a named NPC: begin a conversation.
- Click in the Game view during dialogue, or press E, Space, or Enter: advance one sentence.
- Click a choice or press its number: choose an action.
- F or the Accuse button on day 2: open the suspect selection.
- J or **Journal [J]** at the bottom right: open/close the investigation journal during free exploration. **Esc** or **Close [Esc]** also closes it. Select **Characters** or **Clues** and use the mouse wheel, drag the content, or drag the scrollbar to browse.
- R outside dialogue: return to spawn.

Movement speed is 6.5 units/second. Detailed stair collision is replaced by a smooth invisible ramp and upper landing. The chandelier and exterior tree backdrop are disabled in this scene. Movement pauses while dialogue is open. The camera is orthographic and looks straight down. Geometry between the camera and player temporarily hides to reveal the room below.

## Scripted runthrough

1. Read the hunter's opening thought.
2. Meet whichever guests you want. Introductions are optional, free, and repeat when revisiting a character. All five are in the main hall with a validated walking path from spawn.
3. Speak to Elara whenever you are ready and choose **Interrogate Elara - 1 time point**. The HUD's remaining point count starts at 1 and changes to 0. The interrogation cannot charge twice. Other interrogations are marked unavailable because the supplied script only defines Elara's.
4. Read the interrogation, then explicitly choose **Continue to day 2**. A brief fade to black shows Day II before fading into the next morning. Movement and dialogue input pause during the roughly two-second transition. There is no real-time countdown in this prototype.
5. Inspect **Harold's bag** to unlock accusations. Conversations with the four surviving guests are optional and free; day 2 has zero time points. Harold's marker represents his crime scene rather than a living character.
6. Open Accuse, select a suspect, then confirm or go back. Sebes leads to the script's win ending; Daisy, Percy, or Elara lead to their respective responses and the lose ending. Choose Play again to restart.

## Current Goal

The runtime-created panel below the day/time HUD displays these milestones:

| Story event | Goal |
| --- | --- |
| Start / Play again | Meet the guests (optional). Speak to Elara to investigate. |
| Finish Elara's introduction | Interrogate Elara — costs 1 time point. |
| Finish every interrogation line | Choose “Continue to day 2” to proceed. |
| Finish the day-two fade transition | Find and inspect Harold’s bag. |
| Finish every bag inspection line / unlock accusations | Review your clues, then accuse a suspect [F]. |

`MansionPrototype.RefreshObjective()` chooses the text and visibility from story state. The only additional milestone flag distinguishes a completed interrogation from `Interrogated`, which becomes true as soon as its time point is charged. Goals stay at their current milestone during dialogue and do not regress after repeat conversations or cancelled accusations. The panel hides throughout the date transition and from the ending narration onward, then resets on Play again. Its background and text do not receive mouse raycasts.

## Journal

The journal opens as a panel in the lower-right area, with a title, Characters/Clues tabs, a close button, and a masked vertical scroll view. Both tabs show an explicit empty message before anything is recorded. The entry is hidden and disabled during conversations, choices/accusations, date transitions, and endings, so it cannot cover the existing dialogue or portraits. While the journal is open, movement, E interactions, F / Accuse, and R remain available. The background outside the panel does not intercept clicks. Starting a conversation, accusation, transition, or ending automatically closes the journal and retains the normal story input lock. Only the frames that open/close the journal consume simultaneous world input. Viewing the journal itself does not spend points, advance dates, or change the objective.

- **Characters:** finishing a day-one introduction unlocks that guest's existing portrait, name, and an identity summarized from their introduction in `PrototypeDialogue.json`. Daisy, Sebes, Percy, Elara, and Harold are supported. Unknown guests have no visible profile or portrait. Finishing the day-two opening narration marks an already-known Harold as deceased; it does not reveal a previously unknown profile. Introductions remain optional and repeated conversations do not add duplicates.
- **Clues:** finishing all Elara interrogation lines adds **Attempted Safe Break-in**, labelled **Character testimony**, sourced to Elara on Day I. Finishing all Harold bag-inspection lines adds **Missing Clockmaking Tools**, labelled **Scene discovery**, sourced to Harold's bag on Day II. These summaries reflect the current dialogue, include no suspect verdict, and are each recorded only once. New discoveries show **New clue added to Journal** for three seconds; repeat inspections do not retrigger it.
- **Clue cards:** each clue has a pale cream card with a thin dark-brown border, 14-unit padding and card spacing, an 84 x 84 image frame, and a text column containing a bold title, smaller type/source, and the original wrapped description. Layout groups derive card heights from TMP preferred heights; the existing panel size, fonts, and scroll viewport are retained. The safe clue uses Elara's existing portrait/crop without stretching. The tools clue uses an optional Sprite or a temporary tool-bag icon drawn from UI geometry; missing portraits/unknown clues use a default note icon. Card decoration and images do not receive raycasts, so wheel/content dragging still reaches the scroll view.
- **Reset:** Play again clears profiles, clues, Harold's known status, the notification, open state, and the selected tab. Nothing persists across runs or app restarts.
- **Code:** `MansionJournalData.cs` stores session discoveries; `MansionJournal.cs` owns the lower-right UI, scrolling, notification, and input isolation. `MansionPrototype.cs` creates the journal and forwards completed story events. No scene rebuild is required.

## Editing

- Dialogue: `Assets/MansionPrototype/Data/PrototypeDialogue.json`. Each array item is one sentence/block. Speaker changes are handled by the story director. Each character's `identity` supplies their journal profile, based on their existing introduction.
- Journal tools image: select **Mansion Prototype** in the MansionTest hierarchy, then assign **Journal Tools Icon** on its **MansionPrototype** component. Import replacement art as **Sprite (2D and UI)**. Assign it outside Play mode to save the reference with the scene; reopen the journal or switch tabs to refresh after a runtime assignment. Leaving it empty uses the temporary tool-bag icon. No matching tools/bag image was found among the existing project art.
- Imported portraits: `Assets/MansionPrototype/Portraits`. The original JPGs remain in `Art-assets`; UV framing displays a portrait crop without changing the source images. An aspect-ratio fitter preserves the upper-body crop's natural proportions inside a large portrait frame anchored independently of the compact text box.
- Original script: `SourceAssets/Dialogue/Werewolf Game Dialogue (Prototype).pdf`. This replaces the earlier source revision, including Elara's revised day-two response and Sebes's revised accusation response. The lose narration uses a neutral pronoun for any suspect.
- Styling: `Assets/MansionPrototype/Fonts` contains Crimson Text and its SIL Open Font License, sourced from https://github.com/google/fonts/tree/main/ofl/crimsontext. A prebaked TextMesh Pro atlas avoids runtime font generation. The compact HUD shows the day and a geometric clock icon with the remaining point count, with Current Goal underneath. Goal text uses the project's default TMP font to support its em dash and curly quotes; portrait captions are omitted.
- Scene authoring helper: **Tools > Mansion > Build scripted prototype**. This rebuilds the prototype characters and their starting positions in MansionTest, so save a copy first if you have customized their placement.

The prototype reuses `Player.cs`, `NPCDialog.cs`, and `GameManager.cs`. Optional prototype fields leave the original SampleScene's behavior available. The prototype director uses the existing action counter and accusation result, while pacing day changes after dialogue rather than running the original automatic night timer. Character bodies are colored capsule placeholders; portraits and names identify the scripted characters.

## Verification

Unity compilation and Play mode checks cover all five introductions, portrait assignment, movement locking, the single-point charge, prevention of a second charge, the deliberate day transition, all day-two interactions, evidence gating (optional greetings and testimonies), accusation cancellation, and all four endings. Static navigation checks verify paths from spawn to all five NPC positions. Dialogue and choice layouts were rendered at 1600 x 900 and inspected.

The paragraph above records the existing prototype's verification. For Current Goal, the runtime scripts compiled successfully with Unity 6000.3.19f1's bundled C# compiler and the project's existing assembly references, with output isolated in a temporary directory. Fourteen isolated checks of the actual `RefreshObjective()` method passed using UI stubs for milestone priority, hidden states, and reset. These checks do not run Unity or validate scene interaction/layout. The following manual Play mode checks have not been run as part of this change:

1. Start MansionTest and check the initial goal below the HUD. Speak to any other guest and repeat that conversation: the initial goal must remain. Skip the other introductions and speak directly to Elara: finishing her introduction must show the interrogation goal.
2. Leave Elara's menu, talk to another guest, and return: the goal must not regress. Begin the interrogation: charge exactly one point, retain the current goal until the final line is advanced, then show the Continue goal alongside the Continue button.
3. Choose Continue: hide the entire goal panel throughout the roughly two-second transition, including while the HUD already says Day II. Restore the bag goal when the fade ends. Optional guest conversations and an accusation attempt before finding evidence must preserve this goal.
4. Inspect Harold's bag: keep the bag goal until the last line is advanced, then show the accusation goal. Repeat the inspection and other conversations; try F / Accuse, Go back, and Keep investigating. The accusation goal must remain.
5. Complete a winning or losing accusation: hide the goal during the ending narration and result card. Play again must restore Day I, one point, and the initial goal. Repeat with the other outcome.
6. At 1600 x 900 and a narrower Game view, check wrapping and that the goal stays below the HUD and clear of portraits, dialogue, and buttons. Verify clicking the goal area still advances an open dialogue and that all existing buttons and number shortcuts work.

### Journal manual Play mode checks (not yet run)

The current journal scripts compiled successfully with Unity's bundled C# compiler and existing project references, with output isolated in a temporary directory. Fifty-two data/JSON checks passed when the records were introduced. For the revised behavior that allows exploration while open, seventeen isolated checks of the actual journal methods passed for movement/input availability, automatic closure on story events, opening/closing frame isolation, and reset. The background's click-through configuration was also checked. These checks use engine/UI stubs and do not run Unity, exercise a real EventSystem, or validate rendered layout/scrolling. The following checks still require Play mode:

For the clue-card change, the scripts compiled again successfully. Ten isolated checks of the actual portrait lookup helper covered missing textures/arrays, reordered characters, absent/invalid crop data, and valid crops; thirteen clue checks covered stable image IDs, unchanged descriptions/sources, deduplication, reset, and rediscovery. The layout setup was inspected for automatic height, preserved aspect ratio, and non-intercepting decoration. Long-text rendering, different Game view resolutions, and real scrolling still require the manual checks below.

1. Finish the opening thought, then open Journal by button and by J. Verify both empty tabs, tab selection, Close, J, and Esc. On subsequent frames while open, WASD/arrows must move and R must return to spawn. E near an NPC must start the normal conversation and close the journal immediately. On Day II, F or clicking Accuse outside the journal panel must close it and enter the existing accusation/evidence-gating flow. Viewing notes alone must not consume points or advance the date. Close using J+E/F/R and using the close button while pressing those keys: no action should leak through the closing frame. Repeat J+E/F/R when opening.
2. Complete all five introductions in any order. Check exactly one portrait/name/identity per known guest; partial introductions must not unlock a profile. Repeat an introduction and check it does not duplicate. Scroll the five profiles with wheel, content drag, and scrollbar; check wrapping and clipping at 1600 x 900 and a narrower Game view.
3. Begin Elara's interrogation: the first point charge must not add a clue yet. Advance its final sentence and verify one notification. The journal entry must remain unavailable during the Continue choice. Continue to Day II and finish the opening narration: a known Harold now has deceased status, while the safe clue appears once under Clues with the testimony source.
4. Try accusing before inspecting the bag: existing evidence gating must remain. Partially inspect the bag: no tools clue or accusation unlock yet. Finish the last line: verify the second notification, the tools clue with its scene-discovery source, and the existing accusation unlock/current goal. Repeat the inspection: still only two clues, with no repeat notification.
5. Open the accusation flow, go back, and keep investigating: records and objective must remain unchanged. The journal must be unavailable during suspect selection, confirmation, the date fade, and either ending. Check J plus a dialogue-advance key cannot open the journal or also advance that sentence.
6. Play again after either outcome: both tabs must be empty, Characters selected, Harold status cleared, panel closed, and no stale notification visible. On a separate run, skip everyone except Elara, proceed to Day II, and inspect the bag: both clues must still unlock, only Elara's profile appears, and accusation/win/lose rules still work.
7. In Clues, check separate bordered cards, Elara's existing portrait, the tools placeholder, text order, and unchanged clue wording. Scroll to the bottom of the last card using wheel, scrollbar, and dragging over text/images/borders. Try 1600 x 900, 1280 x 720, and 1024 x 768; temporarily lengthen a card's TMP Description text in Play mode and confirm the card grows, the next card moves down, and all lines remain reachable. Check Characters retains its original layout.
8. Assign a non-square tools Sprite, reopen Clues, and check aspect preservation inside the same 84 x 84 frame. Clear the Sprite and verify the tool-bag placeholder returns. Temporarily clear Elara's portrait reference in Play mode and reopen Clues: a default note icon should appear without an exception. Restore the reference before continuing other portrait-based interactions. After Play again, no clue cards should remain until rediscovered.
