using UnityEngine;

// ============================================================
// SCRIPT: EnceradorAbility
// Gestiona la habilidad de encerar aliados del Encerador.
//
// El Encerador escanea NPCs cercanos cada 'cooldownCera' segundos
// y elige el aliado más cercano que:
//   - Esté vivo
//   - No sea él mismo
//   - No tenga ya NpcCeraEffect activo
//   - No supere el tope global de encerados simultáneos
// Si encuentra uno válido, lo encera: le agrega NpcCeraEffect,
// que se auto-destruye al expirar la duración.
// ============================================================
[RequireComponent(typeof(SpriteRenderer))]
public class EnceradorAbility : MonoBehaviour
{
    [Header("Habilidad de Encerado")]
    [Tooltip("Radio de búsqueda de aliados a encerar")]
    public float radioCera = 5f;

    [Tooltip("Segundos entre cada intento de encerar")]
    public float cooldownCera = 4f;

    [Tooltip("Cuánto dura la cera en el NPC objetivo")]
    public float duracionCera = 10f;

    private float timerCera = 1.5f; // pequeño delay inicial antes del primer encerado

    void Update()
    {
        timerCera -= Time.deltaTime;
        if (timerCera <= 0f)
        {
            timerCera = cooldownCera;
            IntentarEncerar();
        }
    }

    void IntentarEncerar()
    {
        // Respetar el tope global
        if (NpcCeraEffect.EnceredosActivos >= NpcCeraEffect.MaxEnceredos) return;

        EnemyHealth objetivo = BuscarObjetivo();
        if (objetivo == null) return;

        // Agregar el efecto de cera al aliado elegido
        NpcCeraEffect efecto = objetivo.GetComponent<NpcCeraEffect>();
        if (efecto == null)
            efecto = objetivo.gameObject.AddComponent<NpcCeraEffect>();

        efecto.Activar(duracionCera);
    }

    EnemyHealth BuscarObjetivo()
    {
        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth mejor  = null;
        float mejorDist    = radioCera;

        foreach (EnemyHealth e in todos)
        {
            if (e == null || e.gameObject == gameObject) continue;
            if (!e.IsAlive()) continue;

            // Los Enceradores no se encerán entre sí (son unidades de apoyo,
            // no deben gastar el efecto en sus propios congéneres).
            if (e.GetComponent<EnceradorAbility>() != null) continue;

            // No encerar a alguien que ya está encerado
            NpcCeraEffect existente = e.GetComponent<NpcCeraEffect>();
            if (existente != null && existente.EstaEncerado) continue;

            // Preferir al más cercano
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < mejorDist)
            {
                mejorDist = dist;
                mejor     = e;
            }
        }

        return mejor;
    }
}
