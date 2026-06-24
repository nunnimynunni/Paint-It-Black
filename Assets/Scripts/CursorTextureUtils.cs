using UnityEngine;

// Helper compartido para construir texturas de cursor a partir de un Sprite,
// escaladas a un tamaño en pixels específico (para que el cursor del mouse
// se vea del mismo tamaño que su ícono equivalente en el HUD/UI).
//
// Usa Graphics.Blit + RenderTexture (GPU) en vez de sprite.texture.GetPixels()
// directo: así funciona aunque la textura original NO tenga "Read/Write
// Enabled" tildado en sus Import Settings (un Texture2D común del proyecto no
// lo tiene por defecto, y GetPixels() tiraría una excepción en ese caso).
public static class CursorTextureUtils
{
    public static Texture2D SpriteATexturaEscalada(Sprite sprite, int targetW, int targetH)
    {
        if (sprite == null || sprite.texture == null) return null;
        targetW = Mathf.Max(4, targetW);
        targetH = Mathf.Max(4, targetH);

        RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
        RenderTexture anterior = RenderTexture.active;

        Graphics.Blit(sprite.texture, rt);
        RenderTexture.active = rt;

        Texture2D resultado = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        resultado.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        resultado.Apply();

        RenderTexture.active = anterior;
        RenderTexture.ReleaseTemporary(rt);

        return resultado;
    }
}
