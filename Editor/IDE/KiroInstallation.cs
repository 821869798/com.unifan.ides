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
    internal class KiroInstallation : CodeEditorInstallation
    {
        protected override string EditorName => "Kiro";
        protected override string ExecutableName => "kiro";
        protected override string WindowsExecutablePattern => ".*Kiro.*\\.exe$";
        protected override string macOSAppPattern => ".*Kiro.*\\.app$";
        protected override string LinuxDesktopFileName => "kiro.desktop";

        protected override CodeEditorInstallation CreateInstallationInstance() => new KiroInstallation();

        protected override List<string> GetDefaultPaths()
        {
            var candidates = new List<string>();

#if UNITY_EDITOR_WIN
            var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            var programFiles = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            foreach (var basePath in new[] { localAppPath, programFiles })
            {
                candidates.Add(IOPath.Combine(basePath, "Kiro", "Kiro.exe"));
            }
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Kiro*.app"));
#elif UNITY_EDITOR_LINUX
			candidates.Add("/usr/bin/kiro");
			candidates.Add("/bin/kiro");
			candidates.Add("/usr/local/bin/kiro");
			candidates.AddRange(GetXdgCandidates());
#endif

            return candidates;
        }

        public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
        {
            return new KiroInstallation().TryDiscoverInstallationInternal(editorPath, out installation);
        }

        public static IEnumerable<IVisualStudioInstallation> GetInstallations()
        {
            return new KiroInstallation().GetInstallationsInternal();
        }

        public new static void Initialize()
        {
            CodeEditorInstallation.Initialize();
        }
    }
}
