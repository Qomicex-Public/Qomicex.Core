using System.Text.Json.Nodes;
using System.Text.Json;
using Qomicex.Core.Modules.Helpers.Resources.Expansion.CurseForge;
using Qomicex.Core.Modules.Helpers.Resources.Expansion.Modrinth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Qomicex.Core.Modules.Helpers.Resources.Expansion.Local.Shaders;

namespace Qomicex.Core.Modules.Helpers.Resources.Expansion.Local
{
    public class Resourcepack: LocalResourceBase
    {
        private readonly string _gameDirectory;
        private readonly string _version;
        private readonly bool _versionSegmented;
        private readonly string _apiKey;

        public Resourcepack(string gameDirectory, string version, bool versionSegmented, string apiKey)
        {
            _gameDirectory = gameDirectory;
            _version = version;
            _versionSegmented = versionSegmented;
            _apiKey = apiKey;
        }

        private List<string> GetResourcePackFiles()
        {
            string resourcepackDirectory = _versionSegmented
                ? Path.Combine(_gameDirectory, "versions", _version, "resourcepacks")
                : Path.Combine(_gameDirectory, "resourcepacks");

            if (!Directory.Exists(resourcepackDirectory))
                return new List<string>();

            var entries = new List<string>();
            entries.AddRange(Directory.GetFiles(resourcepackDirectory, "*.zip"));

            foreach (var dir in Directory.GetDirectories(resourcepackDirectory))
            {
                if (File.Exists(Path.Combine(dir, "pack.mcmeta")))
                    entries.Add(dir);
            }

            return entries;
        }

        private static JsonObject? ReadMcmetaFromZip(string zipPath)
        {
            var bytes = TryReadFileFromZip(zipPath, "pack.mcmeta");
            if (bytes == null)
                return null;

            try
            {
                string jsonContent = Encoding.UTF8.GetString(bytes);
                return JsonNode.Parse(jsonContent)!.AsObject();
            }
            catch
            {
                return null;
            }
        }

        private static JsonObject? ReadMcmetaFromFolder(string folderPath)
        {
            string mcmetaPath = Path.Combine(folderPath, "pack.mcmeta");
            if (!File.Exists(mcmetaPath))
                return null;

            try
            {
                string jsonContent = File.ReadAllText(mcmetaPath);
                return JsonNode.Parse(jsonContent)!.AsObject();
            }
            catch
            {
                return null;
            }
        }

        private static string ReadIconFromZip(string zipPath)
        {
            var bytes = TryReadFileFromZip(zipPath, "pack.png");
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            return Convert.ToBase64String(bytes);
        }

