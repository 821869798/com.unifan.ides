/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using IOPath = System.IO.Path;

namespace Microsoft.Unity.VisualStudio.Editor
{
    internal class AntigravityInstallation : CodeEditorInstallation
    {
        protected override string EditorName => "Antigravity";
        protected override string ExecutableName => "antigravity";
        protected override string WindowsExecutablePattern => ".*Antigravity.*\\.exe$";
        protected override string macOSAppPattern => ".*Antigravity.*\\.app$";
        protected override string LinuxDesktopFileName => "antigravity.desktop";

        protected override CodeEditorInstallation CreateInstallationInstance() => new AntigravityInstallation();

        protected override List<string> GetDefaultPaths()
        {
            var candidates = new List<string>();

#if UNITY_EDITOR_WIN
            var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            var programFiles = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            foreach (var basePath in new[] { localAppPath, programFiles })
            {
                candidates.Add(IOPath.Combine(basePath, "Antigravity", "Antigravity.exe"));
            }
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Antigravity*.app"));
			candidates.Add("/Applications/Antigravity.app");
#elif UNITY_EDITOR_LINUX
			candidates.Add("/usr/bin/antigravity");
			candidates.Add("/bin/antigravity");
			candidates.Add("/usr/local/bin/antigravity");
			candidates.AddRange(GetXdgCandidates());
#endif

            return candidates;
        }

        public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
        {
            return new AntigravityInstallation().TryDiscoverInstallationInternal(editorPath, out installation);
        }

        public static IEnumerable<IVisualStudioInstallation> GetInstallations()
        {
            return new AntigravityInstallation().GetInstallationsInternal();
        }

        public new static void Initialize()
        {
            CodeEditorInstallation.Initialize();
        }
    }
}
