using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Qomicex.Core.Modules.Helpers.Resources.Expansion.Local
{
    public class LocalResourceBase
    {
        // CurseForge 专用 MurmurHash2：先过滤空白字节再计算
        public static long CurseForgeFingerprint(byte[] data)
        {
            // 过滤 0x09 (Tab), 0x0A (LF), 0x0D (CR), 0x20 (Space)
            var filtered = data.Where(b => b != 0x09 && b != 0x0A && b != 0x0D && b != 0x20).ToArray();
            return MurmurHash2(filtered, 1);
        }

        // 标准 Murmur2 算法实现 (32-bit)
        public static long MurmurHash2(byte[] data, uint seed = 1)
        {
            const uint m = 0x5bd1e995;
            const int r = 24;
            uint len = (uint)data.Length;
            uint h = seed ^ len;
            int i = 0;

            while (len >= 4)
            {
                uint k = BitConverter.ToUInt32(data, i);
                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;

                i += 4;
                len -= 4;
            }

            switch (len)
            {
                case 3: h ^= (uint)(data[i + 2] << 16); goto case 2;
                case 2: h ^= (uint)(data[i + 1] << 8); goto case 1;
                case 1: h ^= data[i]; h *= m; break;
            }

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }

        /// <summary>
        /// 尝试从 zip 中读取指定文件，未找到时返回 null 而不是抛异常
        /// </summary>
        internal static byte[]? TryReadFileFromZip(string path, string fileName)
        {
            try
            {
                using (var fileStream = File.OpenRead(path))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    var entry = archive.Entries.FirstOrDefault(e =>
                        e.FullName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                    if (entry == null)
                        return null;

                    using (var entryStream = entry.Open())
                    using (var memoryStream = new MemoryStream())
                    {
                        entryStream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
