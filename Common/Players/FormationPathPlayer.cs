using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Xianxia.Common.Players;

public class FormationPathPlayer : ModPlayer
{
	public const int MaxTier = 4;
	public const int MaxStage = 2;
	private const int DamagePerExperience = 50;

	private static readonly int[] DefenseCycleTrials = [2, 8, 18, 32, 50];
	private static readonly int[] DamageTrials = [500, 2500, 7500, 18000, 40000];
	private static readonly int[] TribulationTrials = [0, 0, 3, 12, 24];

	public int Tier { get; private set; }
	public int Stage { get; private set; }
	public int Experience { get; private set; }
	public int DefenseCycles { get; private set; }
	public int DamageHandled { get; private set; }
	public int TribulationStrikesIntercepted { get; private set; }
	public int VeinsLinked { get; private set; }
	private int damageExperienceRemainder;

	public int RankIndex => Tier * 3 + Stage;
	public bool IsMaximumRank => Tier == MaxTier && Stage == MaxStage;
	public int ExperienceRequired => IsMaximumRank ? 0 : 80 + RankIndex * 55;
	public string TierRealmName => Player.GetModPlayer<AlchemyPlayer>().GetTierRealmName(Tier);
	public string StageName => Player.GetModPlayer<AlchemyPlayer>().GetStageName(Stage);

	public string CurrentTrialLocalizationKey => Stage switch
	{
		0 => "AbilityTree.Paths.Formations.Trials.Combat",
		1 => "AbilityTree.Paths.Formations.Trials.Damage",
		2 when Tier == 0 => "AbilityTree.Paths.Formations.Trials.Vein",
		2 when Tier == 1 => "AbilityTree.Paths.Formations.Trials.Foundation",
		_ => "AbilityTree.Paths.Formations.Trials.Tribulation"
	};

	public int CurrentTrialProgress => Stage switch
	{
		0 => DefenseCycles,
		1 => DamageHandled,
		2 when Tier == 0 => VeinsLinked,
		2 when Tier == 1 => DamageHandled,
		_ => TribulationStrikesIntercepted
	};

	public int CurrentTrialTarget => Stage switch
	{
		0 => DefenseCycleTrials[Tier],
		1 => DamageTrials[Tier],
		2 when Tier == 0 => 1,
		2 when Tier == 1 => 5000,
		_ => TribulationTrials[Tier]
	};

	public bool RealmRequirementMet => Stage < MaxStage || Tier >= MaxTier
		|| Player.GetModPlayer<CultivationPlayer>().RealmIndex >= Tier + 1;
	public bool CurrentTrialComplete =>
		CurrentTrialProgress >= CurrentTrialTarget && RealmRequirementMet;

	public override void Initialize()
	{
		Tier = 0;
		Stage = 0;
		Experience = 0;
		DefenseCycles = 0;
		DamageHandled = 0;
		TribulationStrikesIntercepted = 0;
		VeinsLinked = 0;
		damageExperienceRemainder = 0;
	}

	public override void SaveData(TagCompound tag)
	{
		tag["formationPathTier"] = Tier;
		tag["formationPathStage"] = Stage;
		tag["formationPathExperience"] = Experience;
		tag["formationDefenseCycles"] = DefenseCycles;
		tag["formationDamageHandled"] = DamageHandled;
		tag["formationTribulationStrikes"] = TribulationStrikesIntercepted;
		tag["formationVeinsLinked"] = VeinsLinked;
		tag["formationDamageExpRemainder"] = damageExperienceRemainder;
	}

	public override void LoadData(TagCompound tag)
	{
		Tier = Math.Clamp(tag.GetInt("formationPathTier"), 0, MaxTier);
		Stage = Math.Clamp(tag.GetInt("formationPathStage"), 0, MaxStage);
		Experience = Math.Max(0, tag.GetInt("formationPathExperience"));
		DefenseCycles = Math.Max(0, tag.GetInt("formationDefenseCycles"));
		DamageHandled = Math.Max(0, tag.GetInt("formationDamageHandled"));
		TribulationStrikesIntercepted = Math.Max(0,
			tag.GetInt("formationTribulationStrikes"));
		VeinsLinked = Math.Max(0, tag.GetInt("formationVeinsLinked"));
		damageExperienceRemainder = Math.Clamp(
			tag.GetInt("formationDamageExpRemainder"), 0, DamagePerExperience - 1);
		NormalizeExperience(showMessage: false);
	}

