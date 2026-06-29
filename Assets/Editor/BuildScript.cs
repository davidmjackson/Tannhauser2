using System.Linq;
using UnityEditor;
using UnityEngine;

// Command-line build helper for Tannhauser.
// Run from the command line (Editor must be closed) with:
//   Unity.exe -quit -batchmode -projectPath "<project>" -executeMethod BuildScript.BuildWindows -logFile <log>
// Produces a standalone Windows x64 build at Builds/Windows/Tannhauser.exe.
public static class BuildScript
{
    public static void BuildWindows()
    {
        // Use whatever scenes are enabled in File > Build Settings.
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Windows/Tannhauser.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"BUILD SUCCEEDED: {summary.totalSize} bytes at {options.locationPathName}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"BUILD FAILED: {summary.result} ({summary.totalErrors} errors)");
            EditorApplication.Exit(1);
        }
    }
}
