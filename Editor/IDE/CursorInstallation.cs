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
    internal class CursorInstallation : CodeEditorInstallation
    {
        protected override string EditorName => "Cursor";
        protected override string ExecutableName => "cursor";
        protected override string WindowsExecutablePattern => ".*Cursor.*\\.exe$";
        protected override string macOSAppPattern => ".*Cursor.*\\.app$";
        protected override string LinuxDesktopFileName => "cursor.desktop";

        protected override CodeEditorInstallation CreateInstallationInstance() => new CursorInstallation();

        protected override List<string> GetDefaultPaths()
        {
            var candidates = new List<string>();

#if UNITY_EDITOR_WIN
            var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            var programFiles = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            foreach (var basePath in new[] { localAppPath, programFiles })
            {
                candidates.Add(IOPath.Combine(basePath, "Cursor", "Cursor.exe"));
            }
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Cursor*.app"));
			candidates.Add("/Applications/Cursor.app");
#elif UNITY_EDITOR_LINUX
			candidates.Add("/usr/bin/cursor");
			candidates.Add("/bin/cursor");
			candidates.Add("/usr/local/bin/cursor");
			candidates.AddRange(GetXdgCandidates());
#endif

            return candidates;
        }

        public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
        {
            return new CursorInstallation().TryDiscoverInstallationInternal(editorPath, out installation);
        }

        public static IEnumerable<IVisualStudioInstallation> GetInstallations()
        {
            return new CursorInstallation().GetInstallationsInternal();
        }

        public new static void Initialize()
        {
            CodeEditorInstallation.Initialize();
        }
    }
}
