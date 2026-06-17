using UnityEngine;

public class HitBox : MonoBehaviour
{
    SpriteRenderer sr;
    Color colorOriginal;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        colorOriginal = sr.color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Disparo"))
            sr.color = Color.red;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Disparo"))
            sr.color = colorOriginal;
    }
}