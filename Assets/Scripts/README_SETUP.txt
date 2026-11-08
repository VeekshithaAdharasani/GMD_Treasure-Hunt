TREASURE HUNT - THE LOST CAVE (Unity scripts)

INCLUDED:
- PlayerMovement.cs: auto-forward movement and A/D or Left/Right lane switching.
- CameraFollow.cs: follows the player.
- GameManager.cs: score, health, win/game-over panels, restart.
- Collectible.cs: blue/gold crystal or coin collection.
- Obstacle.cs: damage when the player collides with an obstacle.
- TreasureChest.cs: wins when the player reaches the chest.

INSTALL:
1. Stop Play mode.
2. Copy all .cs files into your project's Assets/Scripts folder (create Scripts if needed).
3. Wait for Unity to compile; fix any red Console errors before continuing.

PLAYER:
1. Attach PlayerMovement.cs to the same Player root GameObject that has Character Controller.
2. Remove/disable duplicate PlayerMovement scripts on child objects.
3. Set Player near the cave start and near floor level.
4. Movement uses the new Input System (Keyboard.current); Active Input Handling can be Input System Package or Both.

CAMERA:
1. Attach CameraFollow.cs to Main Camera.
2. Drag Player into CameraFollow.Target.
3. Start offset suggestion: X=0, Y=4, Z=-7. Adjust to suit your cave.

GAME MANAGER:
1. Create an empty GameObject named GameManager and attach GameManager.cs.
2. Optional: create TextMeshPro UI texts and assign Score Text and Health Text.
3. Optional: create Win/Game Over panels and assign them.
4. Restart button OnClick can call GameManager.RestartGame.

COLLECTIBLES:
1. Add a collider to each crystal/coin and enable Is Trigger. If its existing collider is needed for something else, add a separate Sphere Collider set to Is Trigger.
2. Attach Collectible.cs.
3. Set Treasure Type and Points: BlueCrystal=10, GoldCrystal=50, Coin=5.
4. Player root needs Character Controller.

OBSTACLES:
1. Add a collider to each obstacle and attach Obstacle.cs. For a simple setup, enable Is Trigger.
2. Place it in the player's path.

TREASURE CHEST:
1. Add a Box Collider to the chest and enable Is Trigger.
2. Attach TreasureChest.cs and place the chest at the end of the cave.

SAVE:
- Save scene with Ctrl+S.
- Add the scene to Build Profiles/Scene List if you need restart to reload it.

IMPORTANT:
This is a gameplay starter kit, not an automatically completed scene. Scripts still need to be attached, references assigned, and colliders configured. Lane movement assumes the cave runs along world +Z and lanes use world X = -laneDistance, 0, +laneDistance. Adjust if your path is rotated or offset.
