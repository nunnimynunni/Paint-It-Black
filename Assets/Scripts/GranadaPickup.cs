using UnityEngine;

[DisallowMultipleComponent]
public class GranadaPickup : MonoBehaviour
{
    [Header("Config")]
    public int cantidad = 2;

    [Header("Visual")]
    private const float ALTURA_BOB = 0.22f;
    private const float VELOCIDAD_BOB = 3f;

    private Vector3 posInicial;
    private float bobTimer;

    void Start()
    {
        posInicial = transform.position;
    }

    void Update()
    {
        bobTimer += Time.deltaTime * VELOCIDAD_BOB;
        transform.position = posInicial + Vector3.up * Mathf.Sin(bobTimer) * ALTURA_BOB;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return;

        playerataque pa = other.GetComponentInParent<playerataque>();
        if (pa == null) pa = other.transform.root.GetComponentInChildren<playerataque>();
        if (pa == null) return;

        pa.AgregarGranadas(cantidad);
        Destroy(gameObject);
    }
}
