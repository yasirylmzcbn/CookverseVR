using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed;
    void Start()
    {
        // Rotate forward by rotation
        Debug.Log("Bullet instantiated");
        GetComponent<Rigidbody>().linearVelocity = transform.forward * speed;
        Debug.Log(GetComponent<Rigidbody>().linearVelocity);
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
            return;
        }
        Enemy enemy = collision.collider.GetComponentInParent<Enemy>(); //currently hitting the enemy body
        if (enemy != null)
        {
            enemy.Damage(1);
            Destroy(gameObject);
            return;
        }
    }
}
