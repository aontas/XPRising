using System;
using ProjectM;
using XPRising.Systems;

namespace XPRising.Models;

public struct MasteryData()
{
    private const double MinGrowth = 0;
    public const double MaxMastery = 100;
    private const double BaseEffectiveness = 1;

    private double _mastery = 0;
    private double _effectiveness = BaseEffectiveness;
    public double Mastery { get => _mastery; set => _mastery = Math.Clamp(value, 0, MaxMastery); }
    public double Effectiveness { get => _effectiveness; set => _effectiveness = Math.Max(value, BaseEffectiveness); }
    public double Growth = 1;

    public double CalculateBaseMasteryGrowth(double value, Random random)
    {
        return value * Math.Max(random.NextDouble() * 0.8, 0.2) * Growth * 0.001;
    }
    
    public MasteryData ResetMastery(double maxEffectiveness, double growthPerEffectiveness)
    {
        Effectiveness = Math.Min(maxEffectiveness, Effectiveness + Mastery / MaxMastery);
        Mastery = 0;

        // Set the growth rate to a reduced amount
        var additionalEffectiveness = Effectiveness - BaseEffectiveness;
        if (additionalEffectiveness > 0 && growthPerEffectiveness > 0)
        {
            Growth = Math.Max(MinGrowth,
                1 - additionalEffectiveness / (additionalEffectiveness + Math.Abs(growthPerEffectiveness)));
        }

        return this;
    }

    public override string ToString()
    {
        return $"[{Mastery:F3},{Effectiveness:F3},{Growth:F3}]";
    }
}

public struct StatConfig(UnitStatType type, double strength, double rate)
{
    public UnitStatType type = type;
    public double strength = strength;
    public double rate = rate;
}