using TMPro;
using UnityEngine;

namespace Paradox.UI;

/// <summary>
/// Adds a Latin Extended fallback to Among Us TMP fonts so Polish diacritics
/// (ą ć ę ł ń ó ś ź ż and uppercase variants) render in PARADOX UI.
/// </summary>
public static class ParadoxFontSupport
{
    private static bool _configured;

    public static void EnsurePolishGlyphs()
    {
        if (_configured)
            return;

        try
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts == null || fonts.Length == 0)
                return;

            TMP_FontAsset? fallback = null;

            // Among Us ships Noto Sans assets for East Asian languages.
            // They also contain the Latin Extended glyphs needed by Polish.
            foreach (var font in fonts)
            {
                if (font == null)
                    continue;

                if (font.name == "NotoSansJP-Regular SDF")
                {
                    fallback = font;
                    break;
                }
            }

            if (fallback == null)
            {
                foreach (var font in fonts)
                {
                    if (font == null)
                        continue;

                    if (font.name == "NotoSansSC-Regular SDF" ||
                        font.name == "NotoSansKR-Regular SDF")
                    {
                        fallback = font;
                        break;
                    }
                }
            }

            if (fallback == null)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    "PARADOX Polish font fallback not found. Polish diacritics may be missing.");
                return;
            }

            foreach (var font in fonts)
            {
                if (font == null || font == fallback || font.fallbackFontAssetTable == null)
                    continue;

                var exists = false;
                foreach (var existing in font.fallbackFontAssetTable)
                {
                    if (existing == fallback)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    font.fallbackFontAssetTable.Add(fallback);
            }

            _configured = true;
            ParadoxPlugin.Instance.Log.LogInfo(
                $"PARADOX Polish glyph fallback enabled: {fallback.name}");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX could not configure Polish font fallback: {e}");
        }
    }
}
