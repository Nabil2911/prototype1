using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlyingEnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrolling, WaitingAtPoint, Chasing, ReturningToPatrol }

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 3f;

    public float patrolWaitTime = 1.5f;

    [Header("Chase")]
    public Transform player;

    public float detectionRange = 10f;

    public float chaseSpeed = 6f;

    public float pathUpdateRate = 0.3f;

    public float chaseStopDistance = 0.5f;

    [Header("Obstacle Layer")]
    public LayerMask obstacleLayer;

    public float enemyRadius = 0.6f;

    [Header("Grid")]
    public float nodeSize = 1f;
    public int gridRadius = 20;

    [Header("General")]
    public float waypointReachedDist = 0.4f;

    private EnemyState currentState = EnemyState.Patrolling;
    private int currentPatrolIndex = 0;
    private List<Vector3> currentPath = new List<Vector3>();
    private int pathIndex = 0;
    private float pathTimer = 0f;
    private bool isWaiting = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        if (patrolPoints.Length > 0)
        {
            currentPatrolIndex = Random.Range(0, patrolPoints.Length);
            RequestPath(patrolPoints[currentPatrolIndex].position);
        }
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        pathTimer += Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrol(distToPlayer);
                break;

            case EnemyState.WaitingAtPoint:
                if (distToPlayer <= detectionRange)
                {
                    StopAllCoroutines();
                    isWaiting = false;
                    currentState = EnemyState.Chasing;
                    ClearPath();
                }
                break;

            case EnemyState.Chasing:
                HandleChase(distToPlayer);
                break;

            case EnemyState.ReturningToPatrol:
                HandleReturn(distToPlayer);
                break;
        }
    }

    void HandlePatrol(float distToPlayer)
    {
        if (distToPlayer <= detectionRange)
        {
            StopAllCoroutines();
            isWaiting = false;
            currentState = EnemyState.Chasing;
            ClearPath();
            return;
        }

        if (patrolPoints.Length == 0) return;

        if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            RequestPath(patrolPoints[currentPatrolIndex].position);

        MoveAlongPath(patrolSpeed);

        if (!isWaiting &&
            Vector2.Distance(transform.position, patrolPoints[currentPatrolIndex].position) <= waypointReachedDist)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        currentState = EnemyState.WaitingAtPoint;
        ClearPath();

        yield return new WaitForSeconds(patrolWaitTime);

        if (patrolPoints.Length > 1)
        {
            int next;
            do { next = Random.Range(0, patrolPoints.Length); }
            while (next == currentPatrolIndex);
            currentPatrolIndex = next;
        }

        isWaiting = false;
        currentState = EnemyState.Patrolling;
        RequestPath(patrolPoints[currentPatrolIndex].position);
    }

    void HandleChase(float distToPlayer)
    {
        if (distToPlayer > detectionRange)
        {
            currentState = EnemyState.ReturningToPatrol;
            currentPatrolIndex = GetNearestPatrolIndex();
            RequestPath(patrolPoints[currentPatrolIndex].position);
            return;
        }

        if (distToPlayer <= chaseStopDistance)
        {
            StopMovement();
            return;
        }

        if (pathTimer >= pathUpdateRate)
        {
            RequestPath(player.position);
            pathTimer = 0f;
        }

        MoveAlongPath(chaseSpeed);
    }

    void HandleReturn(float distToPlayer)
    {
        if (distToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chasing;
            ClearPath();
            return;
        }

        MoveAlongPath(patrolSpeed);

        if (currentPath.Count > 0 && pathIndex >= currentPath.Count)
        {
            currentState = EnemyState.Patrolling;
            ClearPath();
        }
    }

    void MoveAlongPath(float speed)
    {
        if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            StopMovement();
            return;
        }

        Vector3 target = currentPath[pathIndex];
        target.z = transform.position.z;

        Vector2 dir = (Vector2)target - (Vector2)transform.position;
        float dist = dir.magnitude;

        if (dist > 0.01f)
        {
            Vector2 moveDir = dir.normalized;

            if (rb != null)
                rb.linearVelocity = moveDir * speed;
            else
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0f, 0f, angle),
                8f * Time.deltaTime
            );
        }

        if (dist <= waypointReachedDist)
            pathIndex++;
    }

    void StopMovement()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    void ClearPath()
    {
        currentPath.Clear();
        pathIndex = 0;
        StopMovement();
    }

    void RequestPath(Vector3 destination)
    {
        List<Vector3> path = FindPath(transform.position, destination);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            pathIndex = 0;
        }
    }

    List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        Vector3 center = (start + end) * 0.5f;
        float dynamicRadius = Mathf.Max(gridRadius, Vector3.Distance(start, end) * 0.6f);

        int cols = Mathf.CeilToInt((dynamicRadius * 2f) / nodeSize);
        int rows = Mathf.CeilToInt((dynamicRadius * 2f) / nodeSize);
        Vector3 origin = center - new Vector3(dynamicRadius, dynamicRadius, 0f);

        Node[,] grid = new Node[cols, rows];
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 worldPos = origin + new Vector3(x * nodeSize, y * nodeSize, 0f);
                bool blocked = Physics2D.OverlapCircle(
                    new Vector2(worldPos.x, worldPos.y), enemyRadius, obstacleLayer);
                grid[x, y] = new Node(worldPos, x, y, !blocked);
            }
        }

        Node startNode = WorldToNode(grid, cols, rows, origin, start);
        Node endNode   = WorldToNode(grid, cols, rows, origin, end);

        if (startNode == null || endNode == null) return null;

        startNode.walkable = true;

        if (!endNode.walkable) endNode = FindNearestWalkable(grid, cols, rows, endNode);
        if (endNode == null) return null;

        List<Node> openList     = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openList.Add(startNode);

        int safetyCounter = 0;
        const int maxIterations = 5000;

        while (openList.Count > 0 && safetyCounter < maxIterations)
        {
            safetyCounter++;

            Node current = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < current.fCost ||
                   (openList[i].fCost == current.fCost && openList[i].hCost < current.hCost))
                    current = openList[i];
            }

            openList.Remove(current);
            closedSet.Add(current);

            if (current == endNode)
                return RetracePath(startNode, endNode);

            foreach (Node neighbor in GetNeighbors(grid, cols, rows, current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

                int newG = current.gCost + GetDistance(current, neighbor);
                if (newG < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost  = newG;
                    neighbor.hCost  = GetDistance(neighbor, endNode);
                    neighbor.parent = current;
                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        return null;
    }

    Node WorldToNode(Node[,] grid, int cols, int rows, Vector3 origin, Vector3 worldPos)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt((worldPos.x - origin.x) / nodeSize), 0, cols - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((worldPos.y - origin.y) / nodeSize), 0, rows - 1);
        return grid[x, y];
    }

    Node FindNearestWalkable(Node[,] grid, int cols, int rows, Node from)
    {
        Node best = null;
        int bestDist = int.MaxValue;
        for (int x = 0; x < cols; x++)
        for (int y = 0; y < rows; y++)
        {
            if (!grid[x, y].walkable) continue;
            int d = Mathf.Abs(x - from.gridX) + Mathf.Abs(y - from.gridY);
            if (d < bestDist) { bestDist = d; best = grid[x, y]; }
        }
        return best;
    }

    List<Node> GetNeighbors(Node[,] grid, int cols, int rows, Node node)
    {
        List<Node> neighbors = new List<Node>();
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;

            int nx = node.gridX + dx;
            int ny = node.gridY + dy;

            if (nx < 0 || nx >= cols || ny < 0 || ny >= rows) continue;

            if (dx != 0 && dy != 0)
            {
                if (!grid[node.gridX + dx, node.gridY].walkable ||
                    !grid[node.gridX, node.gridY + dy].walkable)
                    continue;
            }

            neighbors.Add(grid[nx, ny]);
        }
        return neighbors;
    }

    List<Vector3> RetracePath(Node start, Node end)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = end;
        while (current != start)
        {
            path.Add(current.worldPos);
            current = current.parent;
        }
        path.Reverse();
        return SimplifyPath(path);
    }

    List<Vector3> SimplifyPath(List<Vector3> path)
    {
        if (path.Count <= 2) return path;

        List<Vector3> simplified = new List<Vector3>();
        Vector3 lastDir = Vector3.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 newDir = (path[i] - path[i - 1]).normalized;
            if (newDir != lastDir)
                simplified.Add(path[i - 1]);
            lastDir = newDir;
        }
        simplified.Add(path[path.Count - 1]);
        return simplified;
    }

    int GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return dx > dy
            ? 14 * dy + 10 * (dx - dy)
            : 14 * dx + 10 * (dy - dx);
    }

    int GetNearestPatrolIndex()
    {
        int nearest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = Vector2.Distance(transform.position, patrolPoints[i].position);
            if (d < minDist) { minDist = d; nearest = i; }
        }
        return nearest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, enemyRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = pathIndex; i < currentPath.Count - 1; i++)
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);

            if (pathIndex < currentPath.Count)
                Gizmos.DrawSphere(currentPath[pathIndex], 0.2f);
        }
    }

    class Node
    {
        public Vector3 worldPos;
        public int gridX, gridY;
        public bool walkable;
        public int gCost, hCost;
        public Node parent;
        public int fCost => gCost + hCost;

        public Node(Vector3 pos, int x, int y, bool walkable)
        {
            worldPos = pos;
            gridX = x;
            gridY = y;
            this.walkable = walkable;
        }
    }
}