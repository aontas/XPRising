using System;
using ProjectM;

namespace XPRising.Models;

public struct MasteryData()
{
    private const double MinGrowth = 0;
    private const double MaxMastery = 100;
    private const double BaseEffectiveness = 1;

    private double _mastery = 0;
    private double _effectiveness = BaseEffectiveness;
    public double Mastery { get => _mastery; set => _mastery = Math.Clamp(value, 0, MaxMastery); }
    public double Effectiveness { get => _effectiveness; set => _effectiveness = Math.Max(value, BaseEffectiveness); }
    public double Growth = 1;

    public float CalculateMasteryPercentage(float requiredMastery)
    {
        return (float)((_mastery - requiredMastery) / (MaxMastery - requiredMastery));
    }
    
    public double CalculateBaseMasteryGrowth(double value)
    {
        return value * Growth;
    }
    
    public MasteryData ResetMastery(double maxEffectiveness, double growthPerEffectiveness)
    {
        Effectiveness = Math.Min(maxEffectiveness, Effectiveness + _mastery / MaxMastery);
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