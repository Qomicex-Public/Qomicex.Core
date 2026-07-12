using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Qomicex.Core.Modules.Helpers.Resources.Expansion.Local
{
    public class LocalResourceBase
    {
        // CurseForge 专用 fingerprint：过滤空白字节 + MurmurHash2 64-bit
        public static long CurseForgeFingerprint(byte[] data)
        {
            var filtered = data.Where(b => b != 0x09 && b != 0x0A && b != 0x0D && b != 0x20).ToArray();
            return MurmurHash2(filtered, 1);
        }

        // MurmurHash2 64-bit (MurmurHash64A)
        public static long MurmurHash2(byte[] data, uint seed = 1)
        {
            const ulong m = 0xc6a4a7935bd1e995;
            const int r = 47;
            int len = data.Length;
            ulong h = seed ^ ((ulong)len * m);
            int i = 0;

            while (len >= 8)
            {
                ulong k = BitConverter.ToUInt64(data, i);
                k *= m;
                k ^= k >> r;
                k *= m;

                h ^= k;
                h *= m;

                i += 8;
                len -= 8;
            }

            switch (len)
            {
                case 7: h ^= (ulong)data[i + 6] << 48; goto case 6;
                case 6: h ^= (ulong)data[i + 5] << 40; goto case 5;
                case 5: h ^= (ulong)data[i + 4] << 32; goto case 4;
                case 4: h ^= (ulong)data[i + 3] << 24; goto case 3;
                case 3: h ^= (ulong)data[i + 2] << 16; goto case 2;
                case 2: h ^= (ulong)data[i + 1] << 8; goto case 1;
                case 1: h ^= data[i]; h *= m; break;
            }

            h ^= h >> 47;
            h *= m;
            h ^= h >> 47;

            return unchecked((long)h);
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
