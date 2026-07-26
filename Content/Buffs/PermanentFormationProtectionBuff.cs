using Terraria;
using Terraria.ModLoader;

namespace Xianxia.Content.Buffs;

public class PermanentFormationProtectionBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_62";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.statDefense += 12;
		player.endurance += 0.06f;
	}
}

public class PermanentFormationGatheringBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_29";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}
}

public class PermanentFormationSuppressionBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_46";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}
}

public class PermanentFormationRestorationBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_2";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.lifeRegen += 8;
	}
}

public class PermanentFormationRelayProtectionBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_62";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.statDefense += 24;
		player.endurance += 0.12f;
	}
}

public class PermanentFormationRelayGatheringBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_29";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}
}

public class PermanentFormationRelaySuppressionBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_46";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}
}

public class PermanentFormationRelayRestorationBuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_2";

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.lifeRegen += 20;
	}
}
