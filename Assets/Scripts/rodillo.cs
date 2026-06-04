using UnityEngine;

public class rodillo : MonoBehaviour
{
    public float lifetime = 0.4f;

    public void Init(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        Destroy(gameObject, lifetime);
    }
}