// Editor tool: convert the Nunito .ttf fonts into TextMeshPro font assets (dynamic SDF).
// TMP font assets can't be authored by hand — they need TMP's font engine — so this runs the
// same TMP_FontAsset.CreateFontAsset(...) the Font Asset Creator uses, for every Nunito .ttf at once.
//
// Menu:  Tools ▸ TMP ▸ Convert Nunito Fonts to TMP
// Output: Assets/ali bhai (3)/ali bhai/Nunito/TMP/<Name> SDF.asset  (skips ones already made)

using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;

public static class NunitoTMPConverter
{
    const string NunitoRoot   = "Assets/ali bhai (3)/ali bhai/Nunito";
    const string OutputFolder = "Assets/ali bhai (3)/ali bhai/Nunito/TMP";

    [MenuItem("Tools/TMP/Convert Nunito Fonts to TMP")]
    public static void Convert()
    {
        if (!AssetDatabase.IsValidFolder(NunitoRoot))
        {
            Debug.LogError($"[Nunito→TMP] Source folder not found: {NunitoRoot}");
            return;
        }
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder(NunitoRoot, "TMP");

        // Every imported .ttf/.otf under the Nunito folder (root variable fonts + static weights).
        var guids = AssetDatabase.FindAssets("t:Font", new[] { NunitoRoot });
        int created = 0, skipped = 0, failed = 0;

        foreach (var guid in guids)
        {
            string ttfPath = AssetDatabase.GUIDToAssetPath(guid);
            string ext = Path.GetExtension(ttfPath).ToLowerInvariant();
            if (ext != ".ttf" && ext != ".otf") continue;

            string baseName = Path.GetFileNameWithoutExtension(ttfPath);
            string outPath  = $"{OutputFolder}/{baseName} SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath) != null) { skipped++; continue; }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null) { failed++; continue; }

            // Dynamic SDF font asset: 90pt sampling, 9px padding, 1024² atlas (TMP defaults).
            // The atlas fills on demand; the source .ttf must stay in the project.
            var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (fontAsset == null) { failed++; Debug.LogWarning($"[Nunito→TMP] CreateFontAsset failed: {baseName}"); continue; }

            fontAsset.name = baseName + " SDF";
            AssetDatabase.CreateAsset(fontAsset, outPath);

            // Save the atlas texture + material as sub-assets so the font asset is self-contained.
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                fontAsset.atlasTextures[0].name = baseName + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = baseName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            EditorUtility.SetDirty(fontAsset);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Nunito→TMP] Done — created {created}, skipped {skipped} (already existed), failed {failed}. Output: {OutputFolder}");
    }
}
