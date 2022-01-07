using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    /// <summary>
    /// This is needed as Unity throws "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
    /// when trying to build from Setup() steps in tests.
    /// </summary>
    public static class BuildMultiprocessTestPlayer
    {
        public const string MultiprocessBaseMenuName = "Netcode/Multiprocess Test";
        public const string BuildAndExecuteMenuName = MultiprocessBaseMenuName + "/Build Test Player #t";
        public const string MainSceneName = "MultiprocessTestScene";

        private static string BuildPathDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "MultiprocessTests");
        public static string BuildPath => Path.Combine(BuildPathDirectory, "MultiprocessTestPlayer");
        public const string BuildInfoFileName = "BuildInfo.json";

#if UNITY_EDITOR
        [MenuItem(BuildAndExecuteMenuName)]
        public static void BuildRelease()
        {
            var report = BuildPlayerUtility();
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }

        [MenuItem(MultiprocessBaseMenuName + "/Build Test Player (Debug)")]
        public static void BuildDebug()
        {
            var report = BuildPlayerUtility(BuildTarget.NoTarget, null, true);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }

        [MenuItem(MultiprocessBaseMenuName + "/Delete Test Build")]
        public static void DeleteBuild()
        {
            if (Directory.Exists(BuildPathDirectory))
            {
                Directory.Delete(BuildPathDirectory, recursive: true);
            }
            else
            {
                Debug.Log($"[{nameof(BuildMultiprocessTestPlayer)}] build directory does not exist ({BuildPathDirectory}) not deleting anything");
            }
        }

        private static BuildReport BuildPlayerUtility(BuildTarget buildTarget = BuildTarget.NoTarget, string buildPathExtension = null, bool buildDebug = false)
        {
            SaveBuildInfo(new BuildInfo() { BuildPath = BuildPath });

            if (buildTarget == BuildTarget.NoTarget)
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                {
                    buildPathExtension += ".exe";
                    buildTarget = BuildTarget.StandaloneWindows64;
                } else if (Application.platform == RuntimePlatform.OSXPlayer ||
                    Application.platform == RuntimePlatform.OSXEditor)
                {
                    buildPathExtension += ".app";
                    buildTarget = BuildTarget.StandaloneOSX;
                }
            }

            var buildPathToUse = BuildPath;
            buildPathToUse += buildPathExtension;

            var buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/MultiprocessTestScene.unity" };
            buildPlayerOptions.locationPathName = buildPathToUse;
            buildPlayerOptions.target = buildTarget;
            var buildOptions = BuildOptions.None;
            if (buildDebug)
            {
                buildOptions |= BuildOptions.Development;
                buildOptions |= BuildOptions.AllowDebugging;
            }

            buildOptions |= BuildOptions.StrictMode;
            buildOptions |= BuildOptions.IncludeTestAssemblies;
            buildPlayerOptions.options = buildOptions;

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize} bytes at {summary.outputPath}");
            }

            return report;
        }

        [MenuItem(MultiprocessBaseMenuName + "/Windows Standalone Player")]
        public static void BuildWindowsStandalonePlayer()
        {
            var report = BuildPlayerUtility(BuildTarget.StandaloneWindows64, ".exe");
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }

        [MenuItem(MultiprocessBaseMenuName + "/Build OSX")]
        public static void BuildOSX()
        {
            var report = BuildPlayerUtility(BuildTarget.StandaloneOSX, ".app");
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }
        
#endif

        [Serializable]
        public struct BuildInfo
        {
            public string BuildPath;
            public bool IsDebug;
        }

        public static bool DoesBuildInfoExist()
        {
            var buildfileInfo = new FileInfo(Path.Combine(Application.streamingAssetsPath, BuildInfoFileName));
            return buildfileInfo.Exists;
        }

        public static BuildInfo ReadBuildInfo()
        {
            var jsonString = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, BuildInfoFileName));
            return JsonUtility.FromJson<BuildInfo>(jsonString);
        }

        public static void SaveBuildInfo(BuildInfo toSave)
        {
            var buildInfoJson = JsonUtility.ToJson(toSave);
            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, BuildInfoFileName), buildInfoJson);
        }

        /// <summary>
        /// Determine if the MultiprocessTestPlayer has been built so we can decide how to
        /// let the test runner know
        /// </summary>
        /// <returns></returns>
        public static bool IsMultiprocessTestPlayerAvailable()
        {
            bool answer = false;
            try
            {
                var buildInfoPath = Path.Combine(Application.streamingAssetsPath, BuildInfoFileName);
                var buildInfoFileInfo = new FileInfo(buildInfoPath);
                if (!buildInfoFileInfo.Exists)
                {
                    return false;
                }
                var buildPath = ReadBuildInfo().BuildPath;
                FileInfo buildPathFileInfo = null;
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXEditor:
                        buildPathFileInfo = new FileInfo($"{buildPath}.app/Contents/MacOS/testproject");
                        break;
                    case RuntimePlatform.WindowsEditor:
                        buildPathFileInfo = new FileInfo($"{buildPath}.exe");
                        break;
                    case RuntimePlatform.LinuxEditor:
                        buildPathFileInfo = new FileInfo($"{buildPath}");
                        break;
                }

                if (buildPathFileInfo != null && buildPathFileInfo.Exists)
                {
                    answer = true;
                }

            }
            catch (Exception e)
            {
                Debug.LogException(e);
                answer = false;
            }

            return answer;
        }
    }
}
