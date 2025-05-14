using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Player movement
public class PlayerController : MonoBehaviour
{
    public float moveDelay = 0.05f;  // Delay between grid steps (for grid-based feel)

    public float maxSpeed = 5f;
    private Vector2 input;
    private Animator animator;
    public bool isMoving;

    public LayerMask solidObjectLayer;
    public LayerMask interactablesLayer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Snap to grid at start
        transform.position = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));

    }

    void Update()
    {
        if (isMoving) return;

        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        // Prevent diagonal
        if (input.x != 0) input.y = 0;

        if (input != Vector2.zero)
        {
            animator.SetFloat("moveX", input.x);
            animator.SetFloat("moveY", input.y);

            Vector2 targetPos = (Vector2)transform.position + input;

            if (IsWalkable(targetPos))
            {
                StartCoroutine(Move(targetPos));
            }
        }

        animator.SetBool("isMoving", isMoving);
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;
        // lock z-axis before moving
        targetPos.z = transform.position.z;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, maxSpeed * Time.deltaTime);
            Vector2Int playerGrid = Vector2Int.RoundToInt(transform.position);
            GridManager.Instance.UpdatePlayerPosition(playerGrid);

            yield return null;
        }

        transform.position = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));

        yield return new WaitForSeconds(moveDelay);
        isMoving = false; // Now player can move again immediately


        Debug.Log($"Player step complete at {transform.position}");
        GameManager.Instance.RegisterPlayerStep();  // metrics tracking

        CheckGoalReached();
    }
    private bool IsWalkable(Vector2 targetPos)
    {

        if (GridManager.Instance.walkableGrid == null)
        {
            Debug.LogError("walkableGrid is null!");
            return false;
        }

        Vector2Int grid = Vector2Int.RoundToInt(targetPos);

        if (grid.x < 0 || grid.x >= GridManager.Instance.walkableGrid.GetLength(0) ||
            grid.y < 0 || grid.y >= GridManager.Instance.walkableGrid.GetLength(1))
            return false;

        return GridManager.Instance.walkableGrid[grid.x, grid.y];
    }

    void CheckGoalReached()
    {
        Vector3Int gridPos = GridManager.Instance.goalTilemap.WorldToCell(transform.position);

        if (GridManager.Instance.goalTilemap.HasTile(gridPos))
        {
            GameManager.Instance.EndGame("Player has reached the goal!");
        }
    }
}
