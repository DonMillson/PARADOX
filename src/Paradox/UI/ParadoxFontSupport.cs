using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Paradox.UI;

/// <summary>
/// Polish glyph support for PARADOX UI.
/// The stock Among Us TMP atlases do not contain the whole Polish alphabet.
/// We therefore create a runtime TMP font from a Windows font and assign it
/// directly to PARADOX labels.
/// </summary>
public static class ParadoxFontSupport
{
    private const string PolishCharacters = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ";

    private static TMP_FontAsset? _polishFont;
    private static float _nextAttemptAt;
    private static int _attemptCount;

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
        if (_polishFont != null)
            return;

        // Plugin Load happens before the game's UI/font engine is fully ready.
        // Never permanently give up after that early attempt; retry once the lobby is alive.
        var now = Time.realtimeSinceStartup;
        if (now < _nextAttemptAt)
            return;

        _nextAttemptAt = now + 2f;
        _attemptCount++;

        try
        {
            // Path-based Font construction is the most reliable route in current
            // IL2CPP Among Us builds; the OS font factory can be stripped at runtime.
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
                    var source = new Font(path);
                    source.name = "PARADOX Polish Source";

                    asset = TMP_FontAsset.CreateFontAsset(
                        source,
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
                        $"PARADOX Polish font path failed ({path}): {e.Message}");
                }

                if (TryAccept(asset, path))
                    return;
            }

            // Secondary route for Unity builds where CreateDynamicFontFromOSFont survives stripping.
            var families = new[] { "Arial", "Segoe UI", "Tahoma", "Calibri" };

            foreach (var family in families)
            {
                TMP_FontAsset? asset = null;

                try
                {
                    var source = Font.CreateDynamicFontFromOSFont(family, 64);
                    if (source == null)
                        continue;

                    asset = TMP_FontAsset.CreateFontAsset(
                        source,
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
                        $"PARADOX Polish OS-font fallback failed ({family}): {e.Message}");
                }

                if (TryAccept(asset, family))
                    return;
            }

            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX Polish font attempt #{_attemptCount} failed; will retry after UI/font engine is ready.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX Polish font attempt #{_attemptCount} failed: {e}");
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
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX could not preload Polish glyphs from {source}: {e.Message}");
        }

        if (!SupportsPolish(asset))
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX font {source} was created but does not expose all Polish glyphs.");
            UnityEngine.Object.Destroy(asset);
            return false;
        }

        _polishFont = asset;
        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX Polish dynamic font READY from {source}; attempts={_attemptCount}.");
        return true;
    }

    public static void ApplyTo(TMP_Text? text)
    {
        if (text == null || ParadoxPlugin.Localizer.CurrentLanguage != Localization.Language.Polish)
            return;

        EnsurePolishGlyphs();

        if (_polishFont == null)
            return;

        if (text.font == null || text.font.Pointer != _polishFont.Pointer)
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
