using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    private float gameStartTime;
    private bool gameEnded = false;
    public Text endGameText;

    // Metrics
    public int playerSteps = 0;
    public int npcSteps = 0;
    public int trailInfluencedMoves = 0;
    public int playerSightings = 0;
    public float exploreTime = 0f;
    public float chaseTime = 0f;

    // metrics logging methods so player and npc can feed data to here

    public bool IsGameEnded() => gameEnded;

    public void RegisterPlayerStep()
    {
        playerSteps++;
        Debug.Log($"Player step counted: {playerSteps}");
    }
    public void RegisterNPCStep()
    {
        npcSteps++;
        Debug.Log($"NPC step counted: {npcSteps}");
    }
    public void RegisterTrailUse()
    {
        trailInfluencedMoves++;
        Debug.Log($"Trail used: {trailInfluencedMoves}");
    }
    public void RegisterSighting()
    {
        playerSightings++;
        Debug.Log($"Sighting registered: {playerSightings}");
    }

    public void AddExploreTime(float delta) => exploreTime += delta;
    public void AddChaseTime(float delta) => chaseTime += delta;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // persists across scenes 
        gameStartTime = Time.time;
    }

    public void EndGame(string result)
    {
        if (gameEnded) return;

        gameEnded = true;
        float duration = Time.time - gameStartTime;

        // Console Output
        Debug.Log("========== GAME METRICS ==========");
        Debug.Log($"Result: {result}");
        Debug.Log($"Time Elapsed: {duration:F2} seconds");
        Debug.Log($"Player Moves: {playerSteps}");
        Debug.Log($"NPC Moves: {npcSteps}");
        Debug.Log($"Trail-Influenced Moves: {trailInfluencedMoves}");
        Debug.Log($"Player Sightings by NPC: {playerSightings}");
        Debug.Log($"Explore Time: {exploreTime:F2}s");
        Debug.Log($"Chase Time: {chaseTime:F2}s");
        Debug.Log("==================================");

        //  In-Game UI
        if (endGameText != null)
        {
            endGameText.text =
                $"Result: {result}\n" +
                $"Time: {duration:F2}s\n" +
                $"Player Steps: {playerSteps}\n" +
                $"NPC Steps: {npcSteps}\n" +
                $"Trail Uses: {trailInfluencedMoves}\n" +
                $"Sightings: {playerSightings}\n" +
                $"Explore: {exploreTime:F2}s | Chase: {chaseTime:F2}s";

        }

        // Save to File
        string logLine = $"[{System.DateTime.Now}] " +
                         $"Result: {result}, " +
                         $"Time: {duration:F2}s, " +
                         $"PlayerSteps: {playerSteps}, " +   
                         $"NPCSteps: {npcSteps}, " +         
                         $"TrailUses: {trailInfluencedMoves}, " +
                         $"Sightings: {playerSightings}, " +
                         $"ExploreTime: {exploreTime:F2}s, " +
                         $"ChaseTime: {chaseTime:F2}s\n";

        string path = Application.dataPath + "/game_metrics.txt";
        System.IO.File.AppendAllText(path, logLine);

        // Freeze game
        Time.timeScale = 0f;
    }
}
