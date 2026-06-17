using UnityEngine;

// Poner en un GameObject con un Collider2D (isTrigger = true) sobre una zona específica
// del piso. Cuando el jugador (Forastero) entra, arranca las oleadas del EnemySpawner
// indicado. Por defecto se usa una sola vez (la zona se desactiva después de disparar).
[RequireComponent(typeof(Collider2D))]
public class WaveTriggerZone : MonoBehaviour
{
    [Tooltip("El EnemySpawner que hay que arrancar al entrar en esta zona")]
    public EnemySpawner spawner;
    [Tooltip("Si está activo, la zona deja de funcionar después de la primera vez que se pisa")]
    public bool oneShot = true;

    private bool triggered = false;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered && oneShot) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (ph == null) return;

        if (spawner != null) spawner.StartWaves();

        triggered = true;
        if (oneShot) gameObject.SetActive(false);
    }
}
