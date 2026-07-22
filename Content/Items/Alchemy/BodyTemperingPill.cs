using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Buffs;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Tiles;
using Xianxia.Common.Players;

namespace Xianxia.Content.Items.Alchemy;

public class BodyTemperingPill : ModItem, IAlchemyPill
{
	public int RequiredAlchemyTier => 0;
	public int RequiredAlchemyStage => 2;
	public int AlchemyExperience => 28;
	public int SaturationCost => 25;
	public int BaseBuffDuration => 60 * 60 * 5;
	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.EatFood;
		Item.useTime = 20;
		Item.useAnimation = 20;
		Item.UseSound = SoundID.Item3;
		Item.buffType = ModContent.BuffType<BodyTemperingBuff>();
		Item.buffTime = 60 * 60 * 5;
		Item.value = Item.buyPrice(silver: 75);
		Item.rare = ItemRarityID.Orange;
	}

	public override void AddRecipes()
	{
		CreateRecipe(2)
			.AddIngredient(ItemID.BottledWater)
			.AddIngredient<Ironroot>(2)
			.AddIngredient(ItemID.Deathweed)
			.AddIngredient<ProfoundIronOre>(3)
			.AddIngredient<SpiritStone>()
			.AddTile<AlchemyCauldronTile>()
			.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
			.Register();
	}
}
