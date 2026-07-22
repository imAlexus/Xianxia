using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Abilities;
using Xianxia.Common.Players;

namespace Xianxia.Content.Buffs;

public class QiResistanceBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Ironskin}";

	public override void Update(Player player, ref int buffIndex)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		player.statDefense += 10 + (cultivation.GetAbilityLevel(CultivationAbility.QiResistance) - 1) * 2;
	}
}
