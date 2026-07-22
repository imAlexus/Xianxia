using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Buffs;

public class SpiritualPressureDebuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Slow}";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.pvpBuff[Type] = true;
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		npc.velocity *= npc.boss ? 0.985f : 0.94f;
	}
}
