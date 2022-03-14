using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wow.tools.api.Utils;
using WoWTools.SpellDescParser;

namespace wow.tools.api.Controllers
{
    public struct TTItem
    {
        public string Name { get; set; }
        public int IconFileDataID { get; set; }
        public int ExpansionID { get; set; }
        public int ClassID { get; set; }
        public int SubClassID { get; set; }
        public int InventoryType { get; set; }
        public int ItemLevel { get; set; }
        public int OverallQualityID { get; set; }
        public bool HasSparse { get; set; }
        public string FlavorText { get; set; }
        public TTItemEffect[] ItemEffects { get; set; }
        public TTItemStat[] Stats { get; set; }
        public string Speed { get; set; }
        public string DPS { get; set; }
        public string MinDamage { get; set; }
        public string MaxDamage { get; set; }
        public int RequiredLevel { get; set; }
    }

    public struct TTItemEffect
    {
        public TTSpell Spell { get; set; }
        public int TriggerType { get; set; }
    }

    public struct TTSpell
    {
        public int SpellID { get; set; }
        public string Name { get; set; }
        public string SubText { get; set; }
        public string Description { get; set; }
        public int IconFileDataID { get; set; }
    }

    public struct TTItemStat
    {
        public int StatTypeID { get; set; }
        public int Value { get; set; }
        public bool IsCombatRating { get; set; }
    }

    [ApiController]
    [Route("api/tooltip")]
    public class TooltipController : ControllerBase
    {
        private readonly SQLiteConnection db;

        public TooltipController()
        {
            db = Program.cnnOut;
        }

