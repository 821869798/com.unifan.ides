/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using SimpleJSON;
using IOPath = System.IO.Path;
using System.IO.Compression;

namespace Microsoft.Unity.VisualStudio.Editor
{
    internal abstract class CodeEditorInstallation : VisualStudioInstallation
    {
        protected abstract string EditorName { get; }
        protected abstract string ExecutableName { get; }
        protected abstract string WindowsExecutablePattern { get; }
        protected abstract string macOSAppPattern { get; }
        protected abstract string LinuxDesktopFileName { get; }

        private static readonly IGenerator _generator = GeneratorFactory.GetInstance(GeneratorStyle.SDK);

        public override bool SupportsAnalyzers => true;
        public override Version LatestLanguageVersionSupported => new Version(13, 0);

        private string GetExtensionPath()
        {
            var vscode = IsPrerelease ? ".vscode-insiders" : ".vscode";
            var extensionsPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), vscode, "extensions");
            if (!Directory.Exists(extensionsPath))
                return null;

            return Directory
                .EnumerateDirectories(extensionsPath, $"{MicrosoftUnityExtensionId}*")
                .OrderByDescending(n => n)
                .FirstOrDefault();
        }

        public override string[] GetAnalyzers()
        {
            var vstuPath = GetExtensionPath();
            return string.IsNullOrEmpty(vstuPath) ? Array.Empty<string>() : VisualStudioInstallation.GetAnalyzers(vstuPath);
        }

        public override IGenerator ProjectGenerator => _generator;

        protected bool IsCandidateForDiscovery(string path)
        {
#if UNITY_EDITOR_OSX
			return Directory.Exists(path) && Regex.IsMatch(path, macOSAppPattern, RegexOptions.IgnoreCase);
#elif UNITY_EDITOR_WIN
            return File.Exists(path) && Regex.IsMatch(path, WindowsExecutablePattern, RegexOptions.IgnoreCase);
#else
			return File.Exists(path) && path.EndsWith(ExecutableName, StringComparison.OrdinalIgnoreCase);
#endif
        }

        public bool TryDiscoverInstallationInternal(string editorPath, out IVisualStudioInstallation installation)
        {
            installation = null;

            if (string.IsNullOrEmpty(editorPath) || !IsCandidateForDiscovery(editorPath))
                return false;

            Version version = null;
            var isPrerelease = false;

            try
            {
                var manifestBase = GetRealPath(editorPath);

#if UNITY_EDITOR_WIN
                manifestBase = IOPath.GetDirectoryName(manifestBase);
#elif UNITY_EDITOR_OSX
				manifestBase = IOPath.Combine(manifestBase, "Contents");
#else
				var parent = Directory.GetParent(manifestBase);
				manifestBase = parent?.Name == "bin" ? parent.Parent?.FullName : parent?.FullName;
#endif

                if (manifestBase == null)
                    return false;

                var manifestFullPath = IOPath.Combine(manifestBase, "resources", "app", "package.json");
                if (File.Exists(manifestFullPath))
                {
                    var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestFullPath));
                    Version.TryParse(manifest.version.Split('-').First(), out version);
                    isPrerelease = manifest.version.ToLower().Contains("insider");
                }
            }
            catch (Exception)
            {
            }

            isPrerelease = isPrerelease || editorPath.ToLower().Contains("insider");
            installation = CreateInstallationInstance();

            var inst = installation as CodeEditorInstallation;
            inst.IsPrerelease = isPrerelease;
            inst.Name = $"{EditorName}{(isPrerelease ? " - Insider" : string.Empty)}{(version != null ? $" [{version.ToString(3)}]" : string.Empty)}";
            inst.Path = editorPath;
            inst.Version = version ?? new Version();

            return true;
        }

        protected abstract CodeEditorInstallation CreateInstallationInstance();

        [Serializable]
        protected class Manifest
        {
            public string name;
            public string version;
        }

        public IEnumerable<IVisualStudioInstallation> GetInstallationsInternal()
        {
            var candidates = GetDefaultPaths();

            foreach (var candidate in candidates.Distinct())
            {
                if (TryDiscoverInstallationInternal(candidate, out var installation))
                    yield return installation;
            }
        }

        protected abstract List<string> GetDefaultPaths();

