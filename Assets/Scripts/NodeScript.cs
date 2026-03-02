using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Navigation node for pathfinding
public class NodeScript : MonoBehaviour
{
    [Header("Node Connections")]
    [Tooltip("List of nodes this node connects to")]
    public List<NodeScript> connectedNodes = new List<NodeScript>();

    [Header("Visualization")]
    [Tooltip("Color of the connection lines in the editor")]
    public Color gizmoColor = Color.green;
    
    [Tooltip("Show node connections in the scene view")]
    public bool showConnections = true;

    private static List<NodeScript> allNodes = new List<NodeScript>();

    private void Awake()
    {
        if (!allNodes.Contains(this))
        {
            allNodes.Add(this);
        }
    }

    private void OnDestroy()
    {
        allNodes.Remove(this);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Hide nodes by default until challenge starts
        SetNodeVisibility(false);
    }

    private void SetNodeVisibility(bool visible)
    {
        // Hide/show all renderers on this node
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }

    // Static method to show/hide all nodes
    public static void SetAllNodesVisible(bool visible)
    {
        foreach (NodeScript node in allNodes)
        {
            if (node != null)
            {
                node.SetNodeVisibility(visible);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Draw connections between nodes in the Scene view
    private void OnDrawGizmos()
    {
        if (!showConnections || connectedNodes == null) return;

        Gizmos.color = gizmoColor;
        foreach (NodeScript connectedNode in connectedNodes)
        {
            if (connectedNode != null)
            {
                // Draw line to connected node
                Gizmos.DrawLine(transform.position, connectedNode.transform.position);
                
                // Draw a small sphere at the midpoint for better visualization
                Vector3 midpoint = (transform.position + connectedNode.transform.position) / 2f;
                Gizmos.DrawSphere(midpoint, 0.1f);
            }
        }
    }

    // Helper method to check if this node is connected to another
    public bool IsConnectedTo(NodeScript otherNode)
    {
        return connectedNodes.Contains(otherNode);
    }

    // Helper method to add a bidirectional connection
    public void ConnectTo(NodeScript otherNode, bool bidirectional = true)
    {
        if (otherNode == null || otherNode == this) return;

        if (!connectedNodes.Contains(otherNode))
        {
            connectedNodes.Add(otherNode);
        }

        if (bidirectional && !otherNode.connectedNodes.Contains(this))
        {
            otherNode.connectedNodes.Add(this);
        }
    }

    // Helper method to get all connected nodes
    public List<NodeScript> GetConnectedNodes()
    {
        return connectedNodes;
    }

    #region Static Pathfinding Methods

    // Find all nodes in the scene
    public static List<NodeScript> FindAllNodes()
    {
        return FindObjectsOfType<NodeScript>().ToList();
    }

    // Get the closest node to a specific position
    public static NodeScript GetClosestNodeToPosition(Vector3 position)
    {
        List<NodeScript> allNodes = FindAllNodes();
        
        if (allNodes.Count == 0)
        {
            Debug.LogWarning("NodeScript: No nodes found in the scene!");
            return null;
        }

        NodeScript closest = null;
        float closestDistance = float.MaxValue;

        foreach (NodeScript node in allNodes)
        {
            if (node == null) continue;

            float distance = Vector3.Distance(position, node.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = node;
            }
        }

        return closest;
    }

    // Find shortest path between two nodes using BFS (unweighted graph)
    public static List<NodeScript> FindShortestUnweightedPath(NodeScript start, NodeScript end)
    {
        if (start == null || end == null)
        {
            Debug.LogWarning("NodeScript: Start or end node is null!");
            return new List<NodeScript>();
        }

        if (start == end)
        {
            return new List<NodeScript> { start };
        }

        Dictionary<NodeScript, NodeScript> previous = new Dictionary<NodeScript, NodeScript>();
        HashSet<NodeScript> visited = new HashSet<NodeScript>();
        Queue<NodeScript> queue = new Queue<NodeScript>();

        visited.Add(start);
        queue.Enqueue(start);

        bool foundPath = false;

        while (queue.Count > 0)
        {
            NodeScript current = queue.Dequeue();

            if (current == end)
            {
                foundPath = true;
                break;
            }

            foreach (NodeScript neighbor in current.GetConnectedNodes())
            {
                if (neighbor == null || visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);
                previous[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        // Reconstruct path
        List<NodeScript> path = new List<NodeScript>();
        NodeScript currentNode = end;

        if (!foundPath)
        {
            Debug.LogWarning($"NodeScript: No path found from {start.name} to {end.name}");
            return path;
        }

        while (currentNode != null)
        {
            path.Add(currentNode);
            if (currentNode == start)
                break;
            previous.TryGetValue(currentNode, out currentNode);
        }

        path.Reverse();
        return path;
    }

    // Backward-compatible alias
    public static List<NodeScript> FindShortestPath(NodeScript start, NodeScript end)
    {
        return FindShortestUnweightedPath(start, end);
    }

    // Find path from player position to the closest item of a specific type
    // Returns both the path and the target item
    public static (List<NodeScript> path, GameObject targetItem) FindPathToClosestItemWithTarget(Vector3 playerPosition, ItemType targetItemType)
    {
        // Find all items of the target type
        ShelfItemData[] allItems = FindObjectsOfType<ShelfItemData>();
        List<GameObject> targetItems = new List<GameObject>();

        foreach (ShelfItemData item in allItems)
        {
            if (item.itemType == targetItemType)
            {
                targetItems.Add(item.gameObject);
            }
        }

        Debug.Log($"NodeScript: FindPathToClosestItemWithTarget - Looking for {targetItemType}, found {targetItems.Count} items of that type out of {allItems.Length} total items");

        if (targetItems.Count == 0)
        {
            Debug.LogWarning($"NodeScript: No items of type {targetItemType} found in the scene!");
            return (new List<NodeScript>(), null);
        }

        // Find closest target item to player
        GameObject closestItem = null;
        float closestItemDistance = float.MaxValue;

        foreach (GameObject item in targetItems)
        {
            float distance = Vector3.Distance(playerPosition, item.transform.position);
            if (distance < closestItemDistance)
            {
                closestItemDistance = distance;
                closestItem = item;
            }
        }

        if (closestItem == null)
        {
            return (new List<NodeScript>(), null);
        }

        ShelfItemData closestData = closestItem.GetComponent<ShelfItemData>();
        Debug.Log($"NodeScript: Closest {targetItemType} item is {closestData?.itemType} at {closestItem.name} (distance: {closestItemDistance})");

        // Find closest node to player and closest node to target item
        NodeScript startNode = GetClosestNodeToPosition(playerPosition);
        NodeScript endNode = GetClosestNodeToPosition(closestItem.transform.position);

        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("NodeScript: Could not find start or end node!");
            return (new List<NodeScript>(), closestItem);
        }

        Debug.Log($"NodeScript: Finding path from {startNode.name} to {endNode.name} for item type {targetItemType}");

        // Find the shortest path
        return (FindShortestUnweightedPath(startNode, endNode), closestItem);
    }

    // Keep the old method for backward compatibility
    public static List<NodeScript> FindPathToClosestItem(Vector3 playerPosition, ItemType targetItemType)
    {
        var result = FindPathToClosestItemWithTarget(playerPosition, targetItemType);
        return result.path;
    }

    // Find path to a specific item (used when a target item is already cached)
    public static List<NodeScript> FindPathToSpecificItem(Vector3 playerPosition, GameObject targetItem)
    {
        if (targetItem == null)
        {
            Debug.LogWarning("NodeScript: Target item is null!");
            return new List<NodeScript>();
        }

        // Find closest node to player and closest node to target item
        NodeScript startNode = GetClosestNodeToPosition(playerPosition);
        NodeScript endNode = GetClosestNodeToPosition(targetItem.transform.position);

        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("NodeScript: Could not find start or end node for path to specific item!");
            return new List<NodeScript>();
        }

        Debug.Log($"NodeScript: Finding path from {startNode.name} to {endNode.name} for target item {targetItem.name}");

        // Find the shortest path
        return FindShortestUnweightedPath(startNode, endNode);
    }

    #endregion
}
