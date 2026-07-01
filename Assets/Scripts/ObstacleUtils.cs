using UnityEngine;

// ============================================================
// Feedback de playtest: "los proyectiles (del jugador y de los enemigos)
// deben detenerse/destruirse al chocar con objetos del mapa (obstáculos),
// nada debería poder atravesarlos, y los NPCs deben mantener una distancia
// mínima de esos objetos".
//
// El proyecto NO tiene un tag/layer dedicado para "obstáculo": los objetos
// de mapa (cascadas, casas, árboles, etc.) son simplemente GameObjects
// Untagged en el layer Default con un Collider2D SÓLIDO (m_IsTrigger: 0).
// Por eso, en vez de pedir que se etiquete cada objeto a mano en el Editor,
// esta clase define qué es "un obstáculo" por exclusión: cualquier
// Collider2D que NO sea trigger y que no pertenezca a un jugador, un NPC
// (de combate o ambiental) ni a un pickup/feedback de munición.
// ============================================================
public static class ObstacleUtils
{
    public static bool EsObstaculoSolido(Collider2D other)
    {
        if (other == null) return false;
        if (other.isTrigger) return false; // pickups, zonas, outlines, etc. nunca son obstáculo

        if (other.GetComponent<PlayerHealth>() != null || other.GetComponentInParent<PlayerHealth>() != null)
            return false;
        if (other.GetComponent<EnemyHealth>() != null || other.GetComponentInParent<EnemyHealth>() != null)
            return false;
        if (other.GetComponent<NPCMovement>() != null || other.GetComponentInParent<NPCMovement>() != null)
            return false;

        // El agua (río, etc.) no detiene proyectiles — las balas van por el aire.
        if (other.GetComponent<MarcadorAgua>() != null || other.GetComponentInParent<MarcadorAgua>() != null)
            return false;

        return true;
    }
}