        private static string ReadIconFromFolder(string folderPath)
        {
            string iconPath = Path.Combine(folderPath, "pack.png");
            if (!File.Exists(iconPath))
                return string.Empty;

            try
            {
                byte[] bytes = File.ReadAllBytes(iconPath);
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }


        private static (string sha1, long cfHash) ComputeHashesForFile(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            string sha1;
            using (SHA1 sha1Obj = SHA1.Create())
            {
                byte[] hashBytes = sha1Obj.ComputeHash(fileBytes);
                sha1 = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            long cfHash = MurmurHash2(fileBytes);
            return (sha1, cfHash);
        }

        private static (string sha1, long cfHash) ComputeHashesForFolder(string folderPath)
        {
            using (var memStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                    {
                        string relativePath = Path.GetRelativePath(folderPath, file)
                            .Replace('\\', '/');
                        archive.CreateEntryFromFile(file, relativePath);
                    }
                }

                memStream.Position = 0;
                byte[] zipBytes = memStream.ToArray();
                string sha1;
                using (SHA1 sha1Obj = SHA1.Create())
                {
                    byte[] hashBytes = sha1Obj.ComputeHash(zipBytes);
                    sha1 = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
                long cfHash = MurmurHash2(zipBytes);
                return (sha1, cfHash);
            }
        }

        public async Task<List<ResourcePackInfo>> GetResourcePackList()
        {
            var entries = GetResourcePackFiles();
            Trace.WriteLine($"Fetching resource pack list: {_version}, dir: {(_versionSegmented ? Path.Combine(_gameDirectory, "versions", _version, "resourcepacks") : Path.Combine(_gameDirectory, "resourcepacks"))}");
            var sha1List = new List<string>();
            var mHashList = new List<long>();
            var packInfos = new List<ResourcePackInfo>();

            foreach (var entry in entries)
            {
                Trace.WriteLine($"Fetching resource pack: {entry}");
                bool isDirectory = Directory.Exists(entry);

                JsonObject? mcmeta = isDirectory
                    ? ReadMcmetaFromFolder(entry)
                    : ReadMcmetaFromZip(entry);

                string description = mcmeta?["pack"]?["description"]?.ToString() ?? "";
                int packFormat = mcmeta?["pack"]?["pack_format"]?.GetValue<int>() ?? 0;

                string icon = isDirectory
                    ? ReadIconFromFolder(entry)
                    : ReadIconFromZip(entry);

                string sha1;
                long cfHash;
                if (isDirectory)
                {
                    (sha1, cfHash) = ComputeHashesForFolder(entry);
                }
                else
                {
                    (sha1, cfHash) = ComputeHashesForFile(entry);
                }

                sha1List.Add(sha1);
                mHashList.Add(cfHash);

                string fallbackName = Path.GetFileNameWithoutExtension(entry);

                packInfos.Add(new ResourcePackInfo
                {
                    FilePath = entry,
                    IsDirectory = isDirectory,
                    Sha1Hash = sha1,
                    CFHash = cfHash,
                    Name = fallbackName,
                    Description = description,
                    PackFormat = packFormat,
                    Icon = icon
                });
            }

            var cfDict = new Dictionary<long, CurseForgeBase.FingerprintsFilesMeta>();
            var mrDict = new Dictionary<string, ModrinthBase.ProjectVersionInfo>();

            if (sha1List.Count > 0)
            {
                try
                {
                    CurseForgeBase cf = new CurseForgeBase(_apiKey, "", "");
                    cfDict = await cf.GetInfoFromHashesDictAsync(mHashList);
                }
                catch { }

                try
                {
                    ModrinthBase mr = new ModrinthBase();
                    mrDict = await mr.GetProjectVersionsFromHashesDictAsync(sha1List);
                }
                catch { }
            }

            foreach (var packInfo in packInfos)
            {
                if (cfDict.TryGetValue(packInfo.CFHash, out var cfMeta))
                {
                    packInfo.CurseForgeId = cfMeta.ModId;
                    packInfo.CurseForgeMeta = cfMeta;
                }

                if (mrDict.TryGetValue(packInfo.Sha1Hash, out var mrMeta))
                {
                    packInfo.ModrinthId = mrMeta.ProjectId ?? "";
                    packInfo.ModrinthMeta = mrMeta;
                    if (!string.IsNullOrEmpty(mrMeta.Name))
                        packInfo.Name = mrMeta.Name;
                    else
                    {
                        if (cfMeta?.ModId > 0)
                        {
                            var cf = new CurseForge.ResourcePacks(_apiKey);
                            packInfo.Name = cf.GetInfoAsync(cfMeta.ModId.ToString()).Result.Name;
                        }
                    }
                    if (!string.IsNullOrEmpty(mrMeta.VersionNumber))
                        packInfo.Version = mrMeta.VersionNumber;
                    else
                    {
                        if (cfMeta?.ModId > 0)
                        {
                            var cf = new CurseForge.ResourcePacks(_apiKey);
                            var file = cf.GetInfoAsync(cfMeta.ModId.ToString()).Result.Files.FirstOrDefault(d => d.FileId == cfMeta.FileId);
                            packInfo.Version = file?.FileName ?? string.Empty;
                        }
                    }
                }
            }

            return packInfos;
        }

        public class ResourcePackInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;

            public string FilePath { get; set; } = string.Empty;
            public bool IsDirectory { get; set; }
            public int PackFormat { get; set; }
            /// <summary>
            /// 资源包图标的Base64编码字符串，如果没有图标则为空字符串
            /// </summary>
            public string Icon { get; set; } = string.Empty;
            public int CurseForgeId { get; set; }
            public string ModrinthId { get; set; } = string.Empty;
            public string Sha1Hash { get; set; } = string.Empty;
            public long CFHash { get; set; }
            public CurseForgeBase.FingerprintsFilesMeta? CurseForgeMeta { get; set; }
            public ModrinthBase.ProjectVersionInfo? ModrinthMeta { get; set; }
        }
    }
}
