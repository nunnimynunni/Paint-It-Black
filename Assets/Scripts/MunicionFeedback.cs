using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// SCRIPT: MunicionFeedback
// Feedback visual pedido junto al sistema de reposición de munición:
// al recoger una gota (AmmoPickup) aparece un texto "+N" flotando hacia
// arriba y desvaneciéndose, tiñado del color de munición recogido, para
// que quede clarísimo qué se rellenó y cuánto.
//
// Se arma 100% por código reutilizando el mismo Canvas que ya usan
// PaintCanvasPuzzle/GameManager (sin crear ningún prefab ni asignar nada
// a mano en el Editor), siguiendo el mismo esquema que DamagePopup pero
// con UI Text de pantalla en vez de TextMeshPro en el mundo, porque acá
// no hace falta un prefab previo.
// ============================================================
public static class MunicionFeedback
{
    public static void Mostrar(Vector3 posicionMundo, PaintColor color, int cantidad)
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        Canvas canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject textoObj = new GameObject("MunicionFeedback_Texto", typeof(RectTransform));
        textoObj.transform.SetParent(canvas.transform, false);

        Text txt = textoObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // Agrandado (feedback de playtest: "el número de suma está muy
        // pequeño, hacelo más grande para verlo bien en el mapa").
        txt.fontSize = 38;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;

        Color tint = PaintColorUtils.ToUnityColor(color);
        tint.a = 1f;
        txt.color = tint;
        txt.text = "+" + cantidad;

        // Sombra negra simple para que el texto agrandado siga siendo
        // legible sobre fondos claros del mapa.
        Outline sombra = textoObj.AddComponent<Outline>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(2f, -2f);

        RectTransform rect = textoObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, 70f);

        MunicionFeedbackAnimador animador = textoObj.AddComponent<MunicionFeedbackAnimador>();
        animador.Iniciar(posicionMundo, txt);
    }
}

// Componente auxiliar: necesita ser un MonoBehaviour propio para poder
// correr la coroutine de animación (la clase de arriba es estática).
public class MunicionFeedbackAnimador : MonoBehaviour
{
    public void Iniciar(Vector3 posicionMundo, Text texto)
    {
        StartCoroutine(Animar(posicionMundo, texto));
    }

    IEnumerator Animar(Vector3 posicionMundo, Text texto)
    {
        RectTransform rect = texto.GetComponent<RectTransform>();
        const float duracion = 0.9f;
        float elapsed = 0f;
        Camera cam = Camera.main;

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;

            Vector3 posFlotante = posicionMundo + Vector3.up * (0.4f + t * 0.9f);
            if (cam != null)
                rect.position = cam.WorldToScreenPoint(posFlotante);

            Color c = texto.color;
            c.a = 1f - t;
            texto.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}
