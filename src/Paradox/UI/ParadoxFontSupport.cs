using Il2CppInterop.Runtime;
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
            var objects = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>());
            if (objects == null || objects.Length == 0)
                return;

            var fonts = new List<TMP_FontAsset>();

            foreach (var obj in objects)
            {
                if (obj == null)
                    continue;

                var font = obj.Cast<TMP_FontAsset>();
                if (font != null)
                    fonts.Add(font);
            }

            TMP_FontAsset? fallback = null;

            foreach (var font in fonts)
            {
                if (font.name == "NotoSansJP-Regular SDF")
                {
                    fallback = font;
                    break;
                }
            }

            if (fallback == null)
            {
                fallback = fonts.FirstOrDefault(font =>
                    font.name == "NotoSansSC-Regular SDF" ||
                    font.name == "NotoSansKR-Regular SDF");
            }

            if (fallback == null)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    "PARADOX Polish font fallback not found. Polish diacritics may be missing.");
                return;
            }

            foreach (var font in fonts)
            {
                if (font == fallback || font.fallbackFontAssetTable == null)
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
