using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Content.Items.Alchemy;
using Xianxia.Content.Tiles;
using Xianxia.Common.Items;

namespace Xianxia.Common.Players;

public class AlchemyPlayer : ModPlayer
{
	public const int MaxTier = 4;
	public const int StagesPerTier = 3;
	public const int MaxStage = StagesPerTier - 1;
	public const int MaxRankIndex = MaxTier * StagesPerTier + MaxStage;
	public const float MaximumSaturation = 100f;

	private int saturationDecayTimer;

	public int Tier { get; private set; }
	public int Stage { get; private set; }
	public int Experience { get; private set; }
	public float Saturation { get; private set; }
	public int RankIndex => Tier * StagesPerTier + Stage;
	public bool IsMaximumRank => RankIndex >= MaxRankIndex;
	public string TierRealmName => GetTierRealmName(Tier);
	public string StageName => GetStageName(Stage);
	public int ExperienceRequired => IsMaximumRank ? 0 : 40 + RankIndex * 25;
	public float PillEffectiveness => MathHelper.Lerp(1f, 0.5f,
		MathHelper.Clamp(Saturation / MaximumSaturation, 0f, 1f));
	public int BonusYieldPercent => Math.Min(45, RankIndex * 2 + GetNearbyCauldronTier() * 5);
	public int ImpurityChancePercent => Math.Max(5, 35 - RankIndex * 2 - GetNearbyCauldronTier() * 7);

	public override void Initialize()
	{
		Tier = 0;
		Stage = 0;
		Experience = 0;
		Saturation = 0f;
		saturationDecayTimer = 0;
	}

	public override void SaveData(TagCompound tag)
	{
		tag["alchemyProgressionVersion"] = 2;
		tag["alchemyTier"] = Tier;
		tag["alchemyStage"] = Stage;
		tag["alchemyExperience"] = Experience;
		tag["pillSaturation"] = Saturation;
	}

	public override void LoadData(TagCompound tag)
	{
		if (tag.ContainsKey("alchemyTier"))
		{
			Tier = Math.Clamp(tag.GetInt("alchemyTier"), 0, MaxTier);
			Stage = Math.Clamp(tag.GetInt("alchemyStage"), 0, MaxStage);
		}
		else
		{
			// Development migration from the unreleased 0-10 Mastery prototype.
			int oldLevel = Math.Clamp(tag.GetInt("alchemyLevel"), 0, 10);
			int migratedRank = oldLevel == 10
				? MaxRankIndex
				: oldLevel / 2 * StagesPerTier + oldLevel % 2;
			Tier = migratedRank / StagesPerTier;
			Stage = migratedRank % StagesPerTier;
		}

		Experience = IsMaximumRank ? 0 : Math.Max(0, tag.GetInt("alchemyExperience"));
		Saturation = MathHelper.Clamp(tag.GetFloat("pillSaturation"), 0f, MaximumSaturation);
		NormalizeExperience();
	}

	public override void PostUpdate()
	{
		if (Saturation <= 0f)
			return;
		if (++saturationDecayTimer >= 180)
		{
			saturationDecayTimer = 0;
			Saturation = Math.Max(0f, Saturation - 1f);
		}
	}

