using Terraria;
using Terraria.ModLoader;
using Xianxia.Common.Abilities;
using Xianxia.Common.Players;

namespace Xianxia.Content.Buffs;

public class SectProtectionFormationBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_62";

	public override void Update(Player player, ref int buffIndex)
	{
		int level = player.GetModPlayer<CultivationPlayer>()
			.GetAbilityLevel(CultivationAbility.SectProtectionFormation);
		player.statDefense += 18 + level * 2;
		player.endurance += 0.08f + level * 0.004f;
	}
}
