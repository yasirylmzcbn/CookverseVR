using UnityEngine;

// Highlights a target object with an outline visible through walls
public class TargetItemHighlighter : MonoBehaviour
{
    private static GameObject currentHighlightedItem;
    private static Material outlineMaterial;
    private static GameObject outlineObject;

    [Header("Highlight Settings")]
    private static Color highlightColor = new Color(0f, 133f / 255f, 1f, 0.84f); // Same blue as nav path
    private static float outlineWidth = 0.008f;

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
            if (Application.isPlaying)
            {
                Object.Destroy(outlineObject);
            }
            else
            {
                Object.DestroyImmediate(outlineObject);
            }
            outlineObject = null;
        }

        currentHighlightedItem = null;
    }

    // Create an outline effect that's visible through walls
    private static void CreateOutlineEffect(GameObject target)
    {
        MeshFilter[] filters = target.GetComponentsInChildren<MeshFilter>(true);
        SkinnedMeshRenderer[] skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        if (filters.Length == 0 && skinnedRenderers.Length == 0)
        {
            Debug.LogWarning($"TargetItemHighlighter: No mesh found on target object {target.name}");
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
        outlineObject.transform.localScale = Vector3.one;
        outlineObject.transform.SetParent(target.transform, true);

        int outlineIndex = 0;

        // Create outlines for MeshFilter meshes
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null) continue;
            if (IsGeneratedOutlineTransform(filters[i].transform)) continue;
            MeshRenderer sourceRenderer = filters[i].GetComponent<MeshRenderer>();
            if (sourceRenderer == null || !sourceRenderer.enabled) continue;

            CreateOutlineMesh(filters[i].sharedMesh, filters[i].transform, outlineIndex++);
        }

        // Create outlines for SkinnedMeshRenderer meshes
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            if (skinnedRenderers[i].sharedMesh == null || !skinnedRenderers[i].enabled) continue;
            if (IsGeneratedOutlineTransform(skinnedRenderers[i].transform)) continue;
            CreateOutlineMesh(skinnedRenderers[i].sharedMesh, skinnedRenderers[i].transform, outlineIndex++);
        }

        if (outlineIndex == 0)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(outlineObject);
            }
            else
            {
                Object.DestroyImmediate(outlineObject);
            }
            outlineObject = null;
            Debug.LogWarning($"TargetItemHighlighter: Mesh components found but nothing eligible to outline on {target.name}");
            return;
        }

        Debug.Log($"TargetItemHighlighter: Highlighted {target.name}");
    }

    private static void CreateOutlineMesh(Mesh sourceMesh, Transform sourceTransform, int index)
    {
        GameObject outlineMesh = new GameObject($"OutlineMesh_{index}");
        outlineMesh.transform.SetParent(outlineObject.transform, true);

        // Use world transform so nested hierarchies align correctly
        outlineMesh.transform.position = sourceTransform.position;
        outlineMesh.transform.rotation = sourceTransform.rotation;

        Vector3 parentLossyScale = outlineObject.transform.lossyScale;
        Vector3 sourceLossyScale = sourceTransform.lossyScale;
        Vector3 relativeScale = new Vector3(
            parentLossyScale.x != 0f ? sourceLossyScale.x / parentLossyScale.x : sourceLossyScale.x,
            parentLossyScale.y != 0f ? sourceLossyScale.y / parentLossyScale.y : sourceLossyScale.y,
            parentLossyScale.z != 0f ? sourceLossyScale.z / parentLossyScale.z : sourceLossyScale.z
        );
        outlineMesh.transform.localScale = relativeScale * (1f + outlineWidth);

        MeshFilter meshFilter = outlineMesh.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = sourceMesh;

        MeshRenderer meshRenderer = outlineMesh.AddComponent<MeshRenderer>();
        meshRenderer.material = outlineMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private static bool IsGeneratedOutlineTransform(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "ItemOutline" || current.name.StartsWith("OutlineMesh_"))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    // Create a material that renders through walls (like spectral arrow effect)
    private static void CreateOutlineMaterial()
    {
        // Use a shader that supports depth overrides for through-wall rendering
        Shader outlineShader = Shader.Find("Hidden/Internal-Colored");
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("Unlit/Color");
        }
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("Sprites/Default");
        }

        outlineMaterial = new Material(outlineShader);
        outlineMaterial.color = highlightColor;

        // Render as overlay so it is visible through walls
        outlineMaterial.renderQueue = 4000;
        
        // Enable transparency
        outlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        outlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        outlineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        outlineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        outlineMaterial.SetInt("_ZWrite", 0);
        outlineMaterial.EnableKeyword("_ALPHABLEND_ON");
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
