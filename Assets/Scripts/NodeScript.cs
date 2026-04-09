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
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        if (!allNodes.Contains(this))
        {
            allNodes.Add(this);
        }
        // Cache renderers for performance
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
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

    public void SetNodeVisibility(bool visible)
    {
        // Hide/show all renderers on this node
        if (cachedRenderers == null)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }
        
        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
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
        // Clean up any destroyed nodes first, then return the static list 
        // to prevent extreme lag from FindObjectsOfType every 0.5s
        allNodes.RemoveAll(n => n == null);
        return allNodes;
    }

    // Get the closest node to a specific position (flattened Y check to avoid ceiling/floor confusion)
    public static NodeScript GetClosestNodeToPosition(Vector3 position)
    {
        List<NodeScript> nodes = FindAllNodes();
        
        if (nodes.Count == 0)
        {
            Debug.LogWarning("NodeScript: No nodes found in the scene!");
            return null;
        }

        NodeScript closest = null;
        float closestDistance = float.MaxValue;

        foreach (NodeScript node in nodes)
        {
            if (node == null) continue;

            Vector3 nodePos = node.transform.position;
            nodePos.y = position.y; // Flatten Y to only care about X/Z distance
            float distance = Vector3.Distance(position, nodePos);
            
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
    public static (List<NodeScript> path, GameObject targetItem) FindPathToClosestItemWithTarget(Vector3 playerPosition, ItemType targetItemType, List<GameObject> validItemPool = null, HashSet<GameObject> excludedItems = null)
    {
        // Find all nodes sorted by distance horizontally so height differences don't mess up closest node selection
        List<NodeScript> allNodesDistSorted = FindAllNodes();
        allNodesDistSorted.Sort((a, b) => 
        {
            Vector3 aPos = a.transform.position; aPos.y = playerPosition.y;
            Vector3 bPos = b.transform.position; bPos.y = playerPosition.y;
            return Vector3.Distance(playerPosition, aPos).CompareTo(Vector3.Distance(playerPosition, bPos));
        });

        if (allNodesDistSorted.Count == 0)
        {
            Debug.LogWarning("NodeScript: Could not find a start node near the player!");
            return (new List<NodeScript>(), null);
        }

        // Try the absolute closest node first
        NodeScript startNode = allNodesDistSorted[0];

        // Find all items of the target type
        ShelfItemData[] allItems = FindObjectsOfType<ShelfItemData>();
        List<GameObject> targetItems = new List<GameObject>();

        foreach (ShelfItemData item in allItems)
        {
            if (item.itemType != targetItemType)
            {
                continue;
            }

            // Optional filter: explicitly verify the item belongs to the safely spawned list
            if (validItemPool != null)
            {
                if (!validItemPool.Contains(item.gameObject))
                {
                    continue;
                }
            }

            // Exclude already highlighted items
            if (excludedItems != null && excludedItems.Contains(item.gameObject))
            {
                continue;
            }

            targetItems.Add(item.gameObject);
        }

        Debug.Log($"NodeScript: FindPathToClosestItemWithTarget - Looking for {targetItemType} dynamically, found {targetItems.Count} valid new items.");

        if (targetItems.Count == 0)
        {
            Debug.LogWarning($"NodeScript: No new items of type {targetItemType} found! Retrying without exclusions...");
            // If we ran out, bypass exclusions
            if (excludedItems != null && excludedItems.Count > 0)
            {
                return FindPathToClosestItemWithTarget(playerPosition, targetItemType, validItemPool, null);
            }
            return (new List<NodeScript>(), null);
        }

        // Prefer the nearest item strictly based on horizontal distance to player, completely ignoring nodes to start with
        targetItems.Sort((a, b) =>
        {
            Vector3 aPos = a.transform.position; aPos.y = playerPosition.y;
            Vector3 bPos = b.transform.position; bPos.y = playerPosition.y;
            return Vector3.Distance(playerPosition, aPos).CompareTo(Vector3.Distance(playerPosition, bPos));
        });

        foreach (GameObject item in targetItems)
        {
            // Get the node closest to the specific item
            NodeScript endNode = GetClosestNodeToPosition(item.transform.position);
            if (endNode == null) continue;

            // Find shortest path from the player's physically closest node to the item's closest node
            List<NodeScript> path = FindShortestUnweightedPath(startNode, endNode);
            
            if (path != null && path.Count > 0)
            {
                Debug.Log($"NodeScript: Successfully matched path from {startNode.name} to {endNode.name} for {item.name}");
                return (path, item);
            }
        }

        // Fallback: If the absolute closest node to the player is disconnected, check the next closest nodes
        Debug.LogWarning("NodeScript: Closest node was entirely disconnected. Trying alternative start nodes.");
        foreach (NodeScript alternativeStart in allNodesDistSorted.Take(5))
        {
            foreach (GameObject item in targetItems)
            {
                NodeScript endNode = GetClosestNodeToPosition(item.transform.position);
                if (endNode == null) continue;

                List<NodeScript> path = FindShortestUnweightedPath(alternativeStart, endNode);
                if (path != null && path.Count > 0)
                {
                    return (path, item);
                }
            }
        }

        Debug.LogWarning($"NodeScript: No reachable path found to any item of type {targetItemType}");
        return (new List<NodeScript>(), null);
    }

    // Keep the old method for backward compatibility
    public static List<NodeScript> FindPathToClosestItem(Vector3 playerPosition, ItemType targetItemType, Transform searchRoot = null)
    {
        var result = FindPathToClosestItemWithTarget(playerPosition, targetItemType, null, null);
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
