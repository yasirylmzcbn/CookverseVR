using UnityEngine;
using System.Collections.Generic;

// Handles visualization of navigation paths
public static class NavigationPathVisualizer
{
    private static Color pathColor = new Color(0f, 1f, 0f, 0.8f);
    private static float pathLineWidth = 0.02f; // Much thinner line

    private static LineRenderer pathLineRenderer;
    private static GameObject pathLineObject;

    // Initialize the LineRenderer for path visualization
    private static void InitializePathLineRenderer()
    {
        if (pathLineRenderer != null) return;

        // Create a new GameObject for the line renderer
        pathLineObject = new GameObject("PathLineRenderer");
        pathLineRenderer = pathLineObject.AddComponent<LineRenderer>();

        // Configure the line renderer
        pathLineRenderer.startWidth = pathLineWidth;
        pathLineRenderer.endWidth = pathLineWidth;
        pathLineRenderer.positionCount = 0;
        pathLineRenderer.useWorldSpace = true;

        // Create and assign material
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material lineMaterial = new Material(shader);
        lineMaterial.color = pathColor;
        pathLineRenderer.material = lineMaterial;

        // Set gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(pathColor, 0.0f), new GradientColorKey(pathColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(pathColor.a, 0.0f), new GradientAlphaKey(pathColor.a, 1.0f) }
        );
        pathLineRenderer.colorGradient = gradient;

        pathLineRenderer.enabled = false;
    }

    // Show the path visualization
    public static void ShowPath(List<NodeScript> path)
    {
        InitializePathLineRenderer();

        if (path == null || path.Count == 0)
        {
            HidePath();
            return;
        }

        pathLineRenderer.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
            {
                // Use the exact node position (aligned with node heights)
                pathLineRenderer.SetPosition(i, path[i].transform.position);
            }
        }

        pathLineRenderer.enabled = true;
        Debug.Log($"PathVisualizer: Showing path with {path.Count} nodes");
    }

    // Hide the path visualization
    public static void HidePath()
    {
        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.enabled = false;
            Debug.Log("PathVisualizer: Path hidden");
        }
    }

    // Update the path color
    public static void SetPathColor(Color newColor)
    {
        pathColor = newColor;
        
        if (pathLineRenderer != null && pathLineRenderer.material != null)
        {
            pathLineRenderer.material.color = newColor;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(pathColor, 0.0f), new GradientColorKey(pathColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(pathColor.a, 0.0f), new GradientAlphaKey(pathColor.a, 1.0f) }
            );
            pathLineRenderer.colorGradient = gradient;
        }
    }

    // Update the line width
    public static void SetLineWidth(float newWidth)
    {
        pathLineWidth = newWidth;
        
        if (pathLineRenderer != null)
        {
            pathLineRenderer.startWidth = pathLineWidth;
            pathLineRenderer.endWidth = pathLineWidth;
        }
    }

    // Cleanup method (call when needed)
    public static void Cleanup()
    {
        if (pathLineObject != null)
        {
            Object.Destroy(pathLineObject);
            pathLineObject = null;
            pathLineRenderer = null;
        }
    }
}
