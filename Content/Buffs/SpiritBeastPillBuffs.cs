using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Content.Buffs;

public class BeastBloodTemperingBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Endurance}";

	public override void Update(Player player, ref int buffIndex)
	{
		player.statDefense += 12;
		player.GetDamage(DamageClass.Generic) += 0.1f;
		player.endurance += 0.05f;
	}
}

public class FlameMeridianBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Inferno}";

	public override void Update(Player player, ref int buffIndex)
	{
		player.GetDamage(DamageClass.Magic) += 0.18f;
		player.buffImmune[BuffID.OnFire] = true;
		player.buffImmune[BuffID.OnFire3] = true;
	}
}

public class ThunderResistanceBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Electrified}";

	public override void Update(Player player, ref int buffIndex)
	{
		player.statDefense += 20;
		player.endurance += 0.12f;
		player.buffImmune[BuffID.Electrified] = true;
	}
}

public class CoreRefinementBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Clairvoyance}";

	public override void Update(Player player, ref int buffIndex)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		cultivation.EquipmentPassiveQiBonus += 12;
		cultivation.EquipmentMeditationQiBonus += 12;
	}
}
