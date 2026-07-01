using UnityEngine;
using UnityEditor;

// Corre desde: Paint-It-Black → Agregar Hitbox a NPCs
//
// Agrega un BoxCollider2D trigger grande (cuerpo completo) a cada prefab de NPC.
// El CapsuleCollider2D chico (pies) sigue manejando el movimiento/colisión con
// estructuras. El BoxCollider2D nuevo es solo para recibir proyectiles.
// El Projectile.cs ya usa GetComponentInParent<EnemyHealth>(), así que detecta
// tanto el colisionador de los pies como este nuevo trigger del cuerpo.
public class AgregarHitboxNPCs
{
    static readonly string[] PREFAB_PATHS = {
        "Assets/Assets/NPCs enemigos/Soldado Pistola/SoldadoPistola.prefab",
        "Assets/Assets/NPCs enemigos/Soldado Porra/SoldadoPorra.prefab",
        "Assets/Assets/NPCs enemigos/Soldado Anti Disturbios/SoldadoAntiDisturbios.prefab",
    };

    [MenuItem("Paint-It-Black/Agregar Hitbox a NPCs")]
    static void Agregar()
    {
        int count = 0;

        foreach (string path in PREFAB_PATHS)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;

                // Si ya tiene BoxCollider2D trigger, no duplicar
                bool yaTiene = false;
                foreach (var bc in root.GetComponents<BoxCollider2D>())
                    if (bc.isTrigger) { yaTiene = true; break; }

                if (yaTiene) continue;

                BoxCollider2D col = root.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.offset    = new Vector2(0f, 0.45f);  // torso/centro del cuerpo
                col.size      = new Vector2(0.65f, 1.1f); // cubre cuerpo completo sin llegar a la cabeza
                count++;
            }
        }

        Debug.Log($"✅ Hitbox de cuerpo agregado a {count} prefabs.");
    }
}