#if UNITY_EDITOR_LINUX
		private static readonly Regex DesktopFileExecEntry = new Regex(@"Exec=(\S+)", RegexOptions.Singleline | RegexOptions.Compiled);

		protected IEnumerable<string> GetXdgCandidates()
		{
			var envdirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
			if (string.IsNullOrEmpty(envdirs))
				yield break;

			foreach (var dir in envdirs.Split(':'))
			{
				try
				{
					var desktopFile = IOPath.Combine(dir, $"applications/{LinuxDesktopFileName}");
					if (!File.Exists(desktopFile))
						continue;

					var match = DesktopFileExecEntry.Match(File.ReadAllText(desktopFile));
					if (match.Success)
					{
						yield return match.Groups[1].Value;
						break;
					}
				}
				catch
				{
				}
			}
		}

		[System.Runtime.InteropServices.DllImport("libc")]
		private static extern int readlink(string path, byte[] buffer, int buflen);

		internal static string GetRealPath(string path)
		{
			byte[] buf = new byte[512];
			int ret = readlink(path, buf, buf.Length);
			if (ret == -1) return path;
			char[] cbuf = new char[512];
			int chars = System.Text.Encoding.Default.GetChars(buf, 0, ret, cbuf, 0);
			return new String(cbuf, 0, chars);
		}
#else
        internal static string GetRealPath(string path) => path;
