using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Content.Buffs;

public class SpiritGatheringBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Clairvoyance}";

	public override void Update(Player player, ref int buffIndex)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		cultivation.EquipmentPassiveQiBonus += 2;
		cultivation.EquipmentMeditationQiBonus += 2;
	}
}
