using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Content.Tiles;

namespace Xianxia.Common.Players;

public class ArtifactForgingPlayer : ModPlayer
{
	public const int MaxTier = 4;
	public const int MaxStage = 2;
	public const int StagesPerTier = 3;

	public int Tier { get; private set; }
	public int Stage { get; private set; }
	public int Experience { get; private set; }
	public int RankIndex => Tier * StagesPerTier + Stage;
	public bool IsMaximumRank => Tier == MaxTier && Stage == MaxStage;
	public int ExperienceRequired => IsMaximumRank ? 0 : 55 + RankIndex * 35;
	public string TierRealmName => Player.GetModPlayer<AlchemyPlayer>().GetTierRealmName(Tier);
	public string StageName => Player.GetModPlayer<AlchemyPlayer>().GetStageName(Stage);

	public override void Initialize()
	{
		Tier = 0;
		Stage = 0;
		Experience = 0;
	}

	public override void SaveData(TagCompound tag)
	{
		tag["artifactForgingTier"] = Tier;
		tag["artifactForgingStage"] = Stage;
		tag["artifactForgingExperience"] = Experience;
	}

	public override void LoadData(TagCompound tag)
	{
		Tier = Math.Clamp(tag.GetInt("artifactForgingTier"), 0, MaxTier);
		Stage = Math.Clamp(tag.GetInt("artifactForgingStage"), 0, MaxStage);
		Experience = Math.Max(0, tag.GetInt("artifactForgingExperience"));
		Normalize(showMessage: false);
	}

	public override void OnEnterWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
			SyncState();
	}

	public override void CopyClientState(ModPlayer targetCopy)
	{
		ArtifactForgingPlayer clone = (ArtifactForgingPlayer)targetCopy;
		clone.Tier = Tier;
		clone.Stage = Stage;
		clone.Experience = Experience;
	}

	public override void SendClientChanges(ModPlayer clientPlayer)
	{
		ArtifactForgingPlayer old = (ArtifactForgingPlayer)clientPlayer;
		if (old.Tier != Tier || old.Stage != Stage || old.Experience != Experience)
			SyncState();
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
		Xianxia.SendArtifactForgingState(Player.whoAmI, Tier, Stage, Experience,
			toWho, fromWho);

	internal void SetStateFromNetwork(int tier, int stage, int experience)
	{
		Tier = Math.Clamp(tier, 0, MaxTier);
		Stage = Math.Clamp(stage, 0, MaxStage);
		Experience = Math.Max(0, experience);
		Normalize(showMessage: false);
	}

	public bool MeetsRequirement(int tier, int stage) =>
		RankIndex >= Math.Clamp(tier, 0, MaxTier) * StagesPerTier
			+ Math.Clamp(stage, 0, MaxStage);

	public int GetNearbyForgeTier()
	{
		if (Player.adjTile[ModContent.TileType<ProfoundArtifactForgeTile>()])
			return 2;
		if (Player.adjTile[ModContent.TileType<SpiritJadeArtifactForgeTile>()])
			return 1;
		return 0;
	}

	public void RecordCraftedArtifact(int experience)
	{
		if (experience <= 0 || IsMaximumRank)
			return;

		Experience += experience;
		Normalize(showMessage: Player.whoAmI == Main.myPlayer);
		SyncState();
	}

	public int CalculateCraftExperience(int requiredTier, int baseExperience,
		float qualityExperienceMultiplier)
	{
		int tierDifference = Math.Max(0, Tier - Math.Clamp(requiredTier, 0, MaxTier));
		float obsoleteTierMultiplier = tierDifference switch
		{
			0 => 1f,
			1 => 0.25f,
			2 => 0.10f,
			3 => 0.05f,
			_ => 0.02f
		};
		return Math.Max(1, (int)MathF.Round(Math.Max(1, baseExperience)
			* Math.Max(0.01f, qualityExperienceMultiplier)
			* obsoleteTierMultiplier));
	}

	internal void DebugSetRank(int tier, int stage)
	{
		Tier = Math.Clamp(tier, 0, MaxTier);
		Stage = Math.Clamp(stage, 0, MaxStage);
		Experience = 0;
		SyncState();
	}

	private void Normalize(bool showMessage)
	{
		int oldTier = Tier;
		int oldStage = Stage;
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

		if (showMessage && (oldTier != Tier || oldStage != Stage))
			Main.NewText(Mod.GetLocalization("Forging.Advanced")
				.Format(Tier, TierRealmName, StageName), new Color(255, 185, 75));
	}

	private void SyncState()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			Xianxia.SendArtifactForgingState(Player.whoAmI, Tier, Stage, Experience);
	}
}
