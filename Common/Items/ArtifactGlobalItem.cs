using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Artifacts;

namespace Xianxia.Common.Items;

public enum ArtifactQuality : byte
{
	Crude,
	Common,
	Refined,
	Earth,
	Heaven
}

public sealed class ArtifactGlobalItem : GlobalItem
{
	public override bool InstancePerEntity => true;
	public ArtifactQuality Quality { get; private set; } = ArtifactQuality.Common;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
		entity.ModItem is ISpiritualArtifact;

	public float PowerMultiplier => Quality switch
	{
		ArtifactQuality.Crude => 0.82f,
		ArtifactQuality.Refined => 1.12f,
		ArtifactQuality.Earth => 1.27f,
		ArtifactQuality.Heaven => 1.48f,
		_ => 1f
	};

	public float QiCostMultiplier => Quality switch
	{
		ArtifactQuality.Crude => 1.20f,
		ArtifactQuality.Refined => 0.92f,
		ArtifactQuality.Earth => 0.82f,
		ArtifactQuality.Heaven => 0.70f,
		_ => 1f
	};

	public float ForgingExperienceMultiplier => Quality switch
	{
		ArtifactQuality.Crude => 0.5f,
		ArtifactQuality.Refined => 2f,
		ArtifactQuality.Earth => 5f,
		ArtifactQuality.Heaven => 10f,
		_ => 1f
	};

	public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
	{
		if (item.ModItem is ISpiritualArtifact)
			damage *= PowerMultiplier;
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (item.ModItem is not ISpiritualArtifact artifact)
			return;

		tooltips.Add(new TooltipLine(Mod, "ArtifactQuality",
			Mod.GetLocalization("Forging.QualityTooltip").Format(
				Mod.GetLocalization($"Forging.Quality.{Quality}").Value,
				(int)MathF.Round(PowerMultiplier * 100f),
				(int)MathF.Round(QiCostMultiplier * 100f),
				(int)MathF.Round(ForgingExperienceMultiplier * 100f)))
		{
			OverrideColor = GetQualityColor()
		});
		string artifactDetails = GetArtifactDetails(item);
		if (!string.IsNullOrEmpty(artifactDetails))
		{
			tooltips.Add(new TooltipLine(Mod, "ArtifactDetails", artifactDetails)
			{
				OverrideColor = new Color(125, 235, 225)
			});
		}
		tooltips.Add(new TooltipLine(Mod, "ForgingMastery",
			Mod.GetLocalization("Forging.MasteryTooltip").Format(
				artifact.RequiredForgingTier,
				Main.LocalPlayer.GetModPlayer<AlchemyPlayer>()
					.GetTierRealmName(artifact.RequiredForgingTier),
				Main.LocalPlayer.GetModPlayer<AlchemyPlayer>()
					.GetStageName(artifact.RequiredForgingStage),
				artifact.ForgingExperience,
				GetCurrentForgingExperience(artifact))));
	}

	private int GetCurrentForgingExperience(ISpiritualArtifact artifact)
	{
		if (Main.gameMenu || Main.LocalPlayer is not { active: true })
			return Math.Max(1, (int)MathF.Round(
				artifact.ForgingExperience * ForgingExperienceMultiplier));
		return Main.LocalPlayer.GetModPlayer<ArtifactForgingPlayer>()
			.CalculateCraftExperience(artifact.RequiredForgingTier,
				artifact.ForgingExperience, ForgingExperienceMultiplier);
	}

	private string GetArtifactDetails(Item item)
	{
		if (item.ModItem is VerdantAntlerStaff)
			return Mod.GetLocalization("Forging.ArtifactQiCost")
				.Format(GetCurrentQiCost(5));
		if (item.ModItem is FlameSpiritFan)
			return Mod.GetLocalization("Forging.ArtifactQiCost")
				.Format(GetCurrentQiCost(15));
		if (item.ModItem is ThunderclapSeal)
			return Mod.GetLocalization("Forging.ArtifactQiCost")
				.Format(GetCurrentQiCost(28));
		if (item.ModItem is JadeAntlerTalisman)
		{
			return Mod.GetLocalization("Forging.JadeAntlerStats").Format(
				(int)MathF.Round(5f * PowerMultiplier),
				(int)MathF.Round(2f * PowerMultiplier),
				(int)MathF.Round(6f * PowerMultiplier));
		}
		if (item.ModItem is BeastSoulBanner)
		{
			return Mod.GetLocalization("Forging.BeastSoulStats").Format(
				(int)MathF.Round(16f * PowerMultiplier),
				(int)MathF.Round(10f * PowerMultiplier),
				(int)MathF.Round(8f * PowerMultiplier),
				(int)MathF.Round(72f * PowerMultiplier));
		}
		return string.Empty;
	}

	private int GetCurrentQiCost(int baseCost)
	{
		int qualityAdjustedCost = Math.Max(1,
			(int)MathF.Ceiling(baseCost * QiCostMultiplier));
		if (Main.gameMenu || Main.LocalPlayer is not { active: true })
			return qualityAdjustedCost;
		return Main.LocalPlayer.GetModPlayer<CultivationPlayer>()
			.GetFinalQiCost(qualityAdjustedCost);
	}

	public override void OnCreated(Item item, ItemCreationContext context)
	{
		if (context is not RecipeItemCreationContext
			|| item.ModItem is not ISpiritualArtifact artifact
			|| Main.gameMenu || Main.LocalPlayer is not { active: true })
			return;

		ArtifactForgingPlayer forging =
			Main.LocalPlayer.GetModPlayer<ArtifactForgingPlayer>();
		int requiredRank = artifact.RequiredForgingTier
			* ArtifactForgingPlayer.StagesPerTier + artifact.RequiredForgingStage;
		int margin = Math.Max(0, forging.RankIndex - requiredRank);
		int roll = Main.rand.Next(100) + margin * 4
			+ forging.GetNearbyForgeTier() * 11;
		Quality = roll switch
		{
			< 16 => ArtifactQuality.Crude,
			< 64 => ArtifactQuality.Common,
			< 89 => ArtifactQuality.Refined,
			< 109 => ArtifactQuality.Earth,
			_ => ArtifactQuality.Heaven
		};
		int experience = forging.CalculateCraftExperience(
			artifact.RequiredForgingTier, artifact.ForgingExperience,
			ForgingExperienceMultiplier);
		forging.RecordCraftedArtifact(experience);
	}

	public override void SaveData(Item item, TagCompound tag)
	{
		if (Quality != ArtifactQuality.Common)
			tag["artifactQuality"] = (byte)Quality;
	}

	public override void LoadData(Item item, TagCompound tag) =>
		Quality = tag.ContainsKey("artifactQuality")
			? (ArtifactQuality)Math.Clamp(tag.GetByte("artifactQuality"),
				(byte)ArtifactQuality.Crude, (byte)ArtifactQuality.Heaven)
			: ArtifactQuality.Common;

	public override void NetSend(Item item, BinaryWriter writer) =>
		writer.Write((byte)Quality);

	public override void NetReceive(Item item, BinaryReader reader) =>
		Quality = (ArtifactQuality)Math.Clamp(reader.ReadByte(),
			(byte)ArtifactQuality.Crude, (byte)ArtifactQuality.Heaven);

	private Color GetQualityColor() => Quality switch
	{
		ArtifactQuality.Crude => new Color(155, 125, 105),
		ArtifactQuality.Refined => new Color(90, 225, 155),
		ArtifactQuality.Earth => new Color(235, 180, 70),
		ArtifactQuality.Heaven => new Color(225, 105, 255),
		_ => new Color(205, 210, 220)
	};
}
