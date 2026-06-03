using UnityEngine;

public class rodillo : MonoBehaviour
{
    public float lifetime = 0.4f;

    private bool hasHit = false;
    private Collider2D col;

    public void Init(Vector2 dir)
    {
        col = GetComponent<Collider2D>();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!hasHit)
        {
            hasHit = true;
        }
        else
        {
            if (col != null)
                col.enabled = false;
        }
    }
}