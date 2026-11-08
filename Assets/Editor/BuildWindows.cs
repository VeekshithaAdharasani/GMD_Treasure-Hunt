using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class BuildWindows
{
    [MenuItem("Tools/BuildWindows")]
    public static void Run()
    {
        string[] scenes = new string[] { "Assets/Scenes/LostCave_Main.unity" };
        string outDir = "Builds/Win_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(outDir);
        BuildPlayerOptions opts = new BuildPlayerOptions();
        opts.scenes = scenes;
        opts.locationPathName = outDir + "/The Lost Cave.exe";
        opts.target = BuildTarget.StandaloneWindows64;
        opts.options = BuildOptions.None;
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("Build result: " + report.summary.result + " path=" + outDir);
    }
}