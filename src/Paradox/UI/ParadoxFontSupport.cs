using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Paradox.UI;

/// <summary>
/// Reliable Polish font support for PARADOX UI.
/// Among Us' baked TMP atlases contain only part of Latin Extended, so fallback
/// assets shipped by the game still render boxes for letters such as ę/ł/ż.
/// On Windows we build a dynamic TMP font from a system font that contains Polish.
/// </summary>
public static class ParadoxFontSupport
{
    private const string PolishCharacters = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ";

    private static TMP_FontAsset? _polishFont;
    private static bool _attempted;

    public static TMP_FontAsset? PolishFont
    {
        get
        {
            EnsurePolishGlyphs();
            return _polishFont;
        }
    }

    public static void EnsurePolishGlyphs()
    {
        if (_polishFont != null || _attempted)
            return;

        _attempted = true;

        try
        {
            var families = new[]
            {
                ("Arial", "Regular"),
                ("Segoe UI", "Regular"),
                ("Tahoma", "Regular"),
                ("Calibri", "Regular")
            };

            foreach (var candidate in families)
            {
                TMP_FontAsset? asset = null;

                try
                {
                    asset = TMP_FontAsset.CreateFontAsset(candidate.Item1, candidate.Item2, 64);
                }
                catch (Exception e)
                {
                    ParadoxPlugin.Instance.Log.LogWarning(
                        $"PARADOX could not create TMP font {candidate.Item1}: {e.Message}");
                }

                if (TryAccept(asset, candidate.Item1))
                    return;
            }

            var paths = new[]
            {
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\segoeui.ttf",
                @"C:\Windows\Fonts\tahoma.ttf",
                @"C:\Windows\Fonts\calibri.ttf"
            };

            foreach (var path in paths)
            {
                if (!File.Exists(path))
                    continue;

                TMP_FontAsset? asset = null;

                try
                {
                    // AU 2026 exposes the public 7-argument path overload.
                    asset = TMP_FontAsset.CreateFontAsset(
                        path,
                        0,
                        64,
                        6,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024);
                }
                catch (Exception e)
                {
                    ParadoxPlugin.Instance.Log.LogWarning(
                        $"PARADOX could not create TMP font from {path}: {e.Message}");
                }

                if (TryAccept(asset, path))
                    return;
            }

            ParadoxPlugin.Instance.Log.LogError(
                "PARADOX could not create a Windows TMP font with Polish glyphs.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogError(
                $"PARADOX Polish font setup failed: {e}");
        }
    }

    private static bool TryAccept(TMP_FontAsset? asset, string source)
    {
        if (asset == null)
            return false;

        asset.name = "PARADOX Polish Dynamic";
        asset.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            asset.TryAddCharacters(PolishCharacters);
        }
        catch
        {
            // Final validation below decides whether this asset is usable.
        }

        if (!SupportsPolish(asset))
        {
            UnityEngine.Object.Destroy(asset);
            return false;
        }

        _polishFont = asset;
        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX Polish dynamic font ready: {source}");
        return true;
    }

    public static void ApplyTo(TMP_Text? text)
    {
        if (text == null || ParadoxPlugin.Localizer.CurrentLanguage != Localization.Language.Polish)
            return;

        EnsurePolishGlyphs();

        if (_polishFont == null)
            return;

        text.font = _polishFont;
    }

    public static bool SupportsPolish(TMP_FontAsset? font)
    {
        if (font == null)
            return false;

        try
        {
            foreach (var character in PolishCharacters)
            {
                if (!font.HasCharacter(character, false, true))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
