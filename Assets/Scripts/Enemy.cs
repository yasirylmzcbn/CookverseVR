using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float health = 10; 
    
    public Color damageColor = Color.yellow;
    public float flashDuration = 0.1f;

    Renderer rend;
    MaterialPropertyBlock mpb;
    Color originalColor;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();

        // Grab original color from the material
        rend.GetPropertyBlock(mpb);

        // URP/HDRP: "_BaseColor"
        // Built-in: "_Color"

        //if (mpb.HasColor("_BaseColor"))
        //    originalColor = mpb.GetColor("_BaseColor");
        //else
            originalColor = rend.sharedMaterial.color;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FlashDamage()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        mpb.SetColor("_BaseColor", damageColor);
        rend.SetPropertyBlock(mpb);

        yield return new WaitForSeconds(flashDuration);

        mpb.SetColor("_BaseColor", originalColor);
        rend.SetPropertyBlock(mpb);
    }

    public void Damage(int d)
    {
        health -= d;
        if (health < 0)
        {
            Die();
        }
        else
        {
            FlashDamage();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
