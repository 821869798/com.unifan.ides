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
    internal class TraeCNInstallation : CodeEditorInstallation
    {
        protected override string EditorName => "Trae CN";
        protected override string ExecutableName => "trae-cn";
        protected override string WindowsExecutablePattern => ".*Trae CN.*\\.exe$";
        protected override string macOSAppPattern => ".*Trae CN.*\\.app$";
        protected override string LinuxDesktopFileName => "trae-cn.desktop";

        protected override CodeEditorInstallation CreateInstallationInstance() => new TraeCNInstallation();

        protected override List<string> GetDefaultPaths()
        {
            var candidates = new List<string>();

#if UNITY_EDITOR_WIN
            var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            var programFiles = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            foreach (var basePath in new[] { localAppPath, programFiles })
            {
                candidates.Add(IOPath.Combine(basePath, "Trae CN", "Trae CN.exe"));
            }
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Trae CN*.app"));
#elif UNITY_EDITOR_LINUX
			candidates.Add("/usr/bin/trae-cn");
			candidates.Add("/bin/trae-cn");
			candidates.Add("/usr/local/bin/trae-cn");
			candidates.AddRange(GetXdgCandidates());
#endif

            return candidates;
        }

        public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
        {
            return new TraeCNInstallation().TryDiscoverInstallationInternal(editorPath, out installation);
        }

        public static IEnumerable<IVisualStudioInstallation> GetInstallations()
        {
            return new TraeCNInstallation().GetInstallationsInternal();
        }

        public new static void Initialize()
        {
            CodeEditorInstallation.Initialize();
        }
    }
}
