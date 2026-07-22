using UnityEngine;

// ============================================================
// SCRIPT: UpgradeSystem
// FASE 5 del vertical slice: sistema de recompensas REAL del puzzle
// "Copia de Patrón bajo Presión", fiel al texto exacto del GDD 3.7:
//
//   "La recompensa es una mejora permanente o temporal.
//    - Mejoras de Arma: incremento de daño, reducción de cooldown,
//      mayor velocidad de disparo, capacidad de munición, área de
//      efecto ampliada.
//    - Mejoras de Vida y Defensa: recuperación de puntos de vida,
//      escudo temporal, reducción de daño recibido, vida extra.
//    - Mejoras de Habilidades Pasivas: aumento de velocidad de
//      movimiento, rebote de proyectiles."
//
// Esto reemplaza el contador abstracto "Puntos de Mejora" que había puesto
// antes (sin árbol de mejoras donde gastarlos no tenía sentido): ahora cada
// puzzle resuelto con éxito otorga DIRECTAMENTE una de estas mejoras al azar,
// aplicada de inmediato. Las mejoras de stat (daño/cooldown/velocidad de
// disparo/munición/área/vida y defensa pasiva/velocidad de movimiento/
// rebote) son PERMANENTES (acumulables); curación y escudo temporal son
// de efecto TEMPORAL/inmediato, tal como dice el GDD ("permanente o
// temporal").
//
// Quién la usa:
//   - PuzzleStructure.ManejarResultado(true) llama OtorgarMejoraAleatoria().
//   - playerataque.cs lee ArmaDanoMultiplier / ArmaCooldownMultiplier /
//     ArmaVelocidadDisparoMultiplier / ArmaAreaEfectoMultiplier al disparar.
//   - AmmoManager lee/usa AumentarCapacidadMunicion().
//   - PlayerHealth lee VidaReduccionDanoPercent y usa Heal()/
//     ActivarEscudoTemporal()/AddVidaExtra().
//   - PlayerAnimator lee PasivaVelocidadMovimientoMultiplier.
//   - Projectile lee PasivaRebote.
// Todos los multiplicadores arrancan "neutros" (1f / 0f / false), así que
// sin ninguna mejora ganada el juego se comporta exactamente igual que antes.
// ============================================================
public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance;

    public enum TipoMejora
    {
        ArmaDano,
        ArmaCooldown,
        ArmaVelocidadDisparo,
        ArmaMunicion,
        ArmaAreaEfecto,
        VidaCuracion,
        VidaEscudoTemporal,
        VidaReduccionDano,
        VidaExtra,
        PasivaVelocidadMovimiento,
        PasivaRebote,
    }

    [Header("Mejoras de Arma (permanentes, acumulables)")]
    public float ArmaDanoMultiplier = 1f;
    [Tooltip("Multiplica el cooldown del Spray y el Rodillo (menor = más rápido)")]
    public float ArmaCooldownMultiplier = 1f;
    [Tooltip("Multiplica la velocidad de disparo del Pincel (mayor = dispara más seguido)")]
    public float ArmaVelocidadDisparoMultiplier = 1f;
    public float ArmaAreaEfectoMultiplier = 1f;

    [Header("Mejoras de Vida y Defensa")]
    [Range(0f, 0.75f)] public float VidaReduccionDanoPercent = 0f; // permanente
    public int VidaExtraDisponibles = 0; // permanente

    [Header("Mejoras de Habilidades Pasivas (permanentes)")]
    public float PasivaVelocidadMovimientoMultiplier = 1f;
    public bool PasivaRebote = false;

    [Tooltip("Cantidad de mejoras distintas obtenidas hasta ahora, solo informativo/debug")]
    public int MejorasObtenidas { get; private set; } = 0;

    void Awake()
    {
        Instance = this;
    }

    // No hay ningún GameObject "UpgradeSystem" puesto a mano en la escena
    // (siguiendo la idea de minimizar el trabajo en el Editor): se crea solo
    // la primera vez que hace falta, vía AddComponent en runtime.
    public static UpgradeSystem EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("UpgradeSystem_Runtime");
            Instance = go.AddComponent<UpgradeSystem>();
        }
        return Instance;
    }

    // Llamado por PuzzleStructure al ganar el puzzle. Elige una mejora al
    // azar entre las 11 del GDD, la aplica, y devuelve una descripción en
    // español lista para mostrar en pantalla (lo usa PaintCanvasPuzzle).
    public string OtorgarMejoraAleatoria()
    {
        var valores = (TipoMejora[])System.Enum.GetValues(typeof(TipoMejora));
        TipoMejora elegida = valores[Random.Range(0, valores.Length)];
        MejorasObtenidas++;
        string mensaje = AplicarMejora(elegida);

        // Pedido del usuario: mientras dure el potenciador (outline verde) suena
        // "Final Boss". Cuando el outline se apaga, PlayerHealth llama
        // MusicManager.TerminarPotenciador() y vuelve a "Peleas Genericas".
        if (MusicManager.Instance != null) MusicManager.Instance.CrossfadeABoss();

        return mensaje;
    }

    // Mientras el beneficio esté activo, el protagonista muestra un outline
    // verde (PlayerHealth.MostrarBuffOutline), con tope de 30s. Para mejoras
    // permanentes (sin un "fin" real) el outline dura los 30s completos como
    // aviso visual de que se obtuvo algo; para el escudo temporal dura
    // exactamente lo mismo que protege (10s), y para la curación instantánea
    // dura un poco menos, solo a modo de aviso.
    const float DURACION_AVISO_PERMANENTE = 60f;
    const float DURACION_ESCUDO = 10f;
    const float DURACION_AVISO_CURACION = 6f;

    string AplicarMejora(TipoMejora tipo)
    {
        switch (tipo)
        {
            case TipoMejora.ArmaDano:
                ArmaDanoMultiplier += 0.15f;
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora de Arma: +15% de daño";

            case TipoMejora.ArmaCooldown:
                ArmaCooldownMultiplier = Mathf.Max(0.4f, ArmaCooldownMultiplier - 0.15f);
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora de Arma: cooldown reducido";

            case TipoMejora.ArmaVelocidadDisparo:
                ArmaVelocidadDisparoMultiplier += 0.25f;
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora de Arma: mayor velocidad de disparo";

            case TipoMejora.ArmaMunicion:
                if (AmmoManager.instance != null) AmmoManager.instance.AumentarCapacidadMunicion(10);
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora de Arma: +10 de capacidad de munición por color";

            case TipoMejora.ArmaAreaEfecto:
                ArmaAreaEfectoMultiplier += 0.2f;
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora de Arma: área de efecto ampliada";

            case TipoMejora.VidaCuracion:
                if (PlayerHealth.Instance != null) PlayerHealth.Instance.Heal(30);
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_CURACION);
                return "Mejora de Vida: +30 puntos de vida recuperados";

            case TipoMejora.VidaEscudoTemporal:
                if (PlayerHealth.Instance != null) PlayerHealth.Instance.ActivarEscudoTemporal(DURACION_ESCUDO);
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_ESCUDO);
                return "Mejora de Vida: escudo temporal por 10 segundos";

            case TipoMejora.VidaReduccionDano:
                VidaReduccionDanoPercent = Mathf.Min(0.75f, VidaReduccionDanoPercent + 0.1f);
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora de Defensa: -10% de daño recibido (permanente)";

            case TipoMejora.VidaExtra:
                VidaExtraDisponibles++;
                if (PlayerHealth.Instance != null) PlayerHealth.Instance.AddVidaExtra(1);
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora de Vida: +1 vida extra";

            case TipoMejora.PasivaVelocidadMovimiento:
                PasivaVelocidadMovimientoMultiplier += 0.1f;
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora Pasiva: +10% de velocidad de movimiento";

            case TipoMejora.PasivaRebote:
                PasivaRebote = true;
                PlayerHealth.Instance?.MostrarBuffOutline(DURACION_AVISO_PERMANENTE);
                return "Mejora Pasiva: los proyectiles ahora rebotan a otro enemigo";
        }

        return "Mejora desconocida";
    }
}
