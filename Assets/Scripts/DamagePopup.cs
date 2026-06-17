using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    public static void Create(GameObject prefab, Vector3 position, int damage, Color color)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, position + Vector3.up * 0.5f, Quaternion.identity);
        go.GetComponent<DamagePopup>().Setup(damage, color);
    }

    private TextMeshPro tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Default";
            mr.sortingOrder = 10;
        }
    }

    public void Setup(int damage, Color color)
    {
        tmp.text = "-" + damage;
        tmp.fontSize = 6;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        color.a = 1f;
        tmp.color = color;
        StartCoroutine(Animar());
    }

    IEnumerator Animar()
    {
        float duracion = 1f;
        float elapsed = 0f;
        Vector3 inicio = transform.position;

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;
            transform.position = inicio + Vector3.up * t * 1.5f;
            Color c = tmp.color;
            c.a = 1f - t;
            tmp.color = c;
            yield return null;
        }

        Destroy(gameObject);
    }
}
