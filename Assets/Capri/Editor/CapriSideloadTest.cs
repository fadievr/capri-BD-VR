// Capri → Release → 3. Install built APK on headset: installs the APK from "2. Build release APK" on a USB-connected
// Quest with adb, the way SideQuest would, then opens it. Results go to the Console and Logs/install-local.txt.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace CapriRoom.EditorTools
{
    public static class CapriSideloadTest
    {
        private const string Package = "com.capriroom.quest";

        [MenuItem("Capri/Release/3. Install built APK on headset")]
        public static void InstallLocal()
        {
            string adb = FindAdb();
            string root = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string apk = Path.Combine(root, "Builds", "CapriRoom-v" + CapriRelease.Version + ".apk");
            string logPath = Path.Combine(root, "Logs", "install-local.txt");
            var log = new StringBuilder();
            log.AppendLine("apk: " + apk + (File.Exists(apk) ? "" : " (MISSING)"));
            if (adb != null && File.Exists(apk))
            {
                log.AppendLine("install:\n" + Adb(adb, $"install -r \"{apk}\""));
                log.AppendLine("after: " + Version(adb));
                Adb(adb, $"shell am force-stop {Package}");
                Adb(adb, $"shell monkey -p {Package} -c android.intent.category.LAUNCHER 1");
                log.AppendLine("launched");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllText(logPath, log.ToString());
            Debug.Log("[Capri] " + log.ToString().Replace("\n", " | "));
        }

        private static string Version(string adb)
        {
            var dump = Adb(adb, $"shell dumpsys package {Package}");
            var sb = new StringBuilder();
            foreach (var line in dump.Split('\n'))
                if (line.Contains("versionName=") || line.Contains("lastUpdateTime=")) sb.Append(line.Trim()).Append("  ");
            return sb.Length > 0 ? sb.ToString() : "not installed";
        }

        private static string Adb(string adb, string args)
        {
            var start = new ProcessStartInfo(adb, args)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using (var p = Process.Start(start))
            {
                string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit(180000);
                return o;
            }
        }

        private static string FindAdb()
        {
            string sdk = null;
            try { sdk = UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath; } catch { }
            var candidates = new[]
            {
                sdk == null ? null : Path.Combine(sdk, "platform-tools", "adb.exe"),
                Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "SDK", "platform-tools", "adb.exe"),
            };
            foreach (var c in candidates) if (c != null && File.Exists(c)) return c;
            return null;
        }
    }
}
