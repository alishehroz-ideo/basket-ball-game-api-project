using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Command-line WebGL builder.
// Invoked headless via:
//   Unity.exe -quit -batchmode -projectPath <proj> -buildTarget WebGL \
//             -executeMethod WebGLBuilder.BuildWebGL -logFile <log>
public static class WebGLBuilder
{
    const string OutputPath = "Build/WebGL";

    [MenuItem("Build/WebGL (localhost)")]
    public static void BuildWebGL()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuilder] No enabled scenes in Build Settings; aborting.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Serve-from-localhost-friendly: no server-side Content-Encoding needed,
        // and a JS decompression fallback in case the host adds its own encoding.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;

        // DEBUG: surface real C# exceptions + managed stack traces in the browser
        // console (default minimal mode reports runtime errors as "undefined").
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        Debug.Log($"[WebGLBuilder] Building {scenes.Length} scene(s) -> {OutputPath}");
        foreach (var s in scenes) Debug.Log("  scene: " + s);

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[WebGLBuilder] BUILD SUCCEEDED ({summary.totalSize} bytes) at {OutputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[WebGLBuilder] BUILD FAILED: result={summary.result}, errors={summary.totalErrors}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
