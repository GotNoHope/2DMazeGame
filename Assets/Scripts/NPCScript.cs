using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class NPCMonteCarlo : MonoBehaviour
{
    public Transform player;
    public float maxSpeed = 3.5f;
    public float acceleration = 8f;
    public int stepsBeforeRecalculate = 25;

    private Vector2 targetPosition;
    private int stepsCommitted = 0;
    private Vector2 moveDirection;
    private Vector2Int currentGridPos;
    private Vector2Int? lastSeenPlayerGrid = null;
    //private string lastAction = "none";
    public int visionRadius = 6;
    private Vector2Int lastMoveDir = Vector2Int.zero;

    // Player info
    private Vector2Int lastPlayerGridPos;
    private Vector2Int playerDirection;
    private float memoryDuration = 2f; // how long to remember after losing sight
    private float timeSinceLastSeen = 0f;

    private bool canChase = false;
    private bool isMoving = false;

    private SpriteRenderer spriteRenderer;
    private float scanCooldown = 0f;
    public float scanDelay = 2f;
    private int stepsTowardLastSeen = 0;
    public int maxStepsToLastSeen = 10;

    // Metrics 
    float exploreTime = 0f;
    float chaseTime = 0f;

    enum NPCMode { Explore, Search, Chase }
    private NPCMode currentMode = NPCMode.Explore;

    private int exploreStepsTaken = 0;
    public int exploreStepsBeforeSearch = 5;

    private List<Vector2Int> possibleMoves = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    private HashSet<Vector2Int> visitedTiles = new HashSet<Vector2Int>();

    public LayerMask solidObjectLayer;
    public LayerMask playerLayer;

    void Start()
    {
        Debug.Log("NPCMonteCarlo Start() running");

        transform.position = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = Color.gray;
        //player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(StartChaseAfterDelay(3f));

        if (GridManager.Instance == null)
        {
            Debug.LogError("NPCMonteCarlo: GridManager not found in scene!");
            enabled = false; // disable to avoid further crashes
            return;
        }

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogError("Player object not found!");
            enabled = false;
            return;
        }

        player = playerGO.transform;

    }

    IEnumerator StartChaseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canChase = true;
        spriteRenderer.color = Color.white;
    }

    void Update()
    {
        float delta = Time.deltaTime;

        if (!canChase || isMoving) return;

        currentGridPos = Vector2Int.RoundToInt(transform.position);


        Vector2Int currentPlayerGrid = GridManager.PlayerGridPos;
        // Update player movement direction
        playerDirection = currentPlayerGrid - lastPlayerGridPos;
        lastPlayerGridPos = currentPlayerGrid;


        switch (currentMode)
        {
            //case NPCMode.Search: HandleSearch(); break;
            case NPCMode.Chase: HandleChase(); GameManager.Instance.AddChaseTime(delta); break;
            case NPCMode.Explore: HandleExplore(); GameManager.Instance.AddExploreTime(delta); break;
        }

        // Game end condition check (victory/defeat)
        if (currentGridPos == GridManager.PlayerGridPos)
        {
            GameManager.Instance.EndGame("defeat");
            Debug.Log($"Explore Time: {exploreTime:F2}s | Chase Time: {chaseTime:F2}s");
            return;
        }
    }


    void HandleChase()
    {
        spriteRenderer.color = Color.blue;
        stepsCommitted = 0; // Chase always overrides previous commitments

        if (lastSeenPlayerGrid == null || currentGridPos == lastSeenPlayerGrid.Value)
        {
            if (CanSeePlayer())
            {
                Debug.Log("Player still visible — continuing chase!");
                lastSeenPlayerGrid = GridManager.PlayerGridPos;
                timeSinceLastSeen = 0f; // RESET here
                return;
            }
            Debug.Log("Lost player — switching to search.");
            lastSeenPlayerGrid = null;
            currentMode = NPCMode.Explore;
            exploreStepsTaken = 0;
            scanCooldown = scanDelay;
            stepsCommitted = 0;
            return;
        }

        stepsTowardLastSeen++;
        if (stepsTowardLastSeen > maxStepsToLastSeen)
        {
            lastSeenPlayerGrid = null;
            currentMode = NPCMode.Explore;
            exploreStepsTaken = 0;
            scanCooldown = scanDelay;
            stepsTowardLastSeen = 0;
            return;
        }

        timeSinceLastSeen += Time.deltaTime;
        if (timeSinceLastSeen > memoryDuration)
        {
            lastSeenPlayerGrid = null;
            currentMode = NPCMode.Explore;
            exploreStepsTaken = 0;
            scanCooldown = scanDelay;
            timeSinceLastSeen = 0;
            Debug.Log("Memory expired — giving up.");
            return;
        }

        moveDirection = RunMonteCarloGrid("chase");
        Vector2Int nextPos = currentGridPos + Vector2Int.RoundToInt(moveDirection);
        if (IsWalkable(nextPos)) { 
            StartCoroutine(MoveTo(nextPos));
            Debug.Log($"NPC decided to move {moveDirection} to {nextPos}");
        }

    }
    
    void HandleExplore()
    {
        spriteRenderer.color = Color.yellow;
        //Debug.Log($"HandleExplore called. stepsCommitted: {stepsCommitted}, isMoving: {isMoving}");

        // Immediately check if player is visible 
        if (CanSeePlayer())
        {
            lastSeenPlayerGrid = GridManager.PlayerGridPos;
            currentMode = NPCMode.Chase;
            timeSinceLastSeen = 0f;
            stepsCommitted = 0;
            Debug.Log("Player spotted during exploration! Switching to Chase mode.");
            return; // important to stop exploring this frame
        }

        if (Vector2Int.Distance(currentGridPos, GridManager.PlayerGridPos) <= 2)
        {
            lastSeenPlayerGrid = GridManager.PlayerGridPos;
            currentMode = NPCMode.Chase;
            timeSinceLastSeen = 0f;
            stepsCommitted = 0;
            Debug.Log("🔊 Player detected by proximity! Switching to Chase mode.");
            return;
        }

        if (!isMoving)
        {
            // Decide what to do: move
            moveDirection = RunMonteCarloGrid("explore");

            Vector2Int nextPos = currentGridPos + Vector2Int.RoundToInt(moveDirection);

            if (IsWalkable(nextPos))
            {
                stepsCommitted = stepsBeforeRecalculate;
                StartCoroutine(MoveTo(nextPos));
                Debug.Log($"Exploration: Next move {moveDirection}, Target: {nextPos}, Walkable: {IsWalkable(nextPos)}");
            }
            else
            {
                Debug.LogWarning($"NPC exploration blocked at {nextPos}.");
                // No movement started if blocked, but still count as exploration attempt
            }

            // Always after trying a move (whether successful or blocked)
            exploreStepsTaken++;
            stepsCommitted = Mathf.Max(0, stepsCommitted - 1); // Decrement safely

            // If all committed steps are used up, replan next moves naturally
            if (stepsCommitted <= 0)
            {
                Debug.Log("All committed steps finished. Waiting to replan exploration.");
                // No scan or search — will naturally trigger next RunMonteCarloGrid
            }

            // Force exploration refresh if too many exploration steps taken
            if (exploreStepsTaken >= exploreStepsBeforeSearch)
            {
                Debug.Log("Too many exploration steps taken, resetting exploration behavior.");
                exploreStepsTaken = 0; // Just reset counter, continue exploring normally
                                       // No scanning or searching
            }
        }
    }

    IEnumerator MoveTo(Vector2Int targetGridPos)
    {
        isMoving = true;
        Vector2 targetPos = new Vector2(targetGridPos.x, targetGridPos.y);

        while (Vector2.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, maxSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        visitedTiles.Add(targetGridPos);
        isMoving = false;

        stepsCommitted = Mathf.Max(0, stepsCommitted - 1);

        // metrics
        GameManager.Instance.RegisterNPCStep();
        Debug.Log($"Actually moving to {targetGridPos}");
        //Debug.Log($"Move finished. Steps remaining: {stepsCommitted}");

    }

    Vector2Int RunMonteCarloGrid(string mode)
    {
        Vector2Int bestMove = Vector2Int.zero;
        float bestScore = float.MaxValue;
        string bestAction = "move";

        // All movement directions
        List<string> options = new List<string> { "up", "down", "left", "right" };

        foreach (string option in options)
        {
            // temporary logs
            //Debug.Log($"Best action: {bestAction}, Move: {bestMove}");

            Vector2Int simPos = currentGridPos;
            float score = 0f;

            Vector2Int move = DirectionFromString(option);

            // Skip exact reverse moves unless we're chasing
            if (mode != "chase" && move == -lastMoveDir)
                continue;

            simPos = currentGridPos + move;

            if (!IsWalkable(simPos)) continue;

            if (mode == "chase" && lastSeenPlayerGrid != null)
            {
                Vector2Int predictedPlayerPos = PredictPlayerPosition();

                // if blocked by wall, avoid scoring
                if (IsBlockedByWall(currentGridPos, simPos))
                {
                    score = float.MaxValue; // Never pick this move
                }
                else
                {
                    // Distance to predicted player position
                    float dist = Vector2Int.Distance(simPos, predictedPlayerPos);
                    score = dist;

                    // Slight boost if aligned with player direction
                    if (((Vector2)(predictedPlayerPos - simPos)).normalized == (Vector2)move)
                        score -= 1f;
                }
            }
            else if (mode == "explore")
            {
                //bool visited = visitedTiles.Contains(simPos);
                //score += visited ? 5f : -10f;

                // Penalize previously visited tiles
                if (visitedTiles.Contains(simPos))
                    score += 8f;

                if (visitedTiles.Contains(simPos) && simPos == currentGridPos)
                    score += 50f; // Major penalty if staying in place

                // Favour open areas
                int openNeighbors = CountOpenNeighbors(simPos);
                score -= openNeighbors * 1.5f;

                // Direction variation to avoid loops
                Vector2Int dir = move;
                if (dir == Vector2Int.up) score -= 1f;
                if (dir == Vector2Int.right) score -= 0.2f;
                if (move == lastMoveDir) score += 10;

                float desperationMultiplier = (exploreStepsTaken >= exploreStepsBeforeSearch) ? 2.0f : 1.0f; // Boost when desperate

                // Prioritize tiles near the player's recent trail (only if available)
                if (GridManager.PlayerTrail != null)
                {
                    // putting it into an array makes recent steps much more desirable to the NPC than older ones
                    Vector2Int[] trailArray = GridManager.PlayerTrail.ToArray();

                    for (int i = 0; i < trailArray.Length; i++)
                    {
                        Vector2Int trailPos = trailArray[i];

                        Debug.DrawLine((Vector2)simPos + new Vector2(0.5f, 0.5f), (Vector2)trailPos + new Vector2(0.5f, 0.5f), Color.cyan, 0.2f);

                        float dist = Vector2Int.Distance(simPos, trailPos);

                        if (dist < 15f)
                        {
                            float freshnessWeight = 1f + ((float)(trailArray.Length - i) / trailArray.Length); // recent = higher weight
                            score -= desperationMultiplier * freshnessWeight * (6f / (dist + 0.5f));

                            // Trail count 
                            GameManager.Instance.RegisterTrailUse();
                            break; // Count only once per move option
                        }
                    }
                }
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestMove = move;
                bestAction = option;
            }
        }

        //lastAction = bestAction;
        lastMoveDir = bestMove;

        Debug.Log($"Best action: {bestAction}, Move: {bestMove}");

        return bestMove;
    }


    Vector2Int DirectionFromString(string dir)
    {
        switch (dir)
        {
            case "up": return Vector2Int.up;
            case "down": return Vector2Int.down;
            case "left": return Vector2Int.left;
            case "right": return Vector2Int.right;
            default:
                Debug.LogWarning($"DirectionFromString got invalid input: {dir}");
                return Vector2Int.zero;

        };
    }


    int CountOpenNeighbors(Vector2Int pos)
    {
        int count = 0;
        foreach (var dir in possibleMoves)
        {
            if (IsWalkable(pos + dir))
                count++;
        }
        return count;
    }


    bool IsWalkable(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= GridManager.Instance.walkableGrid.GetLength(0) ||
            gridPos.y < 0 || gridPos.y >= GridManager.Instance.walkableGrid.GetLength(1))
            return false;

        return GridManager.Instance.walkableGrid[gridPos.x, gridPos.y];
    }

    //bool IsWalkable(Vector2Int gridPos)
    //{
    //    var offset = GridManager.Instance.gridOffset;
    //    int gx = gridPos.x - offset.x;
    //    int gy = gridPos.y - offset.y;

    //    if (gx < 0 || gx >= GridManager.Instance.walkableGrid.GetLength(0) ||
    //        gy < 0 || gy >= GridManager.Instance.walkableGrid.GetLength(1))
    //        return false;

    //    return GridManager.Instance.walkableGrid[gx, gy];
    //}


    bool CanSeePlayer()
    {

        int rayCount = 180;
        float angleStep = 360f / rayCount;
        Vector2 rayStart = (Vector2)transform.position + new Vector2(0.5f, 0.5f);
        Vector2Int playerGrid = GridManager.PlayerGridPos;

        // Fallback: direct proximity
        if (Vector2Int.Distance(currentGridPos, playerGrid) <= 1)
        {
            GameManager.Instance.RegisterSighting();
            return true;
        }
        // Log amount of sightings on player
        //if (CanSeePlayer())
        //{
        //    GameManager.Instance.RegisterSighting();
        //}

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, dir, visionRadius, playerLayer | solidObjectLayer);



            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                // Debug to see what raycast hits
                if (hit.collider != null)
                {
                    Debug.Log($"Raycast hit: {hit.collider.name}");
                }

                if (hit.collider.CompareTag("Player"))
                { 
                    Debug.DrawRay(rayStart, dir * visionRadius, Color.green, 0.2f); // Hit player
                    GameManager.Instance.RegisterSighting(); // metrics
                    return true;
                }

                if (((1 << hit.collider.gameObject.layer) & solidObjectLayer) != 0)
                {
                    Debug.DrawRay(rayStart, dir * visionRadius, Color.red, 0.2f); // Hit wall
                    break;
                }
            }
        }

        return false;
    }

    bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < GridManager.Instance.walkableGrid.GetLength(0) &&
               pos.y >= 0 && pos.y < GridManager.Instance.walkableGrid.GetLength(1);
    }

    bool IsBlockedByWall(Vector2Int from, Vector2Int to)
    {
        Vector2 direction = ((Vector2)(to - from)).normalized;
        float distance = Vector2.Distance(from, to);

        Vector2 rayStart = (Vector2)from + new Vector2(0.5f, 0.5f);
        Debug.DrawRay(rayStart, direction * distance, Color.magenta, 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, direction, distance, solidObjectLayer);
        return hit.collider != null;
    }

    Vector2Int PredictPlayerPosition()
    {
        if (GridManager.PlayerTrail.Count == 0)
            return lastSeenPlayerGrid ?? currentGridPos;

        Vector2Int[] trail = GridManager.PlayerTrail.ToArray();

        // Use last known + direction if possible
        if (trail.Length >= 2)
        {
            Vector2Int last = trail[trail.Length - 1];
            Vector2Int beforeLast = trail[trail.Length - 2];
            Vector2Int inferredDir = last - beforeLast;
            Vector2Int projected = last + inferredDir;

            if (IsWalkable(projected))
                return projected;
        }

        return trail[trail.Length - 1];
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canChase = false;
            isMoving = false;
            spriteRenderer.color = Color.red;
            StopAllCoroutines();
            other.GetComponent<PlayerController>().enabled = false;

            GameManager.Instance.EndGame("NPC has caught the player!");
        }
    }
}
