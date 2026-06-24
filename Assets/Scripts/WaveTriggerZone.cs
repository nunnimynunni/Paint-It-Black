using UnityEngine;

// ============================================================
// DEPRECADO (vertical slice, corrección pedida por el usuario):
// el combate YA NO arranca pisando una zona del piso. Ahora arranca directo
// al terminar la interacción con Gomez (ver GomezInteraction.StartExit(),
// que llama EnemySpawner.StartWaves() directamente).
//
// Este script se deja a propósito sin lógica y además desactiva su propio
// GameObject al iniciar, por si en la escena todavía queda el objeto
// "ZonaInicioCombate": así queda inerte sin necesidad de entrar al Editor
// a borrarlo a mano. Se puede borrar el componente/objeto cuando quieran
// hacer limpieza, pero no es necesario para que el slice funcione bien.
// ============================================================
public class WaveTriggerZone : MonoBehaviour
{
    void Awake()
    {
        gameObject.SetActive(false);
    }
}
