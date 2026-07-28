using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ModLoader;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Sect;

namespace Xianxia.Common.Systems;

public class SectContributionCurrency : CustomCurrencySingleCoin
{
	private readonly int tokenType;

	public SectContributionCurrency(int tokenType)
		: this(tokenType, "Mods.Xianxia.Currencies.SectContribution",
			new Color(100, 235, 160))
	{
	}

	protected SectContributionCurrency(int tokenType, string textKey,
		Color textColor) : base(tokenType, 999999L)
	{
		this.tokenType = tokenType;
		CurrencyTextKey = textKey;
		CurrencyTextColor = textColor;
		CurrencyDrawScale = 1f;
	}

	public override bool Accepts(Item item) =>
		item is not null && !item.IsAir && item.type == tokenType;

	public override long CountCurrency(out bool overFlowing, Item[] inventory,
		params int[] ignoreSlots)
	{
		overFlowing = false;
		HashSet<int> ignored = new(ignoreSlots);
		long total = 0L;
		for (int slot = 0; slot < inventory.Length; slot++)
		{
			if (!ignored.Contains(slot) && Accepts(inventory[slot]))
				total += inventory[slot].stack;
		}
		return total;
	}

	public override bool TryPurchasing(long price, List<Item[]> inventories,
		List<Point> slotCoins, List<Point> slotsEmpty, List<Point> slotEmptyBank,
		List<Point> slotEmptyBank2, List<Point> slotEmptyBank3,
		List<Point> slotEmptyBank4)
	{
		long available = 0L;
		foreach (Item[] inventory in inventories)
			foreach (Item item in inventory)
				if (Accepts(item))
					available += item.stack;

		if (available < price)
			return false;

		long remaining = price;
		foreach (Item[] inventory in inventories)
		{
			for (int slot = 0; slot < inventory.Length && remaining > 0; slot++)
			{
				Item item = inventory[slot];
				if (!Accepts(item))
					continue;
				int consumed = (int)System.Math.Min(remaining, item.stack);
				item.stack -= consumed;
				remaining -= consumed;
				if (item.stack <= 0)
					item.TurnToAir();
			}
			if (remaining <= 0)
				break;
		}
		return remaining <= 0;
	}
}

public sealed class SpiritStoneCurrency : SectContributionCurrency
{
	public SpiritStoneCurrency(int itemType)
		: base(itemType, "Mods.Xianxia.Currencies.SpiritStone",
			new Color(85, 225, 255))
	{
	}
}

public static class SectCurrencySystem
{
	public static int ContributionCurrencyId { get; private set; } = -1;
	public static int SpiritStoneCurrencyId { get; private set; } = -1;

	public static void Register()
	{
		ContributionCurrencyId = CustomCurrencyManager.RegisterCurrency(
			new SectContributionCurrency(ModContent.ItemType<SectContributionToken>()));
		SpiritStoneCurrencyId = CustomCurrencyManager.RegisterCurrency(
			new SpiritStoneCurrency(ModContent.ItemType<SpiritStone>()));
	}

	public static void Reset()
	{
		ContributionCurrencyId = -1;
		SpiritStoneCurrencyId = -1;
	}
}
