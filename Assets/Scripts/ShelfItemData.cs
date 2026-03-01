using UnityEngine;

public class ShelfItemData : MonoBehaviour
{
    [Header("Item Identity")]
    public ItemType itemType;

    [Header("Optional UI Override")]
    [Tooltip("If left empty, enum name will be formatted automatically.")]
    public string displayName;

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName))
            return displayName;

        // Auto-format enum name into readable text
        return FormatEnumName(itemType.ToString());
    }

    private string FormatEnumName(string rawName)
    {
        // Remove category prefix like "Food_"
        if (rawName.Contains("_"))
        {
            rawName = rawName.Substring(rawName.IndexOf("_") + 1);
        }

        // Add spaces before capital letters
        rawName = System.Text.RegularExpressions.Regex.Replace(rawName, "(\\B[A-Z])", " $1");

        return rawName;
    }
}

public enum ItemType
{
    Food_Apple,
    Food_Cheese,
    Food_Tomato,
    Food_Bread,
    Food_SearedGroundBeef
}