using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CookwareSlot : MonoBehaviour
{
    [Header("Knob Reference")]
    [Tooltip("The VRKnob that controls heat for this cookware.")]
    [SerializeField] private KitchenKnob heatKnob;

    [Header("Socket Interactors (Ingredient Anchors)")]
    [Tooltip("All XRSocketInteractors on this cookware that ingredients snap into.")]
    [SerializeField] private XRSocketInteractor[] ingredientSockets;

    [Header("Cooking Settings")]
    [Tooltip("Multiplier applied to knob value before passing to ingredient Cook().")]
    [SerializeField] private float heatMultiplier = 1f;

    private void Awake()
    {
        // Auto-find sockets on this GameObject if none assigned
        if (ingredientSockets == null || ingredientSockets.Length == 0)
            ingredientSockets = GetComponentsInChildren<XRSocketInteractor>();
    }

    private void Update()
    {
        if (heatKnob == null) return;

        float heat = heatKnob.CurrentValue * heatMultiplier;

        // Only cook if heat is actually on
        if (heat <= 0f) return;

        foreach (var socket in ingredientSockets)
        {
            Ingredient ingredient = GetIngredientInSocket(socket);
            Debug.Log($"yasir123 Checking socket {socket.gameObject.name} for ingredient. Found: {(ingredient != null ? ingredient.gameObject.name : "None")}");
            if (ingredient != null)
                ingredient.Cook(heat);
        }
    }

    private Ingredient GetIngredientInSocket(XRSocketInteractor socket)
    {
        // GetFirstInteractableSelected returns the snapped object if any
        var interactable = socket.GetOldestInteractableSelected();
        if (interactable == null) return null;

        return ((MonoBehaviour)interactable).GetComponentInChildren<Ingredient>();
    }
}