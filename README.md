# 2D Maze Game – Unity Project

This repository contains the full Unity project for a 2D top-down maze game featuring AI pathfinding, player tracking, and adaptive behaviour via Monte Carlo heuristics.

---

## Project Structure

- `Assets/Scenes/GameScene.unity` – Main scene to run the game
- `Assets/Scripts/` – Contains the player controller, NPC logic, Monte Carlo system, and supporting utilities
- `Assets/Prefabs/` – Includes maze tiles, NPC prefab, and player prefab
- `ProjectSettings/` – Unity-specific configuration
- `.gitignore` – Ensures unnecessary files (e.g., `Library/`, `.vs/`) are not tracked

---

## ▶How to Run the Game

1. **Open the Unity project**  
   Launch the Unity Editor and open this folder via `File → Open Project`.

2. **Load the game scene**  
   Navigate to:  Assets/Scenes/GameScene.unity

Open this scene before entering Play Mode.

3. **Set up the environment manually**  
- Drag the **Player** and **NPC** prefabs into the **bottom section of the maze grid**.
- Ensure both are positioned on valid walkable tiles at game start.

4. **Adjust lighting for gameplay**  
- Set the **Global Light 2D (Global Light)** intensity to `0` (completely dark).
- Ensure the **Player** GameObject has a **2D Spotlight** (or Point Light) child attached for visibility.
- This spotlight acts as the player's field of vision and creates the intended darkness mechanic.

5. **Press Play**  
You can now test and play the game.  
- The **NPC will begin exploring** using a Monte Carlo heuristic system.
- If it **spots or detects the player**, it will enter **chase mode** with predictive movement.

---

##  Notes for Developers

- The project uses a custom Monte Carlo decision engine instead of A* or FSM for AI movement.
- All pathfinding logic is done per-frame using context-sensitive scoring in `RunMonteCarloGrid()`.
- Lighting requires Unity’s **2D Renderer Pipeline** with **2D Lights enabled**.

---

##  Tips

- For best visual results, ensure your URP settings are configured for 2D lighting.
- You can adjust the `stepsBeforeRecalculate`, `memoryDuration`, or `scanCooldown` variables in the NPC script to tune difficulty.

---

## Requirements

- Unity 2021.3 LTS or newer
- 2D URP package (for 2D lighting)

---

## License

This project is for educational or non-commercial use unless otherwise stated. Attribution required if code or logic is reused.

---


