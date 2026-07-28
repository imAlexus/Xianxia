using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Artifacts;

public class ArtifactForge : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<ArtifactForgeTile>());
		Item.width = 48;
		Item.height = 42;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(gold: 2);
		Item.rare = ItemRarityID.Green;
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<ProfoundIronBar>(10)
			.AddIngredient<SpiritStone>(5)
			.AddTile(TileID.Anvils)
			.Register();
}

public class SpiritJadeArtifactForge : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SpiritJadeArtifactForgeTile>());
		Item.width = 48;
		Item.height = 42;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(gold: 7);
		Item.rare = ItemRarityID.LightRed;
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<ArtifactForge>()
			.AddIngredient<SpiritJadeBar>(15)
			.AddIngredient<QiGatheringBeastCore>(3)
			.AddTile(TileID.MythrilAnvil)
			.Register();
}

public class ProfoundArtifactForge : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<ProfoundArtifactForgeTile>());
		Item.width = 48;
		Item.height = 42;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(gold: 18);
		Item.rare = ItemRarityID.Cyan;
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<SpiritJadeArtifactForge>()
			.AddIngredient<ProfoundIronBar>(20)
			.AddIngredient<FoundationBeastCore>(3)
			.AddTile(TileID.MythrilAnvil)
			.Register();
}
