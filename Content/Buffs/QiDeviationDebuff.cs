using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Buffs;

public sealed class QiDeviationDebuff : ModBuff
{
	public override string Texture =>
		$"Terraria/Images/Buff_{BuffID.Weak}";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
		Main.pvpBuff[Type] = false;
	}
}
