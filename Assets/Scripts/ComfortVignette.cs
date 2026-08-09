using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class ComfortVignette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MazeConfig config;
    [SerializeField] private PlayerMovement player;

    [Header("Shape")]
    [SerializeField, Range(0f, 1f)]
    private float innerRadius = 0.45f;

    [SerializeField, Range(0f, 1.5f)]
    private float outerRadius = 1f;

    [SerializeField, Range(64, 512)]
    private int textureSize = 256;

    private Image vignetteImage;

    private float currentAlpha;
    private float alphaVelocity;

    private void Awake()
    {
        vignetteImage = GetComponent<Image>();

        vignetteImage.raycastTarget = false;
        vignetteImage.sprite = CreateVignetteSprite();

        SetAlpha(0f);
    }

    private void Update()
    {
        if (config == null || player == null)
            return;

        float targetAlpha =
            player.NormalizedSpeed *
            config.VignetteMaxAlpha;

        currentAlpha =
            Mathf.SmoothDamp(
                currentAlpha,
                targetAlpha,
                ref alphaVelocity,
                config.VignetteSmoothTime);

        SetAlpha(currentAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = Color.black;
        color.a = alpha;

        vignetteImage.color = color;
    }

    private Sprite CreateVignetteSprite()
    {
        Texture2D texture =
            new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false);

        texture.name = "RuntimeComfortVignette";
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels =
            new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float normalizedX =
                    (x / (float)(textureSize - 1)) * 2f - 1f;

                float normalizedY =
                    (y / (float)(textureSize - 1)) * 2f - 1f;

                float distance =
                    Mathf.Sqrt(
                        normalizedX * normalizedX +
                        normalizedY * normalizedY);

                float alpha =
                    Mathf.InverseLerp(
                        innerRadius,
                        outerRadius,
                        distance);

                alpha =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        alpha);

                pixels[y * textureSize + x] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                texture.width,
                texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }
}