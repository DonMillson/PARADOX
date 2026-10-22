using Paradox.Roles;
using UnityEngine;

namespace Paradox.UI;

/// <summary>
/// Shared PARADOX palette and a tiny, runtime-generated rounded panel sprite.
/// The gradient is generated once and does not require an external asset bundle.
/// </summary>
public static class ParadoxVisualTheme
{
    public static readonly Color32 TextPrimary = new(241, 246, 255, 255);
    public static readonly Color32 TextMuted = new(149, 168, 191, 255);
    public static readonly Color32 Cyan = new(87, 222, 226, 255);
    public static readonly Color32 Violet = new(168, 117, 244, 255);
    public static readonly Color32 Off = new(159, 169, 188, 255);
    public static readonly Color32 Planned = new(206, 176, 107, 255);

    private static Sprite? _panelSprite;

    public static Color32 FactionAccent(RoleFaction faction) => faction switch
    {
        RoleFaction.Impostor => new Color32(255, 102, 126, 255),
        RoleFaction.Crewmate => new Color32(93, 221, 235, 255),
        _ => new Color32(245, 202, 106, 255)
    };

    public static Color32 FactionRow(RoleFaction faction) => faction switch
    {
        RoleFaction.Impostor => new Color32(93, 40, 68, 255),
        RoleFaction.Crewmate => new Color32(33, 90, 105, 255),
        _ => new Color32(101, 82, 45, 255)
    };

    public static Sprite PanelSprite
    {
        get
        {
            if (_panelSprite != null)
                return _panelSprite;

            const int size = 64;
            const float radius = 7f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PARADOX_GradientPanel",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var dark = new Color(0.035f, 0.055f, 0.105f, 1f);
            var top = new Color(0.095f, 0.128f, 0.215f, 1f);
            var cyan = new Color(0.10f, 0.36f, 0.42f, 1f);
            var violet = new Color(0.32f, 0.16f, 0.48f, 1f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var edgeX = Mathf.Min(x, size - 1 - x);
                    var edgeY = Mathf.Min(y, size - 1 - y);
                    var cornerX = Mathf.Max(0f, radius - edgeX);
                    var cornerY = Mathf.Max(0f, radius - edgeY);
                    var cornerDistance = Mathf.Sqrt(cornerX * cornerX + cornerY * cornerY);
                    var alpha = Mathf.Clamp01(radius + 0.5f - cornerDistance);

                    var uvx = x / (float)(size - 1);
                    var uvy = y / (float)(size - 1);
                    var baseColor = Color.Lerp(dark, top, uvy);
                    var tealGlow = Mathf.Clamp01(1f - Mathf.Sqrt(
                        (uvx - 0.05f) * (uvx - 0.05f) +
                        (uvy - 1f) * (uvy - 1f)) * 1.65f);
                    var violetGlow = Mathf.Clamp01(1f - Mathf.Sqrt(
                        (uvx - 1f) * (uvx - 1f) +
                        (uvy - 0.8f) * (uvy - 0.8f)) * 1.5f);
                    baseColor = Color.Lerp(baseColor, cyan, tealGlow * 0.17f);
                    baseColor = Color.Lerp(baseColor, violet, violetGlow * 0.15f);

                    // Soft inset edge keeps small cards readable without thick boxes.
                    var rim = Mathf.Min(edgeX, edgeY);
                    if (rim < 2f)
                        baseColor = Color.Lerp(baseColor, Cyan, (2f - rim) * 0.08f);

                    baseColor.a = alpha;
                    pixels[y * size + x] = (Color32)baseColor;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            _panelSprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _panelSprite.name = "PARADOX_RoundedGradientPanel";
            return _panelSprite;
        }
    }
}
