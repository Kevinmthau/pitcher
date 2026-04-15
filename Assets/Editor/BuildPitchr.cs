using System;
using System.IO;

using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Pitchr.Editor
{
    public static class BuildPitchr
    {
        private const string ApkPath = "Builds/Android/Pitchr.apk";

        public static void PerformAndroidBuild()
        {
            if (!Directory.Exists("Builds/Android"))
            {
                Directory.CreateDirectory("Builds/Android");
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                target = BuildTarget.Android,
                locationPathName = ApkPath,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Pitchr Android build failed: {report.summary.result}");
            }

            UnityEngine.Debug.Log($"Pitchr APK built at {Path.GetFullPath(ApkPath)}");
        }
    }
}
