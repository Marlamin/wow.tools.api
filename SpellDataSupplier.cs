using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using wow.tools.api.Utils;
using WoWTools.SpellDescParser;
namespace wow.tools.api
{
    public class SpellDataSupplier : ISupplier
    {
        private SQLiteConnection db;
        private string build;
        private byte level;
        private sbyte difficulty;
        private short mapID;
        private sbyte expansion = -2;
        private uint itemID;
        
        public SpellDataSupplier(string build, byte level = 60, sbyte difficulty = 0, short mapID = -1, uint itemID = 0)
        {
            this.build = build;
            this.level = level;
            this.difficulty = difficulty;
            this.mapID = mapID;
            this.itemID = itemID;
            this.db = Program.cnnOut;
        }

        // Stat/Effect Point parsing based on work done by simc & https://github.com/TrinityCore/SpellWork 
        public double? SupplyEffectPoint(int spellID, uint? effectIndex)
        {
            var points = 0.0f;

            using (var query = new SQLiteCommand("SELECT * FROM SpellEffect WHERE SpellID = :id AND EffectIndex = :effectIndex AND DifficultyID = :difficulty"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.Parameters.AddWithValue(":effectIndex", effectIndex - 1);
                query.Parameters.AddWithValue(":difficulty", difficulty);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return points;

                while (reader.Read())
                {
                    var effectPoints = reader.GetFloat(reader.GetOrdinal("EffectBasePointsF"));
                    var spellAttributes = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
 
                    using (var subQuery = new SQLiteCommand("SELECT * FROM SpellMisc WHERE ID = :id"))
                    {
                        subQuery.Connection = db;
                        subQuery.Parameters.AddWithValue(":id", spellID);
                        subQuery.ExecuteNonQuery();

                        var subReader = subQuery.ExecuteReader();
                        if (subReader.HasRows)
                        {
                            while (subReader.Read())
                            {
                                var spellAttrs = new List<int>();
                                for (int i = 0; i < 14; i++)
                                {
                                    spellAttrs.Add(subReader.GetInt32(subReader.GetOrdinal("Attributes_" + i)));
                                }
                                spellAttributes = spellAttrs.ToArray();
                            }
                        }
                    }

                    var coefficient = reader.GetFloat(reader.GetOrdinal("Coefficient"));

                    var scalingClass = reader.GetInt32(reader.GetOrdinal("ScalingClass"));

                    if (coefficient >= 0.0f)
                    {
                        // TODO: Not yet implemented

                        //SpellScaling? ItemLevel based scaling?
                        // TODO ScalingClass -1, -2, -3, -4, -5, -6, -7, -8, -9

                        // -6 - Sta
                        // -7 RandProp ?
                        // -8 RandProp DamageReplaceStatF 
                        // -9 RandProp DamageReplaceStatF
                        // ItemLevel based scaling
                        if (itemID == 0)
                            return effectPoints;

                        var itemLevel = 0;
                        var itemSlot = 0;
                        var itemQuality = 0;

                        using (var subquery = new SQLiteCommand("SELECT ItemLevel, InventoryType, OverallQualityID FROM ItemSparse WHERE ID = :id"))
                        {
                            subquery.Connection = db;
                            subquery.Parameters.AddWithValue(":id", itemID);
                            subquery.ExecuteNonQuery();

                            var subreader = subquery.ExecuteReader();
                            if (!subreader.HasRows)
                                return effectPoints;

                            while (subreader.Read())
                            {
                                itemLevel = subreader.GetInt32(subreader.GetOrdinal("ItemLevel"));
                                itemSlot = subreader.GetInt32(subreader.GetOrdinal("InventoryType"));
                                itemQuality = subreader.GetInt32(subreader.GetOrdinal("OverallQualityID"));
                            }
                        }

                        var multiplier = GetItemMultiplier(itemSlot, itemLevel, build);

                        if (scalingClass == -7)
                        {
                            var (RandomPropField, RandomPropIndex) = TooltipUtils.GetRandomPropertyByInventoryType(itemQuality, itemSlot, 0);

                            using var rpropQuery = new SQLiteCommand("SELECT " + RandomPropField + "F_0" + " FROM RandPropPoints WHERE ID = :id");
                            rpropQuery.Connection = db;
                            rpropQuery.Parameters.AddWithValue(":id", itemLevel);
                            rpropQuery.ExecuteNonQuery();

                            var rpropReader = rpropQuery.ExecuteReader();
                            float randProp = 0.0f;

                            while (rpropReader.Read())
                            {
                                randProp = rpropReader.GetFloat(0);
                            }

                            points = randProp * (float)multiplier;
                        }
                        else if(scalingClass == -8 || scalingClass == -9)
                        {
                            var randomPropField = "DamageReplaceStatF";
                            if (scalingClass == -9)
                            {
                                randomPropField = "DamageSecondaryF";
                            }

                            using var rpropQuery = new SQLiteCommand("SELECT " + randomPropField + " FROM RandPropPoints WHERE ID = :id");
                            rpropQuery.Connection = db;
                            rpropQuery.Parameters.AddWithValue(":id", itemLevel);
                            rpropQuery.ExecuteNonQuery();

                            var rpropReader = rpropQuery.ExecuteReader();
                            float randProp = 0.0f;

                            while (rpropReader.Read())
                            {
                                randProp = rpropReader.GetFloat(0);
                            }

                            points = randProp;
                        }

                        return points * coefficient;
                    }
                    else
                    {
                        //using (var subQuery = new SQLiteCommand("SELECT * FROM ExpectedStat WHERE Lvl = :id AND ExpansionID = :expansionID"))
                        //{
                        //    subQuery.Connection = db;
                        //    subQuery.Parameters.AddWithValue(":id", level);
                        //    subQuery.Parameters.AddWithValue(":expansionID", this.expansion);
                        //    subQuery.ExecuteNonQuery();

                        //    var subReader = subQuery.ExecuteReader();
                        //    if (subReader.HasRows)
                        //    {
                        //    }
                        //}

                        var expectedStatType =
                            TooltipUtils.GetExpectedStatTypeBySpellEffect(reader.GetInt32(reader.GetOrdinal("Effect")), reader.GetInt16(reader.GetOrdinal("EffectAura")), reader.GetInt32(reader.GetOrdinal("EffectMiscValue_0")));
                        if (expectedStatType != TooltipUtils.ExpectedStatType.None)
                        {
                            if ((spellAttributes[0] & 0x80000) == 0x80000)
                                expectedStatType = TooltipUtils.ExpectedStatType.CreatureAutoAttackDps;

                        }

                        return effectPoints;
                    }
                }
            }

            return points;
        }

        private double GetItemMultiplier(int itemSlot, int itemLevel, string build){
            var multiplierRow = GameTableProvider.GetCombatRatingsMultByILVLRow(itemLevel, build);

            double multiplier;
            switch ((TooltipUtils.InventoryType)itemSlot)
            {
                case TooltipUtils.InventoryType.Neck:
                case TooltipUtils.InventoryType.Finger:
                    multiplier = multiplierRow.JewelryMultiplier;
                    break;
                case TooltipUtils.InventoryType.Trinket:
                    multiplier = multiplierRow.TrinketMultiplier;
                    break;
                case TooltipUtils.InventoryType.OneHand:
                case TooltipUtils.InventoryType.Shield:
                case TooltipUtils.InventoryType.Ranged:
                case TooltipUtils.InventoryType.TwoHand:
                case TooltipUtils.InventoryType.MainHand:
                case TooltipUtils.InventoryType.OffHand:
                case TooltipUtils.InventoryType.HeldInOffhand:
                case TooltipUtils.InventoryType.RangedRight:
                    multiplier = multiplierRow.WeaponMultiplier;
                    break;
                default:
                    multiplier = multiplierRow.ArmorMultiplier;
                    break;
            }

            return multiplier;
        }
        public double? SupplyRadius(int spellID, uint? effectIndex, int radiusIndex)
        {
            using (var query = new SQLiteCommand("SELECT EffectRadiusIndex_0, EffectRadiusIndex_1 FROM SpellEffect WHERE SpellID = :id AND EffectIndex = :effectIndex"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.Parameters.AddWithValue(":effectIndex", effectIndex - 1);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    var radiusIndex0 = reader.GetInt32(0);
                    var radiusIndex1 = reader.GetInt32(1);

                    var spellRadiusID = 0;

                    if (radiusIndex == 0)
                    {
                        spellRadiusID = radiusIndex0;
                    }
                    else if (radiusIndex == 1)
                    {
                        spellRadiusID = radiusIndex1;
                    }

                    if (spellRadiusID == 0)
                    {
                        if (radiusIndex == 1)
                        {
                            spellRadiusID = radiusIndex0;
                        }
                        else if (radiusIndex == 0)
                        {
                            spellRadiusID = radiusIndex1;
                        }
                    }

                    if (spellRadiusID == 0)
                        return null;

                    using var rquery = new SQLiteCommand("SELECT Radius FROM SpellRadius WHERE ID = :id");
                    rquery.Connection = db;
                    rquery.Parameters.AddWithValue(":id", spellRadiusID);
                    rquery.ExecuteNonQuery();

                    var rreader = rquery.ExecuteReader();
                    if (!rreader.HasRows)
                        return 0;

                    while (rreader.Read())
                    {
                        return rreader.GetDouble(0);
                    }
                }
            }

            return null;
        }
        public int? SupplyDuration(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT Duration FROM SpellDuration WHERE ID IN (SELECT DurationIndex FROM SpellMisc WHERE SpellID = :id)"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyEffectAmplitude(int spellID, uint? effectIndex)
        {
            effectIndex ??= 1;

            using (var query = new SQLiteCommand("SELECT EffectAmplitude FROM SpellEffect WHERE SpellID = :id AND EffectIndex = :effectIndex"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.Parameters.AddWithValue(":effectIndex", effectIndex - 1);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyAuraPeriod(int spellID, uint? effectIndex)
        {
            effectIndex ??= 1;

            using (var query = new SQLiteCommand("SELECT EffectAuraPeriod FROM SpellEffect WHERE SpellID = :id AND EffectIndex = :effectIndex"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.Parameters.AddWithValue(":effectIndex", effectIndex - 1);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyChainTargets(int spellID, uint? effectIndex)
        {
            effectIndex ??= 1;

            using (var query = new SQLiteCommand("SELECT EffectChainTargets FROM SpellEffect WHERE SpellID = :id AND EffectIndex = :effectIndex"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.Parameters.AddWithValue(":effectIndex", effectIndex - 1);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyMaxTargetLevel(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT MaxTargetLevel FROM SpellTargetRestrictions WHERE SpellID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyMaxTargets(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT MaxTargets FROM SpellTargetRestrictions WHERE SpellID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyProcCharges(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT ProcCharges FROM SpellAuraOptions WHERE SpellID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyMaxStacks(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT CumulativeAura FROM SpellAuraOptions WHERE SpellID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyProcChance(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT ProcChance FROM SpellAuraOptions WHERE SpellID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public int? SupplyMinRange(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT RangeMin_0, RangeMin_1 FROM SpellRange WHERE ID IN (SELECT RangeIndex FROM SpellMisc WHERE SpellID = :id)"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    var rangeMin0 = reader.GetInt32(0);
                    var rangeMin1 = reader.GetInt32(1);

                    if (rangeMin0 != 0)
                    {
                        return rangeMin0;
                    }

                    if (rangeMin1 != 0)
                    {
                        return rangeMin1;
                    }
                }
            }

            return null;
        }
        public int? SupplyMaxRange(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT RangeMax_0, RangeMax_1 FROM SpellRange WHERE ID IN (SELECT RangeIndex FROM SpellMisc WHERE SpellID = :id)"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    var rangeMax0 = reader.GetInt32(0);
                    var rangeMax1 = reader.GetInt32(1);

                    if (rangeMax0 != 0)
                    {
                        return rangeMax0;
                    }

                    if (rangeMax1 != 0)
                    {
                        return rangeMax1;
                    }
                }
            }

            return null;
        }
        public int? SupplyEffectMisc(int spellID, uint? effectIndex)
        {
            effectIndex ??= 1;

            using (var query = new SQLiteCommand("SELECT EffectMiscValue_0 FROM SpellEffect WHERE SpellID = :id AND EffectIndex = :effectIndex"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.Parameters.AddWithValue(":effectIndex", effectIndex - 1);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return 0;

                while (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }

            return null;
        }
        public string? SupplySpellName(int spellID)
        {
            using (var query = new SQLiteCommand("SELECT Name_lang FROM SpellName WHERE ID = :id"))
            {
                query.Connection = db;
                query.Parameters.AddWithValue(":id", spellID);
                query.ExecuteNonQuery();

                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                    return "Unknown Spell";

                while (reader.Read())
                {
                    return reader.GetString(0);
                }
            }

            return null;
        }
    }
}
