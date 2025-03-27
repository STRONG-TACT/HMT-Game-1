using UnityEditor;
using System.IO;
public static class AutoBuild
{          
        public static void PerformMacBuild()
        {
            string buildPath = "build/StandaloneOSX/DiceAdventure.app";
            Directory.CreateDirectory("build/StandaloneOSX");
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneOSX, BuildOptions.None);
        }

        public static void PerformWindowsBuild()
        {
            string buildPath = "build/StandaloneWindows64/DiceAdventure.exe";
            Directory.CreateDirectory("build/StandaloneWindows64");
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        }

        public static void PerformWebGLBuild()
        {
            string buildPath = "build//DiceAdventureWebGL";
            Directory.CreateDirectory(buildPath);
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.WebGL, BuildOptions.None);
        }
    
}
