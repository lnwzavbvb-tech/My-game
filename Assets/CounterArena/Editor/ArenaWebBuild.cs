using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ArenaWebBuild
{
    public const string Version = "0.2.1-pixel-restart-fix";
    public static void Build()
    {
        ArenaProjectBuilder.CreateScene();
        ArenaValidation.Run();
        string releaseVersion = Version + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        PlayerSettings.bundleVersion = releaseVersion;
        PlayerSettings.WebGL.template = "PROJECT:TeamPlaytest";
        // GitHub Pages does not provide custom Content-Encoding headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.initialMemorySize = 128;
        PlayerSettings.WebGL.maximumMemorySize = 1024;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.stripEngineCode = false;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        const string output = "Builds/WebGL";
        Directory.CreateDirectory(output);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/CounterArena/Scenes/Sector07.unity" },
            locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Web build failed: " + report.summary.result);
        File.WriteAllText(output + "/.nojekyll", "");
        File.WriteAllText(output + "/build-info.json", "{\"version\":\"" + releaseVersion + "\",\"builtAtUtc\":\"" + DateTime.UtcNow.ToString("o") + "\"}");
        AssetDatabase.SaveAssets();
        long totalBytes = 0;
        foreach (string file in Directory.GetFiles(output, "*", SearchOption.AllDirectories)) totalBytes += new FileInfo(file).Length;
        if (Directory.GetFiles(output + "/Build", "*.wasm").Length != 1) throw new Exception("WebAssembly output missing.");
        Debug.Log("ARENA_WEB_BUILD_PASSED " + totalBytes + " bytes / " + releaseVersion);
    }
}