#endif

        public override void CreateExtraFiles(string projectDirectory)
        {
            try
            {
                var vscodeDirectory = IOPath.Combine(projectDirectory.NormalizePathSeparators(), ".vscode");
                Directory.CreateDirectory(vscodeDirectory);

                var enablePatch = !File.Exists(IOPath.Combine(vscodeDirectory, ".vstupatchdisable"));

                CreateRecommendedExtensionsFile(vscodeDirectory, enablePatch);
                CreateSettingsFile(vscodeDirectory, enablePatch);
                CreateLaunchFile(vscodeDirectory, enablePatch);
            }
            catch (IOException)
            {
            }
        }

        private const string DefaultLaunchFileContent = @"{
    ""version"": ""0.2.0"",
    ""configurations"": [
        {
            ""name"": ""Attach to Unity"",
            ""type"": ""vstuc"",
            ""request"": ""attach""
        }
     ]
}";

        private static void CreateLaunchFile(string vscodeDirectory, bool enablePatch)
        {
            var launchFile = IOPath.Combine(vscodeDirectory, "launch.json");
            if (File.Exists(launchFile))
            {
                if (enablePatch) PatchLaunchFile(launchFile);
                return;
            }

            File.WriteAllText(launchFile, DefaultLaunchFileContent);
        }

        private static void PatchLaunchFile(string launchFile)
        {
            try
            {
                var content = File.ReadAllText(launchFile);
                var launch = JSONNode.Parse(content);
                var configurations = launch["configurations"] as JSONArray;

                if (configurations == null)
                {
                    configurations = new JSONArray();
                    launch.Add("configurations", configurations);
                }

                if (configurations.Linq.Any(entry => entry.Value["type"]?.Value == "vstuc"))
                    return;

                var defaultContent = JSONNode.Parse(DefaultLaunchFileContent);
                configurations.Add(defaultContent["configurations"][0]);

                WriteAllTextFromJObject(launchFile, launch);
            }
            catch (Exception)
            {
            }
        }

        private void CreateSettingsFile(string vscodeDirectory, bool enablePatch)
        {
            var settingsFile = IOPath.Combine(vscodeDirectory, "settings.json");
            if (File.Exists(settingsFile))
            {
                if (enablePatch) PatchSettingsFile(settingsFile);
                return;
            }

            File.WriteAllText(settingsFile, GetDefaultSettingsContent());
        }

        private string GetDefaultSettingsContent()
        {
            const string excludes = @"    ""files.exclude"": {
        ""**/.DS_Store"": true,
        ""**/.git"": true,
        ""**/.vs"": true,
        ""**/.gitmodules"": true,
        ""**/.vsconfig"": true,
        ""**/*.booproj"": true,
        ""**/*.pidb"": true,
        ""**/*.suo"": true,
        ""**/*.user"": true,
        ""**/*.userprefs"": true,
        ""**/*.unityproj"": true,
        ""**/*.dll"": true,
        ""**/*.exe"": true,
        ""**/*.pdf"": true,
        ""**/*.mid"": true,
        ""**/*.midi"": true,
        ""**/*.wav"": true,
        ""**/*.gif"": true,
        ""**/*.ico"": true,
        ""**/*.jpg"": true,
        ""**/*.jpeg"": true,
        ""**/*.png"": true,
        ""**/*.psd"": true,
        ""**/*.tga"": true,
        ""**/*.tif"": true,
        ""**/*.tiff"": true,
        ""**/*.3ds"": true,
        ""**/*.3DS"": true,
        ""**/*.fbx"": true,
        ""**/*.FBX"": true,
        ""**/*.lxo"": true,
        ""**/*.LXO"": true,
        ""**/*.ma"": true,
        ""**/*.MA"": true,
        ""**/*.obj"": true,
        ""**/*.OBJ"": true,
        ""**/*.asset"": true,
        ""**/*.cubemap"": true,
        ""**/*.flare"": true,
        ""**/*.mat"": true,
        ""**/*.meta"": true,
        ""**/*.prefab"": true,
        ""**/*.unity"": true,
        ""build/"": true,
        ""Build/"": true,
        ""Library/"": true,
        ""library/"": true,
        ""obj/"": true,
        ""Obj/"": true,
        ""Logs/"": true,
        ""logs/"": true,
        ""ProjectSettings/"": true,
        ""UserSettings/"": true,
        ""temp/"": true,
        ""Temp/"": true
    }";

            return @$"{{
{excludes},
    ""files.associations"": {{
        ""*.asset"": ""yaml"",
        ""*.meta"": ""yaml"",
        ""*.prefab"": ""yaml"",
        ""*.unity"": ""yaml"",
    }},
    ""explorer.fileNesting.enabled"": true,
    ""explorer.fileNesting.patterns"": {{
        ""*.sln"": ""*.csproj"",
    }},
    ""dotnet.defaultSolution"": ""{IOPath.GetFileName(ProjectGenerator.SolutionFile())}""
}}";
        }

        private void PatchSettingsFile(string settingsFile)
        {
            try
            {
                var content = File.ReadAllText(settingsFile);
                var settings = JSONNode.Parse(content);
                var excludes = settings["files.exclude"] as JSONObject;

                if (excludes == null) return;

                var patchList = new List<string>();
                var patched = false;

                foreach (var exclude in excludes)
                {
                    if (!bool.TryParse(exclude.Value, out var exc) || !exc) continue;

                    var key = exclude.Key;
                    if (!key.EndsWith(".sln") && !key.EndsWith(".csproj")) continue;
                    if (!Regex.IsMatch(key, @"^(\*\*[\\/])?\*\.(sln|csproj)$")) continue;

                    patchList.Add(key);
                    patched = true;
                }

                var solutionFile = IOPath.GetFileName(ProjectGenerator.SolutionFile());
                if (settings["dotnet.defaultSolution"]?.Value != solutionFile)
                {
                    settings["dotnet.defaultSolution"] = solutionFile;
                    patched = true;
                }

                if (!patched) return;

                foreach (var patch in patchList) excludes.Remove(patch);
                WriteAllTextFromJObject(settingsFile, settings);
            }
            catch (Exception)
            {
            }
        }

        private const string MicrosoftUnityExtensionId = "visualstudiotoolsforunity.vstuc";
        private const string DefaultRecommendedExtensionsContent = "{\n    \"recommendations\": [\n      \"" + MicrosoftUnityExtensionId + "\"\n    ]\n}";

        private static void CreateRecommendedExtensionsFile(string vscodeDirectory, bool enablePatch)
        {
            var extensionFile = IOPath.Combine(vscodeDirectory, "extensions.json");
            if (File.Exists(extensionFile))
            {
                if (enablePatch) PatchRecommendedExtensionsFile(extensionFile);
                return;
            }

            File.WriteAllText(extensionFile, DefaultRecommendedExtensionsContent);
        }

        private static void PatchRecommendedExtensionsFile(string extensionFile)
        {
            try
            {
                var content = File.ReadAllText(extensionFile);
                var extensions = JSONNode.Parse(content);
                var recommendations = extensions["recommendations"] as JSONArray;

                if (recommendations == null)
                {
                    recommendations = new JSONArray();
                    extensions.Add("recommendations", recommendations);
                }

                if (recommendations.Linq.Any(entry => entry.Value.Value == MicrosoftUnityExtensionId))
                    return;

                recommendations.Add(MicrosoftUnityExtensionId);
                WriteAllTextFromJObject(extensionFile, extensions);
            }
            catch (Exception)
            {
            }
        }

        private static void WriteAllTextFromJObject(string file, JSONNode node)
        {
            using (var fs = File.Open(file, FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(node.ToString(aIndent: 4));
            }
        }

        public override bool Open(string path, int line, int column, string solution)
        {
            var application = Path;
            line = Math.Max(1, line);
            column = Math.Max(0, column);

            var directory = IOPath.GetDirectoryName(solution);
            var workspace = TryFindWorkspace(directory);
            var target = workspace ?? directory;

            ProcessRunner.Start(string.IsNullOrEmpty(path)
                ? ProcessStartInfoFor(application, $"\"{target}\"")
                : ProcessStartInfoFor(application, $"\"{target}\" -g \"{path}\":{line}:{column}"));

            return true;
        }

        private static string TryFindWorkspace(string directory)
        {
            var files = Directory.GetFiles(directory, "*.code-workspace", SearchOption.TopDirectoryOnly);
            return files.Length == 1 ? files[0] : null;
        }

        private static ProcessStartInfo ProcessStartInfoFor(string application, string arguments)
        {
#if UNITY_EDITOR_OSX
			arguments = $"-n \"{application}\" --args {arguments}";
			application = "open";
			return ProcessRunner.ProcessStartInfoFor(application, arguments, redirect: false, shell: true);
#else
            return ProcessRunner.ProcessStartInfoFor(application, arguments, redirect: false);
#endif
        }

        public static void Initialize() { }
    }
}
