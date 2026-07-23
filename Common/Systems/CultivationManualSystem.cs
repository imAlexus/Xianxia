using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Xianxia.Common.Players;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Accessories;
using Xianxia.Content.Items.Alchemy;
using Xianxia.Content.Items.Armor;
using Xianxia.Content.Items.Guides;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Items.Weapons;

namespace Xianxia.Common.Systems;

public class CultivationManualSystem : ModSystem
{
	private const int PageCount = 16;

	private readonly record struct ManualIndexEntry(string LocalizationKey, int TargetPage);
	private readonly record struct ManualTextLine(string Text, float Y);
	private readonly record struct ManualIngredient(int ItemType, int Stack = 1);
	private readonly record struct ManualRecipe(
		int ResultType,
		int ResultStack,
		int StationType,
		ManualIngredient[] Ingredients
	);
	private readonly record struct ManualRecipeGroup(
		string LocalizationKey,
		ManualRecipe[] Recipes
	);

	private static readonly ManualIndexEntry[] IndexEntries =
	[
		new("Fundamentals", 1),
		new("QiMeditation", 2),
		new("PassiveAbilities", 3),
		new("CombatTechniques", 4),
		new("Movement", 5),
		new("BreakthroughResources", 6),
		new("WorldConfiguration", 7),
		new("CraftingRecipes", 8),
		new("CultivationGrowth", 9),
		new("QiRequirements", 10),
		new("SpiritualQiZones", 11),
		new("SpiritMines", 12),
		new("AbilityProgression", 13),
		new("SpiritBeasts", 14),
		new("AlchemyPath", 15)
	];

	private static bool isOpen;
	private static int currentPage;
	private static float scrollOffset;
	private static bool draggingScrollBar;
	private static float scrollBarGrabOffset;

	public static void Toggle()
	{
		isOpen = !isOpen;
		if (isOpen)
		{
			ResetScroll();
		}
		SoundEngine.PlaySound(isOpen ? SoundID.MenuOpen : SoundID.MenuClose);
	}

	public override void OnWorldUnload()
	{
		isOpen = false;
		currentPage = 0;
		ResetScroll();
	}