        [HttpGet("item/{ItemID}")]
        public async Task<IActionResult> GetItemTooltip(int itemID, string build)
        {
            var result = new TTItem();

            // Basic Item information -- generally always available
            using (var query = new SQLiteCommand("SELECT IconFileDataID, ClassID, SubclassID, InventoryType FROM Item WHERE ID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", itemID);
                await query.ExecuteNonQueryAsync();

                var reader = await query.ExecuteReaderAsync();
                if (!reader.HasRows)
                    return NotFound();

                while (reader.Read())
                {
                    result.IconFileDataID = reader.GetInt32(0);
                    result.ClassID = reader.GetInt32(1);
                    result.SubClassID = reader.GetInt32(2);
                    result.InventoryType = reader.GetInt32(3);
                }
            }

            // Icons in Item.db2 can be 0. Look up the proper one in ItemModifiedAppearance => ItemAppearance
            if (result.IconFileDataID == 0)
            {
                using var query = new SQLiteCommand("SELECT DefaultIconFileDataID FROM ItemAppearance WHERE ID IN (SELECT ItemAppearanceID FROM ItemModifiedAppearance WHERE ItemID = :id)");
                query.Connection = db;
                query.Parameters.AddWithValue(":id", itemID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                while (reader.Read())
                {
                    result.IconFileDataID = reader.GetInt32(0);
                }
            }

            using (var query = new SQLiteCommand("SELECT * FROM ItemSparse WHERE ID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", itemID);
                await query.ExecuteNonQueryAsync();

                var reader = await query.ExecuteReaderAsync();
                if (!reader.HasRows)
                    result.HasSparse = false;

                while (reader.Read())
                {
                    result.HasSparse = true;
                    result.ItemLevel = reader.GetInt32(reader.GetOrdinal("ItemLevel"));
                    result.OverallQualityID = reader.GetInt32(reader.GetOrdinal("OverallQualityID"));
                    result.Name = reader.GetString(reader.GetOrdinal("Display_lang"));
                    result.FlavorText = reader.GetString(reader.GetOrdinal("Description_lang"));
                    result.ExpansionID = reader.GetInt32(reader.GetOrdinal("ExpansionID"));
                    result.RequiredLevel = reader.GetInt32(reader.GetOrdinal("RequiredLevel"));

                    var itemDelay = reader.GetInt32(reader.GetOrdinal("ItemDelay")) / 1000f;
                    var itemFlags1 = reader.GetInt32(reader.GetOrdinal("Flags_1"));

                    var targetDamageDB = GetDamageDBByItemSubClass(result.SubClassID, (itemFlags1 & 0x200) == 0x200);

                    var statTypes = new List<int>();
                    for (int i = 0; i < 10; i++)
                    {
                        statTypes.Add(reader.GetInt32(reader.GetOrdinal("StatModifier_bonusStat_" + i)));
                    }

                    if (statTypes.Count > 0 && statTypes.Any(x => x != -1) && statTypes.Any(x => x != 0))
                    {
                        var (RandomPropField, RandomPropIndex) = TooltipUtils.GetRandomPropertyByInventoryType(result.OverallQualityID, result.InventoryType, result.SubClassID);

                        using var rpropQuery = new SQLiteCommand("SELECT " + RandomPropField + "F_" + RandomPropIndex + " FROM RandPropPoints WHERE ID = :id");
                        rpropQuery.Connection = db;
                        rpropQuery.Parameters.AddWithValue(":id", result.ItemLevel);
                        rpropQuery.ExecuteNonQuery();

                        var rpropReader = rpropQuery.ExecuteReader();
                        float randProp = 0.0f;

                        while (rpropReader.Read())
                        {
                            randProp = rpropReader.GetFloat(0);
                        }

                        var statPercentEditor = new List<int>();
                        for (int i = 0; i < 10; i++)
                        {
                            statPercentEditor.Add(reader.GetInt32(reader.GetOrdinal("StatPercentEditor_" + i)));
                        }

                        var statList = new Dictionary<int, TTItemStat>();
                        for (var statIndex = 0; statIndex < statTypes.Count; statIndex++)
                        {
                            if (statTypes[statIndex] == -1 || statTypes[statIndex] == 0)
                                continue;

                            var stat = TooltipUtils.CalculateItemStat(statTypes[statIndex], randProp, result.ItemLevel, statPercentEditor[statIndex], 0.0f, result.OverallQualityID, result.InventoryType, result.SubClassID, build);

                            if (stat.Value == 0)
                                continue;

                            if (statList.TryGetValue(statTypes[statIndex], out var currStat))
                            {
                                currStat.Value += stat.Value;
                            }
                            else
                            {
                                statList.Add(statTypes[statIndex], stat);
                            }
                        }

                        result.Stats = statList.Values.ToArray();
                    }

                    var quality = result.OverallQualityID;
                    if (quality == 7) // Heirloom == Rare
                        quality = 3;

                    if (quality == 5) // Legendary = Epic
                        quality = 4;

                    using var damageQuery = new SQLiteCommand("SELECT Quality_" + quality + " FROM " + targetDamageDB +" WHERE ItemLevel = :ilvl");
                    damageQuery.Connection = db;
                    damageQuery.Parameters.AddWithValue(":ilvl", result.ItemLevel);
                    damageQuery.ExecuteNonQuery();

                    var damageReader = damageQuery.ExecuteReader();
                    float itemDamage = 0.0f;

                    while (damageReader.Read())
                    {
                        itemDamage = damageReader.GetFloat(0);
                    }

                    var dmgVariance = reader.GetFloat(reader.GetOrdinal("DmgVariance"));

                    //Use. as decimal separator
                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";
                    result.MinDamage = Math.Floor(itemDamage * itemDelay * (1 - dmgVariance * 0.5)).ToString(nfi);
                    result.MaxDamage = Math.Floor(itemDamage * itemDelay * (1 + dmgVariance * 0.5)).ToString(nfi);
                    result.Speed = itemDelay.ToString("F2", nfi);
                    result.DPS = itemDamage.ToString("F2", nfi);
                }
            }

            if (!result.HasSparse)
            {
                using (var query = new SQLiteCommand("SELECT * FROM ItemSearchName WHERE ID = :id"))
                {
                    query.Connection = db;
                    query.Parameters.AddWithValue(":id", itemID);
                    await query.ExecuteNonQueryAsync();

                    var reader = await query.ExecuteReaderAsync();
                    if (!reader.HasRows)
                        result.Name = "Unknown Item";

                    while (reader.Read())
                    {
                        result.Name = reader.GetString(reader.GetOrdinal("Display_lang"));
                        result.RequiredLevel = reader.GetInt32(reader.GetOrdinal("RequiredLevel"));
                        result.ExpansionID = reader.GetInt32(reader.GetOrdinal("ExpansionID"));
                        result.ItemLevel = reader.GetInt32(reader.GetOrdinal("ItemLevel"));
                        result.OverallQualityID = reader.GetInt32(reader.GetOrdinal("OverallQualityID"));
                    }
                }
            }

            using (var query = new SQLiteCommand("SELECT ItemEffectID FROM ItemXItemEffect WHERE ItemID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", itemID);
                await query.ExecuteNonQueryAsync();

                var reader = await query.ExecuteReaderAsync();

                var itemEffects = new List<TTItemEffect>();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var itemEffectID = reader.GetInt32(reader.GetOrdinal("ItemEffectID"));
                        using (var subquery = new SQLiteCommand("SELECT * FROM ItemEffect WHERE ID = :id"))
                        {
                            subquery.Connection = db;
                            subquery.Parameters.AddWithValue(":id", itemEffectID);
                            await subquery.ExecuteNonQueryAsync();

                            var subreader = await subquery.ExecuteReaderAsync();

                            if (subreader.HasRows)
                            {
                                while (subreader.Read())
                                {
                                    var triggerType = subreader.GetInt32(subreader.GetOrdinal("TriggerType"));
                                    var spellID = subreader.GetInt32(subreader.GetOrdinal("SpellID"));

                                    itemEffects.Add(new TTItemEffect() { Spell = new TTSpell() { SpellID = spellID, Description = "Loading.." }, TriggerType = triggerType });
                                }
                            }
                        }
                    }
                }

                result.ItemEffects = itemEffects.ToArray();
            }

            /* Fixups */
            // Classic ExpansionID column has 254, make 0. ¯\_(ツ)_/¯
            if (result.ExpansionID == 254)
                result.ExpansionID = 0;

