using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Buffs;

public class BodyTemperingBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Wrath}";

	public override void Update(Player player, ref int buffIndex)
	{
		player.statDefense += 8;
		player.GetDamage(DamageClass.Melee) += 0.1f;
	}
}
