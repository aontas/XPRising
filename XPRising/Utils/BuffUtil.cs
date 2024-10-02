using System.Text.RegularExpressions;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using XPRising.Transport;
using XPRising.Utils.Prefabs;
using XPShared;

namespace XPRising.Utils;

public static class BuffUtil
{
    // Punishment debuff
    public static PrefabGUID SeverePunishmentDebuff = new PrefabGUID((int)Buffs.Buff_General_Garlic_Fever);
    public static PrefabGUID MinorPunishmentDebuff = new PrefabGUID((int)Buffs.Buff_General_Garlic_Area_Inside);
    // Marks
    public static PrefabGUID HostileMarkBuff = new PrefabGUID((int)Buffs.Buff_Cultist_BloodFrenzy_Buff);

    // LevelUp Buff
    public static int LevelUpBuffId = (int)Effects.AB_ChurchOfLight_Priest_HealBomb_Buff;
    public static PrefabGUID LevelUpBuff = new PrefabGUID(LevelUpBuffId);

    // Fun buffs
    public static PrefabGUID HolyNuke = new PrefabGUID((int)Effects.AB_Paladin_HolyNuke_Buff);
    public static PrefabGUID PigTransformDebuff = new PrefabGUID((int)Remainders.Witch_PigTransformation_Buff);
        
    public static PrefabGUID BloodBuffVBlood0 = new PrefabGUID((int)Effects.AB_BloodBuff_VBlood_0);
    public static PrefabGUID BloodBuffBase = new PrefabGUID((int)Effects.AB_BloodBuff_Base);
        
    public static int BuffGuid = (int)Effects.AB_BloodBuff_VBlood_0;
    public static int ForbiddenBuffGuid = (int)SetBonus.SetBonus_MaxHealth_Minor_Buff_01;
    public static PrefabGUID AppliedBuff = BloodBuffVBlood0;
    
    public static ModifyUnitStatBuff_DOTS MakeBuff(UnitStatType type, double strength) {
        ModifyUnitStatBuff_DOTS buff;

        var modType = ModificationType.Add;
        if (Helper.multiplierStats.Contains(type)) {
            modType = ModificationType.Multiply;
        }
        buff = (new ModifyUnitStatBuff_DOTS() {
            StatType = type,
            Value = (float)strength,
            ModificationType = modType,
            Modifier = 1,
            Id = ModificationId.NewId(0)
        });
        return buff;
    }

    public static double CalcBuffValue(double strength, double effectiveness, double rate, UnitStatType type)
    {
        effectiveness = Math.Max(effectiveness, 1);
        return strength * rate * effectiveness;
    }

    public static void ApplyBuff(Entity User, Entity Char, PrefabGUID GUID)
    {
        var des = Plugin.Server.GetExistingSystemManaged<DebugEventsSystem>();
        var fromCharacter = new FromCharacter()
        {
            User = User,
            Character = Char
        };
        var buffEvent = new ApplyBuffDebugEvent()
        {
            BuffPrefabGUID = GUID
        };
        des.ApplyBuff(fromCharacter, buffEvent);
    }

    public static void RemoveBuff(Entity Char, PrefabGUID GUID)
    {
        if (BuffUtility.HasBuff(Plugin.Server.EntityManager, Char, GUID) &&
            BuffUtility.TryGetBuff(Plugin.Server.EntityManager, Char, GUID, out var buffEntity))
        {
            Plugin.Server.EntityManager.AddComponent<DestroyTag>(buffEntity);
        }
    }

    public static bool HasBuff(Entity player, PrefabGUID BuffGUID)
    {
        return BuffUtility.HasBuff(Plugin.Server.EntityManager, player, BuffGUID);
    }
        
    public static bool IsItemEquipBuff(PrefabGUID prefabGuid)
    {
        switch ((Items)prefabGuid.GuidHash)
        {
            case Items.Item_EquipBuff_Armor_Base:
            case Items.Item_EquipBuff_Base:
            case Items.Item_EquipBuff_Clothes_Base:
            case Items.Item_EquipBuff_MagicSource_Base:
            case Items.Item_EquipBuff_MagicSource_BloodKey_T01:
            case Items.Item_EquipBuff_MagicSource_General:
            case Items.Item_EquipBuff_MagicSource_NoAbility_Base:
            case Items.Item_EquipBuff_MagicSource_Soulshard:
            case Items.Item_EquipBuff_MagicSource_Soulshard_Dracula:
            case Items.Item_EquipBuff_MagicSource_Soulshard_Manticore:
            case Items.Item_EquipBuff_MagicSource_Soulshard_Solarus:
            case Items.Item_EquipBuff_MagicSource_Soulshard_TheMonster:
            case Items.Item_EquipBuff_MagicSource_T06_Blood:
            case Items.Item_EquipBuff_MagicSource_T06_Chaos:
            case Items.Item_EquipBuff_MagicSource_T06_Frost:
            case Items.Item_EquipBuff_MagicSource_T06_Illusion:
            case Items.Item_EquipBuff_MagicSource_T06_Storm:
            case Items.Item_EquipBuff_MagicSource_T06_Unholy:
            case Items.Item_EquipBuff_MagicSource_T08_Blood:
            case Items.Item_EquipBuff_MagicSource_T08_Chaos:
            case Items.Item_EquipBuff_MagicSource_T08_Frost:
            case Items.Item_EquipBuff_MagicSource_T08_Illusion:
            case Items.Item_EquipBuff_MagicSource_T08_Storm:
            case Items.Item_EquipBuff_MagicSource_T08_Unholy:
            case Items.Item_EquipBuff_MagicSource_TriggerBuffOnPrimaryHit:
            case Items.Item_EquipBuff_MagicSource_TriggerCastOnPrimaryHit:
            case Items.Item_EquipBuff_MagicSource_Utility_Base:
            case Items.Item_EquipBuff_Shared_General:
            case Items.Item_EquipBuff_Weapon_Base:
                return true;
            default:
                if (Enum.IsDefined((EquipBuffs)prefabGuid.GuidHash))
                {
                    return true;
                }
                break;
        }

        return false;
    }

    private static readonly Dictionary<ulong, FrameTimer> FrameTimers = new();
    public static void ApplyStatBuffOnDelay(User userData, Entity user, Entity character)
    {
        // If there is an existing timer, restart that
        if (FrameTimers.TryGetValue(userData.PlatformId, out var timer))
        {
            timer.Start();
        }
        else
        {
            // Create a new timer that fires once after 100ms 
            var newTimer = new FrameTimer();
            newTimer.Initialise(() =>
            {
                // Apply the buff
                ApplyBuff(user, character, AppliedBuff);
                // Update the UI
                // ClientActionHandler.SendPlayerData(userData);
                // Remove the timer and dispose of it
                if (FrameTimers.Remove(userData.PlatformId, out timer)) timer.Stop();
            }, TimeSpan.FromMilliseconds(200), true).Start();
            
            FrameTimers.Add(userData.PlatformId, newTimer);
        }
    }
}