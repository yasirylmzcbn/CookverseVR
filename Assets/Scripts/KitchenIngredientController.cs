using Cookverse.Assets.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

public class KitchenIngredientController : MonoBehaviour
{
    [Header("Ingredient")]
    [SerializeField] private Ingredient ingredientType;

    public Ingredient IngredientType => ingredientType;
    public bool IsProteinIngredient => ingredientType == Ingredient.DraculaWing
                                     || ingredientType == Ingredient.MerewolfSteak
                                     || ingredientType == Ingredient.ManticoreTail;
    public bool IsVegetableIngredient => !IsProteinIngredient;

    [Header("Forms")]
    [SerializeField] private GameObject rawForm;
    [SerializeField] private GameObject choppedForm;
    [SerializeField] private GameObject cookedForm;

    [Header("Dragging")]
    [Tooltip("Fixed kitchen camera (assign in Inspector).")]
    public Camera kitchenCamera;

    [Tooltip("What layers can be interacted with (ingredients + kitchen items).")]
    public LayerMask interactLayers = ~0;

    [Header("Drag Surface")]
    public LayerMask dragSurfaceLayers = ~0;
    public float surfaceRayDistance = 1000f;
    public bool useFixedDragY = false;
    public float fixedDragY = 0.9f;
    public float hoverYOffset = 0.02f;

    [SerializeField] private Rigidbody rb;

    [Header("Snapping Assist")]
    [Tooltip("Bigger = easier to 'hit' kitchen items with the cursor (sphere cast).")]
    public float snapSphereCastRadius = 0.25f;

    [Tooltip("How far we search for nearby slots while dragging (performance).")]
    public float slotSearchRadius = 2.0f;

    [Header("Drag Visuals")]
    [Tooltip("If true, while dragging and within snap range of a slot, hide this ingredient's own renderers (slot ghost preview remains visible).")]
    public bool hideDraggedVisualsInSnapRange = true;

    private readonly Collider[] _snapOverlapBuffer = new Collider[32];
    private readonly RaycastHit[] _clickHitBuffer = new RaycastHit[32];

    private bool isDragging;
    private Vector3 dragOffset;
    private float lockedY;

    private IngredientSlotBehaviour currentSlot;
    private IngredientSlotBehaviour hoverSlot;

    public float cookLevel = 0f; // 0 = raw, 1 = cooked

    // Remembers where the ingredient was last sitting freely (counter/table/etc.)
    private struct FreeState
    {
        public bool hasValue;
        public Transform parent;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public bool rbKinematic;
        public bool rbUseGravity;
        public RigidbodyConstraints rbConstraints;
    }

    private FreeState freeState;

    private void Awake()
    {
        if (rb == null) TryGetComponent(out rb);
        SaveFreeState();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryBeginDrag();

        if (isDragging)
            DragMove();

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
            EndDragAndTryPlace();
    }

    private void TryBeginDrag()
    {
        if (kitchenCamera == null) return;

        if (!IsTopmostIngredientUnderPointer())
            return;


        if (currentSlot != null)
        {
            if (!currentSlot.CanRemoveIngredient())
                return;

            currentSlot.RemoveIngredient(this);
            currentSlot = null;

            BeginDragInternal();
            return;
        }

        BeginDragInternal();
    }

    private bool IsTopmostIngredientUnderPointer()
    {
        if (kitchenCamera == null || Mouse.current == null) return false;

        Ray ray = kitchenCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        KitchenIngredientController bestIngredient = null;
        float bestDistance = float.PositiveInfinity;

        int hitCount = Physics.RaycastNonAlloc(ray, _clickHitBuffer, 500f, interactLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _clickHitBuffer[i].collider;
            if (col == null) continue;

            KitchenIngredientController ingredient = col.GetComponentInParent<KitchenIngredientController>();
            if (ingredient == null) continue;

            float d = _clickHitBuffer[i].distance;
            if (d < bestDistance)
            {
                bestDistance = d;
                bestIngredient = ingredient;
            }
        }

        // Fallback to a small sphere cast to be more forgiving for tiny colliders.
        if (bestIngredient == null)
        {
            hitCount = Physics.SphereCastNonAlloc(ray, snapSphereCastRadius, _clickHitBuffer, 500f, interactLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _clickHitBuffer[i].collider;
                if (col == null) continue;

                KitchenIngredientController ingredient = col.GetComponentInParent<KitchenIngredientController>();
                if (ingredient == null) continue;

                float d = _clickHitBuffer[i].distance;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    bestIngredient = ingredient;
                }
            }
        }

