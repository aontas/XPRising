using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ProjectM;
using XPRising.Extensions;
using XPRising.Systems;

namespace XPRising.Models;

public class GlobalMasteryConfig
{
    public struct BonusData
    {
        public enum Type
        {
            Fixed,
            Ratio,
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public Type BonusType;
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public UnitStatType StatType;
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public float Value;
        public int RequiredMastery;
        public float InactiveMultiplier;
    }

    public struct ActiveBonusData
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public BonusData.Type BonusType;
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public UnitStatTypeExtensions.Category StatCategory;
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public float Value;
    }

    public struct PointsData
    {
        public float ValuePerMastery;
        public List<string> AllowedSkillTrees;
    }

    public struct MasteryConfig
    {
        public List<string> Templates;
        public List<BonusData> BaseBonus;
        public List<ActiveBonusData> ActiveBonus;
        public List<PointsData> Points;
        public float MaxEffectiveness;
        public float DecayValue;
        public float GrowthPerEffectiveness;

        public void CopyTo(ref MasteryConfig otherConfig)
        {
            if (BaseBonus?.Count > 0) otherConfig.BaseBonus = this.BaseBonus.ToList();
            if (ActiveBonus?.Count > 0) otherConfig.ActiveBonus = this.ActiveBonus.ToList();
            if (Points?.Count > 0) otherConfig.Points = this.Points.ToList();
            if (MaxEffectiveness != 0) otherConfig.MaxEffectiveness = this.MaxEffectiveness;
            if (DecayValue != 0) otherConfig.DecayValue = this.DecayValue;
            if (GrowthPerEffectiveness != 0) otherConfig.GrowthPerEffectiveness = this.GrowthPerEffectiveness;
        }
    }

    public struct SkillTreeNode
    {
        public string Name;
        public int Cost;
        public int MaxPurchaseCount;
        public List<BonusData> Bonus;
        public List<SkillTreeNode> Children;
    }

    public struct SkillTree
    {
        public string Name;
        public SkillTreeNode BaseNode;
    }

    public Dictionary<GlobalMasterySystem.MasteryType, MasteryConfig> Mastery;
    public List<SkillTree> SkillTrees;
    public Dictionary<string, MasteryConfig> MasteryTemplates;
    public MasteryConfig DefaultWeaponMasteryConfig;
    public MasteryConfig DefaultBloodMasteryConfig;
}