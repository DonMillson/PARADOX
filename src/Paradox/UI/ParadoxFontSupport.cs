using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Paradox.UI;

/// <summary>
/// Reliable Polish font support for PARADOX UI.
/// Builds a dynamic TMP font from a Windows font family and applies it directly
/// to PARADOX labels when Polish is selected.
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
                "Arial",
                "Segoe UI",
                "Tahoma",
                "Calibri"
            };

            foreach (var family in families)
            {
                Font? osFont = null;
                TMP_FontAsset? asset = null;

                try
                {
                    osFont = Font.CreateDynamicFontFromOSFont(family, 64);
                    if (osFont == null)
                        continue;

                    asset = TMP_FontAsset.CreateFontAsset(
                        osFont,
                        64,
                        6,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024,
                        AtlasPopulationMode.Dynamic,
                        true);
                }
                catch (Exception e)
                {
                    ParadoxPlugin.Instance.Log.LogWarning(
                        $"PARADOX could not create TMP font {family}: {e.Message}");
                }

                if (TryAccept(asset, family))
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
