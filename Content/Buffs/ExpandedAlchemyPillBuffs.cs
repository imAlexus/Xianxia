using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Content.Buffs;

public class FoundationStabilizationBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Endurance}";
	public override void Update(Player player, ref int buffIndex)
	{
		player.statDefense += 15;
		player.lifeRegen += 4;
		player.GetModPlayer<AlchemyPillEffectPlayer>().FoundationStabilization = true;
	}
}

public class GoldenCoreTemperingBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Wrath}";
	public override void Update(Player player, ref int buffIndex)
	{
		player.GetDamage(DamageClass.Generic) += 0.15f;
		player.GetModPlayer<AlchemyPillEffectPlayer>().GoldenCoreTempering = true;
	}
}

public class NascentSoulAwakeningBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Clairvoyance}";
	public override void Update(Player player, ref int buffIndex)
	{
		player.lifeRegen += 8;
		player.GetDamage(DamageClass.Magic) += 0.1f;
		player.GetModPlayer<AlchemyPillEffectPlayer>().NascentSoulAwakening = true;
	}
}

public class SoulNourishingBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Regeneration}";
	public override void Update(Player player, ref int buffIndex)
	{
		player.lifeRegen += 12;
		player.GetModPlayer<AlchemyPillEffectPlayer>().SoulNourishing = true;
	}
}

public class VoidInsightBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.ShadowDodge}";
	public override void Update(Player player, ref int buffIndex)
	{
		player.moveSpeed += 0.1f;
		player.GetModPlayer<AlchemyPillEffectPlayer>().VoidInsight = true;
	}
}

public class HeavenlyRebirthBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Lifeforce}";
	public override void Update(Player player, ref int buffIndex) =>
		player.GetModPlayer<AlchemyPillEffectPlayer>().HeavenlyRebirth = true;
}

public class TribulationWardBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Endurance}";
	public override void Update(Player player, ref int buffIndex)
	{
		player.statDefense += 10;
		player.buffImmune[BuffID.Electrified] = true;
		player.GetModPlayer<AlchemyPillEffectPlayer>().TribulationWard = true;
	}
}

public class SpiritBeastLureBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Battle}";
	public override void Update(Player player, ref int buffIndex) =>
		player.GetModPlayer<AlchemyPillEffectPlayer>().SpiritBeastLure = true;
}

public class ConcealmentBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Calm}";
	public override void Update(Player player, ref int buffIndex)
	{
		player.aggro -= 1200;
		player.GetModPlayer<AlchemyPillEffectPlayer>().Concealment = true;
	}
}

public class MeridianOpeningBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Clairvoyance}";
	public override void Update(Player player, ref int buffIndex) =>
		player.GetModPlayer<AlchemyPillEffectPlayer>().MeridianOpening = true;
}

public class FoundationAscensionBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.MagicPower}";
	public override void Update(Player player, ref int buffIndex) =>
		player.GetModPlayer<AlchemyPillEffectPlayer>().FoundationAscension = true;
}

public class GoldenCoreCondensationBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Wrath}";
	public override void Update(Player player, ref int buffIndex) =>
		player.GetModPlayer<AlchemyPillEffectPlayer>().GoldenCoreCondensation = true;
}

public class NascentSoulIntegrationBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Lifeforce}";
	public override void Update(Player player, ref int buffIndex) =>
		player.GetModPlayer<AlchemyPillEffectPlayer>().NascentSoulIntegration = true;
}
