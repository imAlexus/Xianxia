using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Alchemy;

public class PillDregs : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(copper: 50);
		Item.rare = ItemRarityID.White;
	}
}
