using UnityEngine;

// ============================================================
// SCRIPT: CameraFollow
// FASE 1 del vertical slice: cámara con seguimiento centrado al jugador
// y límites de mapa (para que no se vea "fuera" del escenario dibujado).
// ============================================================
//
// CÓMO FUNCIONA:
// - Sigue a "target" (el Player) con suavizado (Lerp), no de forma rígida,
//   para que no se sienta brusca al moverse en diagonal o al chocar con NPCs.
// - Si useBounds = true, clampea la posición final para que el "rectángulo
//   visible" de la cámara (calculado con orthographicSize y aspect, igual
//   que ya hace EnemySpawner.GetOffscreenPosition) nunca se salga del área
//   definida por minBounds/maxBounds.
//
// SETUP EN UNITY:
// 1. Poner este script en la Main Camera de la escena de juego (SampleScene).
// 2. Arrastrar el Player al campo "target".
// 3. Crear dos GameObjects vacíos en las esquinas inferior-izquierda y
//    superior-derecha del área jugable del mapa (ej: "MapBoundsMin" y
//    "MapBoundsMax") y asignarlos en minBoundsPoint / maxBoundsPoint.
//    (Mismo patrón que EntryStart/EntryEnd en CombatEndTrigger.)
// 4. Si el mapa todavía no está armado/agrandado, dejar useBounds en false
//    y la cámara sigue al jugador sin límites mientras se decide el tamaño
//    final del mapa.
// ============================================================
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target; // el Player

    [Header("Suavizado")]
    [Tooltip("Más alto = la cámara alcanza al jugador más rápido. 0 = sigue instantáneo (rígido).")]
    public float smoothSpeed = 8f;

    [Header("Límites de mapa")]
    public bool useBounds = false;
    [Tooltip("GameObject vacío en la esquina inferior-izquierda del área jugable")]
    public Transform minBoundsPoint;
    [Tooltip("GameObject vacío en la esquina superior-derecha del área jugable")]
    public Transform maxBoundsPoint;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Feedback de playtest: "volver a armar el seguimiento de cámara".
        // Si target no se asignó a mano en el Editor (o el componente se
        // agrega por código/YAML sin arrastrar la referencia), se busca solo
        // al GameObject con tag "Player" para no depender de ese paso manual.
        if (target == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null) target = jugador.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        // Suavizado: si smoothSpeed es 0 o muy alto, igual converge bien con Lerp por deltaTime.
        Vector3 next = (smoothSpeed <= 0f)
            ? desired
            : Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        if (useBounds && minBoundsPoint != null && maxBoundsPoint != null && cam != null)
            next = ClampToBounds(next);

        transform.position = next;
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = minBoundsPoint.position.x + halfWidth;
        float maxX = maxBoundsPoint.position.x - halfWidth;
        float minY = minBoundsPoint.position.y + halfHeight;
        float maxY = maxBoundsPoint.position.y - halfHeight;

        // Si el mapa es más chico que lo que ve la cámara en algún eje,
        // evitamos invertir el clamp (min > max) y centramos en ese eje.
        float x = (minX <= maxX) ? Mathf.Clamp(pos.x, minX, maxX)
                                  : (minBoundsPoint.position.x + maxBoundsPoint.position.x) / 2f;
        float y = (minY <= maxY) ? Mathf.Clamp(pos.y, minY, maxY)
                                  : (minBoundsPoint.position.y + maxBoundsPoint.position.y) / 2f;

        return new Vector3(x, y, pos.z);
    }
}