	public override void PostUpdate()
	{
		if (!IsMaximumRank && Experience >= ExperienceRequired
			&& CurrentTrialComplete)
			NormalizeExperience(showMessage: Player.whoAmI == Main.myPlayer);
	}

	public override void CopyClientState(ModPlayer targetCopy)
	{
		FormationPathPlayer clone = (FormationPathPlayer)targetCopy;
		clone.Tier = Tier;
		clone.Stage = Stage;
		clone.Experience = Experience;
		clone.DefenseCycles = DefenseCycles;
		clone.DamageHandled = DamageHandled;
		clone.TribulationStrikesIntercepted = TribulationStrikesIntercepted;
		clone.VeinsLinked = VeinsLinked;
		clone.damageExperienceRemainder = damageExperienceRemainder;
	}

	public override void SendClientChanges(ModPlayer clientPlayer)
	{
		FormationPathPlayer old = (FormationPathPlayer)clientPlayer;
		if (old.Tier != Tier || old.Stage != Stage || old.Experience != Experience
			|| old.DefenseCycles != DefenseCycles
			|| old.DamageHandled != DamageHandled
			|| old.TribulationStrikesIntercepted != TribulationStrikesIntercepted
			|| old.VeinsLinked != VeinsLinked)
			SyncState();
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
		Xianxia.SendFormationPathState(Player.whoAmI, Tier, Stage, Experience,
			DefenseCycles, DamageHandled, TribulationStrikesIntercepted,
			VeinsLinked, toWho, fromWho);

	internal void SetStateFromNetwork(int tier, int stage, int experience,
		int defenseCycles, int damageHandled, int tribulationStrikes, int veinsLinked)
	{
		Tier = Math.Clamp(tier, 0, MaxTier);
		Stage = Math.Clamp(stage, 0, MaxStage);
		Experience = Math.Max(0, experience);
		DefenseCycles = Math.Max(0, defenseCycles);
		DamageHandled = Math.Max(0, damageHandled);
		TribulationStrikesIntercepted = Math.Max(0, tribulationStrikes);
		VeinsLinked = Math.Max(0, veinsLinked);
		NormalizeExperience(showMessage: false);
	}

	public void RecordDefenseCycle()
	{
		DefenseCycles++;
		GainExperience(8 + Tier * 2);
	}

	public void RecordDamageHandled(int amount)
	{
		if (amount <= 0)
			return;
		DamageHandled += amount;
		damageExperienceRemainder += amount;
		int experience = damageExperienceRemainder / DamagePerExperience;
		damageExperienceRemainder %= DamagePerExperience;
		GainExperience(experience);
	}

	public void RecordTribulationStrike(int absorbedDamage)
	{
		if (absorbedDamage <= 0)
			return;
		TribulationStrikesIntercepted++;
		RecordDamageHandled(absorbedDamage);
		GainExperience(25 + Tier * 8);
	}

	public void RecordVeinLink()
	{
		VeinsLinked++;
		GainExperience(40);
	}

	private void GainExperience(int amount)
	{
		if (amount <= 0 || IsMaximumRank)
		{
			TryAdvance();
			return;
		}
		Experience += amount;
		TryAdvance();
	}

	private void TryAdvance()
	{
		NormalizeExperience(showMessage: Player.whoAmI == Main.myPlayer);
		SyncState();
	}

	private void NormalizeExperience(bool showMessage)
	{
		int previousTier = Tier;
		int previousStage = Stage;
		while (!IsMaximumRank && Experience >= ExperienceRequired
			&& CurrentTrialComplete)
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
		if (showMessage && (Tier != previousTier || Stage != previousStage))
		{
			Main.NewText(Mod.GetLocalization("FormationPath.Advanced")
				.Format(Tier, TierRealmName, StageName),
				new Color(70, 220, 255));
		}
	}

	private void SyncState()
	{
		if (Main.netMode == Terraria.ID.NetmodeID.SinglePlayer)
			return;
		Xianxia.SendFormationPathState(Player.whoAmI, Tier, Stage, Experience,
			DefenseCycles, DamageHandled, TribulationStrikesIntercepted,
			VeinsLinked);
	}
}
