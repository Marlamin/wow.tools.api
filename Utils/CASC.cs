using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using System;
using wow.tools.api.Controllers;

namespace wow.tools.api.Utils
{
    public class CASC
    {
        public struct RootFile
        {
            public MultiDictionary<ulong, RootEntry> entriesLookup;
            public MultiDictionary<uint, RootEntry> entriesFDID;
        }

        public struct RootEntry
        {
            public ContentFlags contentFlags;
            public LocaleFlags localeFlags;
            public ulong lookup;
            public uint fileDataID;
            public string md5;
        }

        public static async Task<RootFile> GetRoot(string hash, bool parseIt = false, string cdnDir = "wow")
        {
            var root = new RootFile
            {
                entriesLookup = new MultiDictionary<ulong, RootEntry>(),
                entriesFDID = new MultiDictionary<uint, RootEntry>(),
            };
            var rootPath = Path.Combine(SettingsManager.cacheDir, "tpr", cdnDir, "data", hash.Substring(0, 2), hash.Substring(2, 2), hash);
            byte[] content;
            if (cdnDir == "wow")
            {
                content = await File.ReadAllBytesAsync(rootPath);
            }
            else
            {
                content = BLTE.DecryptFile(Path.GetFileNameWithoutExtension(rootPath), await File.ReadAllBytesAsync(rootPath), "wowdevalpha");
            }
            if (!parseIt) return root;

            var newRoot = false;

            using (var ms = new MemoryStream(BLTE.Parse(content)))
            using (var bin = new BinaryReader(ms))
            {
                var header = bin.ReadUInt32();
                if (header == 1296454484)
                {
                    uint totalFiles = bin.ReadUInt32();
                    uint namedFiles = bin.ReadUInt32();
                    newRoot = true;
                }
                else
                {
                    bin.BaseStream.Position = 0;
                }

                while (bin.BaseStream.Position < bin.BaseStream.Length)
                {
                    var count = bin.ReadUInt32();
                    var contentFlags = (ContentFlags)bin.ReadUInt32();
                    var localeFlags = (LocaleFlags)bin.ReadUInt32();

                    var entries = new RootEntry[count];
                    var filedataIds = new int[count];

                    var fileDataIndex = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        entries[i].localeFlags = localeFlags;
                        entries[i].contentFlags = contentFlags;

                        filedataIds[i] = fileDataIndex + bin.ReadInt32();
                        entries[i].fileDataID = (uint)filedataIds[i];
                        fileDataIndex = filedataIds[i] + 1;
                    }

                    if (!newRoot)
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            entries[i].md5 = Convert.ToHexString(bin.ReadBytes(16));
                            entries[i].lookup = bin.ReadUInt64();
                            root.entriesLookup.Add(entries[i].lookup, entries[i]);
                            root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            entries[i].md5 = Convert.ToHexString(bin.ReadBytes(16));
                        }

                        for (var i = 0; i < count; ++i)
                        {
                            if (contentFlags.HasFlag(ContentFlags.NoNames))
                            {
                                entries[i].lookup = 0;
                            }
                            else
                            {
                                entries[i].lookup = bin.ReadUInt64();
                                root.entriesLookup.Add(entries[i].lookup, entries[i]);
                            }

                            root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                        }
                    }
                }
            }

            return root;
        }
    }
}
