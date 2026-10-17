using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mepOS.Loader
{
    /// <summary>
    /// Silent, Non-blocking HTTP Auto-Updater for mepOS.
    /// Uses native .NET WebClient with TLS 1.2. Zero third-party dependencies.
    /// Runs purely on background threads without delaying Revit startup.
    /// </summary>
    public static class UpdateManager
    {
        // Public GitHub distribution endpoint
        public const string DefaultManifestUrl = 
            "https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main/version.json";

        public static void CheckForUpdatesAsync(string installDir)
        {
            Task.Run(() =>
            {
                try
                {
                    PerformUpdateCheck(installDir);
                }
                catch
                {
                    // Silent fail - offline mode or network timeout must never disrupt user
                }
            });
        }

        private static void PerformUpdateCheck(string installDir)
        {
            if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir)) return;

            // Ensure modern TLS 1.2 is active for GitHub HTTPS
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

            string localVersionFile = Path.Combine(installDir, "version.json");
            string localVersionStr = "1.0.0";
            if (File.Exists(localVersionFile))
            {
                try
                {
                    string localJson = File.ReadAllText(localVersionFile);
                    string v = ExtractJsonValue(localJson, "version");
                    if (!string.IsNullOrEmpty(v)) localVersionStr = v;
                }
                catch { }
            }

            // 1. Fetch remote version.json with 3-second timeout
            string remoteJson = null;
            var request = (HttpWebRequest)WebRequest.Create(DefaultManifestUrl);
            request.Timeout = 3000;
            request.ReadWriteTimeout = 3000;
            request.UserAgent = "mepOS-Revit-Suite-Updater";
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK) return;
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    remoteJson = reader.ReadToEnd();
                }
            }

            if (string.IsNullOrEmpty(remoteJson)) return;

            string remoteVersionStr = ExtractJsonValue(remoteJson, "version");
            string coreUrl = ExtractJsonValue(remoteJson, "coreDownloadUrl");
            string loaderUrl = ExtractJsonValue(remoteJson, "loaderDownloadUrl");

            if (string.IsNullOrEmpty(remoteVersionStr) || string.IsNullOrEmpty(coreUrl)) return;

            // 2. Compare SemVer versions
            Version localVer, remoteVer;
            if (!Version.TryParse(NormalizeVersion(localVersionStr), out localVer) ||
                !Version.TryParse(NormalizeVersion(remoteVersionStr), out remoteVer))
            {
                return;
            }

            if (remoteVer <= localVer)
            {
                // Already up to date
                return;
            }

            // 3. Download & Hot-Swap mepOS.Core.dll (Never locked by Revit)
            string coreTarget = Path.Combine(installDir, "mepOS.Core.dll");
            string coreTmp = Path.Combine(installDir, "mepOS.Core.dll.tmp");

            using (var wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "mepOS-Revit-Suite-Updater");
                wc.DownloadFile(coreUrl, coreTmp);
            }

            var fiCore = new FileInfo(coreTmp);
            if (fiCore.Exists && fiCore.Length > 5000)
            {
                // Atomically overwrite active core file on disk
                File.Copy(coreTmp, coreTarget, true);
                try { File.Delete(coreTmp); } catch { }

                // Download PDB if available for diagnostic stack traces
                try
                {
                    string pdbUrl = coreUrl.Replace(".dll", ".pdb");
                    string pdbTarget = Path.Combine(installDir, "mepOS.Core.pdb");
                    using (var wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "mepOS-Revit-Suite-Updater");
                        wc.DownloadFile(pdbUrl, pdbTarget);
                    }
                }
                catch { }

                // 4. Check if mepOS.Loader.dll needs hot-swap update
                if (!string.IsNullOrEmpty(loaderUrl))
                {
                    try
                    {
                        string loaderTarget = Path.Combine(installDir, "mepOS.Loader.dll");
                        string loaderTmp = Path.Combine(installDir, "mepOS.Loader.dll.tmp");
                        string loaderOld = Path.Combine(installDir, "mepOS.Loader.dll.old");

                        using (var wc = new WebClient())
                        {
                            wc.Headers.Add("User-Agent", "mepOS-Revit-Suite-Updater");
                            wc.DownloadFile(loaderUrl, loaderTmp);
                        }

                        var fiLoader = new FileInfo(loaderTmp);
                        if (fiLoader.Exists && fiLoader.Length > 3000)
                        {
                            if (File.Exists(loaderOld)) File.Delete(loaderOld);
                            File.Move(loaderTarget, loaderOld);
                            File.Move(loaderTmp, loaderTarget);
                        }
                    }
                    catch { }
                }

                // 5. Save updated version.json
                File.WriteAllText(localVersionFile, remoteJson);
            }
        }

        private static string ExtractJsonValue(string json, string key)
        {
            try
            {
                var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"([^\"]+)\"");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeVersion(string v)
        {
            v = (v ?? "").Trim().TrimStart('v', 'V');
            int dotCount = v.Split('.').Length - 1;
            if (dotCount == 0) return v + ".0.0.0";
            if (dotCount == 1) return v + ".0.0";
            if (dotCount == 2) return v + ".0";
            return v;
        }
    }
}
