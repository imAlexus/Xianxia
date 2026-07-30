using System;
using Terraria.ModLoader;
using Xianxia.Common.Elements;

namespace Xianxia.Common.Players;

/// <summary>
/// Central runtime statistics for spiritual elements. Equipment, buffs,
/// techniques, and the future Spiritual Root system write modifiers here.
/// Multi-element content uses the average modifier of its declared elements.
/// </summary>
public class ElementalCultivationPlayer : ModPlayer
{
	public const float MaximumResistancePercent = 75f;
	public const float MaximumQiCostReductionPercent = 60f;
	public const float MaximumAffinity = 100f;

	private readonly float[] powerPercent =
		new float[SpiritualElementInfo.ElementCount];
	private readonly float[] resistancePercent =
		new float[SpiritualElementInfo.ElementCount];
	private readonly float[] qiCostReductionPercent =
		new float[SpiritualElementInfo.ElementCount];
	private readonly float[] masteryGainPercent =
		new float[SpiritualElementInfo.ElementCount];
	private readonly float[] affinity =
		new float[SpiritualElementInfo.ElementCount];

	public override void ResetEffects()
	{
		Array.Clear(powerPercent);
		Array.Clear(resistancePercent);
		Array.Clear(qiCostReductionPercent);
		Array.Clear(masteryGainPercent);
		Array.Clear(affinity);
	}

	public void AddPower(SpiritualElement elements, float percent) =>
		AddToElements(powerPercent, elements, percent);

	public void AddResistance(SpiritualElement elements, float percent) =>
		AddToElements(resistancePercent, elements, percent);

	public void AddQiCostReduction(SpiritualElement elements, float percent) =>
		AddToElements(qiCostReductionPercent, elements, percent);

	public void AddMasteryGain(SpiritualElement elements, float percent) =>
		AddToElements(masteryGainPercent, elements, percent);

	public void AddAffinity(SpiritualElement elements, float amount) =>
		AddToElements(affinity, elements, amount);

	public void SetAffinity(SpiritualElement element, float amount)
	{
		int index = SpiritualElementInfo.GetIndex(element);
		if (index < 0)
			return;

		affinity[index] = Math.Clamp(Sanitize(amount), 0f, MaximumAffinity);
	}

	public float GetPowerPercent(SpiritualElement elements) =>
		GetAverage(powerPercent, elements);

	public float GetResistancePercent(SpiritualElement elements) =>
		Math.Clamp(GetAverage(resistancePercent, elements),
			-100f, MaximumResistancePercent);

	public float GetQiCostReductionPercent(SpiritualElement elements) =>
		Math.Clamp(GetAverage(qiCostReductionPercent, elements),
			-100f, MaximumQiCostReductionPercent);

	public float GetMasteryGainPercent(SpiritualElement elements) =>
		GetAverage(masteryGainPercent, elements);

	public float GetAffinity(SpiritualElement elements) =>
		Math.Clamp(GetAverage(affinity, elements), 0f, MaximumAffinity);

	public float GetPowerMultiplier(SpiritualElement elements) =>
		Math.Max(0.1f, 1f + GetPowerPercent(elements) / 100f);

	public float GetIncomingDamageMultiplier(SpiritualElement elements) =>
		Math.Max(0.25f, 1f - GetResistancePercent(elements) / 100f);

	public float GetMasteryGainMultiplier(SpiritualElement elements) =>
		Math.Max(0.1f, 1f + GetMasteryGainPercent(elements) / 100f);

	public int ModifyQiCost(int baseCost, SpiritualElement elements)
	{
		if (baseCost <= 0)
			return 0;

		float multiplier = 1f - GetQiCostReductionPercent(elements) / 100f;
		return Math.Max(1, (int)MathF.Ceiling(baseCost * multiplier));
	}

	private static void AddToElements(
		float[] values,
		SpiritualElement elements,
		float amount)
	{
		float safeAmount = Sanitize(amount);
		foreach (SpiritualElement element in elements.Enumerate())
		{
			int index = SpiritualElementInfo.GetIndex(element);
			if (index >= 0)
				values[index] = Sanitize(values[index] + safeAmount);
		}
	}

	private static float GetAverage(float[] values, SpiritualElement elements)
	{
		float total = 0f;
		int count = 0;
		foreach (SpiritualElement element in elements.Enumerate())
		{
			int index = SpiritualElementInfo.GetIndex(element);
			if (index < 0)
				continue;

			total += values[index];
			count++;
		}
		return count > 0 ? total / count : 0f;
	}

	private static float Sanitize(float value) =>
		float.IsFinite(value) ? value : 0f;
}
