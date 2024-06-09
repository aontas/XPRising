using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace XPRising.Models;

public struct SpawnNpcListen
{
    public float Duration { get; set; }
    public int EntityIndex { get; set; }
    public int EntityVersion { get; set; }
    public SpawnOptions Options { get; set; }
    public bool Process { get; set; }

    public SpawnNpcListen(float duration = 0.0f, int entityIndex = 0, int entityVersion = 0, SpawnOptions options = default, bool process = true)
    {
        Duration = duration;
        EntityIndex = entityIndex;
        EntityVersion = entityVersion;
        Options = options;
        Process = process;
    }

    public Entity getEntity()
    {
        Entity entity = new Entity()
        {
            Index = this.EntityIndex,
            Version = this.EntityVersion,
        };
        return entity;
    }
}

public struct SpawnOptions
{
    public bool ModifyBlood { get; set; }
    public PrefabGUID BloodType { get; set; }
    public float BloodQuality { get; set; }
    public bool BloodConsumeable { get; set; }
    public bool ModifyStats { get; set; }
    public UnitStats UnitStats { get; set; }
    public bool Process { get; set; }

    public SpawnOptions(bool modifyBlood = false, PrefabGUID bloodType = default, float bloodQuality = 0, bool bloodConsumeable = true, bool modifyStats = false, UnitStats unitStats = default, bool process = false)
    {
        ModifyBlood = modifyBlood;
        BloodType = bloodType;
        BloodQuality = bloodQuality;
        BloodConsumeable = bloodConsumeable;
        ModifyStats = modifyStats;
        UnitStats = unitStats;
        Process = process;
    }
}