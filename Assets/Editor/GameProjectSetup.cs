using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SherrysGame.Editor
{
    [InitializeOnLoad]
    internal static class GameProjectSetup
    {
        static GameProjectSetup()
        {
            EditorApplication.delayCall += Configure;
        }

        private static void Configure()
        {
            PlayerSettings.companyName = "SherrysGame";
            PlayerSettings.productName = "Street Match (N & S): OK County — Study Aid";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 1000;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            // This project uses uGUI's legacy input module, including touch support.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty input = settings.FindProperty("activeInputHandler");
            if (input != null && input.intValue != 0)
            {
                input.intValue = 0;
                settings.ApplyModifiedProperties();
            }
        }

        [MenuItem("Street Match (N & S): OK County — Study Aid/Open Game Scene")]
        private static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Scenes/SherrysGame.unity");
        }

        [MenuItem("Street Match (N & S): OK County — Study Aid/Build WebGL")]
        public static void BuildWebGL()
        {
            Configure();
            Directory.CreateDirectory("Builds/WebGL");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SherrysGame.unity" },
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException("WebGL build failed: " + report.summary.result);
            Debug.Log("Street Match (N & S): OK County — Study Aid built to Builds/WebGL.");
        }
    }
}
