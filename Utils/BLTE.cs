﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace wow.tools.api
{
    public static class BLTE
    {
        public struct BLTEChunkInfo
        {
            public bool isFullChunk;
            public int compSize;
            public int decompSize;
            public byte[] checkSum;
        }
        
        public static byte[] Parse(byte[] content)
        {
            using (var bin = new BinaryReader(new MemoryStream(content)))
            {
                if (bin.ReadUInt32() != 0x45544c42) { throw new Exception("Not a BLTE file"); }

                var blteSize = bin.ReadUInt32(true);

                BLTEChunkInfo[] chunkInfos;

                if (blteSize == 0)
                {
                    // These are always uncompressed
                    chunkInfos = new BLTEChunkInfo[1];
                    chunkInfos[0].isFullChunk = false;
                    chunkInfos[0].compSize = Convert.ToInt32(bin.BaseStream.Length - 8);
                    chunkInfos[0].decompSize = Convert.ToInt32(bin.BaseStream.Length - 8 - 1);
                    chunkInfos[0].checkSum = new byte[16];
                }
                else
                {
                    var bytes = bin.ReadBytes(4);

                    var chunkCount = bytes[1] << 16 | bytes[2] << 8 | bytes[3] << 0;

                    var supposedHeaderSize = 24 * chunkCount + 12;

                    if (supposedHeaderSize != blteSize)
                    {
                        throw new Exception("Invalid header size!");
                    }

                    if (supposedHeaderSize > bin.BaseStream.Length)
                    {
                        throw new Exception("Not enough data");
                    }

                    chunkInfos = new BLTEChunkInfo[chunkCount];

                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunkInfos[i].isFullChunk = true;
                        chunkInfos[i].compSize = bin.ReadInt32(true);
                        chunkInfos[i].decompSize = bin.ReadInt32(true);
                        chunkInfos[i].checkSum = new byte[16];
                        chunkInfos[i].checkSum = bin.ReadBytes(16);
                    }
                }

                var totalSize = chunkInfos.Sum(c => c.decompSize);

                using (var result = new MemoryStream(totalSize))
                {
                    for (var index = 0; index < chunkInfos.Length; index++)
                    {
                        var chunk = chunkInfos[index];

                        if (chunk.compSize > (bin.BaseStream.Length - bin.BaseStream.Position))
                        {
                            throw new Exception("Trying to read more than is available!");
                        }

                        HandleDataBlock(bin.ReadBytes(chunk.compSize), index, chunk, result);
                    }

                    return result.ToArray();
                }
            }
        }
        private static void HandleDataBlock(byte[] data, int index, BLTEChunkInfo chunk, MemoryStream result)
        {
            switch (data[0])
            {
                case 0x4E: // N (no compression)
                    result.Write(data, 1, data.Length - 1);
                    break;
                case 0x5A: // Z (zlib, compressed)
                    using (var stream = new MemoryStream(data, 3, chunk.compSize - 3))
                    using (var ds = new DeflateStream(stream, CompressionMode.Decompress))
                    {
                        ds.CopyTo(result);
                    }
                    break;
                case 0x45: // E (encrypted)
                    throw new Exception("Unsupported mode in wow.tools.api " + data[0].ToString("X") + "!");
                    break;
                case 0x46: // F (frame)
                default:
                    throw new Exception("Unsupported mode " + data[0].ToString("X") + "!");
            }
        }
    }
}