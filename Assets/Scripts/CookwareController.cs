using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CookwareController : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    [SerializeField] private StoveKnob knob;

    private bool isCooking = false;
    [SerializeField] private float heatMultiplier = 1f; // scaled by knob (0-1)

    void Start()
    {
        socket = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnIngredientAdded);
        socket.selectExited.AddListener(OnIngredientRemoved);

        if (knob != null)
            knob.onKnobChanged += OnKnobChanged;
    }

    void OnKnobChanged(bool isOn, float heatLevel)
    {
        isCooking = isOn;
        heatMultiplier = heatLevel; // 0-1 from knob
    }

    void Update()
    {
        if (!isCooking || !socket.hasSelection) return;

        // Cook all socketed ingredients
        foreach (var interactable in socket.interactablesSelected)
        {
            var ingredient = interactable.transform.GetComponent<Ingredient>();
            Debug.Log($"yasir123 Cooking {interactable.transform.name} with heat multiplier {heatMultiplier}");
            if (ingredient != null)
                ingredient.Cook(heatMultiplier);
        }
    }

    void OnIngredientAdded(SelectEnterEventArgs args)
    {
        // Optionally re-check cooking state
        isCooking = knob != null && knob.IsOn;
    }

    void OnIngredientRemoved(SelectExitEventArgs args)
    {
        // Ingredient removed — nothing to reset, state lives on the ingredient
    }

    public GameObject GetSocketedObject()
    {
        if (socket.hasSelection)
            return socket.firstInteractableSelected.transform.gameObject;
        return null;
    }
}