	public override void OnEnterWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
			Xianxia.SendAlchemyState(Player.whoAmI, Tier, Stage, Experience, Saturation);
	}

	public override void CopyClientState(ModPlayer targetCopy)
	{
		AlchemyPlayer clone = (AlchemyPlayer)targetCopy;
		clone.Tier = Tier;
		clone.Stage = Stage;
		clone.Experience = Experience;
		clone.Saturation = Saturation;
	}

	public override void SendClientChanges(ModPlayer clientPlayer)
	{
		AlchemyPlayer old = (AlchemyPlayer)clientPlayer;
		if (old.Tier != Tier || old.Stage != Stage || old.Experience != Experience
			|| Math.Abs(old.Saturation - Saturation) > 0.001f)
			Xianxia.SendAlchemyState(Player.whoAmI, Tier, Stage, Experience, Saturation);
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
		Xianxia.SendAlchemyState(Player.whoAmI, Tier, Stage, Experience, Saturation,
			toWho, fromWho);

	internal void SetAlchemyStateFromNetwork(int tier, int stage, int experience,
		float saturation)
	{
		Tier = Math.Clamp(tier, 0, MaxTier);
		Stage = Math.Clamp(stage, 0, MaxStage);
		Experience = Math.Max(0, experience);
		Saturation = MathHelper.Clamp(saturation, 0f, MaximumSaturation);
		NormalizeExperience();
	}

	public bool MeetsRequirement(int requiredTier, int requiredStage) =>
		RankIndex >= Math.Clamp(requiredTier, 0, MaxTier) * StagesPerTier
			+ Math.Clamp(requiredStage, 0, MaxStage);

	public bool CanConsumePill(int saturationCost) =>
		Saturation + saturationCost <= MaximumSaturation;

	public void AddSaturation(int amount) =>
		Saturation = MathHelper.Clamp(Saturation + amount, 0f, MaximumSaturation);

	public void ReduceSaturation(float amount) =>
		Saturation = Math.Max(0f, Saturation - amount);

	public void GainExperience(int amount)
	{
		if (amount <= 0 || IsMaximumRank)
			return;

		int previousTier = Tier;
		int previousStage = Stage;
		Experience += amount;
		NormalizeExperience();
		if ((Tier != previousTier || Stage != previousStage) && Player.whoAmI == Main.myPlayer)
		{
			string key = Tier > previousTier ? "Alchemy.TierUp" : "Alchemy.StageUp";
			Main.NewText(Mod.GetLocalization(key).Format(Tier, TierRealmName, StageName),
				Tier > previousTier ? new Color(245, 205, 95) : new Color(105, 235, 185));
		}
	}

	public void HandleCraftedPill(Item result, IAlchemyPill pill)
	{
		int cauldronTier = GetNearbyCauldronTier();
		int bonusChance = Math.Min(45, RankIndex * 2 + cauldronTier * 5);
		int impurityChance = Math.Max(5, 35 - RankIndex * 2 - cauldronTier * 7);
		bool impure = Main.rand.Next(100) < impurityChance;
		result.GetGlobalItem<AlchemyGlobalItem>()
			.AssignCraftedQuality(this, pill, impure);
		GainExperience(pill.AlchemyExperience);
		if (Main.rand.Next(100) < bonusChance)
		{
			result.stack++;
			Main.NewText(Mod.GetLocalization("Alchemy.BonusYield").Value,
				new Color(115, 240, 205));
		}

		if (impure)
		{
			Player.QuickSpawnItem(Player.GetSource_Misc("XianxiaAlchemyImpurity"),
				ModContent.ItemType<PillDregs>());
		}
	}

	public int GetNearbyCauldronTier()
	{
		if (Player.adjTile[ModContent.TileType<ProfoundAlchemyCauldronTile>()])
			return 2;
		if (Player.adjTile[ModContent.TileType<SpiritJadeCauldronTile>()])
			return 1;
		return 0;
	}

	internal void DebugSetRank(int tier, int stage)
	{
		Tier = Math.Clamp(tier, 0, MaxTier);
		Stage = Math.Clamp(stage, 0, MaxStage);
		Experience = 0;
	}

	internal void DebugSetSaturation(float amount) =>
		Saturation = MathHelper.Clamp(amount, 0f, MaximumSaturation);

	public string GetTierRealmName(int tier) =>
		Mod.GetLocalization($"Cultivation.Realms.{GetTierRealmKey(tier)}").Value;

	public string GetStageName(int stage) =>
		Mod.GetLocalization($"Alchemy.Stages.{GetStageKey(stage)}").Value;

	public static string GetTierRealmKey(int tier) => Math.Clamp(tier, 0, MaxTier) switch
	{
		0 => "Mortal",
		1 => "QiCondensation",
		2 => "FoundationEstablishment",
		3 => "CoreFormation",
		_ => "NascentSoul"
	};

	public static string GetStageKey(int stage) => Math.Clamp(stage, 0, MaxStage) switch
	{
		0 => "Low",
		1 => "Middle",
		_ => "High"
	};

	private void NormalizeExperience()
	{
		while (!IsMaximumRank && Experience >= ExperienceRequired)
		{
			Experience -= ExperienceRequired;
			if (Stage < MaxStage)
				Stage++;
			else
			{
				Tier++;
				Stage = 0;
			}
		}
		if (IsMaximumRank)
			Experience = 0;
	}
}
