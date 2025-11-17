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
    internal class QoderInstallation : CodeEditorInstallation
    {
        protected override string EditorName => "Qoder";
        protected override string ExecutableName => "qoder";
        protected override string WindowsExecutablePattern => ".*Qoder.*\\.exe$";
        protected override string macOSAppPattern => ".*Qoder.*\\.app$";
        protected override string LinuxDesktopFileName => "qoder.desktop";

        protected override CodeEditorInstallation CreateInstallationInstance() => new QoderInstallation();

        protected override List<string> GetDefaultPaths()
        {
            var candidates = new List<string>();

#if UNITY_EDITOR_WIN
            var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            var programFiles = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            foreach (var basePath in new[] { localAppPath, programFiles })
            {
                candidates.Add(IOPath.Combine(basePath, "Qoder", "Qoder.exe"));
            }
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Qoder*.app"));
#elif UNITY_EDITOR_LINUX
			candidates.Add("/usr/bin/qoder");
			candidates.Add("/bin/qoder");
			candidates.Add("/usr/local/bin/qoder");
			candidates.AddRange(GetXdgCandidates());
#endif

            return candidates;
        }

        public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
        {
            return new QoderInstallation().TryDiscoverInstallationInternal(editorPath, out installation);
        }

        public static IEnumerable<IVisualStudioInstallation> GetInstallations()
        {
            return new QoderInstallation().GetInstallationsInternal();
        }

        public new static void Initialize()
        {
            CodeEditorInstallation.Initialize();
        }
    }
}
