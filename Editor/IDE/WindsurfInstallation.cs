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
    internal class WindsurfInstallation : CodeEditorInstallation
    {
        protected override string EditorName => "Windsurf";
        protected override string ExecutableName => "windsurf";
        protected override string WindowsExecutablePattern => ".*Windsurf.*\\.exe$";
        protected override string macOSAppPattern => ".*Windsurf.*\\.app$";
        protected override string LinuxDesktopFileName => "windsurf.desktop";

        protected override CodeEditorInstallation CreateInstallationInstance() => new WindsurfInstallation();

        protected override List<string> GetDefaultPaths()
        {
            var candidates = new List<string>();

#if UNITY_EDITOR_WIN
            var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            var programFiles = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            foreach (var basePath in new[] { localAppPath, programFiles })
            {
                candidates.Add(IOPath.Combine(basePath, "Windsurf", "Windsurf.exe"));
            }
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Windsurf*.app"));
#elif UNITY_EDITOR_LINUX
			candidates.Add("/usr/bin/windsurf");
			candidates.Add("/bin/windsurf");
			candidates.Add("/usr/local/bin/windsurf");
			candidates.AddRange(GetXdgCandidates());
#endif

            return candidates;
        }

        public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
        {
            return new WindsurfInstallation().TryDiscoverInstallationInternal(editorPath, out installation);
        }

        public static IEnumerable<IVisualStudioInstallation> GetInstallations()
        {
            return new WindsurfInstallation().GetInstallationsInternal();
        }

        public new static void Initialize()
        {
            CodeEditorInstallation.Initialize();
        }
    }
}
