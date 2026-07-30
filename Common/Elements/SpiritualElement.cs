using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace Xianxia.Common.Elements;

[Flags]
public enum SpiritualElement : ushort
{
	None = 0,
	Wood = 1 << 0,
	Fire = 1 << 1,
	Earth = 1 << 2,
	Metal = 1 << 3,
	Water = 1 << 4,
	Lightning = 1 << 5,
	Ice = 1 << 6,
	Wind = 1 << 7,
	Void = 1 << 8
}

/// <summary>
/// Implemented by techniques, items, projectiles, buffs, or other content that
/// carries one or more spiritual elements.
/// </summary>
public interface ISpiritualElementSource
{
	SpiritualElement SpiritualElements { get; }
}

public static class SpiritualElementInfo
{
	public const int ElementCount = 9;

	public const SpiritualElement BasicElementMask =
		SpiritualElement.Wood | SpiritualElement.Fire | SpiritualElement.Earth
		| SpiritualElement.Metal | SpiritualElement.Water;

	public const SpiritualElement MutatedElementMask =
		SpiritualElement.Lightning | SpiritualElement.Ice | SpiritualElement.Wind
		| SpiritualElement.Void;

	public const SpiritualElement AllElementMask = BasicElementMask | MutatedElementMask;

	private static readonly SpiritualElement[] OrderedElements =
	[
		SpiritualElement.Wood,
		SpiritualElement.Fire,
		SpiritualElement.Earth,
		SpiritualElement.Metal,
		SpiritualElement.Water,
		SpiritualElement.Lightning,
		SpiritualElement.Ice,
		SpiritualElement.Wind,
		SpiritualElement.Void
	];

	public static IReadOnlyList<SpiritualElement> Elements => OrderedElements;

	public static int GetIndex(SpiritualElement element) =>
		element switch
		{
			SpiritualElement.Wood => 0,
			SpiritualElement.Fire => 1,
			SpiritualElement.Earth => 2,
			SpiritualElement.Metal => 3,
			SpiritualElement.Water => 4,
			SpiritualElement.Lightning => 5,
			SpiritualElement.Ice => 6,
			SpiritualElement.Wind => 7,
			SpiritualElement.Void => 8,
			_ => -1
		};

	public static bool HasElement(
		this SpiritualElement elements,
		SpiritualElement element) =>
		element != SpiritualElement.None && (elements & element) == element;

	public static bool IsSingleElement(this SpiritualElement elements)
	{
		ushort value = (ushort)(elements & AllElementMask);
		return value != 0 && (value & (value - 1)) == 0
			&& (elements & ~AllElementMask) == SpiritualElement.None;
	}

	public static bool IsBasicElement(this SpiritualElement element) =>
		element.IsSingleElement() && (element & BasicElementMask) != 0;

	public static bool IsMutatedElement(this SpiritualElement element) =>
		element.IsSingleElement() && (element & MutatedElementMask) != 0;

	public static int CountElements(this SpiritualElement elements)
	{
		int count = 0;
		ushort value = (ushort)(elements & AllElementMask);
		while (value != 0)
		{
			count += value & 1;
			value >>= 1;
		}
		return count;
	}

	public static IEnumerable<SpiritualElement> Enumerate(
		this SpiritualElement elements)
	{
		foreach (SpiritualElement element in OrderedElements)
		{
			if (elements.HasElement(element))
				yield return element;
		}
	}

	public static string GetLocalizationKey(SpiritualElement element) =>
		element switch
		{
			SpiritualElement.Wood => "Wood",
			SpiritualElement.Fire => "Fire",
			SpiritualElement.Earth => "Earth",
			SpiritualElement.Metal => "Metal",
			SpiritualElement.Water => "Water",
			SpiritualElement.Lightning => "Lightning",
			SpiritualElement.Ice => "Ice",
			SpiritualElement.Wind => "Wind",
			SpiritualElement.Void => "Void",
			_ => "Neutral"
		};

	public static string GetDisplayName(Mod mod, SpiritualElement elements)
	{
		if (elements == SpiritualElement.None)
			return mod.GetLocalization("SpiritualElements.Neutral").Value;

		List<string> names = [];
		foreach (SpiritualElement element in elements.Enumerate())
		{
			names.Add(mod.GetLocalization(
				$"SpiritualElements.{GetLocalizationKey(element)}").Value);
		}
		return names.Count > 0
			? string.Join(" / ", names)
			: mod.GetLocalization("SpiritualElements.Neutral").Value;
	}

	public static Color GetColor(SpiritualElement elements)
	{
		if (elements == SpiritualElement.None)
			return new Color(190, 200, 215);

		int red = 0;
		int green = 0;
		int blue = 0;
		int count = 0;
		foreach (SpiritualElement element in elements.Enumerate())
		{
			Color color = GetSingleElementColor(element);
			red += color.R;
			green += color.G;
			blue += color.B;
			count++;
		}
		return count > 0
			? new Color(red / count, green / count, blue / count)
			: new Color(190, 200, 215);
	}

	private static Color GetSingleElementColor(SpiritualElement element) =>
		element switch
		{
			SpiritualElement.Wood => new Color(80, 220, 115),
			SpiritualElement.Fire => new Color(245, 85, 45),
			SpiritualElement.Earth => new Color(205, 155, 75),
			SpiritualElement.Metal => new Color(205, 220, 235),
			SpiritualElement.Water => new Color(60, 150, 245),
			SpiritualElement.Lightning => new Color(120, 225, 255),
			SpiritualElement.Ice => new Color(155, 235, 255),
			SpiritualElement.Wind => new Color(145, 245, 195),
			SpiritualElement.Void => new Color(175, 85, 235),
			_ => new Color(190, 200, 215)
		};
}
