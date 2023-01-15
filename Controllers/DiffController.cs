using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wow.tools.api.Utils;
using static wow.tools.api.Utils.CASC;

namespace wow.tools.api.Controllers
{
    public struct CASCFile
    {
        public uint id;
        public string filename;
        public string type;
    }

    [Flags]
    public enum LocaleFlags : uint
    {
        All = 0xFFFFFFFF,
        None = 0,
        //Unk_1 = 0x1,
        enUS = 0x2,
        koKR = 0x4,
        //Unk_8 = 0x8,
        frFR = 0x10,
        deDE = 0x20,
        zhCN = 0x40,
        esES = 0x80,
        zhTW = 0x100,
        enGB = 0x200,
        enCN = 0x400,
        enTW = 0x800,
        esMX = 0x1000,
        ruRU = 0x2000,
        ptBR = 0x4000,
        itIT = 0x8000,
        ptPT = 0x10000,
        enSG = 0x20000000, // custom
        plPL = 0x40000000, // custom
        All_WoW = enUS | koKR | frFR | deDE | zhCN | esES | zhTW | enGB | esMX | ruRU | ptBR | itIT | ptPT
    }

    [Flags]
    public enum ContentFlags : uint
    {
        None = 0,
        F00000001 = 0x1,            // unused in 9.0.5
        F00000002 = 0x2,            // unused in 9.0.5
        F00000004 = 0x4,            // unused in 9.0.5
        LoadOnWindows = 0x8,        // added in 7.2.0.23436
        LoadOnMacOS = 0x10,         // added in 7.2.0.23436
        LowViolence = 0x80,         // many models have this flag
        DoNotLoad = 0x100,          // unused in 9.0.5
        F00000200 = 0x200,          // unused in 9.0.5
        F00000400 = 0x400,          // unused in 9.0.5
        UpdatePlugin = 0x800,       // UpdatePlugin.dll / UpdatePlugin.dylib only
        F00001000 = 0x1000,         // unused in 9.0.5
        F00002000 = 0x2000,         // unused in 9.0.5
        F00004000 = 0x4000,         // unused in 9.0.5
        F00008000 = 0x8000,         // unused in 9.0.5
        F00010000 = 0x10000,        // unused in 9.0.5
        F00020000 = 0x20000,        // 1173911 uses in 9.0.5        
        F00040000 = 0x40000,        // 1329023 uses in 9.0.5
        F00080000 = 0x80000,        // 682817 uses in 9.0.5
        F00100000 = 0x100000,       // 1231299 uses in 9.0.5
        F00200000 = 0x200000,       // 7398 uses in 9.0.5: updateplugin, .bls, .lua, .toc, .xsd
        F00400000 = 0x400000,       // 156302 uses in 9.0.5
        F00800000 = 0x800000,       // .skel & .wwf
        F01000000 = 0x1000000,      // unused in 9.0.5
        F02000000 = 0x2000000,      // 969369 uses in 9.0.5
        F04000000 = 0x4000000,      // 1101698 uses in 9.0.5
        Encrypted = 0x8000000,      // File is encrypted
        NoNames = 0x10000000,       // No lookup hash
        UncommonRes = 0x20000000,   // added in 7.0.3.21737
        Bundle = 0x40000000,        // unused in 9.0.5
        NoCompression = 0x80000000  // sounds have this flag
    }

    [ApiController]
    [Route("[controller]")]
    public class DiffController : Controller
    {
        public static async Task<Dictionary<uint, CASCFile>> GetAllFiles()
        {
            var dict = new Dictionary<uint, CASCFile>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT id, filename, type from wow_rootfiles ORDER BY id DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new CASCFile { id = uint.Parse(reader["id"].ToString()), filename = reader["filename"].ToString(), type = reader["type"].ToString() };
                    dict.Add(uint.Parse(reader["id"].ToString()), row);
                }
            }

            return dict;
        }

        [HttpGet]
        [Route("diff_api_invalidate")]
        public ActionResult DiffApiInvalidateCache()
        {
            BuildDiffCache.Invalidate();

            return Ok();
        }

        [HttpGet]
        [Route("diff_api")]
        public async Task<ActionResult> DiffApi(string from, string to, int start = 0, string cdnDir = "wow")
        {
            Console.WriteLine("Serving root diff for root " + from + " => " + to + " (" + cdnDir + ")");

            if (BuildDiffCache.Get(from, to, out ApiDiff diff))
            {
                return Json(new
                {
                    added = diff.added.Count(),
                    modified = diff.modified.Count(),
                    removed = diff.removed.Count(),
                    data = diff.all.ToArray()
                });
            }

            var filedataids = await GetAllFiles();

            var rootFrom = await GetRoot(from, true, cdnDir);
            var rootTo = await GetRoot(to, true, cdnDir);

            var rootFromEntries = rootFrom.entriesFDID;
            var rootToEntries = rootTo.entriesFDID;
            
            var fromEntries = rootFromEntries.Keys.ToHashSet();
            var toEntries = rootToEntries.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            static RootEntry prioritize(List<RootEntry> entries)
            {
                var prioritized = entries.FirstOrDefault(subentry =>
                       subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                );

                if (prioritized.fileDataID != 0)
                {
                    return prioritized;
                }
                else
                {
                    return entries.First();
                }
            }

            Func<RootEntry, DiffEntry> toDiffEntry(string action)
            {
                return delegate (RootEntry entry)
                {
                    var file = filedataids.ContainsKey(entry.fileDataID) ? filedataids[entry.fileDataID] : new CASCFile { filename = "", id = entry.fileDataID, type = "unk" };

                    return new DiffEntry
                    {
                        action = action,
                        filename = file.filename,
                        id = file.id.ToString(),
                        type = file.type,
                        md5 = entry.md5.ToLower()
                    };
                };
            }

            var addedFiles = addedEntries.Select(entry => rootToEntries[entry]).Select(prioritize);
            var removedFiles = removedEntries.Select(entry => rootFromEntries[entry]).Select(prioritize);

            // Modified files are a little bit more tricky, so we can't just throw a LINQ expression at it
            var modifiedFiles = new List<RootEntry>();

            foreach (var entry in commonEntries)
            {
                var originalFile = prioritize(rootFromEntries[entry]);
                var patchedFile = prioritize(rootToEntries[entry]);

                if (originalFile.md5.Equals(patchedFile.md5))
                {
                    continue;
                }

                modifiedFiles.Add(patchedFile);
            }

            var toAddedDiffEntryDelegate = toDiffEntry("added");
            var toRemovedDiffEntryDelegate = toDiffEntry("removed");
            var toModifiedDiffEntryDelegate = toDiffEntry("modified");

            diff = new ApiDiff
            {
                added = addedFiles.Select(toAddedDiffEntryDelegate),
                removed = removedFiles.Select(toRemovedDiffEntryDelegate),
                modified = modifiedFiles.Select(toModifiedDiffEntryDelegate)
            };

            Console.WriteLine($"Added: {diff.added.Count()}, removed: {diff.removed.Count()}, modified: {diff.modified.Count()}, common: {commonEntries.Count()}");

            BuildDiffCache.Add(from, to, diff);

            return Json(new
            {
                added = diff.added.Count(),
                modified = diff.modified.Count(),
                removed = diff.removed.Count(),
                data = diff.all.ToArray()
            });
        }
    }
}
