# Mansion walking and dialogue test

Open `Assets/Scenes/MansionTest/MansionTest.unity` in Unity 6000.3.19f1 and press Play. Click the Game view to give it keyboard focus.

- WASD or arrow keys: move the red player.
- E near the gold caretaker: show dialogue. Walk away to dismiss it.
- R: return to the starting position.

The scene reuses `Player.cs` and `NPCDialog.cs` from the investigation prototype, along with its Canvas and dialogue UI. `Player.walkIn3D` enables X/Z movement, preserves gravity, and enables WASD and respawn. It defaults to false so the original scene retains its X/Y movement. `NPCDialog.interactionPrompt` is optional.

`MansionCamera` follows the player and temporarily hides meshes between the camera and player; their collisions remain active. The FBX is scaled to 15 in this scene, with static mesh colliders and simple URP prototype materials. Original model assets are preserved.

The red player and gold caretaker are placeholder capsules. This is an exploration and dialogue prototype, not a finished level. Detailed stairs and narrow model geometry may still require simplified collision ramps or further level tuning. Source Maya file: `SourceAssets/Mansion/mansion.mb`.

To change the conversation, edit the `Dialog` text under Canvas. To move the NPC, move `Caretaker` (its child `Talk Range` follows). The player spawn is its scene position when Play starts.
