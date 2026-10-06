using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;

namespace Paradox.UI;

/// <summary>
/// Adds fallbacks from fonts already shipped with Among Us.
/// The lobby rows use a different TMP font than category headers, so a font can
/// render "OGÓLNE" correctly while rows still show tofu squares for ł/ż/ś.
/// </summary>
public static class ParadoxFontSupport
{
    private const string PolishCharacters = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ";
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
                if (font != null && !fonts.Any(existing => existing.Pointer == font.Pointer))
                    fonts.Add(font);
            }

            if (fonts.Count == 0)
                return;

            // Find fonts that REALLY contain the glyphs in their own atlas.
            // Do not trust a hard-coded Noto font name: its baked AU atlas can omit Latin Extended.
            var missing = new HashSet<char>(PolishCharacters);
            var selected = new List<TMP_FontAsset>();

            foreach (var font in fonts
                         .OrderByDescending(FontPreference)
                         .ThenByDescending(font => CountDirectPolishGlyphs(font)))
            {
                var contributes = false;

                foreach (var character in missing.ToArray())
                {
                    if (!HasDirectGlyph(font, character))
                        continue;

                    missing.Remove(character);
                    contributes = true;
                }

                if (contributes)
                    selected.Add(font);

                if (missing.Count == 0)
                    break;
            }

            if (selected.Count == 0)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    "PARADOX found no loaded Among Us font containing Polish Latin Extended glyphs.");
                return;
            }

            foreach (var font in fonts)
            {
                if (font.fallbackFontAssetTable == null)
                    continue;

                foreach (var fallback in selected)
                {
                    if (font.Pointer == fallback.Pointer)
                        continue;

                    var exists = false;
                    foreach (var existing in font.fallbackFontAssetTable)
                    {
                        if (existing != null && existing.Pointer == fallback.Pointer)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                        font.fallbackFontAssetTable.Add(fallback);
                }
            }

            if (missing.Count > 0)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    $"PARADOX Polish fallback is partial. Missing glyphs: {new string(missing.ToArray())}");
                return;
            }

            _configured = true;
            ParadoxPlugin.Instance.Log.LogInfo(
                $"PARADOX Polish glyph fallback ready: {string.Join(", ", selected.Select(font => font.name))}");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX could not configure Polish font fallback: {e}");
        }
    }

    private static int CountDirectPolishGlyphs(TMP_FontAsset font)
    {
        var count = 0;

        foreach (var character in PolishCharacters)
        {
            if (HasDirectGlyph(font, character))
                count++;
        }

        return count;
    }

    private static bool HasDirectGlyph(TMP_FontAsset font, char character)
    {
        try
        {
            return font.HasCharacter(character, false, false);
        }
        catch
        {
            return false;
        }
    }

    private static int FontPreference(TMP_FontAsset font)
    {
        var name = font.name ?? string.Empty;

        if (name.Contains("Barlow", StringComparison.OrdinalIgnoreCase))
            return 4;
        if (name.Contains("LiberationSans", StringComparison.OrdinalIgnoreCase))
            return 3;
        if (name.Contains("NotoSans", StringComparison.OrdinalIgnoreCase))
            return 2;

        return 1;
    }
}
