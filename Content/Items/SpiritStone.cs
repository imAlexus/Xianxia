using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Content.Items;

public class SpiritStone : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 36;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(silver: 5);
		Item.rare = ItemRarityID.Blue;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.UseSound = SoundID.Item4;
		Item.consumable = true;
	}

	public override bool? UseItem(Player player)
	{
		player.GetModPlayer<CultivationPlayer>().AddQi(25);
		return true;
	}

}
