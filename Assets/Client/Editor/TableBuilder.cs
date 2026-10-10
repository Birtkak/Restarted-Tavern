using System.IO;
using RestartedTavern.Client.Table;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RestartedTavern.Client.Editor
{
    /// <summary>
    /// Creates the visual table scene (CLIENT_DESIGN §3) and builds it for Windows. Usable from the menu or headless:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.TableBuilder.BuildWindows
    /// The table's UI is built at runtime by <see cref="TableView"/>, so the scene only holds a camera, the event
    /// system and the TableView.
    /// </summary>
    public static class TableBuilder
    {
        public const string ScenePath = "Assets/Scenes/Table.unity";
        public const string WindowsBuildPath = "Builds/Table/RestartedTavern.exe";

        [MenuItem("Restarted Tavern/Create Table Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.18f, 0.11f, 0.06f);
            camera.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("Table").AddComponent<TableView>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            PlayerSettings.companyName = "Birtkak";
            PlayerSettings.productName = "Restarted Tavern";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(DebugTableBuilder.ScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + ScenePath);
        }

        [MenuItem("Restarted Tavern/Build Windows Table")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateScene(); // the menu item recreates it when this script changes
            // No "Made with Unity" intro: the game opens on its own loading screen (user request 2026-10-10).
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
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
