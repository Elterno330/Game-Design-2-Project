# Mansion prototype

Open `Assets/Scenes/MansionTest/MansionTest.unity` in Unity 6000.3.19f1 and press Play. If this scene is already open, reopen it from the Project panel to load the saved changes. Click the Game view for keyboard focus.

## Controls

- WASD / arrow keys: move the red player on the X/Z plane.
- E near a named NPC: begin a conversation.
- Click in the Game view during dialogue, or press E, Space, or Enter: advance one sentence.
- Click a choice or press its number: choose an action.
- F or the Accuse button on day 2: open the suspect selection.
- R outside dialogue: return to spawn.

Movement speed is 6.5 units/second. Detailed stair collision is replaced by a smooth invisible ramp and upper landing. The chandelier and exterior tree backdrop are disabled in this scene. Movement pauses while dialogue is open. The camera is orthographic and looks straight down. Geometry between the camera and player temporarily hides to reveal the room below.

## Scripted runthrough

1. Read the hunter's opening thought.
2. Meet whichever guests you want. Introductions are optional, free, and repeat when revisiting a character. All five are in the main hall with a validated walking path from spawn.
3. Speak to Elara whenever you are ready and choose **Interrogate Elara - 1 time point**. The HUD starts at 1/1 and changes to 0/1. The interrogation cannot charge twice. Other interrogations are marked unavailable because the supplied script only defines Elara's.
4. Read the interrogation, then explicitly choose **Continue to day 2**. A brief fade to black shows Day II before fading into the next morning. Movement and dialogue input pause during the roughly two-second transition. There is no real-time countdown in this prototype.
5. Inspect **Harold's bag** to unlock accusations. Conversations with the four surviving guests are optional and free; day 2 has zero time points. Harold's marker represents his crime scene rather than a living character.
6. Open Accuse, select a suspect, then confirm or go back. Sebes leads to the script's win ending; Daisy, Percy, or Elara lead to their respective responses and the lose ending. Choose Play again to restart.

## Editing

- Dialogue: `Assets/MansionPrototype/Data/PrototypeDialogue.json`. Each array item is one sentence/block. Speaker changes are handled by the story director.
- Imported portraits: `Assets/MansionPrototype/Portraits`. The original JPGs remain in `Art-assets`; UV framing displays a portrait crop without changing the source images. An aspect-ratio fitter preserves the upper-body crop's natural proportions inside a large portrait frame anchored independently of the compact text box.
- Original script: `SourceAssets/Dialogue/Werewolf Game Dialogue (Prototype).pdf`. This replaces the earlier source revision, including Elara's revised day-two response and Sebes's revised accusation response. The lose narration uses a neutral pronoun for any suspect.
- Styling: `Assets/MansionPrototype/Fonts` contains Crimson Text and its SIL Open Font License, sourced from https://github.com/google/fonts/tree/main/ofl/crimsontext. A prebaked TextMesh Pro atlas avoids runtime font generation. The compact HUD shows the day and a geometric clock icon with the remaining point count; header titles, objective instructions, and portrait captions are omitted.
- Scene authoring helper: **Tools > Mansion > Build scripted prototype**. This rebuilds the prototype characters and their starting positions in MansionTest, so save a copy first if you have customized their placement.

The prototype reuses `Player.cs`, `NPCDialog.cs`, and `GameManager.cs`. Optional prototype fields leave the original SampleScene's behavior available. The prototype director uses the existing action counter and accusation result, while pacing day changes after dialogue rather than running the original automatic night timer. Character bodies are colored capsule placeholders; portraits and names identify the scripted characters.

## Verification

Unity compilation and Play mode checks cover all five introductions, portrait assignment, movement locking, the single-point charge, prevention of a second charge, the deliberate day transition, all day-two interactions, evidence gating (optional greetings and testimonies), accusation cancellation, and all four endings. Static navigation checks verify paths from spawn to all five NPC positions. Dialogue and choice layouts were rendered at 1600 x 900 and inspected.
