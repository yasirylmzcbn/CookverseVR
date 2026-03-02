using UnityEngine;

// Highlights a target object with an outline visible through walls
public class TargetItemHighlighter : MonoBehaviour
{
    private static GameObject currentHighlightedItem;
    private static Material outlineMaterial;
    private static GameObject outlineObject;

    [Header("Highlight Settings")]
    private static Color highlightColor = new Color(1f, 0.8f, 0f, 1f); // Gold/yellow color
    private static float outlineWidth = 0.05f;

    // Highlight a specific game object
    public static void HighlightItem(GameObject targetItem)
    {
        // Clear previous highlight
        ClearHighlight();

        if (targetItem == null) return;

        currentHighlightedItem = targetItem;

        // Create outline effect using a duplicated mesh with inverted normals
        CreateOutlineEffect(targetItem);
    }

    // Clear the current highlight
    public static void ClearHighlight()
    {
        if (outlineObject != null)
        {
            Object.Destroy(outlineObject);
            outlineObject = null;
        }

        currentHighlightedItem = null;
    }

    // Create an outline effect that's visible through walls
    private static void CreateOutlineEffect(GameObject target)
    {
        // Get all mesh renderers from the target
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();
        MeshFilter[] filters = target.GetComponentsInChildren<MeshFilter>();

        if (renderers.Length == 0 || filters.Length == 0)
        {
            Debug.LogWarning("TargetItemHighlighter: No mesh found on target object");
            return;
        }

        // Create outline material that renders through walls
        if (outlineMaterial == null)
        {
            CreateOutlineMaterial();
        }

        // Create a parent object for all outline meshes
        outlineObject = new GameObject("ItemOutline");
        outlineObject.transform.position = target.transform.position;
        outlineObject.transform.rotation = target.transform.rotation;
        outlineObject.transform.SetParent(target.transform, true);

        // Create outline for each mesh
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null) continue;

            GameObject outlineMesh = new GameObject($"OutlineMesh_{i}");
            outlineMesh.transform.SetParent(outlineObject.transform, false);
            outlineMesh.transform.localPosition = filters[i].transform.localPosition;
            outlineMesh.transform.localRotation = filters[i].transform.localRotation;
            outlineMesh.transform.localScale = filters[i].transform.localScale * (1f + outlineWidth);

            MeshFilter meshFilter = outlineMesh.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = filters[i].sharedMesh;

            MeshRenderer meshRenderer = outlineMesh.AddComponent<MeshRenderer>();
            meshRenderer.material = outlineMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        Debug.Log($"TargetItemHighlighter: Highlighted {target.name}");
    }

    // Create a material that renders through walls (like spectral arrow effect)
    private static void CreateOutlineMaterial()
    {
        // Use a shader that ignores depth (renders through walls)
        Shader outlineShader = Shader.Find("Unlit/Color");
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("Sprites/Default");
        }

        outlineMaterial = new Material(outlineShader);
        outlineMaterial.color = highlightColor;

        // Set render queue to overlay so it renders on top of everything
        outlineMaterial.renderQueue = 3000; // Transparent queue
        
        // Enable transparency
        outlineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always); // Always render, ignore depth
        outlineMaterial.SetInt("_ZWrite", 0); // Don't write to depth buffer
    }

    // Update highlight color
    public static void SetHighlightColor(Color newColor)
    {
        highlightColor = newColor;
        if (outlineMaterial != null)
        {
            outlineMaterial.color = newColor;
        }
    }

    // Update outline width
    public static void SetOutlineWidth(float width)
    {
        outlineWidth = width;
    }

    // Check if an item is currently highlighted
    public static bool IsHighlighted(GameObject item)
    {
        return currentHighlightedItem == item;
    }

    // Get the currently highlighted item
    public static GameObject GetHighlightedItem()
    {
        return currentHighlightedItem;
    }
}