            return Ok(result);
        }

        private string GetDamageDBByItemSubClass(int itemSubClassID, bool isCasterWeapon)
        {
            switch (itemSubClassID)
            {
                // 1H
                case 0:  //	Axe
                case 4:  //	Mace
                case 7:  //	Sword
                case 9:  //	Warglaives
                case 11: //	Bear Claws
                case 13: //	Fist Weapon
                case 15: //	Dagger
                case 16: //	Thrown
                case 19: //	Wand,
                    if (isCasterWeapon)
                    {
                        return "ItemDamageOneHandCaster";
                    }
                    else
                    {
                        return "ItemDamageOneHand";
                    }
                // 2H
                case 1:  // 2H Axe
                case 2:  // Bow
                case 3:  // Gun
                case 5:  // 2H Mace
                case 6:  // Polearm
                case 8:  // 2H Sword
                case 10: //	Staff,
                case 12: //	Cat Claws,
                case 17: //	Spear,
                case 18: //	Crossbow
                case 20: //	Fishing Pole
                    if (isCasterWeapon)
                    {
                        return "ItemDamageTwoHandCaster";
                    }
                    else
                    {
                        return "ItemDamageTwoHand";
                    }
                case 14: //	14: 'Miscellaneous',
                    return "ItemDamageOneHandCaster";
                default:
                    throw new Exception("Don't know what table to map to unknown SubClassID " + itemSubClassID);
            }
        }

        [HttpGet("spell/{SpellID}")]
        public async Task<IActionResult> GetSpellTooltip(int spellID, string build, byte level = 60, sbyte difficulty = -1, short mapID = -1, uint itemID = 0)
        {
            // If difficulty is -1 fall back to Normal

            var result = new TTSpell();
            result.SpellID = spellID;

            using (var query = new SQLiteCommand("SELECT Name_lang FROM SpellName WHERE ID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                await query.ExecuteNonQueryAsync();

                var reader = await query.ExecuteReaderAsync();
                if (!reader.HasRows)
                    return NotFound();

                while (reader.Read())
                {
                    result.Name = reader.GetString(0);
                }
            }

            using (var query = new SQLiteCommand("SELECT * FROM Spell WHERE ID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                await query.ExecuteNonQueryAsync();

                var reader = await query.ExecuteReaderAsync();
                if (!reader.HasRows)
                    return NotFound();

                while (reader.Read())
                {
                    var dataSupplier = new SpellDataSupplier(build, level, difficulty, mapID, itemID);

                    var nameSubtext = reader.GetString(reader.GetOrdinal("NameSubtext_lang"));
                    var descLang = reader.GetString(reader.GetOrdinal("Description_lang"));

                    if (descLang != string.Empty)
                    {
                        var spellDescParser = new SpellDescParser(descLang);
                        spellDescParser.Parse();

                        var sb = new StringBuilder();
                        spellDescParser.root.Format(sb, spellID, dataSupplier);

                        result.Description = sb.ToString();

                        // Check for PropertyType.SpellDescription nodes and feed those into separate parsers (make sure to add a recursion limit :) )
                        foreach (var node in spellDescParser.root.nodes)
                        {
                            if (node is Property property && property.propertyType == PropertyType.SpellDescription && property.overrideSpellID != null)
                            {
                                using (var subQuery = new SQLiteCommand("SELECT * FROM Spell WHERE ID = :id"))
                                {
                                    subQuery.Connection = db;
                                    subQuery.Parameters.AddWithValue(":id", property.overrideSpellID);
                                    await subQuery.ExecuteNonQueryAsync();

                                    var subReader = await subQuery.ExecuteReaderAsync();
                                    if (subReader.HasRows)
                                    {
                                        while (subReader.Read())
                                        {
                                            var externalSpellDescParser = new SpellDescParser(subReader.GetString(subReader.GetOrdinal("Description_lang")));
                                            externalSpellDescParser.Parse();

                                            var externalSB = new StringBuilder();
                                            externalSpellDescParser.root.Format(externalSB, (int)property.overrideSpellID, dataSupplier);

                                            result.Description = result.Description.Replace("$@spelldesc" + property.overrideSpellID, externalSB.ToString());
                                        }
                                    }
                                    else
                                    {
                                        result.Description = "ERROR: Spell description for override spell " + property.overrideSpellID + " was not found!";
                                    }
                                }
                            }
                        }


                        if (nameSubtext != string.Empty)
                        {
                            result.SubText = nameSubtext;
                        }
                    }
                }
            }

            using (var query = new SQLiteCommand("SELECT SpellIconFileDataID FROM SpellMisc WHERE SpellID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                await query.ExecuteNonQueryAsync();

                var reader = await query.ExecuteReaderAsync();
                if (!reader.HasRows)
                    result.IconFileDataID = 134400;

                while (reader.Read())
                {
                    result.IconFileDataID = reader.GetInt32(0);
                }
            }
            
            return Ok(result);
        }
    }
}