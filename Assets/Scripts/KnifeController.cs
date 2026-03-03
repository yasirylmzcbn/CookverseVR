using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class KnifeController : MonoBehaviour
{
    [Header("Chop Settings")]
    public float minChopVelocity = 0.3f; // minimum downward speed to count as a chop
    [SerializeField] private Collider bladeCollider;
    private Vector3 lastPosition;
    private Vector3 velocity;
    [SerializeField] private LayerMask ingredientLayer;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        Debug.Log("yasir123 KnifeController initialized. Rigidbody found: " + (rb != null));
        lastPosition = transform.position;
        if (bladeCollider == null)
        {
            bladeCollider = GetComponent<Collider>();
            Debug.Log("yasir123 Blade collider auto-assigned: " + (bladeCollider != null));
        }

    }

    void Update()
    {
        if (rb != null)
            Debug.Log($"Velocity: {rb.linearVelocity}, Speed: {rb.linearVelocity.magnitude}");
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        CheckForChop();
    }

    private void CheckForChop()
    {
        float downwardSpeed = -velocity.y;
        if (downwardSpeed < minChopVelocity) return;

        Bounds b = bladeCollider.bounds;

        Collider[] hits = Physics.OverlapBox(
            b.center,
            b.extents,
            Quaternion.identity,
            ingredientLayer
        );

        foreach (Collider hit in hits)
        {
            Ingredient ingredient = hit.GetComponentInParent<Ingredient>();
            if (ingredient != null)
                ingredient.RegisterChop();
        }
    }

    // void OnDrawGizmos()
    // {
    //     if (bladeTip == null || bladeBase == null) return;
    //     Gizmos.color = Color.red;
    //     Vector3 bladeCenter = (bladeTip.position + bladeBase.position) / 2f;
    //     Gizmos.matrix = Matrix4x4.TRS(bladeCenter, transform.rotation, Vector3.one);
    //     Gizmos.DrawWireCube(Vector3.zero, bladeHalfExtents * 2f);
    // }
}