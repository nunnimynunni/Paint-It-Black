using UnityEngine;
using UnityEngine.UI;

public class GifAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;
    public bool loop = true;

    private Image image;
    private int currentFrame;
    private float timer;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.unscaledDeltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame++;
            if (currentFrame >= frames.Length)
                currentFrame = loop ? 0 : frames.Length - 1;
            image.sprite = frames[currentFrame];
        }
    }
}