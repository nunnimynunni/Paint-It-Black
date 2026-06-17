using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class PantallaInicio : MonoBehaviour
{
    public TMP_Text texto;
    public int escenaDestino = 1; // índice en la Scene List (2Menu = 0)
    public float velocidad = 2f;

    void Update()
    {
        if (texto != null)
        {
            float alpha = Mathf.PingPong(Time.time * velocidad, 1f);
            Color c = texto.color;
            c.a = alpha;
            texto.color = c;
        }

        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            Debug.Log("Cargando escena índice " + escenaDestino);
            SceneManager.LoadScene(escenaDestino);
        }
    }
}