        return bestIngredient == this;
    }

    private void BeginDragInternal()
    {
        isDragging = true;

        lockedY = useFixedDragY ? fixedDragY : transform.position.y;

        if (TryGetPointerWorldPoint(out Vector3 pointerWorld))
        {
            // dragOffset = transform.position - pointerWorld;
        }
        else
            dragOffset = Vector3.zero;

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // transform.SetParent(null, true);
        SetHoverSlot(null);

    }

    private void DragMove()
    {
        if (!TryGetPointerWorldPoint(out Vector3 pointerWorld))
            return;

        Vector3 newPos = pointerWorld + dragOffset;
        newPos.y = lockedY + hoverYOffset;
        transform.position = newPos;

        UpdateHoverPreview();
    }

    private void UpdateHoverPreview()
    {
        // Prefer slot under cursor (for intent), else nearest in range
        IngredientSlotBehaviour candidate = SphereCastSlotUnderMouse();

        if (candidate == null)
            candidate = FindNearestAcceptingSlotInRange(transform.position);

        SetHoverSlot(candidate);
        UpdateHoverSlotPreviewVisibility();
        UpdateDraggedVisualsVisibility();
    }

    private void UpdateHoverSlotPreviewVisibility()
    {
        if (hoverSlot == null) return;

        bool inRange = hoverSlot.IsWithinSnapRange(transform.position);
        bool canAccept = hoverSlot.CanAcceptIngredient(this);

        if (hoverSlot is not CookwareSlot cookwareSlot)
            return;

        if (inRange && canAccept)
            cookwareSlot.ShowPreviewFor(this);
        else
            cookwareSlot.HidePreview();
    }

    private void UpdateDraggedVisualsVisibility()
    {
        if (!isDragging)
        {
            return;
        }

        if (!hideDraggedVisualsInSnapRange)
        {
            return;
        }

        bool shouldHide = hoverSlot != null
                          && hoverSlot.IsWithinSnapRange(transform.position)
                          && hoverSlot.CanAcceptIngredient(this);
    }

    private void EndDragAndTryPlace()
    {
        isDragging = false;

        // Try place into hovered slot if in its snap range
        if (hoverSlot != null && hoverSlot.IsWithinSnapRange(transform.position) && hoverSlot.TryPlaceIngredient(this))
        {
            currentSlot = hoverSlot;
            SetHoverSlot(null);
            return;
        }

        // Not placed: keep where dropped
        SetHoverSlot(null);

        if (rb != null)
            rb.isKinematic = false;

        SaveFreeState();
    }

    private void SetHoverSlot(IngredientSlotBehaviour slot)
    {
        if (hoverSlot == slot) return;
        if (hoverSlot is CookwareSlot previousCookwareSlot)
            previousCookwareSlot.HidePreview();

        hoverSlot = slot;
    }

    private IngredientSlotBehaviour SphereCastSlotUnderMouse()
    {
        if (kitchenCamera == null) return null;

        Ray ray = kitchenCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.SphereCast(ray, snapSphereCastRadius, out RaycastHit hit, 500f, interactLayers, QueryTriggerInteraction.Ignore))
            return hit.collider != null ? hit.collider.GetComponentInParent<IngredientSlotBehaviour>() : null;

        return null;
    }

    private IngredientSlotBehaviour FindNearestAcceptingSlotInRange(Vector3 fromWorldPos)
    {
        int count = Physics.OverlapSphereNonAlloc(fromWorldPos, slotSearchRadius, _snapOverlapBuffer, interactLayers, QueryTriggerInteraction.Ignore);

        IngredientSlotBehaviour best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider col = _snapOverlapBuffer[i];
            if (col == null) continue;

            IngredientSlotBehaviour slot = col.GetComponentInParent<IngredientSlotBehaviour>();
            if (slot == null) continue;
            if (!slot.CanAcceptIngredient(this)) continue;

            float d = slot.DistanceToAnchor(fromWorldPos, this);
            if (d <= slot.SnapRange && d < bestDist)
            {
                bestDist = d;
                best = slot;
            }
        }

        return best;
    }

    private bool TryGetPointerWorldPoint(out Vector3 world)
    {
        world = default;
        if (kitchenCamera == null || Mouse.current == null) return false;

        Ray ray = kitchenCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, surfaceRayDistance, dragSurfaceLayers, QueryTriggerInteraction.Ignore))
        {
            world = hit.point;
            return true;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, lockedY, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    private void SaveFreeState()
    {
        freeState.hasValue = true;
        freeState.parent = transform.parent;
        freeState.position = transform.position;
        freeState.rotation = transform.rotation;
        freeState.localScale = transform.localScale;

        if (rb != null)
        {
            freeState.rbKinematic = rb.isKinematic;
            freeState.rbUseGravity = rb.useGravity;
            freeState.rbConstraints = rb.constraints;
        }
    }

    private void RestoreFreeState()
    {
        if (!freeState.hasValue) return;

        transform.SetParent(freeState.parent, true);
        transform.position = freeState.position;
        transform.rotation = freeState.rotation;
        transform.localScale = freeState.localScale;

        if (rb != null)
        {
            rb.isKinematic = freeState.rbKinematic;
            rb.useGravity = freeState.rbUseGravity;
            rb.constraints = freeState.rbConstraints;
        }
    }

    public void SnapInto(Transform anchor)
    {
        SaveFreeState();
        if (anchor != null)
        {
            if (!IsCooked())
                SetToChoppedForm();
            transform.SetParent(anchor, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public void OnRemovedFromSlot()
    {
        RestoreFreeState();
        if (!IsCooked()) SetToRawForm();
    }

    public void SetToRawForm()
    {
        rawForm.SetActive(true);
        choppedForm.SetActive(false);
        cookedForm.SetActive(false);
    }

    public void SetToChoppedForm()
    {
        rawForm.SetActive(false);
        choppedForm.SetActive(true);
        cookedForm.SetActive(false);
    }

    public void SetToCookedForm()
    {
        rawForm.SetActive(false);
        choppedForm.SetActive(false);
        cookedForm.SetActive(true);
    }

    public bool IsCooked() => cookLevel >= 1f;

}