	public override void PostUpdateInput()
	{
		if (isOpen
			&& Main.keyState.IsKeyDown(Keys.Escape)
			&& Main.oldKeyState.IsKeyUp(Keys.Escape))
		{
			isOpen = false;
			SoundEngine.PlaySound(SoundID.MenuClose);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		LegacyGameInterfaceLayer manualLayer = new(
			"Xianxia: Cultivator Manual",
			DrawManual,
			InterfaceScaleType.UI
		);
		if (mouseTextIndex >= 0)
		{
			layers.Insert(mouseTextIndex, manualLayer);
		}
		else
		{
			layers.Add(manualLayer);
		}
	}

	private bool DrawManual()
	{
		if (!isOpen || Main.gameMenu || Main.LocalPlayer is not { active: true })
		{
			return true;
		}

		currentPage = Math.Clamp(currentPage, 0, PageCount - 1);

		int panelWidth = Math.Min(900, Main.screenWidth - 40);
		int panelHeight = Math.Min(640, Main.screenHeight - 40);
		Rectangle panel = new(
			(Main.screenWidth - panelWidth) / 2,
			(Main.screenHeight - panelHeight) / 2,
			panelWidth,
			panelHeight
		);
		Point mouse = Main.MouseScreen.ToPoint();
		if (panel.Contains(mouse))
		{
			Main.LocalPlayer.mouseInterface = true;
		}

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
			new Color(4, 8, 18, 190));
		Main.spriteBatch.Draw(pixel, new Rectangle(panel.X - 5, panel.Y - 5, panel.Width + 10, panel.Height + 10),
			new Color(29, 93, 82));
		Main.spriteBatch.Draw(pixel, new Rectangle(panel.X - 3, panel.Y - 3, panel.Width + 6, panel.Height + 6),
			new Color(52, 216, 190));
		Main.spriteBatch.Draw(pixel, panel, new Color(18, 30, 39));
		Main.spriteBatch.Draw(pixel, new Rectangle(panel.X + 5, panel.Y + 5, panel.Width - 10, panel.Height - 10),
			new Color(224, 218, 181));
		Main.spriteBatch.Draw(pixel, new Rectangle(panel.X + 15, panel.Y + 12, panel.Width - 30, 42),
			new Color(211, 205, 169));

		string manualTitle = Mod.GetLocalization("Manual.Title").Value;
		DrawCentered(manualTitle, new Vector2(panel.Center.X, panel.Y + 33), new Color(34, 82, 67), 1.05f);
		Main.spriteBatch.Draw(pixel, new Rectangle(panel.X + 28, panel.Y + 60, panel.Width - 56, 2),
			new Color(164, 126, 45));
		string pageNumber = Mod.GetLocalization("Manual.PageNumber").Format(currentPage + 1, PageCount);
		Rectangle pageBadge = new(panel.Right - 137, panel.Y + 19, 105, 27);
		Main.spriteBatch.Draw(pixel, pageBadge, new Color(45, 111, 92));
		Main.spriteBatch.Draw(pixel,
			new Rectangle(pageBadge.X + 2, pageBadge.Y + 2, pageBadge.Width - 4, pageBadge.Height - 4),
			new Color(232, 225, 187));
		DrawCentered(pageNumber, pageBadge.Center.ToVector2(), new Color(75, 65, 42), 0.57f);

		string pageTitle = currentPage == 0
			? Mod.GetLocalization("Manual.Index.Title").Value
			: Mod.GetLocalization($"Manual.Pages.{GetContentPageKey(currentPage)}.Title").Value;
		Rectangle titleRibbon = new(panel.X + 30, panel.Y + 70, panel.Width - 60, 34);
		Main.spriteBatch.Draw(pixel, titleRibbon, new Color(198, 180, 128, 210));
		Main.spriteBatch.Draw(pixel, new Rectangle(titleRibbon.X, titleRibbon.Y, titleRibbon.Width, 2),
			new Color(154, 116, 44));
		Main.spriteBatch.Draw(pixel, new Rectangle(titleRibbon.X, titleRibbon.Bottom - 2, titleRibbon.Width, 2),
			new Color(154, 116, 44));
		DrawCentered(pageTitle, titleRibbon.Center.ToVector2(), new Color(103, 67, 24), 0.82f);

		Rectangle footer = new(panel.X + 18, panel.Bottom - 62, panel.Width - 36, 48);
		Main.spriteBatch.Draw(pixel, footer, new Color(41, 94, 78));
		Main.spriteBatch.Draw(pixel,
			new Rectangle(footer.X + 2, footer.Y + 2, footer.Width - 4, footer.Height - 4),
			new Color(202, 196, 161));
		Rectangle textArea = new(panel.X + 34, panel.Y + 114, panel.Width - 68, footer.Y - panel.Y - 126);
		if (currentPage == 0)
		{
			DrawIndex(textArea, mouse);
		}
		else if (currentPage == 8)
		{
			DrawRecipePage(textArea, mouse);
		}
		else
		{
			DrawScrollableText(GetPageBody(currentPage), textArea, mouse,
				new Color(35, 40, 38), 0.78f);
		}

		const int footerGap = 10;
		int footerButtonWidth = Math.Min(140, (footer.Width - footerGap * 5) / 4);
		int footerButtonsWidth = footerButtonWidth * 4 + footerGap * 3;
		int footerButtonsX = footer.Center.X - footerButtonsWidth / 2;
		Rectangle previousButton = new(footerButtonsX, footer.Y + 7, footerButtonWidth, 34);
		Rectangle indexButton = new(previousButton.Right + footerGap, footer.Y + 7, footerButtonWidth, 34);
		Rectangle closeButton = new(indexButton.Right + footerGap, footer.Y + 7, footerButtonWidth, 34);
		Rectangle nextButton = new(closeButton.Right + footerGap, footer.Y + 7, footerButtonWidth, 34);
		DrawButton(previousButton, Mod.GetLocalization("Manual.Previous").Value,
			previousButton.Contains(mouse), currentPage > 0);
		DrawButton(indexButton, Mod.GetLocalization("Manual.IndexButton").Value,
			indexButton.Contains(mouse), currentPage != 0);
		DrawButton(closeButton, Mod.GetLocalization("Manual.Close").Value,
			closeButton.Contains(mouse), true);
		DrawButton(nextButton, Mod.GetLocalization("Manual.Next").Value,
			nextButton.Contains(mouse), currentPage < PageCount - 1);

		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (closeButton.Contains(mouse))
			{
				isOpen = false;
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuClose);
			}
			else if (indexButton.Contains(mouse) && currentPage != 0)
			{
				currentPage = 0;
				ResetScroll();
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (currentPage == 0 && TryOpenIndexPage(mouse, textArea))
			{
				ResetScroll();
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (previousButton.Contains(mouse) && currentPage > 0)
			{
				currentPage--;
				ResetScroll();
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (nextButton.Contains(mouse) && currentPage < PageCount - 1)
			{
				currentPage++;
				ResetScroll();
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
		}

		return true;
	}

	private void DrawIndex(Rectangle area, Point mouse)
	{
		DrawCentered(Mod.GetLocalization("Manual.Index.Hint").Value,
			new Vector2(area.Center.X, area.Y + 12), new Color(74, 85, 73), 0.65f);

		for (int i = 0; i < IndexEntries.Length; i++)
		{
			Rectangle button = GetIndexButtonRectangle(area, i);
			string label = Mod.GetLocalization(
				$"Manual.Index.Entries.{IndexEntries[i].LocalizationKey}").Value;
			DrawIndexButton(button, label, IndexEntries[i].TargetPage + 1, button.Contains(mouse));
		}
	}

	private static bool TryOpenIndexPage(Point mouse, Rectangle area)
	{
		for (int i = 0; i < IndexEntries.Length; i++)
		{
			if (GetIndexButtonRectangle(area, i).Contains(mouse))
			{
				currentPage = IndexEntries[i].TargetPage;
				return true;
			}
		}

		return false;
	}

	private static Rectangle GetIndexButtonRectangle(Rectangle area, int index)
	{
		const int columns = 3;
		const int columnGap = 10;
		const int rowGap = 8;
		int rows = (IndexEntries.Length + columns - 1) / columns;
		int startY = area.Y + 32;
		int availableHeight = area.Bottom - startY;
		int buttonHeight = Math.Min(54, (availableHeight - rowGap * (rows - 1)) / rows);
		int buttonWidth = (area.Width - columnGap * (columns - 1)) / columns;
		int column = index % columns;
		int row = index / columns;
		return new Rectangle(
			area.X + column * (buttonWidth + columnGap),
			startY + row * (buttonHeight + rowGap),
			buttonWidth,
			buttonHeight
		);
	}

	private static string GetContentPageKey(int page) => page switch
	{
		>= 1 and <= 7 => $"Page{page - 1}",
		8 => "Recipes1",
		9 => "CultivationGrowth",
		10 => "QiRequirements",
		11 => "SpiritualQiZones",
		12 => "SpiritMines",
		13 => "AbilityProgression",
		14 => "SpiritBeasts",
		15 => "AlchemyPath",
		_ => "Page0"
	};

	private void DrawRecipePage(Rectangle area, Point mouse)
	{
		ManualRecipeGroup[] groups = BuildRecipeGroups();
		const int rowHeight = 39;
		const int groupHeaderHeight = 28;
		Rectangle contentArea = new(area.X, area.Y, area.Width - 20, area.Height - 18);
		int recipeCount = 0;
		foreach (ManualRecipeGroup group in groups)
		{
			recipeCount += group.Recipes.Length;
		}
		float contentHeight = recipeCount * rowHeight + groups.Length * groupHeaderHeight;
		float maxScroll = Math.Max(0f, contentHeight - contentArea.Height);
		scrollOffset = MathHelper.Clamp(scrollOffset, 0f, maxScroll);

		Rectangle topButton = new(area.Right - 18, area.Y, 18, 20);
		Rectangle track = new(area.Right - 10, area.Y + 26, 8, area.Height - 44);
		int handleHeight = Math.Max(36,
			(int)(track.Height * (contentArea.Height / contentHeight)));
		int handleTravel = track.Height - handleHeight;
		int handleY = track.Y + (maxScroll <= 0f
			? 0
			: (int)(handleTravel * (scrollOffset / maxScroll)));
		Rectangle handle = new(track.X - 2, handleY, track.Width + 4, handleHeight);

		HandleScrollInput(area, mouse, topButton, track, handle, maxScroll, handleTravel);
		DrawScrollBar(track, handle, mouse);
		DrawButton(topButton, "↑", topButton.Contains(mouse), scrollOffset > 0f);

		float contentY = contentArea.Y - scrollOffset;
		int recipeIndex = 0;
		foreach (ManualRecipeGroup group in groups)
		{
			Rectangle header = new(
				contentArea.X,
				(int)contentY,
				contentArea.Width,
				groupHeaderHeight - 4);
			if (header.Y >= contentArea.Y && header.Bottom <= contentArea.Bottom)
			{
				DrawRecipeGroupHeader(header, group.LocalizationKey);
			}
			contentY += groupHeaderHeight;

			foreach (ManualRecipe recipe in group.Recipes)
			{
				Rectangle row = new(
					contentArea.X,
					(int)contentY,
					contentArea.Width,
					rowHeight - 3);
				if (row.Y >= contentArea.Y && row.Bottom <= contentArea.Bottom)
				{
					DrawRecipeRow(row, mouse, recipe, recipeIndex % 2 == 0);
				}

				contentY += rowHeight;
				recipeIndex++;
			}
		}

		if (scrollOffset < maxScroll - 1f)
		{
			string hint = Mod.GetLocalization("Manual.ScrollForMore").Value;
			Vector2 hintSize = FontAssets.MouseText.Value.MeasureString(hint) * 0.52f;
			Rectangle hintBackground = new(
				(int)(contentArea.Right - hintSize.X - 10f), area.Bottom - 17,
				(int)hintSize.X + 10, 16);
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, hintBackground,
				new Color(224, 218, 181, 235));
			DrawCentered(hint, hintBackground.Center.ToVector2(), new Color(72, 83, 72), 0.52f);
		}
	}

	private void DrawRecipeGroupHeader(Rectangle header, string localizationKey)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, header, new Color(45, 111, 92));
		Main.spriteBatch.Draw(pixel,
			new Rectangle(header.X + 2, header.Y + 2, header.Width - 4, header.Height - 4),
			new Color(198, 180, 128));
		DrawCentered(
			Mod.GetLocalization($"Manual.RecipeSections.{localizationKey}").Value,
			header.Center.ToVector2(),
			new Color(78, 57, 25),
			0.62f);
	}

	private void DrawRecipeRow(Rectangle row, Point mouse, ManualRecipe recipe, bool alternate)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, row, alternate
			? new Color(203, 199, 166, 105)
			: new Color(184, 181, 153, 75));

		Rectangle resultSlot = new(row.X + 3, row.Y + 3, 34, 34);
		DrawRecipeItem(resultSlot, mouse, recipe.ResultType, recipe.ResultStack,
			new Color(42, 105, 86));

		Item resultItem = new();
		resultItem.SetDefaults(recipe.ResultType);
		string resultName = resultItem.Name;
		bool isAlchemyPill = resultItem.ModItem is IAlchemyPill;
		Main.spriteBatch.DrawString(FontAssets.MouseText.Value, resultName,
			new Vector2(row.X + 42, row.Y + (isAlchemyPill ? 4 : 11)), new Color(47, 55, 49), 0f,
			Vector2.Zero, isAlchemyPill ? 0.5f : 0.56f, SpriteEffects.None, 0f);

		if (resultItem.ModItem is IAlchemyPill pill)
		{
			string realm = Mod.GetLocalization(
				$"Cultivation.Realms.{AlchemyPlayer.GetTierRealmKey(pill.RequiredAlchemyTier)}").Value;
			string stage = Mod.GetLocalization(
				$"Alchemy.Stages.{AlchemyPlayer.GetStageKey(pill.RequiredAlchemyStage)}").Value;
			string requirement = Mod.GetLocalization("Manual.AlchemyRequirement").Format(realm, stage);
			Main.spriteBatch.DrawString(FontAssets.MouseText.Value, requirement,
				new Vector2(row.X + 42, row.Y + 20), new Color(106, 72, 33), 0f,
				Vector2.Zero, 0.4f, SpriteEffects.None, 0f);
		}

		DrawCentered("=", new Vector2(row.X + 198, row.Center.Y), new Color(103, 67, 24), 0.7f);
		int ingredientX = row.X + 216;
		for (int i = 0; i < recipe.Ingredients.Length; i++)
		{
			Rectangle ingredientSlot = new(ingredientX + i * 39, row.Y + 3, 34, 34);
			ManualIngredient ingredient = recipe.Ingredients[i];
			DrawRecipeItem(ingredientSlot, mouse, ingredient.ItemType, ingredient.Stack,
				new Color(105, 82, 42));
		}

		DrawCentered("@", new Vector2(row.Right - 52, row.Center.Y), new Color(103, 67, 24), 0.65f);
		Rectangle stationSlot = new(row.Right - 37, row.Y + 3, 34, 34);
		DrawRecipeItem(stationSlot, mouse, recipe.StationType, 1, new Color(79, 74, 98));
	}

	private static void DrawRecipeItem(
		Rectangle slot,
		Point mouse,
		int itemType,
		int stack,
		Color borderColor)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, slot, borderColor);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(slot.X + 2, slot.Y + 2, slot.Width - 4, slot.Height - 4),
			new Color(25, 38, 42, 220));

		Item item = new();
		item.SetDefaults(itemType);
		item.stack = stack;
		ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, Main.spriteBatch,
			slot.Center.ToVector2(), 1f, 27f, Color.White);

		if (stack > 1)
		{
			string quantity = stack.ToString();
			DynamicSpriteFont font = FontAssets.ItemStack.Value;
			Vector2 size = font.MeasureString(quantity) * 0.62f;
			Main.spriteBatch.DrawString(font, quantity,
				new Vector2(slot.Right - size.X - 2f, slot.Bottom - size.Y - 1f),
				Color.White, 0f, Vector2.Zero, 0.62f, SpriteEffects.None, 0f);
		}

		if (slot.Contains(mouse))
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.HoverItem = item.Clone();
			Main.hoverItemName = item.Name;
		}
	}

	private static ManualRecipeGroup[] BuildRecipeGroups()
	{
		return
		[
			new("NoviceAttire",
			[
				new(ModContent.ItemType<NoviceDiscipleHeadband>(), 1, ItemID.Loom,
					[new(ItemID.Silk, 2)]),
				new(ModContent.ItemType<NoviceDiscipleRobe>(), 1, ItemID.Loom,
					[new(ItemID.Silk, 8)]),
				new(ModContent.ItemType<NoviceDiscipleTrousers>(), 1, ItemID.Loom,
					[new(ItemID.Silk, 6)])
			]),
			new("MaterialsEquipment", BuildRecipeSection(8)),
			new("AccessoriesTechniques", BuildRecipeSection(9)),
			new("CauldronsBeastPills", BuildRecipeSection(14)),
			new("ProgressionPills", BuildRecipeSection(18)),
			new("UtilityPills", BuildRecipeSection(19))
		];
	}

	private static ManualRecipe[] BuildRecipeSection(int page)
	{
		if (page == 8)
		{
			return
			[
				new(ModContent.ItemType<SpiritJadeBar>(), 1, ItemID.Furnace,
					[new(ModContent.ItemType<SpiritJadeOre>(), 4)]),
				new(ModContent.ItemType<ProfoundIronBar>(), 1, ItemID.Hellforge,
					[new(ModContent.ItemType<ProfoundIronOre>(), 4)]),
				new(ModContent.ItemType<AlchemyCauldron>(), 1, ItemID.IronAnvil,
					[
						new(ModContent.ItemType<ProfoundIronBar>(), 10),
						new(ModContent.ItemType<SpiritJadeBar>(), 5),
						new(ModContent.ItemType<SpiritStone>(), 3)
					]),
				new(ModContent.ItemType<SpiritJadeHeadpiece>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<SpiritJadeBar>(), 10), new(ModContent.ItemType<SpiritStone>())]),
				new(ModContent.ItemType<SpiritJadeRobe>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<SpiritJadeBar>(), 18), new(ModContent.ItemType<SpiritStone>(), 2)]),
				new(ModContent.ItemType<SpiritJadeLeggings>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<SpiritJadeBar>(), 14), new(ModContent.ItemType<SpiritStone>())]),
				new(ModContent.ItemType<SpiritJadeSword>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<SpiritJadeBar>(), 8)]),
				new(ModContent.ItemType<ProfoundIronSpear>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<ProfoundIronBar>(), 10), new(ModContent.ItemType<SpiritStone>(), 2)]),
				new(ModContent.ItemType<FlyingSword>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<SpiritJadeBar>(), 12), new(ModContent.ItemType<SpiritStone>(), 3)])
			];
		}

		if (page == 14)
		{
			return
			[
				new(ModContent.ItemType<SpiritJadeCauldron>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<AlchemyCauldron>()), new(ModContent.ItemType<SpiritJadeBar>(), 12),
						new(ModContent.ItemType<SpiritStone>(), 8)]),
				new(ModContent.ItemType<ProfoundAlchemyCauldron>(), 1, ItemID.MythrilAnvil,
					[new(ModContent.ItemType<SpiritJadeCauldron>()), new(ModContent.ItemType<ProfoundIronBar>(), 15),
						new(ModContent.ItemType<SpiritStone>(), 15)]),
				new(ModContent.ItemType<MeridianCleansingPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<MoonDewFlower>(), 3),
						new(ModContent.ItemType<SpiritGrass>(), 3), new(ModContent.ItemType<PillDregs>(), 2)]),
				new(ModContent.ItemType<GreaterQiRecoveryPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<SpiritGrass>(), 5),
						new(ModContent.ItemType<MoonDewFlower>(), 3), new(ModContent.ItemType<SpiritStone>(), 3),
						new(ModContent.ItemType<QiGatheringBeastCore>())]),
				new(ModContent.ItemType<ResonantSpiritVeinCompass>(), 1, ItemID.IronAnvil,
					[new(ModContent.ItemType<SpiritVeinCompass>()), new(ModContent.ItemType<SpiritJadeBar>(), 10),
						new(ModContent.ItemType<SpiritStone>(), 5)]),
				new(ModContent.ItemType<HeavenlySpiritVeinCompass>(), 1, ItemID.MythrilAnvil,
					[new(ModContent.ItemType<ResonantSpiritVeinCompass>()), new(ModContent.ItemType<ProfoundIronBar>(), 12),
						new(ModContent.ItemType<SpiritStone>(), 12)]),
				new(ModContent.ItemType<BeastBloodTemperingPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<SpiritBeastBlood>(), 3),
						new(ModContent.ItemType<SpiritFur>(), 2), new(ModContent.ItemType<SpiritGrass>()),
						new(ModContent.ItemType<MortalBeastCore>())]),
				new(ModContent.ItemType<FlameMeridianPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<FlameEssence>(), 3),
						new(ModContent.ItemType<FoundationBeastCore>()), new(ModContent.ItemType<FireLotus>(), 2)]),
				new(ModContent.ItemType<ThunderResistancePill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<ThunderEssence>(), 3),
						new(ModContent.ItemType<CoreFormationBeastCore>()), new(ModContent.ItemType<Ironroot>(), 2)]),
				new(ModContent.ItemType<CoreRefinementPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<QiGatheringBeastCore>()),
						new(ModContent.ItemType<FoundationBeastCore>()), new(ModContent.ItemType<SpiritBeastBlood>(), 5),
						new(ModContent.ItemType<MoonDewFlower>(), 2)])
			];
		}

		if (page == 18)
		{
			return
			[
				new(ModContent.ItemType<FoundationStabilizationPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<Ironroot>(), 3),
						new(ModContent.ItemType<SpiritJadeBar>(), 2), new(ModContent.ItemType<QiGatheringBeastCore>()),
						new(ModContent.ItemType<SpiritStone>(), 4)]),
				new(ModContent.ItemType<GoldenCoreTemperingPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<FoundationBeastCore>()),
						new(ModContent.ItemType<FireLotus>(), 3), new(ModContent.ItemType<SpiritJadeBar>(), 3),
						new(ModContent.ItemType<SpiritStone>(), 6)]),
				new(ModContent.ItemType<NascentSoulAwakeningPill>(), 1, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<CoreFormationBeastCore>()),
						new(ModContent.ItemType<ThunderEssence>(), 2), new(ModContent.ItemType<MoonDewFlower>(), 3),
						new(ModContent.ItemType<ProfoundIronBar>(), 2), new(ModContent.ItemType<SpiritStone>(), 10)]),
				new(ModContent.ItemType<SoulNourishingPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<CoreFormationBeastCore>()),
						new(ModContent.ItemType<MoonDewFlower>(), 4), new(ModContent.ItemType<SpiritBeastBlood>(), 5),
						new(ModContent.ItemType<SpiritStone>(), 10)]),
				new(ModContent.ItemType<VoidInsightPill>(), 1, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<ThunderEssence>(), 3),
						new(ModContent.ItemType<ProfoundIronBar>(), 3), new(ModContent.ItemType<MoonDewFlower>(), 3),
						new(ModContent.ItemType<SpiritStone>(), 14)]),
				new(ModContent.ItemType<HeavenlyRebirthPill>(), 1, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<CoreFormationBeastCore>(), 2),
						new(ModContent.ItemType<FlameEssence>(), 3), new(ModContent.ItemType<ThunderEssence>(), 3),
						new(ModContent.ItemType<SpiritBeastBlood>(), 8), new(ModContent.ItemType<SpiritStone>(), 20)])
			];
		}

		if (page == 19)
		{
			return
			[
				new(ModContent.ItemType<TribulationWardPill>(), 1, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<ThunderEssence>(), 2),
						new(ModContent.ItemType<Ironroot>(), 3), new(ModContent.ItemType<FoundationBeastCore>()),
						new(ModContent.ItemType<SpiritStone>(), 8)]),
				new(ModContent.ItemType<SpiritBeastLurePill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<SpiritBeastBlood>(), 3),
						new(ModContent.ItemType<SpiritFur>(), 2), new(ModContent.ItemType<SpiritGrass>(), 2),
						new(ModContent.ItemType<MortalBeastCore>())]),
				new(ModContent.ItemType<ConcealmentPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
					[new(ItemID.BottledWater), new(ModContent.ItemType<MoonDewFlower>(), 2),
						new(ModContent.ItemType<Ironroot>(), 2), new(ModContent.ItemType<SpiritFur>(), 3),
						new(ModContent.ItemType<SpiritStone>(), 3)])
			];
		}

		return
		[
			new(ModContent.ItemType<ProfoundIronRing>(), 1, ItemID.IronAnvil,
				[new(ModContent.ItemType<ProfoundIronBar>(), 6), new(ModContent.ItemType<SpiritStone>(), 2)]),
			new(ModContent.ItemType<SpiritJadePendant>(), 1, ItemID.IronAnvil,
				[new(ModContent.ItemType<SpiritJadeBar>(), 6), new(ModContent.ItemType<SpiritStone>(), 2)]),
			new(ModContent.ItemType<SpiritGatheringTalisman>(), 1, ItemID.WorkBench,
				[
					new(ItemID.Silk, 5),
					new(ModContent.ItemType<SpiritJadeBar>(), 3),
					new(ModContent.ItemType<SpiritStone>(), 3)
				]),
			new(ModContent.ItemType<FireballTechnique>(), 1, ItemID.Bookcase,
				[new(ItemID.Book), new(ModContent.ItemType<SpiritStone>()), new(ItemID.FallenStar, 5)]),
			new(ModContent.ItemType<QiPalmTechnique>(), 1, ItemID.Bookcase,
				[new(ItemID.Book), new(ModContent.ItemType<SpiritStone>(), 2), new(ItemID.FallenStar, 5)]),
			new(ModContent.ItemType<SpiritVeinCompass>(), 1, ItemID.IronAnvil,
				[new(ItemID.Compass), new(ItemID.GoldBar, 8), new(ItemID.FallenStar, 5), new(ItemID.Amethyst, 3)]),
			new(ModContent.ItemType<SpiritVeinCompass>(), 1, ItemID.IronAnvil,
				[new(ItemID.Compass), new(ItemID.PlatinumBar, 8), new(ItemID.FallenStar, 5), new(ItemID.Amethyst, 3)]),
			new(ModContent.ItemType<CultivatorManual>(), 1, ItemID.WorkBench,
				[new(ItemID.Book), new(ModContent.ItemType<SpiritStone>())]),
			new(ModContent.ItemType<QiRecoveryPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
				[
					new(ItemID.BottledWater), new(ModContent.ItemType<SpiritGrass>(), 2),
					new(ItemID.Moonglow), new(ModContent.ItemType<SpiritStone>())
				]),
			new(ModContent.ItemType<SpiritGatheringPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
				[
					new(ItemID.BottledWater), new(ModContent.ItemType<MoonDewFlower>(), 2), new(ItemID.Waterleaf),
					new(ModContent.ItemType<SpiritJadeOre>(), 3), new(ModContent.ItemType<SpiritStone>())
				]),
			new(ModContent.ItemType<BodyTemperingPill>(), 2, ModContent.ItemType<AlchemyCauldron>(),
				[
					new(ItemID.BottledWater), new(ModContent.ItemType<Ironroot>(), 2), new(ItemID.Deathweed),
					new(ModContent.ItemType<ProfoundIronOre>(), 3), new(ModContent.ItemType<SpiritStone>())
				])
		];
	}

	private string GetPageBody(int page) => page switch
	{
		2 => Mod.GetLocalization("Manual.Pages.Page1.Body").Format(FormatKeybind(Xianxia.MeditateKeybind)),
		4 => Mod.GetLocalization("Manual.Pages.Page3.Body").Format(
			FormatKeybind(Xianxia.QiResistanceKeybind),
			FormatKeybind(Xianxia.FireballKeybind),
			FormatKeybind(Xianxia.QiPalmKeybind),
			FormatKeybind(Xianxia.SpiritualPressureKeybind),
			FormatKeybind(Xianxia.NightVisionKeybind)),
		5 => Mod.GetLocalization("Manual.Pages.Page4.Body").Format(
			FormatKeybind(Xianxia.FlameStepKeybind),
			FormatKeybind(Xianxia.QiFlightKeybind),
			FormatKeybind(Xianxia.NascentTeleportKeybind),
			FormatKeybind(Xianxia.AbilityWheelKeybind)),
		13 => Mod.GetLocalization("Manual.Pages.AbilityProgression.Body").Format(
			FormatKeybind(Xianxia.AbilityTreeKeybind)),
		_ => Mod.GetLocalization($"Manual.Pages.{GetContentPageKey(page)}.Body").Value
	};

	private static string FormatKeybind(ModKeybind keybind)
	{
		List<string> keys = keybind.GetAssignedKeys();
		return keys.Count > 0 ? string.Join(" / ", keys) : "-";
	}

	private void DrawScrollableText(
		string text,
		Rectangle area,
		Point mouse,
		Color color,
		float scale)
	{
		Rectangle contentArea = area;
		List<ManualTextLine> lines = LayoutWrappedText(text, contentArea.Width, scale, out float contentHeight);
		bool needsScrollBar = contentHeight > area.Height;
		if (needsScrollBar)
		{
			contentArea = new Rectangle(area.X, area.Y, area.Width - 20, area.Height - 18);
			lines = LayoutWrappedText(text, contentArea.Width, scale, out contentHeight);
		}

		float maxScroll = Math.Max(0f, contentHeight - contentArea.Height);
		scrollOffset = MathHelper.Clamp(scrollOffset, 0f, maxScroll);

		if (needsScrollBar)
		{
			Rectangle topButton = new(area.Right - 18, area.Y, 18, 20);
			Rectangle track = new(area.Right - 10, area.Y + 26, 8, area.Height - 44);
			int handleHeight = Math.Max(36,
				(int)(track.Height * (contentArea.Height / contentHeight)));
			int handleTravel = track.Height - handleHeight;
			int handleY = track.Y + (maxScroll <= 0f
				? 0
				: (int)(handleTravel * (scrollOffset / maxScroll)));
			Rectangle handle = new(track.X - 2, handleY, track.Width + 4, handleHeight);

			HandleScrollInput(area, mouse, topButton, track, handle, maxScroll, handleTravel);
			DrawScrollBar(track, handle, mouse);
			DrawButton(topButton, "↑", topButton.Contains(mouse), scrollOffset > 0f);
			if (scrollOffset < maxScroll - 1f)
			{
				string hint = Mod.GetLocalization("Manual.ScrollForMore").Value;
				Vector2 hintSize = FontAssets.MouseText.Value.MeasureString(hint) * 0.52f;
				Rectangle hintBackground = new(
					(int)(contentArea.Right - hintSize.X - 10f), area.Bottom - 17,
					(int)hintSize.X + 10, 16);
				Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, hintBackground,
					new Color(224, 218, 181, 235));
				DrawCentered(hint, hintBackground.Center.ToVector2(), new Color(72, 83, 72), 0.52f);
			}
		}
		else
		{
			scrollOffset = 0f;
			draggingScrollBar = false;
		}

		DynamicSpriteFont font = FontAssets.MouseText.Value;
		float lineHeight = font.LineSpacing * scale;
		foreach (ManualTextLine line in lines)
		{
			float drawY = contentArea.Y + line.Y - scrollOffset;
			if (drawY < contentArea.Y || drawY + lineHeight > contentArea.Bottom)
			{
				continue;
			}

			Main.spriteBatch.DrawString(font, line.Text, new Vector2(contentArea.X, drawY), color, 0f,
				Vector2.Zero, scale, SpriteEffects.None, 0f);
		}
	}

	private static List<ManualTextLine> LayoutWrappedText(
		string text,
		int width,
		float scale,
		out float contentHeight)
	{
		DynamicSpriteFont font = FontAssets.MouseText.Value;
		float lineHeight = font.LineSpacing * scale;
		float y = 0f;
		List<ManualTextLine> lines = [];
		foreach (string paragraph in text.Replace("\r", string.Empty).Split('\n'))
		{
			if (string.IsNullOrWhiteSpace(paragraph))
			{
				y += lineHeight * 0.65f;
				continue;
			}

			string line = string.Empty;
			foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				string candidate = line.Length == 0 ? word : line + " " + word;
				if (font.MeasureString(candidate).X * scale > width && line.Length > 0)
				{
					lines.Add(new ManualTextLine(line, y));
					y += lineHeight;
					line = word;
				}
				else
				{
					line = candidate;
				}
			}

			if (line.Length > 0)
			{
				lines.Add(new ManualTextLine(line, y));
				y += lineHeight;
			}
			y += lineHeight * 0.3f;
		}

		contentHeight = y;
		return lines;
	}

	private static void HandleScrollInput(
		Rectangle area,
		Point mouse,
		Rectangle topButton,
		Rectangle track,
		Rectangle handle,
		float maxScroll,
		int handleTravel)
	{
		if (area.Contains(mouse))
		{
			PlayerInput.LockVanillaMouseScroll("Xianxia: Cultivator Manual");
			Main.LocalPlayer.mouseInterface = true;

			if (PlayerInput.ScrollWheelDeltaForUI != 0)
			{
				scrollOffset = MathHelper.Clamp(
					scrollOffset - PlayerInput.ScrollWheelDeltaForUI * 0.35f,
					0f,
					maxScroll);
			}
		}

		if (!Main.mouseLeft)
		{
			draggingScrollBar = false;
		}

		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (topButton.Contains(mouse) && scrollOffset > 0f)
			{
				ResetScroll();
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (handle.Contains(mouse))
			{
				draggingScrollBar = true;
				scrollBarGrabOffset = mouse.Y - handle.Y;
				Main.mouseLeftRelease = false;
			}
			else if (track.Contains(mouse))
			{
				draggingScrollBar = true;
				scrollBarGrabOffset = handle.Height * 0.5f;
				Main.mouseLeftRelease = false;
			}
		}

		if (draggingScrollBar && Main.mouseLeft && handleTravel > 0)
		{
			float handleTop = MathHelper.Clamp(
				mouse.Y - scrollBarGrabOffset,
				track.Y,
				track.Bottom - handle.Height);
			scrollOffset = (handleTop - track.Y) / handleTravel * maxScroll;
			Main.LocalPlayer.mouseInterface = true;
		}
	}

	private static void DrawScrollBar(Rectangle track, Rectangle handle, Point mouse)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel,
			new Rectangle(track.X - 2, track.Y - 2, track.Width + 4, track.Height + 4),
			new Color(139, 113, 60, 155));
		Main.spriteBatch.Draw(pixel, track, new Color(76, 72, 57, 155));
		Main.spriteBatch.Draw(pixel, handle,
			handle.Contains(mouse) || draggingScrollBar
				? new Color(65, 190, 158)
				: new Color(42, 122, 98));
	}

	private static void ResetScroll()
	{
		scrollOffset = 0f;
		draggingScrollBar = false;
		scrollBarGrabOffset = 0f;
	}

	private static void DrawButton(Rectangle rectangle, string text, bool hovered, bool enabled)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color border = enabled ? new Color(55, 126, 105) : new Color(115, 108, 89);
		Color fill = enabled
			? hovered ? new Color(74, 170, 139) : new Color(42, 105, 86)
			: new Color(137, 129, 106);
		Main.spriteBatch.Draw(pixel, rectangle, border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4), fill);
		DrawCentered(text, rectangle.Center.ToVector2(), enabled ? Color.White : new Color(190, 184, 163), 0.72f);
	}

	private static void DrawIndexButton(
		Rectangle rectangle,
		string text,
		int page,
		bool hovered)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color border = hovered ? new Color(211, 164, 69) : new Color(55, 126, 105);
		Color fill = hovered ? new Color(58, 139, 112) : new Color(38, 102, 83);
		Main.spriteBatch.Draw(pixel, rectangle, border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4), fill);

		Rectangle pageBadge = new(rectangle.X + 7, rectangle.Y + 7,
			Math.Min(34, rectangle.Height - 14), rectangle.Height - 14);
		Main.spriteBatch.Draw(pixel, pageBadge,
			hovered ? new Color(235, 200, 112) : new Color(201, 181, 119));
		DrawCentered(page.ToString(), pageBadge.Center.ToVector2(), new Color(69, 61, 39), 0.55f);

		Rectangle labelArea = new(pageBadge.Right + 5, rectangle.Y + 3,
			rectangle.Right - pageBadge.Right - 10, rectangle.Height - 6);
		DrawCenteredFitted(text, labelArea.Center.ToVector2(), labelArea.Width,
			Color.White, 0.62f, 0.45f);
	}

	private static void DrawCenteredFitted(
		string text,
		Vector2 center,
		float maximumWidth,
		Color color,
		float preferredScale,
		float minimumScale)
	{
		DynamicSpriteFont font = FontAssets.MouseText.Value;
		float width = font.MeasureString(text).X;
		float scale = width <= 0f
			? preferredScale
			: MathHelper.Clamp(maximumWidth / width, minimumScale, preferredScale);
		DrawCentered(text, center, color, scale);
	}

	private static void DrawCentered(string text, Vector2 center, Color color, float scale)
	{
		DynamicSpriteFont font = FontAssets.MouseText.Value;
		Vector2 size = font.MeasureString(text) * scale;
		Main.spriteBatch.DrawString(font, text, center - size * 0.5f, color, 0f,
			Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}
