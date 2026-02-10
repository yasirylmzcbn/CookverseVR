using System.Collections.Generic;
using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float maxLifetimeSeconds = 5f;

    [Header("Damage")]
    [SerializeField] private float areaRadius = 2.5f;
    [SerializeField] private int damage = 1;

    [Header("Collision")]
    [Tooltip("Optional: only explode when hitting these layers. If set to Everything (-1), any collision will explode.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    private readonly Collider[] _overlapBuffer = new Collider[64];

    private Rigidbody _rb;
    private bool _exploded;

    private void Awake()
    {
        TryGetComponent(out _rb);
    }

    private void Start()
    {
        if (_rb != null)
        {
            // Matches your Bullet.cs pattern (Unity supports linearVelocity in recent versions).
            _rb.linearVelocity = transform.forward * speed;
        }

        if (maxLifetimeSeconds > 0f)
            Destroy(gameObject, maxLifetimeSeconds);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_exploded) return;
        if (collision.collider == null) return;

        // Layer filter
        if (((1 << collision.collider.gameObject.layer) & hitLayers.value) == 0)
            return;

        ExplodeAt(collision.GetContact(0).point);
    }

    private void ExplodeAt(Vector3 position)
    {
        if (_exploded) return;
        _exploded = true;

        int count = Physics.OverlapSphereNonAlloc(position, areaRadius, _overlapBuffer, ~0, QueryTriggerInteraction.Ignore);

        // Prevent damaging the same enemy multiple times due to multiple colliders.
        HashSet<Enemy> damaged = new HashSet<Enemy>();

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            if (!damaged.Add(enemy)) continue;

            enemy.Damage(damage);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.35f);
        Gizmos.DrawSphere(transform.position, areaRadius);
    }
#endif
}
