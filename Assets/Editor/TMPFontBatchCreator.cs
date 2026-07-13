// TMPFontBatchCreator.cs
// Batch-creates TextMeshPro font assets (SDF) from the Noto Sans JP + Nunito TTFs,
// the same kind of asset the "Window > TextMeshPro > Font Asset Creator" tool produces.
//
// Japanese (Noto Sans JP) has thousands of glyphs (Kanji + Hiragana + Katakana), so a STATIC
// atlas is impractical. These are created in DYNAMIC atlas mode: the atlas starts empty and
// TMP rasterizes each glyph on demand at runtime (works in WebGL, keeps the build small).
// Latin (Nunito) is created dynamic too for consistency.
//
// Run from the menu:  Tools > TMP Fonts > Create JP + Nunito SDF (Dynamic)
// Re-running is safe: assets that already exist are skipped (use the "OVERWRITE" item to force).

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

public static class TMPFontBatchCreator
{
    // Each source "static" folder -> a sibling "TMP" folder gets the generated "<Name> SDF.asset".
    static readonly string[] SourceStaticFolders =
    {
        "Assets/ali bhai (3)/ali bhai/Noto_Sans_JP/static",
        "Assets/ali bhai (3)/ali bhai/Nunito/static",
    };

    // Atlas / SDF settings (match the Font Asset Creator defaults for a crisp UI SDF).
    const int  PointSize  = 90;
    const int  Padding    = 9;
    const int  AtlasW     = 1024;
    const int  AtlasH     = 1024;

    [MenuItem("Tools/TMP Fonts/Create JP + Nunito SDF (Dynamic)")]
    static void CreateAll() => Run(overwrite: false);

    [MenuItem("Tools/TMP Fonts/Create JP + Nunito SDF (Dynamic) - OVERWRITE")]
    static void CreateAllOverwrite() => Run(overwrite: true);

    static void Run(bool overwrite)
    {
        int made = 0, skipped = 0, failed = 0;
        foreach (var folder in SourceStaticFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[TMPFonts] Source folder not found: {folder}");
                continue;
            }

            // Output "<parent>/TMP" (e.g. .../Noto_Sans_JP/TMP)
            string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
            string outDir = parent + "/TMP";
            if (!AssetDatabase.IsValidFolder(outDir))
                AssetDatabase.CreateFolder(parent, "TMP");

            foreach (var ttf in Directory.GetFiles(folder, "*.ttf"))
            {
                string file = Path.GetFileName(ttf);
                // Skip italics and variable fonts (TMP can't use variable fonts properly).
                if (file.IndexOf("Italic", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (file.IndexOf("VariableFont", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;

                string ttfAssetPath = folder + "/" + file;
                string name    = Path.GetFileNameWithoutExtension(file) + " SDF";
                string outPath = outDir + "/" + name + ".asset";

                if (!overwrite && AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath) != null)
                {
                    skipped++;
                    continue;
                }

                if (CreateOne(ttfAssetPath, outPath, name)) made++; else failed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMPFonts] Done. Created={made}, Skipped(existing)={skipped}, Failed={failed}. " +
                  "Assets are in each font's /TMP folder (Dynamic mode — Japanese glyphs render at runtime).");
        EditorUtility.DisplayDialog("TMP Font Assets",
            $"Created: {made}\nSkipped (already existed): {skipped}\nFailed: {failed}\n\n" +
            "Generated as DYNAMIC SDF assets in each font's /TMP folder.", "OK");
    }

    static bool CreateOne(string ttfAssetPath, string outPath, string name)
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfAssetPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[TMPFonts] Not importable as a Font (check import settings): {ttfAssetPath}");
            return false;
        }

        // Same call the Font Asset Creator makes, in DYNAMIC mode.
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont, PointSize, Padding, GlyphRenderMode.SDFAA,
            AtlasW, AtlasH, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);

        if (fontAsset == null)
        {
            Debug.LogError($"[TMPFonts] CreateFontAsset returned null for {ttfAssetPath}");
            return false;
        }

        fontAsset.name = name;

        AssetDatabase.CreateAsset(fontAsset, outPath);

        // Embed the atlas texture(s) + material as sub-assets so the .asset is self-contained
        // (exactly like the Font Asset Creator output).
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var tex = fontAsset.atlasTextures[i];
                if (tex == null) continue;
                tex.name = name + " Atlas" + (i == 0 ? "" : " " + i);
                AssetDatabase.AddObjectToAsset(tex, fontAsset);
            }
        }
        if (fontAsset.material != null)
        {
            fontAsset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        return true;
    }
}
#endif
