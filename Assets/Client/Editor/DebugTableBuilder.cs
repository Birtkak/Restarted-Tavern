using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RestartedTavern.Client.Editor
{
    /// <summary>
    /// Creates the debug table scene and builds it for Windows. Usable from the menu or headless:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.DebugTableBuilder.BuildWindows
    /// </summary>
    public static class DebugTableBuilder
    {
        public const string ScenePath = "Assets/Scenes/DebugTable.unity";
        public const string WindowsBuildPath = "Builds/DebugTable/RestartedTavern.exe";

        [MenuItem("Restarted Tavern/Create Debug Table Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f);

            new GameObject("Debug Table").AddComponent<DebugTable>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "Birtkak";
            PlayerSettings.productName = "Restarted Tavern";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + ScenePath);
        }

        [MenuItem("Restarted Tavern/Build Windows Debug Table")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log("Build result: " + report.summary.result + ", " + report.summary.totalErrors + " errors");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
