using UnityEngine;
using System.Collections.Generic;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
}
