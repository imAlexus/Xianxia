using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Alchemy;

namespace Xianxia.Common.Items;

public enum PillQuality : byte
{
	Impure,
	Common,
	Refined,
	Earth,
	Heaven
}

public sealed class AlchemyGlobalItem : GlobalItem
{
	public override bool InstancePerEntity => true;
	public PillQuality Quality { get; private set; } = PillQuality.Common;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
		entity.ModItem is IAlchemyPill;

	public float EffectMultiplier => Quality switch
	{
		PillQuality.Impure => 0.70f,
		PillQuality.Refined => 1.15f,
		PillQuality.Earth => 1.35f,
		PillQuality.Heaven => 1.60f,
		_ => 1f
	};

	public float SaturationMultiplier => Quality switch
	{
		PillQuality.Impure => 1.35f,
		PillQuality.Refined => 0.90f,
		PillQuality.Earth => 0.75f,
		PillQuality.Heaven => 0.60f,
		_ => 1f
	};

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (item.ModItem is not IAlchemyPill pill)
			return;

		AlchemyPlayer alchemy = Main.LocalPlayer.GetModPlayer<AlchemyPlayer>();
		tooltips.Add(new TooltipLine(Mod, "PillQuality",
			Mod.GetLocalization("Alchemy.PillQualityTooltip").Format(
				GetQualityName(), (int)MathF.Round(EffectMultiplier * 100f),
				(int)MathF.Round(SaturationMultiplier * 100f)))
		{
			OverrideColor = GetQualityColor()
		});
		tooltips.Add(new TooltipLine(Mod, "AlchemyMastery",
			Mod.GetLocalization("Alchemy.PillMasteryTooltip").Format(
				pill.RequiredAlchemyTier, alchemy.GetTierRealmName(pill.RequiredAlchemyTier),
				alchemy.GetStageName(pill.RequiredAlchemyStage), pill.AlchemyExperience)));
		tooltips.Add(new TooltipLine(Mod, "PillSaturation",
			Mod.GetLocalization("Alchemy.PillSaturationTooltip").Format(
				GetAdjustedSaturationCost(item, pill)))
		{
			OverrideColor = new Color(210, 145, 225)
		});
	}

	public override bool CanUseItem(Item item, Player player)
	{
		if (item.ModItem is not IAlchemyPill pill)
			return true;

		AlchemyPlayer alchemy = player.GetModPlayer<AlchemyPlayer>();
		int saturationCost = GetAdjustedSaturationCost(item, pill);
		if (!alchemy.CanConsumePill(saturationCost))
		{
			if (player.whoAmI == Main.myPlayer)
				Main.NewText(Mod.GetLocalization("Alchemy.TooSaturated").Value,
					235, 105, 130);
			return false;
		}

		if (pill.BaseBuffDuration > 0)
			item.buffTime = Math.Max(1, (int)MathF.Round(
				pill.BaseBuffDuration
				* alchemy.PillEffectiveness
				* EffectMultiplier));
		return true;
	}

	public override void OnConsumeItem(Item item, Player player)
	{
		if (item.ModItem is IAlchemyPill pill)
			player.GetModPlayer<AlchemyPlayer>().AddSaturation(
				GetAdjustedSaturationCost(item, pill));
	}

	public override void OnCreated(Item item, ItemCreationContext context)
	{
		if (context is not RecipeItemCreationContext
			|| item.ModItem is not IAlchemyPill pill
			|| Main.gameMenu || Main.LocalPlayer is not { active: true })
			return;

		Main.LocalPlayer.GetModPlayer<AlchemyPlayer>()
			.HandleCraftedPill(item, pill);
		Main.LocalPlayer.GetModPlayer<SectPlayer>().RecordPillCrafted();
	}

	public override bool CanStack(Item destination, Item source)
	{
		return destination.GetGlobalItem<AlchemyGlobalItem>().Quality
			== source.GetGlobalItem<AlchemyGlobalItem>().Quality;
	}

	public override void SaveData(Item item, TagCompound tag)
	{
		if (Quality != PillQuality.Common)
			tag["pillQuality"] = (byte)Quality;
	}

	public override void LoadData(Item item, TagCompound tag)
	{
		Quality = tag.ContainsKey("pillQuality")
			? (PillQuality)Math.Clamp(tag.GetByte("pillQuality"),
				(byte)PillQuality.Impure, (byte)PillQuality.Heaven)
			: PillQuality.Common;
	}

	public override void NetSend(Item item, BinaryWriter writer)
	{
		writer.Write((byte)Quality);
	}

	public override void NetReceive(Item item, BinaryReader reader)
	{
		Quality = (PillQuality)Math.Clamp(reader.ReadByte(),
			(byte)PillQuality.Impure, (byte)PillQuality.Heaven);
	}

	public void AssignCraftedQuality(
		AlchemyPlayer alchemy, IAlchemyPill pill, bool impure)
	{
		if (impure)
		{
			Quality = PillQuality.Impure;
			return;
		}

		int requiredRank = pill.RequiredAlchemyTier * AlchemyPlayer.StagesPerTier
			+ pill.RequiredAlchemyStage;
		int masteryMargin = Math.Max(0, alchemy.RankIndex - requiredRank);
		int qualityRoll = Main.rand.Next(100)
			+ masteryMargin * 4
			+ alchemy.GetNearbyCauldronTier() * 10;
		Quality = qualityRoll switch
		{
			< 62 => PillQuality.Common,
			< 88 => PillQuality.Refined,
			< 108 => PillQuality.Earth,
			_ => PillQuality.Heaven
		};
	}

	public static float GetCombinedEffectiveness(Item item, Player player)
	{
		return player.GetModPlayer<AlchemyPlayer>().PillEffectiveness
			* item.GetGlobalItem<AlchemyGlobalItem>().EffectMultiplier;
	}

	public static int GetAdjustedSaturationCost(Item item, IAlchemyPill pill)
	{
		if (pill.SaturationCost <= 0)
			return 0;
		float multiplier = item.GetGlobalItem<AlchemyGlobalItem>()
			.SaturationMultiplier;
		return Math.Max(1,
			(int)MathF.Round(pill.SaturationCost * multiplier));
	}

	private string GetQualityName() =>
		Mod.GetLocalization($"Alchemy.Quality.{Quality}").Value;

	private Color GetQualityColor() => Quality switch
	{
		PillQuality.Impure => new Color(155, 125, 105),
		PillQuality.Refined => new Color(90, 225, 155),
		PillQuality.Earth => new Color(235, 180, 70),
		PillQuality.Heaven => new Color(225, 105, 255),
		_ => new Color(205, 210, 220)
	};
}
