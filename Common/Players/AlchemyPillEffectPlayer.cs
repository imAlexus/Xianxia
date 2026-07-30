using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Xianxia.Content.Buffs;

namespace Xianxia.Common.Players;

public class AlchemyPillEffectPlayer : ModPlayer
{
	private int soulNourishingTimer;

	public bool FoundationStabilization { get; set; }
	public bool GoldenCoreTempering { get; set; }
	public bool NascentSoulAwakening { get; set; }
	public bool SoulNourishing { get; set; }
	public bool VoidInsight { get; set; }
	public bool HeavenlyRebirth { get; set; }
	public bool TribulationWard { get; set; }
	public bool SpiritBeastLure { get; set; }
	public bool Concealment { get; set; }
	public bool MeridianOpening { get; set; }
	public bool FoundationAscension { get; set; }
	public bool GoldenCoreCondensation { get; set; }
	public bool NascentSoulIntegration { get; set; }

	public override void ResetEffects()
	{
		FoundationStabilization = false;
		GoldenCoreTempering = false;
		NascentSoulAwakening = false;
		SoulNourishing = false;
		VoidInsight = false;
		HeavenlyRebirth = false;
		TribulationWard = false;
		SpiritBeastLure = false;
		Concealment = false;
		MeridianOpening = false;
		FoundationAscension = false;
		GoldenCoreCondensation = false;
		NascentSoulIntegration = false;
	}

	public override void PostUpdate()
	{
		if (!SoulNourishing)
		{
			soulNourishingTimer = 0;
			return;
		}

		if (++soulNourishingTimer < 60)
			return;

		soulNourishingTimer = 0;
		CultivationPlayer cultivation = Player.GetModPlayer<CultivationPlayer>();
		cultivation.RestoreQi(Math.Max(25, cultivation.MaxQi / 200));
	}

	public override bool PreKill(
		double damage,
		int hitDirection,
		bool pvp,
		ref bool playSound,
		ref bool genGore,
		ref PlayerDeathReason damageSource)
	{
		if (!HeavenlyRebirth)
			return true;

		int buffIndex = Player.FindBuffIndex(ModContent.BuffType<HeavenlyRebirthBuff>());
		if (buffIndex >= 0)
			Player.DelBuff(buffIndex);
		Player.statLife = Math.Max(1, Player.statLifeMax2 / 4);
		Player.SetImmuneTimeForAllTypes(180);
		Player.HealEffect(Player.statLife, broadcast: true);
		if (Player.whoAmI == Main.myPlayer)
			Main.NewText(Mod.GetLocalization("AlchemyPills.RebirthTriggered").Value,
				new Microsoft.Xna.Framework.Color(255, 220, 120));
		playSound = false;
		genGore = false;
		return false;
	}

	public float QiCostMultiplier =>
		(GoldenCoreTempering ? 0.9f : 1f) * (VoidInsight ? 0.82f : 1f);

	public float TribulationDamageMultiplier =>
		(FoundationStabilization ? 0.9f : 1f) * (TribulationWard ? 0.75f : 1f);

	public float TribulationShieldCostMultiplier => TribulationWard ? 0.8f : 1f;

	public float GetBreakthroughChanceBonus(int targetRealm) => targetRealm switch
	{
		1 when MeridianOpening => 10f,
		2 when FoundationAscension => 12f,
		3 when GoldenCoreCondensation => 15f,
		4 when NascentSoulIntegration => 18f,
		_ => 0f
	};

	public void ConsumeBreakthroughPill(int targetRealm)
	{
		int buffType = targetRealm switch
		{
			1 => ModContent.BuffType<MeridianOpeningBuff>(),
			2 => ModContent.BuffType<FoundationAscensionBuff>(),
			3 => ModContent.BuffType<GoldenCoreCondensationBuff>(),
			4 => ModContent.BuffType<NascentSoulIntegrationBuff>(),
			_ => 0
		};
		int buffIndex = buffType > 0 ? Player.FindBuffIndex(buffType) : -1;
		if (buffIndex >= 0)
			Player.DelBuff(buffIndex);
	}
}
