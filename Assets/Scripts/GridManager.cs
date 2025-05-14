using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;

public class GridManager : MonoBehaviour
{

    public static GridManager Instance;

    public int width = 20;
    public int height = 20;
    public float tileSize = 1f;
    public LayerMask wallLayer;

    public bool[,] walkableGrid;
    public Vector2Int gridOffset;
    //public Vector2 originOffset = Vector2.zero;
    public Tilemap wallTilemap;
    public Tilemap goalTilemap;
    public Tilemap backgroundTilemap;

    public static Queue<Vector2Int> PlayerTrail = new Queue<Vector2Int>();
    public static int maxTrailLength = 6;

    public bool showDebugLines = false;
    public bool showGridGizmos = true;

    public Transform npc;

    //#if UNITY_EDITOR
    //if (Application.isPlaying && debugMode) { /* draw gizmos */ }
    //#endif


    void Awake()
    {
        Instance = this;
        Debug.Log("GridManager Awake Called");
        if (wallTilemap == null)
            wallTilemap = transform.Find("SolidObjects")?.GetComponent<Tilemap>();

        if (backgroundTilemap == null)
            backgroundTilemap = transform.Find("Background")?.GetComponent<Tilemap>();

        if (goalTilemap == null)
            goalTilemap = transform.Find("Goal")?.GetComponent<Tilemap>();

        GenerateGrid();
    }

    public void GenerateGrid()
    {

        if (wallTilemap == null)
        {
            Debug.LogError("No Tilemap assigned to GridManager.");
            return;
        }

        BoundsInt bounds = wallTilemap.cellBounds;
        walkableGrid = new bool[bounds.size.x, bounds.size.y];
        gridOffset = new Vector2Int(bounds.xMin, bounds.yMin);

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cell = new Vector3Int(bounds.x + x, bounds.y + y, 0);
                Vector3 worldPos = wallTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0);

                bool hasWall = Physics2D.OverlapPoint(worldPos, wallLayer) != null;
                bool hasFloor = backgroundTilemap.HasTile(cell); // Check floor presence

                walkableGrid[x, y] = hasFloor && !hasWall;

                if (showDebugLines)
                {
                    // Debug grid
                    Debug.DrawLine(worldPos - Vector3.one * 0.4f, worldPos + Vector3.one * 0.4f, walkableGrid[x, y] ? Color.green : Color.red, 10f);
                }
            }
        }

        //Debug.Log($"Grid generated: {bounds.size.x}x{bounds.size.y}");
        Debug.Log($"walkableGrid created: {walkableGrid.GetLength(0)}x{walkableGrid.GetLength(1)}");

    }


    public static Vector2Int PlayerGridPos { get; private set; }

    public void UpdatePlayerPosition(Vector2Int pos)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameEnded())
            return;

        PlayerGridPos = pos;

        if (pos.x < 0 || pos.x >= walkableGrid.GetLength(0) ||
            pos.y < 0 || pos.y >= walkableGrid.GetLength(1))
            return; // Don't track invalid tiles

        // Clamp Player Trail to Max Length
        if (!PlayerTrail.Contains(pos))
        {
            PlayerTrail.Enqueue(pos);
            Debug.Log($"Added to PlayerTrail: {pos}, Trail Count: {PlayerTrail.Count}");
        }

        if (PlayerTrail.Count > 10)
            PlayerTrail.Dequeue();
    }

    //void OnDrawGizmos()
    //{
    //    if (walkableGrid == null) return;

    //    for (int x = 0; x < walkableGrid.GetLength(0); x++)
    //    {
    //        for (int y = 0; y < walkableGrid.GetLength(1); y++)
    //        {
    //            Vector3 pos = new Vector3(x, y, 0);

    //            Gizmos.color = walkableGrid[x, y] ? Color.green : Color.red;
    //            Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
    //        }
    //    }
    //}

    void OnDrawGizmos()
    {
        if (!showGridGizmos || wallTilemap == null || walkableGrid == null)
            return;

        BoundsInt bounds = wallTilemap.cellBounds;

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cell = new Vector3Int(bounds.x + x, bounds.y + y, 0);
                Vector3 pos = wallTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0);

                if (x >= walkableGrid.GetLength(0) || y >= walkableGrid.GetLength(1)) continue;

                bool isWalkable = walkableGrid[x, y];
                Gizmos.color = isWalkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);


            }
        }

        var npcGO = GameObject.FindGameObjectWithTag("NPC");
        npc = npcGO.transform;

        if (npc != null)
        {
            Vector2Int npcGrid = Vector2Int.RoundToInt(npc.transform.position);
            Vector3 npcGizmoPos = wallTilemap.CellToWorld((Vector3Int)npcGrid) + new Vector3(0.5f, 0.5f, 0);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(npcGizmoPos, Vector3.one * 1.1f);
        }

        Gizmos.color = Color.cyan;
        foreach (var p in PlayerTrail)
        {
            Vector3 pos = new Vector3(p.x + 0.5f, p.y + 0.5f, 0);
            Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);
        }
    }


    public bool IsWalkable(Vector2Int gridPos)
    {
        int gx = gridPos.x - gridOffset.x;
        int gy = gridPos.y - gridOffset.y;

        if (gx < 0 || gy < 0 || gx >= walkableGrid.GetLength(0) || gy >= walkableGrid.GetLength(1))
            return false;

        return walkableGrid[gx, gy];
    }

    void OnDrawGizmosSelected()
    {
        if (wallTilemap == null || walkableGrid == null) return;

        BoundsInt bounds = wallTilemap.cellBounds;

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cell = new Vector3Int(bounds.x + x, bounds.y + y, 0);
                Vector3 pos = wallTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0);

                if (x >= walkableGrid.GetLength(0) || y >= walkableGrid.GetLength(1)) continue;

                bool isWalkable = walkableGrid[x, y];
                Gizmos.color = isWalkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
            }
        }
    }


    [ContextMenu("Regenerate Walkable Grid")]
    void EditorRegenerateGrid()
    {
        GenerateGrid();
    }

}
