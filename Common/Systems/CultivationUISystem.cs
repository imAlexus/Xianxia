using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Xianxia.Common.Players;
using Xianxia.Common.Elements;
using Xianxia.Common.Config;
using Xianxia.Content.Buffs;
using Xianxia.Common.Abilities;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Alchemy;
using Xianxia.Content.Items.Artifacts;
using Xianxia.Content.Items.Formations;
using Xianxia.Content.Items.Sect;

namespace Xianxia.Common.Systems;

public class CultivationUISystem : ModSystem
{
	private enum AbilityMenuPage
	{
		Abilities,
		Paths,
		Sect,
		Character
	}

	private static AbilityMenuPage abilityMenuPage;
	private enum PathMenuPage
	{
		Alchemy,
		Formations,
		Forging
	}
	private static PathMenuPage pathMenuPage;
	private static float abilityTreeScrollOffset;
	private static bool draggingAbilityTreeScrollBar;
	private static int abilityTreeScrollBarGrabOffset;
	private static int selectedTechniqueLoadoutSlot;
	private static bool toggleWheelExpanded;
	private const int BarWidth = 300;
	private const int BarHeight = 22;
	private const int BorderSize = 2;
	private const float WheelInnerRadius = 72f;
	private const float WheelOuterRadius = 205f;
	private const float ToggleWheelInnerRadius = 218f;
	private const float ToggleWheelOuterRadius = 315f;
	private const float WheelStartAngle = -MathHelper.PiOver2;

	private enum AbilityWheelId
	{
		Empty,
		ToggleMenu,
		QiProtection,
		QiSense,
		QiFlight,
		NascentTeleport,
		SpiritualPressure,
		FlameStep,
		Fireball,
		QiPalm,
		QiResistance,
		NightVision,
		QiBurning,
		SpiritualRain,
		SpiritSwordRain,
		SectProtectionFormation
	}

	private readonly record struct AbilityWheelEntry(
		AbilityWheelId Id,
		string Name,
		string Information,
		string BadgeText,
		bool IsPassive,
		bool IsUnlocked,
		bool IsEnabled,
		int IconItemType,
		string IconTexturePath
	);

	public override void PostUpdateInput()
	{
		if (Main.LocalPlayer is not { active: true })
			return;
		CultivationPlayer cultivation = Main.LocalPlayer.GetModPlayer<CultivationPlayer>();
		if (cultivation.IsAbilityTreeOpen
			&& Main.keyState.IsKeyDown(Keys.Escape)
			&& Main.oldKeyState.IsKeyUp(Keys.Escape))
		{
			cultivation.CloseAbilityTree();
			SoundEngine.PlaySound(SoundID.MenuClose);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int resourceBarIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Resource Bars");
		LegacyGameInterfaceLayer qiLayer = new(
			"Xianxia: Qi Bar",
			DrawQiBar,
			InterfaceScaleType.UI
		);

		if (resourceBarIndex >= 0)
		{
			layers.Insert(resourceBarIndex, qiLayer);
		}
		else
		{
			layers.Add(qiLayer);
		}

		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		LegacyGameInterfaceLayer abilityWheelLayer = new(
			"Xianxia: Ability Wheel",
			DrawAbilityWheel,
			InterfaceScaleType.UI
		);

		if (mouseTextIndex >= 0)
		{
			layers.Insert(mouseTextIndex, abilityWheelLayer);
		}
		else
		{
			layers.Add(abilityWheelLayer);
		}

		mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		LegacyGameInterfaceLayer abilityTreeLayer = new(
			"Xianxia: Ability Tree",
			DrawAbilityTree,
			InterfaceScaleType.UI
		);
		if (mouseTextIndex >= 0)
			layers.Insert(mouseTextIndex, abilityTreeLayer);
		else
			layers.Add(abilityTreeLayer);
	}

	private bool DrawAbilityTree()
	{
		if (Main.gameMenu || !Main.LocalPlayer.active)
			return true;

		Player player = Main.LocalPlayer;
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		if (!cultivation.IsAbilityTreeOpen)
			return true;

		player.mouseInterface = true;
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
			new Color(3, 6, 15, 225));
		int width = Math.Min(980, Main.screenWidth - 30);
		int height = Math.Min(650, Main.screenHeight - 30);
		Rectangle panel = new((Main.screenWidth - width) / 2, (Main.screenHeight - height) / 2, width, height);
		Main.spriteBatch.Draw(pixel, panel, new Color(71, 48, 105));
		Rectangle inner = new(panel.X + 4, panel.Y + 4, panel.Width - 8, panel.Height - 8);
		Main.spriteBatch.Draw(pixel, inner, new Color(10, 17, 29, 250));
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Title").Value,
			new Vector2(panel.Center.X, panel.Y + 27), Color.White, 1.05f);
		Rectangle abilitiesTab = new(panel.Center.X - 310, panel.Y + 47, 145, 31);
		Rectangle pathsTab = new(panel.Center.X - 155, panel.Y + 47, 145, 31);
		Rectangle sectTab = new(panel.Center.X, panel.Y + 47, 145, 31);
		Rectangle characterTab = new(panel.Center.X + 155, panel.Y + 47, 145, 31);
		Point mouse = Main.MouseScreen.ToPoint();
		DrawAbilityMenuTab(pixel, abilitiesTab,
			Mod.GetLocalization("AbilityTree.Tabs.Abilities").Value,
			abilityMenuPage == AbilityMenuPage.Abilities, abilitiesTab.Contains(mouse));
		DrawAbilityMenuTab(pixel, pathsTab,
			Mod.GetLocalization("AbilityTree.Tabs.Paths").Value,
			abilityMenuPage == AbilityMenuPage.Paths, pathsTab.Contains(mouse));
		DrawAbilityMenuTab(pixel, sectTab,
			Mod.GetLocalization("AbilityTree.Tabs.Sect").Value,
			abilityMenuPage == AbilityMenuPage.Sect, sectTab.Contains(mouse));
		DrawAbilityMenuTab(pixel, characterTab,
			Mod.GetLocalization("AbilityTree.Tabs.Character").Value,
			abilityMenuPage == AbilityMenuPage.Character, characterTab.Contains(mouse));
		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (abilitiesTab.Contains(mouse))
			{
				abilityMenuPage = AbilityMenuPage.Abilities;
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (pathsTab.Contains(mouse))
			{
				abilityMenuPage = AbilityMenuPage.Paths;
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (sectTab.Contains(mouse))
			{
				abilityMenuPage = AbilityMenuPage.Sect;
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (characterTab.Contains(mouse))
			{
				abilityMenuPage = AbilityMenuPage.Character;
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
		}

		if (abilityMenuPage == AbilityMenuPage.Paths)
		{
			DrawPathPage(pixel, panel, mouse);
			return true;
		}
		if (abilityMenuPage == AbilityMenuPage.Sect)
		{
			DrawSectPage(pixel, panel, mouse);
			return true;
		}
		if (abilityMenuPage == AbilityMenuPage.Character)
		{
			DrawSpiritualRootPage(pixel, panel);
			return true;
		}

		CultivationAbility[][] abilityGroups =
		[
			[CultivationAbility.SpiritBreathing, CultivationAbility.Meditation],
			[CultivationAbility.QiSense, CultivationAbility.SwordIntent,
				CultivationAbility.QiResistance, CultivationAbility.Fireball,
				CultivationAbility.QiPalm, CultivationAbility.SpiritualRain],
			[CultivationAbility.QiProtection, CultivationAbility.FlameStep,
				CultivationAbility.NightVision, CultivationAbility.SpiritSwordRain,
				CultivationAbility.QiBurning],
			[CultivationAbility.GoldenCoreCirculation, CultivationAbility.QiFlight,
				CultivationAbility.SectProtectionFormation],
			[CultivationAbility.NascentSoulRegeneration,
				CultivationAbility.NascentTeleport, CultivationAbility.SpiritualPressure]
		];

		DrawTechniqueLoadoutEditor(
			pixel, panel, mouse, cultivation);
		CultivationAbility? hovered = null;
		Rectangle details = new(panel.X + 18, panel.Bottom - 102,
			panel.Width - 36, 82);
		Rectangle listArea = new(panel.X + 14, panel.Y + 166,
			panel.Width - 48, details.Y - panel.Y - 173);
		const int cardsPerLine = 4;
		const int realmHeaderHeight = 19;
		const int cardHeight = 52;
		const int cardGap = 7;
		const int lineGap = 4;
		const int realmGap = 3;
		int cardWidth = (listArea.Width - cardGap * (cardsPerLine - 1))
			/ cardsPerLine;
		int contentHeight = 0;
		foreach (CultivationAbility[] group in abilityGroups)
		{
			int groupLines = (group.Length + cardsPerLine - 1) / cardsPerLine;
			contentHeight += realmHeaderHeight
				+ groupLines * cardHeight
				+ (groupLines - 1) * lineGap
				+ realmGap;
		}
		float maximumScroll = Math.Max(0f, contentHeight - listArea.Height);
		abilityTreeScrollOffset = MathHelper.Clamp(
			abilityTreeScrollOffset, 0f, maximumScroll);
		Rectangle scrollTrack = new(listArea.Right + 8, listArea.Y, 9, listArea.Height);
		int scrollHandleHeight = maximumScroll <= 0f
			? scrollTrack.Height
			: Math.Max(42, (int)MathF.Round(scrollTrack.Height
				* (listArea.Height / (float)contentHeight)));
		int scrollHandleTravel = Math.Max(0, scrollTrack.Height - scrollHandleHeight);
		int scrollHandleY = scrollTrack.Y + (maximumScroll <= 0f
			? 0
			: (int)MathF.Round(abilityTreeScrollOffset / maximumScroll
				* scrollHandleTravel));
		Rectangle scrollHandle = new(scrollTrack.X, scrollHandleY,
			scrollTrack.Width, scrollHandleHeight);
		HandleAbilityTreeScrollInput(listArea, scrollTrack, scrollHandle,
			mouse, maximumScroll, scrollHandleTravel);
		int currentY = listArea.Y - (int)MathF.Round(abilityTreeScrollOffset);
		ElementalCultivationPlayer elemental =
			player.GetModPlayer<ElementalCultivationPlayer>();
		SpiritualRootPlayer root =
			player.GetModPlayer<SpiritualRootPlayer>();

		for (int realm = 0; realm < abilityGroups.Length; realm++)
		{
			bool realmUnlocked = cultivation.RealmIndex >= realm;
			CultivationAbility[] abilities = abilityGroups[realm];
			int lines = (abilities.Length + cardsPerLine - 1) / cardsPerLine;
			Rectangle realmHeader = new(listArea.X, currentY,
				listArea.Width, realmHeaderHeight);
			string realmName = Mod.GetLocalization(
				$"Cultivation.Realms.{GetRealmLocalizationKey(realm)}").Value;
			if (realmHeader.Top >= listArea.Top
				&& realmHeader.Bottom <= listArea.Bottom)
			{
				Main.spriteBatch.Draw(pixel, realmHeader, realmUnlocked
					? new Color(31, 91, 94, 235)
					: new Color(31, 34, 43, 235));
				DrawCenteredText(realmName, realmHeader.Center.ToVector2(),
					realmUnlocked ? Color.White : new Color(145, 145, 155), 0.58f);
			}

			for (int i = 0; i < abilities.Length; i++)
			{
				CultivationAbility ability = abilities[i];
				int column = i % cardsPerLine;
				int line = i / cardsPerLine;
				Rectangle card = new(
					listArea.X + column * (cardWidth + cardGap),
					currentY + realmHeaderHeight + line * (cardHeight + lineGap),
					cardWidth, cardHeight);
				if (card.Top < listArea.Top || card.Bottom > listArea.Bottom)
					continue;
				bool unlocked = cultivation.IsAbilityUnlocked(ability);
				bool isHovered = card.Contains(mouse);
				bool equipped = CultivationAbilityInfo
					.IsTechniqueLoadoutAbility(ability)
					&& cultivation.IsTechniqueEquipped(ability);
				if (isHovered)
					hovered = ability;
				SpiritualElement elements =
					CultivationAbilityInfo.GetSpiritualElements(ability);
				Color elementColor = elements == SpiritualElement.None
					? new Color(68, 211, 210)
					: SpiritualElementInfo.GetColor(elements);
				bool rootMatched = root.IsRevealed
					&& elemental.GetAffinity(elements) > 0f;
				Color border = unlocked
					? (isHovered ? Color.White
						: equipped ? Color.Gold
						: rootMatched ? Color.Lerp(elementColor, Color.Gold, 0.3f)
						: elementColor)
					: (isHovered ? new Color(145, 145, 155) : new Color(76, 78, 88));
				Main.spriteBatch.Draw(pixel, card, border);
				Main.spriteBatch.Draw(pixel,
					new Rectangle(card.X + 3, card.Y + 3, card.Width - 6, card.Height - 6),
					unlocked
						? Color.Lerp(new Color(18, 49, 58), elementColor, 0.1f)
						: new Color(27, 30, 38));
				if (elements != SpiritualElement.None)
				Main.spriteBatch.Draw(pixel,
					new Rectangle(card.X + 3, card.Y + 3, 3, card.Height - 6),
					elementColor);

				Vector2 iconCenter = new(card.X + 25f, card.Center.Y - 1f);
				DrawTreeAbilityIcon(iconCenter, ability, unlocked, 28f);
				float textCenterX = card.X + 48f + (card.Width - 51f) * 0.5f;
				string name = Mod.GetLocalization($"AbilityTree.Abilities.{ability}.Name").Value;
				DrawCenteredTextFitted(name, new Vector2(textCenterX, card.Y + 14f),
					Math.Max(40f, card.Width - 55f),
					unlocked ? Color.White : new Color(165, 165, 175), 0.56f);
				bool passive = ability is CultivationAbility.QiSense
					or CultivationAbility.QiProtection
					or CultivationAbility.SpiritBreathing
					or CultivationAbility.GoldenCoreCirculation
					or CultivationAbility.NascentSoulRegeneration
					or CultivationAbility.SwordIntent;
				string abilityType = Mod.GetLocalization(passive
					? "AbilityTree.Passive"
					: "AbilityTree.Active").Value;
				string elementName = elements == SpiritualElement.None
					? string.Empty
					: SpiritualElementInfo.GetDisplayName(Mod, elements);
				string status = unlocked
					? string.IsNullOrEmpty(elementName)
						? $"{abilityType}  •  Lv.{cultivation.GetAbilityLevel(ability)}"
						: $"Lv.{cultivation.GetAbilityLevel(ability)}  •  {elementName}"
					: Mod.GetLocalization("AbilityTree.Locked").Value;
				if (equipped)
				status = Mod.GetLocalization(
					"TechniqueLoadout.Equipped").Value
					+ "  •  " + status;
				DrawCenteredTextFitted(status, new Vector2(textCenterX, card.Y + 32f),
					Math.Max(40f, card.Width - 55f),
					unlocked ? elementColor : new Color(135, 135, 145), 0.5f);

				Rectangle experienceBar = new(card.X + 48, card.Bottom - 11,
					Math.Max(8, card.Width - 56), 4);
				Main.spriteBatch.Draw(pixel, experienceBar, new Color(10, 15, 24));
				if (unlocked)
				{
					int required = cultivation.GetAbilityExperienceRequired(ability);
					float progress = required <= 0 ? 1f
						: cultivation.GetAbilityExperience(ability) / (float)required;
					Rectangle fill = new(experienceBar.X, experienceBar.Y,
						(int)(experienceBar.Width * MathHelper.Clamp(progress, 0f, 1f)), experienceBar.Height);
					Main.spriteBatch.Draw(pixel, fill,
						required <= 0 ? Color.Gold : new Color(174, 92, 238));
				}
				if (isHovered && Main.mouseLeft
					&& Main.mouseLeftRelease && unlocked
					&& CultivationAbilityInfo
						.IsTechniqueLoadoutAbility(ability))
				{
					Main.mouseLeftRelease = false;
					bool assigned =
						cultivation.TrySetTechniqueLoadoutSlot(
							cultivation.ActiveTechniqueLoadoutPreset,
							selectedTechniqueLoadoutSlot, ability);
					if (assigned)
						cultivation.TrySelectActiveTechniqueSlot(
							selectedTechniqueLoadoutSlot);
					SoundEngine.PlaySound(assigned
						? SoundID.MenuTick : SoundID.MenuClose);
				}
			}

			currentY += realmHeaderHeight
				+ lines * cardHeight
				+ (lines - 1) * lineGap
				+ realmGap;
		}

		DrawAbilityTreeScrollBar(pixel, scrollTrack, scrollHandle, mouse,
			maximumScroll > 0f);

		Main.spriteBatch.Draw(pixel,
			new Rectangle(details.X - 2, details.Y - 2, details.Width + 4, details.Height + 4),
			new Color(76, 65, 99));
		Main.spriteBatch.Draw(pixel, details, new Color(18, 27, 43, 245));
		if (hovered.HasValue)
		{
			CultivationAbility ability = hovered.Value;
			bool unlocked = cultivation.IsAbilityUnlocked(ability);
			bool realmReached = cultivation.RealmIndex >= CultivationAbilityInfo.RequiredRealm(ability);
			SpiritualElement elements =
				CultivationAbilityInfo.GetSpiritualElements(ability);
			Color headingColor = elements == SpiritualElement.None
				? Color.LightCyan
				: SpiritualElementInfo.GetColor(elements);
			string heading = Mod.GetLocalization(
				$"AbilityTree.Abilities.{ability}.Name").Value;
			if (elements != SpiritualElement.None)
				heading += $"  •  {SpiritualElementInfo.GetDisplayName(Mod, elements)}";
			DrawCenteredTextFitted(heading,
				new Vector2(details.Center.X, details.Y + 15),
				details.Width - 20, headingColor, 0.67f);

			string effect = unlocked
				? GetAbilityTreeDetails(cultivation, ability)
				: realmReached
					? Mod.GetLocalization("Sect.RequiresManual").Value
					: Mod.GetLocalization("AbilityTree.RequiresRealm").Format(
						Mod.GetLocalization($"Cultivation.Realms.{GetRealmLocalizationKey(CultivationAbilityInfo.RequiredRealm(ability))}").Value);
			DrawCenteredTextFitted(effect,
				new Vector2(details.Center.X, details.Y + 40),
				details.Width - 22, unlocked ? Color.White : Color.Gray, 0.57f);
			string synergy = GetRootSynergyText(elemental, root, elements);
			DrawCenteredTextFitted(synergy,
				new Vector2(details.Center.X, details.Y + 65),
				details.Width - 22,
				elements == SpiritualElement.None ? Color.LightGray
					: elemental.GetAffinity(elements) > 0f && root.IsRevealed
						? Color.LightGreen : Color.Gray,
				0.53f);
		}
		else
		{
			DrawCenteredText(Mod.GetLocalization("AbilityTree.Hint").Value,
				details.Center.ToVector2(), Color.LightGray, 0.66f);
		}
		return true;
	}

	private static void HandleAbilityTreeScrollInput(
		Rectangle listArea,
		Rectangle track,
		Rectangle handle,
		Point mouse,
		float maximumScroll,
		int handleTravel)
	{
		if (listArea.Contains(mouse) || track.Contains(mouse))
		{
			PlayerInput.LockVanillaMouseScroll("Xianxia: Ability Tree");
			Main.LocalPlayer.mouseInterface = true;
			if (PlayerInput.ScrollWheelDeltaForUI != 0)
			{
				abilityTreeScrollOffset = MathHelper.Clamp(
					abilityTreeScrollOffset
						- PlayerInput.ScrollWheelDeltaForUI * 0.25f,
					0f, maximumScroll);
			}
		}

		if (!Main.mouseLeft)
			draggingAbilityTreeScrollBar = false;

		if (maximumScroll > 0f && Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (handle.Contains(mouse))
			{
				draggingAbilityTreeScrollBar = true;
				abilityTreeScrollBarGrabOffset = mouse.Y - handle.Y;
				Main.mouseLeftRelease = false;
			}
			else if (track.Contains(mouse))
			{
				draggingAbilityTreeScrollBar = true;
				abilityTreeScrollBarGrabOffset = handle.Height / 2;
				Main.mouseLeftRelease = false;
			}
		}

		if (draggingAbilityTreeScrollBar && Main.mouseLeft
			&& handleTravel > 0)
		{
			float handleTop = MathHelper.Clamp(
				mouse.Y - abilityTreeScrollBarGrabOffset,
				track.Y, track.Bottom - handle.Height);
			abilityTreeScrollOffset =
				(handleTop - track.Y) / handleTravel * maximumScroll;
			Main.LocalPlayer.mouseInterface = true;
		}
	}

	private static void DrawAbilityTreeScrollBar(
		Texture2D pixel,
		Rectangle track,
		Rectangle handle,
		Point mouse,
		bool enabled)
	{
		Main.spriteBatch.Draw(pixel,
			new Rectangle(track.X - 2, track.Y - 2,
				track.Width + 4, track.Height + 4),
			new Color(78, 62, 103, 210));
		Main.spriteBatch.Draw(pixel, track, new Color(20, 28, 43, 245));
		Main.spriteBatch.Draw(pixel, handle,
			enabled
				? handle.Contains(mouse) || draggingAbilityTreeScrollBar
					? new Color(83, 230, 222)
					: new Color(50, 154, 158)
				: new Color(55, 58, 70));
	}

	private void DrawSpiritualRootPage(Texture2D pixel, Rectangle panel)
	{
		Player player = Main.LocalPlayer;
		SpiritualRootPlayer root =
			player.GetModPlayer<SpiritualRootPlayer>();
		CultivationPlayer cultivation =
			player.GetModPlayer<CultivationPlayer>();
		Rectangle content = new(panel.X + 18, panel.Y + 88,
			panel.Width - 36, panel.Height - 112);
		Main.spriteBatch.Draw(pixel, content, new Color(18, 29, 45, 245));
		int gap = 12;
		int leftWidth = (content.Width - gap - 24) / 2;
		Rectangle rootPanel = new(content.X + 8, content.Y + 8,
			leftWidth, content.Height - 16);
		Rectangle statsPanel = new(rootPanel.Right + gap, rootPanel.Y,
			content.Right - rootPanel.Right - gap - 8, rootPanel.Height);
		Main.spriteBatch.Draw(pixel, rootPanel, new Color(20, 35, 52, 245));
		Main.spriteBatch.Draw(pixel, statsPanel, new Color(23, 32, 48, 245));
		DrawCenteredText(Mod.GetLocalization("SpiritualRoots.UI.Title").Value,
			new Vector2(rootPanel.Center.X, rootPanel.Y + 27),
			new Color(185, 135, 255), 0.82f);

		if (!root.IsRevealed)
		{
			DrawCenteredText(Mod.GetLocalization("SpiritualRoots.UI.Hidden").Value,
				new Vector2(rootPanel.Center.X, rootPanel.Center.Y - 18),
				Color.LightGray, 0.8f);
			DrawCenteredTextFitted(
				Mod.GetLocalization("SpiritualRoots.UI.AppraisalHint").Value,
				new Vector2(rootPanel.Center.X, rootPanel.Center.Y + 25),
				rootPanel.Width - 30, Color.LightCyan, 0.62f);
		}
		else
		{
			string quality = Mod.GetLocalization(
				$"SpiritualRoots.Qualities.{root.GetQualityLocalizationKey()}").Value;
			DrawCenteredTextFitted(Mod.GetLocalization("SpiritualRoots.UI.Summary").Format(
					quality, SpiritualElementInfo.GetDisplayName(Mod, root.PrimaryElement),
					root.Purity),
				new Vector2(rootPanel.Center.X, rootPanel.Y + 64),
				rootPanel.Width - 24, Color.Gold, 0.65f);
			DrawCenteredTextFitted(
				Mod.GetLocalization("SpiritualRoots.UI.CultivationBonus").Format(
					MathF.Round(root.CultivationGainBonusPercent, 1)),
				new Vector2(rootPanel.Center.X, rootPanel.Y + 94),
				rootPanel.Width - 24, Color.LightGreen, 0.58f);

			string resonanceText = root.TryGetBiomeMeditationResonance(
				out SpiritualElement resonanceElement,
				out float resonanceBonus,
				out string resonanceBiome)
				? Mod.GetLocalization(
					"SpiritualRoots.UI.BiomeResonanceActive").Format(
						SpiritualElementInfo.GetDisplayName(
							Mod, resonanceElement),
						Mod.GetLocalization(
							$"SpiritualRoots.Biomes.{resonanceBiome}").Value,
						MathF.Round(resonanceBonus, 1))
				: Mod.GetLocalization(
					"SpiritualRoots.UI.BiomeResonanceInactive").Value;
			DrawCenteredTextFitted(resonanceText,
				new Vector2(rootPanel.Center.X, rootPanel.Y + 119),
				rootPanel.Width - 24,
				resonanceBonus > 0f ? Color.LightGreen : Color.Gray, 0.53f);

			int row = 0;
			foreach (SpiritualElement element in root.Elements.Enumerate())
			{
				int affinity = root.GetAffinity(element);
				int y = rootPanel.Y + 145 + row * 42;
				Rectangle bar = new(rootPanel.X + 16, y,
					rootPanel.Width - 32, 20);
				Main.spriteBatch.Draw(pixel, bar, new Color(35, 40, 55));
				Rectangle fill = new(bar.X + 2, bar.Y + 2,
					(int)((bar.Width - 4) * affinity / 100f), bar.Height - 4);
				Main.spriteBatch.Draw(pixel, fill,
					SpiritualElementInfo.GetColor(element));
				DrawCenteredText(Mod.GetLocalization("SpiritualRoots.UI.Affinity").Format(
						SpiritualElementInfo.GetDisplayName(Mod, element), affinity),
					bar.Center.ToVector2(), Color.White, 0.54f);
				row++;
			}
		}

		string foundationGrade = cultivation.RealmIndex >= 2
			? cultivation.GetFoundationQualityName()
			: Mod.GetLocalization("CharacterStats.NotReached").Value;
		string coreGrade = cultivation.RealmIndex >= 3
			? Mod.GetLocalization("BreakthroughGrades.GoldenCoreTier")
				.Format(cultivation.GoldenCoreTier)
			: Mod.GetLocalization("CharacterStats.NotReached").Value;
		DrawCenteredTextFitted(
			Mod.GetLocalization("CharacterStats.BreakthroughGrades").Format(
				foundationGrade, coreGrade),
			new Vector2(rootPanel.Center.X, rootPanel.Bottom - 207),
			rootPanel.Width - 24, Color.Gold, 0.56f);
		DrawCenteredTextFitted(
			Mod.GetLocalization("CharacterStats.GradeQiGathering").Format(
				MathF.Round(
					(cultivation.BreakthroughGradeQiGatheringMultiplier - 1f)
						* 100f, 1)),
			new Vector2(rootPanel.Center.X, rootPanel.Bottom - 184),
			rootPanel.Width - 24, Color.LightGreen, 0.53f);

		DrawHeartDemonPanel(pixel, rootPanel, cultivation);
		DrawCharacterStats(pixel, statsPanel, player, cultivation);
	}

	private void DrawHeartDemonPanel(Texture2D pixel, Rectangle rootPanel,
		CultivationPlayer cultivation)
	{
		Rectangle panel = new(rootPanel.X + 14, rootPanel.Bottom - 165,
			rootPanel.Width - 28, 148);
		Main.spriteBatch.Draw(pixel, panel, new Color(31, 22, 48, 245));
		DrawCenteredText(Mod.GetLocalization(
				"CharacterStats.HeartDemonsTitle").Value,
			new Vector2(panel.Center.X, panel.Y + 18),
			Color.MediumPurple, 0.7f);
		DrawCenteredTextFitted(Mod.GetLocalization(
				"CharacterStats.HeartDemonsPoints").Format(
					cultivation.HeartDemonPoints, 9,
					MathF.Round(cultivation.HeartDemonBreakthroughPenalty, 1),
					MathF.Round(
						(1f - cultivation.HeartDemonCultivationGainMultiplier)
							* 100f, 1)),
			new Vector2(panel.Center.X, panel.Y + 45),
			panel.Width - 16, Color.OrangeRed, 0.58f);
		DrawCenteredTextFitted(Mod.GetLocalization(
				"CharacterStats.HeartDemonsProgress").Format(
					cultivation.BreakthroughFailuresTowardHeartDemon, 2,
					cultivation.DeathsTowardHeartDemon, 5),
			new Vector2(panel.Center.X, panel.Y + 70),
			panel.Width - 16, Color.LightGray, 0.54f);
		Rectangle button = new(panel.X + 38, panel.Bottom - 49,
			panel.Width - 76, 36);
		bool canStart = cultivation.CanStartHeartDemonTrial(out _);
		bool hovered = button.Contains(Main.MouseScreen.ToPoint());
		DrawButton(pixel, button,
			Mod.GetLocalization("CharacterStats.ConfrontHeartDemon").Value,
			hovered && canStart,
			canStart ? new Color(91, 45, 125)
				: new Color(62, 58, 70));
		if (hovered && Main.mouseLeft && Main.mouseLeftRelease)
		{
			Main.mouseLeftRelease = false;
			cultivation.RequestHeartDemonTrialConfirmation();
		}
	}

	private void DrawCharacterStats(Texture2D pixel, Rectangle panel,
		Player player, CultivationPlayer cultivation)
	{
		DrawCenteredText(Mod.GetLocalization("CharacterStats.Title").Value,
			new Vector2(panel.Center.X, panel.Y + 27),
			new Color(105, 235, 205), 0.9f);
		int x = panel.X + 10;
		int width = panel.Width - 20;
		int y = panel.Y + 48;

		DrawCharacterStatBox(pixel, new Rectangle(x, y, width, 58),
			Mod.GetLocalization("CharacterStats.Progression").Value,
			Mod.GetLocalization("CharacterStats.ProgressionRealmValue").Format(
				cultivation.GetRealmName(), cultivation.Stage),
			Mod.GetLocalization("CharacterStats.ProgressionQiValue").Format(
				cultivation.Qi, cultivation.MaxQi,
				cultivation.QiExp, cultivation.NextStageThreshold),
			Color.LightCyan);
		y += 62;
		DrawCharacterStatBox(pixel, new Rectangle(x, y, width, 58),
			Mod.GetLocalization("CharacterStats.CurrentStats").Value,
			Mod.GetLocalization("CharacterStats.CurrentStatsPrimary").Format(
				player.statLife, player.statLifeMax2, player.statDefense),
			Mod.GetLocalization("CharacterStats.CurrentStatsSecondary").Format(
				MathF.Round(player.GetCritChance(DamageClass.Generic), 1),
				MathF.Round(player.endurance * 100f, 1)),
			Color.White);
		y += 62;
		DrawCharacterStatBox(pixel, new Rectangle(x, y, width, 66),
			Mod.GetLocalization("CharacterStats.CultivationBonuses").Value,
			cultivation.GetRealmBonusPrimarySummary(),
			cultivation.GetRealmBonusSecondarySummary(),
			new Color(145, 230, 255));
		y += 70;
		DrawCharacterStatBox(pixel, new Rectangle(x, y, width, 58),
			Mod.GetLocalization("CharacterStats.BreakthroughRecord").Value,
			Mod.GetLocalization("CharacterStats.BreakthroughRecordPrimary").Format(
				cultivation.RealmBreakthroughAttempts,
				cultivation.RealmBreakthroughSuccesses),
			Mod.GetLocalization("CharacterStats.BreakthroughRecordSecondary").Format(
				cultivation.RealmBreakthroughFailures,
				cultivation.BreakthroughPillsConsumed),
			Color.Gold);
		y += 62;
		DrawCharacterStatBox(pixel, new Rectangle(x, y, width, 48),
			Mod.GetLocalization("CharacterStats.TreasureImprints").Value,
			Mod.GetLocalization("CharacterStats.TreasureImprintsValue").Format(
				cultivation.HeavenlyEyeImprints,
				cultivation.HeavenlyRoyalNectarImprints,
				cultivation.HeavenlyBoneMarrowImprints),
			null,
			new Color(255, 205, 125));
		y += 55;
		DrawCenteredText(
			Mod.GetLocalization("CharacterStats.BreakthroughHistory").Value,
			new Vector2(panel.Center.X, y), new Color(185, 135, 255), 0.68f);
		y += 18;
		for (int realm = 1; realm <= 4; realm++)
		{
			Rectangle history = new(x, y, width, 30);
			Main.spriteBatch.Draw(pixel, history,
				realm % 2 == 0
					? new Color(27, 39, 55, 245)
					: new Color(31, 44, 61, 245));
			DrawCenteredTextFitted(
				Mod.GetLocalization("CharacterStats.HistoryEntry").Format(
					cultivation.GetRealmName(realm),
					cultivation.GetSuccessfulBreakthroughCatalystSummary(realm)),
				history.Center.ToVector2(), history.Width - 12,
				realm <= cultivation.RealmIndex ? Color.LightGreen : Color.Gray,
				0.56f);
			y += 33;
		}
	}

	private void DrawCharacterStatBox(Texture2D pixel, Rectangle box,
		string title, string value, string secondValue, Color valueColor)
	{
		Main.spriteBatch.Draw(pixel, box, new Color(29, 42, 59, 245));
		DrawCenteredTextFitted(title,
			new Vector2(box.Center.X, box.Y + 12), box.Width - 12,
			Color.LightGray, 0.54f);
		float valueY = secondValue is null
			? box.Center.Y + 8
			: box.Y + 33;
		DrawCenteredTextFitted(value,
			new Vector2(box.Center.X, valueY), box.Width - 14,
			valueColor, 0.62f);
		if (secondValue is not null)
		{
			DrawCenteredTextFitted(secondValue,
				new Vector2(box.Center.X, box.Bottom - 10), box.Width - 14,
				valueColor, 0.59f);
		}
	}

	private void DrawSectPage(Texture2D pixel, Rectangle panel, Point mouse)
	{
		SectPlayer sect = Main.LocalPlayer.GetModPlayer<SectPlayer>();
		Rectangle content = new(panel.X + 18, panel.Y + 90, panel.Width - 36, panel.Height - 112);
		Main.spriteBatch.Draw(pixel, content, new Color(15, 24, 38, 245));
		DrawCenteredText(Mod.GetLocalization("Sect.UI.Title").Value,
			new Vector2(content.Center.X, content.Y + 28), new Color(105, 235, 205), 0.95f);

		if (!sect.JoinedSect)
		{
			DrawCenteredText(Mod.GetLocalization("Sect.UI.NotJoined").Value,
				new Vector2(content.Center.X, content.Center.Y - 20), Color.LightGray, 0.75f);
			DrawCenteredText(Mod.GetLocalization("Sect.UI.FindElder").Value,
				new Vector2(content.Center.X, content.Center.Y + 20), Color.LightGreen, 0.65f);
			return;
		}

		Rectangle status = new(content.X + 16, content.Y + 55, content.Width - 32, 92);
		Main.spriteBatch.Draw(pixel, status, new Color(20, 39, 52, 245));
		DrawCenteredText(Mod.GetLocalization("Sect.UI.Rank").Format(sect.GetRankName()),
			new Vector2(status.Center.X, status.Y + 21), Color.Gold, 0.74f);
		string rankProgress = sect.Rank >= 3
			? Mod.GetLocalization("AbilityTree.MaxLevel").Value
			: $"{sect.LifetimeContribution}/{sect.NextRankRequirement}";
		DrawCenteredText(Mod.GetLocalization("Sect.UI.Contribution").Format(
				sect.CurrentContribution, rankProgress),
			new Vector2(status.Center.X, status.Y + 49), Color.White, 0.62f);
		DrawCenteredText(Mod.GetLocalization("Sect.UI.Mission").Format(sect.GetMissionDescription()),
			new Vector2(status.Center.X, status.Y + 74),
			sect.IsMissionComplete() ? Color.LightGreen : Color.LightCyan, 0.56f);

		(int item, bool unlocked, int rank, string ability)[] techniques =
		[
			(ModContent.ItemType<SwordIntentManual>(), sect.SwordIntentUnlocked, 0, "SwordIntent"),
			(ModContent.ItemType<SpiritSwordRainManual>(), sect.SpiritSwordRainUnlocked, 1, "SpiritSwordRain"),
			(ModContent.ItemType<SectProtectionFormationManual>(),
				sect.SectProtectionFormationUnlocked, 2, "SectProtectionFormation")
		];
		int cardWidth = (content.Width - 64) / techniques.Length;
		for (int i = 0; i < techniques.Length; i++)
		{
			(int item, bool unlocked, int rank, string ability) = techniques[i];
			Rectangle card = new(content.X + 16 + i * (cardWidth + 16), status.Bottom + 22,
				cardWidth, 150);
			bool hovered = card.Contains(mouse);
			Main.spriteBatch.Draw(pixel, card, hovered ? Color.White : new Color(68, 211, 210));
			Main.spriteBatch.Draw(pixel, new Rectangle(card.X + 3, card.Y + 3,
				card.Width - 6, card.Height - 6), new Color(21, 52, 62));
			DrawPathItemIcon(item, new Vector2(card.Center.X, card.Y + 45), 48f,
				sect.Rank >= rank ? Color.White : Color.Gray);
			DrawCenteredTextFitted(Mod.GetLocalization($"AbilityTree.Abilities.{ability}.Name").Value,
				new Vector2(card.Center.X, card.Y + 82), card.Width - 12,
				sect.Rank >= rank ? Color.LightCyan : Color.Gray, 0.64f);
			string state = unlocked
				? Mod.GetLocalization("Sect.UI.Learned").Value
				: Mod.GetLocalization("Sect.UI.RequiredRank").Format(
					Mod.GetLocalization($"Sect.Ranks.Rank{rank}").Value);
			DrawCenteredTextFitted(state, new Vector2(card.Center.X, card.Y + 112),
				card.Width - 12, unlocked ? Color.LightGreen : Color.LightGray, 0.53f);
			DrawCenteredTextFitted(Mod.GetLocalization($"Sect.UI.Techniques.{ability}").Value,
				new Vector2(card.Center.X, card.Y + 135), card.Width - 12,
				Color.White, 0.48f);
		}

		DrawCenteredText(Mod.GetLocalization("Sect.UI.ElderHint").Value,
			new Vector2(content.Center.X, content.Bottom - 34), Color.Gray, 0.58f);
	}

	private static void DrawAbilityMenuTab(
		Texture2D pixel,
		Rectangle rectangle,
		string label,
		bool selected,
		bool hovered)
	{
		Color border = selected
			? new Color(78, 226, 215)
			: (hovered ? new Color(151, 125, 190) : new Color(76, 65, 99));
		Color background = selected
			? new Color(25, 55, 66, 245)
			: new Color(20, 25, 39, 245);
		Main.spriteBatch.Draw(pixel, rectangle, border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4),
			background);
		if (selected)
			Main.spriteBatch.Draw(pixel,
				new Rectangle(rectangle.X + 2, rectangle.Bottom - 4,
					rectangle.Width - 4, 3),
				new Color(78, 226, 215));
		DrawCenteredText(label, rectangle.Center.ToVector2(), selected ? Color.White : Color.LightGray, 0.7f);
	}

	private void DrawPathPage(Texture2D pixel, Rectangle panel, Point mouse)
	{
		AlchemyPlayer alchemy = Main.LocalPlayer.GetModPlayer<AlchemyPlayer>();
		FormationPathPlayer formations = Main.LocalPlayer.GetModPlayer<FormationPathPlayer>();
		ArtifactForgingPlayer forging =
			Main.LocalPlayer.GetModPlayer<ArtifactForgingPlayer>();
		Rectangle content = new(panel.X + 18, panel.Y + 90, panel.Width - 36, panel.Height - 112);
		const int pathListWidth = 260;
		Rectangle listPanel = new(content.X, content.Y, pathListWidth, content.Height);
		Rectangle detailPanel = new(content.X + pathListWidth + 12, content.Y,
			content.Width - pathListWidth - 12, content.Height);
		Main.spriteBatch.Draw(pixel, listPanel, new Color(15, 24, 38, 245));
		Main.spriteBatch.Draw(pixel, detailPanel, new Color(15, 24, 38, 245));

		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Title").Value,
			new Vector2(listPanel.Center.X, listPanel.Y + 25), Color.White, 0.82f);
		Rectangle alchemyCard = new(listPanel.X + 12, listPanel.Y + 50, listPanel.Width - 24, 70);
		bool hovered = alchemyCard.Contains(mouse);
		Main.spriteBatch.Draw(pixel, alchemyCard,
			pathMenuPage == PathMenuPage.Alchemy
				? new Color(76, 235, 205)
				: hovered ? Color.White : new Color(76, 211, 173));
		Main.spriteBatch.Draw(pixel,
			new Rectangle(alchemyCard.X + 3, alchemyCard.Y + 3, alchemyCard.Width - 6, alchemyCard.Height - 6),
			new Color(24, 75, 68, 245));
		DrawPathItemIcon(ModContent.ItemType<AlchemyCauldron>(),
			new Vector2(alchemyCard.X + 38, alchemyCard.Center.Y), 44f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Alchemy.Name").Value,
			new Vector2(alchemyCard.X + 145, alchemyCard.Y + 27), Color.White, 0.75f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Alchemy.Tier").Format(
			alchemy.Tier, alchemy.TierRealmName, alchemy.StageName),
			new Vector2(alchemyCard.X + 145, alchemyCard.Y + 52), Color.LightGreen, 0.57f);

		Rectangle formationCard = new(listPanel.X + 12, listPanel.Y + 132,
			listPanel.Width - 24, 70);
		bool formationHovered = formationCard.Contains(mouse);
		Main.spriteBatch.Draw(pixel, formationCard,
			pathMenuPage == PathMenuPage.Formations
				? new Color(70, 205, 255)
				: formationHovered ? Color.White : new Color(66, 150, 190));
		Main.spriteBatch.Draw(pixel, new Rectangle(formationCard.X + 3,
			formationCard.Y + 3, formationCard.Width - 6, formationCard.Height - 6),
			new Color(20, 55, 75, 245));
		DrawPathItemIcon(ModContent.ItemType<PermanentFormationCore>(),
			new Vector2(formationCard.X + 38, formationCard.Center.Y), 44f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Formations.Name").Value,
			new Vector2(formationCard.X + 145, formationCard.Y + 25),
			Color.White, 0.7f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Formations.Tier").Format(
			formations.Tier, formations.TierRealmName, formations.StageName),
			new Vector2(formationCard.X + 145, formationCard.Y + 49),
			Color.LightCyan, 0.54f);

		Rectangle forgingCard = new(listPanel.X + 12, listPanel.Y + 214,
			listPanel.Width - 24, 70);
		bool forgingHovered = forgingCard.Contains(mouse);
		Main.spriteBatch.Draw(pixel, forgingCard,
			pathMenuPage == PathMenuPage.Forging
				? new Color(255, 185, 70)
				: forgingHovered ? Color.White : new Color(188, 123, 55));
		Main.spriteBatch.Draw(pixel, new Rectangle(forgingCard.X + 3,
			forgingCard.Y + 3, forgingCard.Width - 6, forgingCard.Height - 6),
			new Color(73, 48, 29, 245));
		DrawPathItemIcon(ModContent.ItemType<ArtifactForge>(),
			new Vector2(forgingCard.X + 38, forgingCard.Center.Y), 44f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Forging.Name").Value,
			new Vector2(forgingCard.X + 145, forgingCard.Y + 25),
			Color.White, 0.7f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Forging.Tier").Format(
			forging.Tier, forging.TierRealmName, forging.StageName),
			new Vector2(forgingCard.X + 145, forgingCard.Y + 49),
			new Color(255, 205, 105), 0.54f);

		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (alchemyCard.Contains(mouse))
			{
				pathMenuPage = PathMenuPage.Alchemy;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (formationCard.Contains(mouse))
			{
				pathMenuPage = PathMenuPage.Formations;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (forgingCard.Contains(mouse))
			{
				pathMenuPage = PathMenuPage.Forging;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
		}
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.FutureHint").Value,
			new Vector2(listPanel.Center.X, listPanel.Bottom - 35), Color.Gray, 0.6f);

		if (pathMenuPage == PathMenuPage.Formations)
		{
			DrawFormationPathPage(pixel, detailPanel, formations);
			return;
		}
		if (pathMenuPage == PathMenuPage.Forging)
		{
			DrawForgingPathPage(pixel, detailPanel, forging, mouse);
			return;
		}

		const int headerHeight = 145;
		Rectangle statsPanel = new(detailPanel.X + 10, detailPanel.Y + 8,
			detailPanel.Width - 205, headerHeight - 16);
		Rectangle cauldronPanel = new(statsPanel.Right + 8, detailPanel.Y + 8,
			detailPanel.Right - statsPanel.Right - 18, headerHeight - 16);
		Main.spriteBatch.Draw(pixel, statsPanel, new Color(18, 31, 48, 245));
		Main.spriteBatch.Draw(pixel, cauldronPanel, new Color(18, 31, 48, 245));

		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Alchemy.Name").Value,
			new Vector2(statsPanel.Center.X, statsPanel.Y + 22), new Color(115, 245, 205), 0.88f);
		string experienceProgress = alchemy.IsMaximumRank
			? Mod.GetLocalization("AbilityTree.MaxLevel").Value
			: $"EXP {alchemy.Experience}/{alchemy.ExperienceRequired}";
		string progress = Mod.GetLocalization("AbilityTree.Paths.Alchemy.Progress").Format(
			alchemy.StageName, experienceProgress);
		DrawCenteredText(progress, new Vector2(statsPanel.Center.X, statsPanel.Y + 49),
			Color.White, 0.61f);
		Rectangle progressBar = new(statsPanel.X + 24, statsPanel.Y + 64,
			statsPanel.Width - 48, 12);
		Main.spriteBatch.Draw(pixel, progressBar, new Color(50, 47, 70));
		float progressRatio = alchemy.IsMaximumRank
			? 1f : alchemy.Experience / (float)alchemy.ExperienceRequired;
		Main.spriteBatch.Draw(pixel, new Rectangle(progressBar.X + 2, progressBar.Y + 2,
			(int)((progressBar.Width - 4) * progressRatio), progressBar.Height - 4),
			new Color(65, 220, 170));

		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Alchemy.Bonuses").Format(
			alchemy.BonusYieldPercent, alchemy.ImpurityChancePercent),
			new Vector2(statsPanel.Center.X, statsPanel.Y + 92), Color.LightGreen, 0.53f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Alchemy.Saturation").Format(
			(int)alchemy.Saturation, (int)(alchemy.PillEffectiveness * 100f)),
			new Vector2(statsPanel.Center.X, statsPanel.Y + 113), Color.LightPink, 0.53f);

		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Alchemy.Cauldrons").Value,
			new Vector2(cauldronPanel.Center.X, cauldronPanel.Y + 21), Color.White, 0.57f);
		int[] cauldrons =
		[
			ModContent.ItemType<AlchemyCauldron>(),
			ModContent.ItemType<SpiritJadeCauldron>(),
			ModContent.ItemType<ProfoundAlchemyCauldron>()
		];
		for (int i = 0; i < cauldrons.Length; i++)
			DrawPathItemIcon(cauldrons[i],
				new Vector2(cauldronPanel.Center.X - 48f + i * 48f, cauldronPanel.Y + 73f), 37f);

		(int itemType, int stage)[][] pillGroups =
		[
			[
				(ModContent.ItemType<QiRecoveryPill>(), 0),
				(ModContent.ItemType<SpiritGatheringPill>(), 1),
				(ModContent.ItemType<BodyTemperingPill>(), 2)
			],
			[
				(ModContent.ItemType<BeastBloodTemperingPill>(), 0),
				(ModContent.ItemType<SpiritBeastLurePill>(), 0),
				(ModContent.ItemType<MeridianCleansingPill>(), 1),
				(ModContent.ItemType<MeridianMendingPill>(), 1),
				(ModContent.ItemType<FoundationStabilizationPill>(), 2),
				(ModContent.ItemType<ConcealmentPill>(), 2)
			],
			[
				(ModContent.ItemType<FlameMeridianPill>(), 0),
				(ModContent.ItemType<GreaterQiRecoveryPill>(), 1),
				(ModContent.ItemType<GoldenCoreTemperingPill>(), 2),
				(ModContent.ItemType<TribulationWardPill>(), 2)
			],
			[
				(ModContent.ItemType<ThunderResistancePill>(), 0),
				(ModContent.ItemType<CoreRefinementPill>(), 1),
				(ModContent.ItemType<NascentSoulAwakeningPill>(), 2)
			],
			[
				(ModContent.ItemType<SoulNourishingPill>(), 0),
				(ModContent.ItemType<VoidInsightPill>(), 1),
				(ModContent.ItemType<HeavenlyRebirthPill>(), 2)
			]
		];

		Rectangle tiersArea = new(detailPanel.X + 8, detailPanel.Y + headerHeight,
			detailPanel.Width - 16, detailPanel.Height - headerHeight - 8);
		int rowHeight = tiersArea.Height / (AlchemyPlayer.MaxTier + 1);
		for (int tier = 0; tier <= AlchemyPlayer.MaxTier; tier++)
		{
			Rectangle row = new(tiersArea.X, tiersArea.Y + tier * rowHeight,
				tiersArea.Width, rowHeight - 3);
			DrawAlchemyTierRow(pixel, row, alchemy, tier, pillGroups[tier], mouse);
		}
	}

	private void DrawForgingPathPage(Texture2D pixel, Rectangle panel,
		ArtifactForgingPlayer forging, Point mouse)
	{
		Rectangle header = new(panel.X + 10, panel.Y + 8,
			panel.Width - 20, 128);
		Main.spriteBatch.Draw(pixel, header, new Color(53, 36, 29, 245));
		DrawPathItemIcon(ModContent.ItemType<ArtifactForge>(),
			new Vector2(header.X + 62, header.Center.Y), 68f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Forging.Name").Value,
			new Vector2(header.Center.X + 30, header.Y + 23),
			new Color(255, 190, 75), 0.9f);
		string exp = forging.IsMaximumRank
			? Mod.GetLocalization("AbilityTree.MaxLevel").Value
			: $"EXP {forging.Experience}/{forging.ExperienceRequired}";
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Forging.Progress")
			.Format(forging.StageName, exp),
			new Vector2(header.Center.X + 30, header.Y + 52), Color.White, 0.62f);
		Rectangle bar = new(header.X + 125, header.Y + 68,
			header.Width - 155, 12);
		Main.spriteBatch.Draw(pixel, bar, new Color(50, 42, 45));
		float ratio = forging.IsMaximumRank ? 1f
			: forging.Experience / (float)forging.ExperienceRequired;
		Main.spriteBatch.Draw(pixel, new Rectangle(bar.X + 2, bar.Y + 2,
			(int)((bar.Width - 4) * ratio), bar.Height - 4),
			new Color(255, 165, 55));
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Forging.Hint").Value,
			new Vector2(header.Center.X + 30, header.Bottom - 22),
			new Color(255, 220, 150), 0.52f);

		int[][] artifacts =
		[
			[ModContent.ItemType<VerdantAntlerStaff>()],
			[ModContent.ItemType<JadeAntlerTalisman>()],
			[ModContent.ItemType<FlameSpiritFan>()],
			[ModContent.ItemType<ThunderclapSeal>()],
			[ModContent.ItemType<BeastSoulBanner>()]
		];
		Rectangle rows = new(panel.X + 8, header.Bottom + 8,
			panel.Width - 16, panel.Bottom - header.Bottom - 16);
		int rowHeight = rows.Height / 5;
		for (int tier = 0; tier <= ArtifactForgingPlayer.MaxTier; tier++)
		{
			Rectangle row = new(rows.X, rows.Y + tier * rowHeight,
				rows.Width, rowHeight - 3);
			bool reached = forging.Tier >= tier;
			bool current = forging.Tier == tier;
			Main.spriteBatch.Draw(pixel, row, tier % 2 == 0
				? new Color(25, 31, 44, 245)
				: new Color(18, 26, 39, 245));
			Rectangle tierBox = new(row.X + 3, row.Y + 3, 150, row.Height - 6);
			Main.spriteBatch.Draw(pixel, tierBox, current
				? new Color(112, 69, 31, 235)
				: reached ? new Color(67, 53, 35, 235)
				: new Color(31, 34, 43, 235));
			DrawCenteredText($"Tier {tier}",
				new Vector2(tierBox.Center.X, tierBox.Y + 18),
				reached ? Color.White : Color.Gray, 0.63f);
			DrawCenteredTextFitted(
				Main.LocalPlayer.GetModPlayer<AlchemyPlayer>()
					.GetTierRealmName(tier),
				new Vector2(tierBox.Center.X, tierBox.Bottom - 17),
				tierBox.Width - 10, reached ? Color.LightGoldenrodYellow : Color.Gray,
				0.5f);

			int itemType = artifacts[tier][0];
			Rectangle artifactCard = new(tierBox.Right + 10, row.Y + 7,
				Math.Min(280, row.Width - tierBox.Width - 25), row.Height - 14);
			bool hovered = artifactCard.Contains(mouse);
			Main.spriteBatch.Draw(pixel, artifactCard, reached
				? hovered ? Color.White : new Color(238, 162, 65)
				: new Color(65, 67, 75));
			Main.spriteBatch.Draw(pixel, new Rectangle(artifactCard.X + 3,
				artifactCard.Y + 3, artifactCard.Width - 6, artifactCard.Height - 6),
				reached ? new Color(69, 48, 31) : new Color(25, 27, 34));
			DrawPathItemIcon(itemType,
				new Vector2(artifactCard.X + 35, artifactCard.Center.Y), 42f,
				reached ? Color.White : new Color(90, 90, 100));
			DrawCenteredTextFitted(ContentSamples.ItemsByType[itemType].Name,
				new Vector2(artifactCard.X + 65
					+ (artifactCard.Width - 70) * 0.5f, artifactCard.Center.Y),
				artifactCard.Width - 78, reached ? new Color(255, 220, 145) : Color.Gray,
				0.58f);

			DrawCenteredTextFitted(
				Mod.GetLocalization($"AbilityTree.Paths.Forging.Tier{tier}").Value,
				new Vector2(artifactCard.Right
					+ (row.Right - artifactCard.Right) * 0.5f, row.Center.Y),
				row.Right - artifactCard.Right - 15,
				reached ? Color.White : Color.Gray, 0.5f);
		}
	}

	private void DrawFormationPathPage(Texture2D pixel, Rectangle panel,
		FormationPathPlayer formations)
	{
		Rectangle header = new(panel.X + 10, panel.Y + 8,
			panel.Width - 20, 188);
		Main.spriteBatch.Draw(pixel, header, new Color(17, 35, 52, 245));
		DrawPathItemIcon(ModContent.ItemType<PermanentFormationCore>(),
			new Vector2(header.X + 58, header.Y + 87), 70f);
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Formations.Name").Value,
			new Vector2(header.Center.X + 35, header.Y + 24),
			new Color(80, 225, 255), 0.9f);
		int requiredExperience = formations.ExperienceRequired;
		int displayedExperience = formations.IsMaximumRank ? 0
			: Math.Min(formations.Experience, requiredExperience);
		int storedExperience = formations.IsMaximumRank ? 0
			: Math.Max(0, formations.Experience - requiredExperience);
		string experience = formations.IsMaximumRank
			? Mod.GetLocalization("AbilityTree.MaxLevel").Value
			: storedExperience > 0
				? Mod.GetLocalization(
					"AbilityTree.Paths.Formations.ExperienceStored")
					.Format(displayedExperience, requiredExperience,
						storedExperience)
				: $"EXP {displayedExperience}/{requiredExperience}";
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Formations.Progress").Format(
			formations.StageName, experience),
			new Vector2(header.Center.X + 35, header.Y + 53),
			Color.White, 0.61f);
		Rectangle progressBar = new(header.X + 135, header.Y + 68,
			header.Width - 165, 11);
		Main.spriteBatch.Draw(pixel, progressBar, new Color(39, 47, 67));
		float ratio = formations.IsMaximumRank ? 1f
			: MathHelper.Clamp(formations.Experience
				/ (float)formations.ExperienceRequired, 0f, 1f);
		Main.spriteBatch.Draw(pixel, new Rectangle(progressBar.X + 2,
			progressBar.Y + 2, (int)((progressBar.Width - 4) * ratio),
			progressBar.Height - 4), new Color(50, 195, 235));
		int radius = 40 + formations.Tier * 15 + formations.Stage * 5;
		int capacity = 10000 + formations.Tier * 10000 + formations.Stage * 2500;
		int integrity = 5000 + formations.Tier * 4000 + formations.Stage * 1000;
		int contentX = header.X + 126;
		int contentWidth = header.Right - contentX - 14;
		int statGap = 5;
		int statWidth = (contentWidth - statGap * 2) / 3;
		string[] statTexts =
		[
			Mod.GetLocalization("AbilityTree.Paths.Formations.StatRadius")
				.Format(radius),
			Mod.GetLocalization("AbilityTree.Paths.Formations.StatQi")
				.Format(capacity),
			Mod.GetLocalization("AbilityTree.Paths.Formations.StatIntegrity")
				.Format(integrity)
		];
		for (int i = 0; i < statTexts.Length; i++)
		{
			Rectangle statBox = new(contentX + i * (statWidth + statGap),
				header.Y + 88, statWidth, 27);
			Main.spriteBatch.Draw(pixel, statBox, new Color(22, 58, 76, 245));
			DrawCenteredTextFitted(statTexts[i], statBox.Center.ToVector2(),
				statBox.Width - 8, Color.LightCyan, 0.61f);
		}

		string trial = formations.IsMaximumRank
			? Mod.GetLocalization("AbilityTree.Paths.Formations.Trials.Mastered").Value
			: Mod.GetLocalization(formations.CurrentTrialLocalizationKey)
				.Format(formations.CurrentTrialProgress,
					formations.CurrentTrialTarget);
		Rectangle trialBox = new(contentX, header.Y + 120, contentWidth, 29);
		Main.spriteBatch.Draw(pixel, trialBox, new Color(27, 43, 63, 245));
		DrawCenteredTextFitted(trial, trialBox.Center.ToVector2(),
			trialBox.Width - 12,
			formations.CurrentTrialComplete ? Color.LightGreen : Color.Orange,
			0.61f);

		string trainingHint = formations.Stage == FormationPathPlayer.MaxStage
			&& formations.Tier < FormationPathPlayer.MaxTier
			? Mod.GetLocalization("AbilityTree.Paths.Formations.RealmGate").Format(
				Main.LocalPlayer.GetModPlayer<AlchemyPlayer>()
					.GetTierRealmName(formations.Tier + 1),
				formations.RealmRequirementMet
					? Mod.GetLocalization("AbilityTree.Paths.Formations.Ready").Value
					: Mod.GetLocalization("AbilityTree.Paths.Formations.Locked").Value)
			: Mod.GetLocalization("AbilityTree.Paths.Formations.ExpHint").Value;
		Rectangle hintBox = new(contentX, header.Y + 154, contentWidth, 26);
		Main.spriteBatch.Draw(pixel, hintBox, new Color(15, 29, 45, 235));
		DrawCenteredTextFitted(trainingHint, hintBox.Center.ToVector2(),
			hintBox.Width - 12,
			formations.RealmRequirementMet ? Color.LightGreen : Color.IndianRed,
			0.55f);

		Rectangle rows = new(panel.X + 8, header.Bottom + 8,
			panel.Width - 16, panel.Bottom - header.Bottom - 16);
		int rowHeight = rows.Height / 5;
		for (int tier = 0; tier <= FormationPathPlayer.MaxTier; tier++)
		{
			Rectangle row = new(rows.X, rows.Y + tier * rowHeight,
				rows.Width, rowHeight - 3);
			bool reached = formations.Tier >= tier;
			bool current = formations.Tier == tier;
			Main.spriteBatch.Draw(pixel, row, tier % 2 == 0
				? new Color(18, 31, 48, 245)
				: new Color(14, 26, 42, 245));
			Rectangle tierBox = new(row.X + 3, row.Y + 3, 150, row.Height - 6);
			Main.spriteBatch.Draw(pixel, tierBox, current
				? new Color(29, 99, 120)
				: reached ? new Color(25, 69, 79) : new Color(31, 34, 43));
			DrawCenteredText($"Tier {tier}",
				new Vector2(tierBox.Center.X, tierBox.Y + 18),
				reached ? Color.White : Color.Gray, 0.65f);
			DrawCenteredTextFitted(
				Main.LocalPlayer.GetModPlayer<AlchemyPlayer>().GetTierRealmName(tier),
				new Vector2(tierBox.Center.X, tierBox.Bottom - 18),
				tierBox.Width - 10, reached ? Color.LightCyan : Color.Gray, 0.5f);
			DrawPathItemIcon(ModContent.ItemType<PermanentFormationCore>(),
				new Vector2(row.X + 190, row.Center.Y), 38f,
				reached ? Color.White : new Color(80, 80, 90));
			DrawCenteredTextFitted(
				Mod.GetLocalization($"AbilityTree.Paths.Formations.Tier{tier}").Value,
				new Vector2(row.X + 215 + (row.Width - 225) * 0.5f, row.Center.Y),
				row.Width - 235, reached ? Color.White : Color.Gray, 0.53f);
		}
	}

	private void DrawAlchemyTierRow(
		Texture2D pixel,
		Rectangle row,
		AlchemyPlayer alchemy,
		int tier,
		(int itemType, int stage)[] pills,
		Point mouse)
	{
		bool tierReached = alchemy.Tier >= tier;
		bool currentTier = alchemy.Tier == tier;
		bool completed = alchemy.Tier > tier;
		Main.spriteBatch.Draw(pixel, row, tier % 2 == 0
			? new Color(18, 29, 45, 245)
			: new Color(14, 24, 39, 245));

		const int tierPanelWidth = 150;
		Rectangle tierPanel = new(row.X + 3, row.Y + 3, tierPanelWidth - 6, row.Height - 6);
		Color tierBackground = currentTier
			? new Color(38, 104, 103, 235)
			: completed ? new Color(30, 76, 65, 235) : new Color(31, 34, 43, 235);
		Main.spriteBatch.Draw(pixel, tierPanel, tierBackground);

		string realm = alchemy.GetTierRealmName(tier);
		DrawCenteredText($"Tier {tier}", new Vector2(tierPanel.Center.X, tierPanel.Y + 18),
			tierReached ? Color.White : Color.Gray, 0.64f);
		DrawCenteredTextFitted(realm, new Vector2(tierPanel.Center.X, tierPanel.Y + 38),
			tierPanel.Width - 10, tierReached ? Color.LightCyan : Color.Gray, 0.52f);
		string tierStatus = completed
			? Mod.GetLocalization("AbilityTree.Paths.Alchemy.TierComplete").Value
			: currentTier
				? Mod.GetLocalization("AbilityTree.Paths.Alchemy.TierCurrent").Format(alchemy.StageName)
				: Mod.GetLocalization("AbilityTree.Locked").Value;
		DrawCenteredTextFitted(tierStatus, new Vector2(tierPanel.Center.X, tierPanel.Bottom - 14),
			tierPanel.Width - 10, currentTier ? Color.LightGreen : Color.Gray, 0.46f);

		int cardsAreaX = row.X + tierPanelWidth + 6;
		int cardsAreaWidth = row.Right - cardsAreaX - 6;
		if (pills.Length == 0)
		{
			DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Alchemy.NoPills").Value,
				new Vector2(cardsAreaX + cardsAreaWidth * 0.5f, row.Center.Y), Color.Gray, 0.56f);
			return;
		}

		const int cardGap = 7;
		int cardWidth = Math.Min(174, (cardsAreaWidth - cardGap * (pills.Length - 1)) / pills.Length);
		for (int i = 0; i < pills.Length; i++)
		{
			(int itemType, int requiredStage) = pills[i];
			Rectangle card = new(cardsAreaX + i * (cardWidth + cardGap), row.Y + 6,
				cardWidth, row.Height - 12);
			bool unlocked = alchemy.MeetsRequirement(tier, requiredStage);
			bool hovered = card.Contains(mouse);
			Color border = unlocked
				? (hovered ? Color.White : new Color(68, 211, 210))
				: (hovered ? new Color(120, 120, 132) : new Color(67, 69, 79));
			Main.spriteBatch.Draw(pixel, card, border);
			Main.spriteBatch.Draw(pixel,
				new Rectangle(card.X + 3, card.Y + 3, card.Width - 6, card.Height - 6),
				unlocked ? new Color(21, 67, 73) : new Color(24, 27, 34));

			bool compact = cardWidth < 125;
			float textCenterX;
			if (compact)
			{
				DrawPathItemIcon(itemType, new Vector2(card.Center.X, card.Y + 27f), 31f,
					unlocked ? Color.White : new Color(95, 95, 105));
				textCenterX = card.Center.X;
			}
			else
			{
				DrawPathItemIcon(itemType, new Vector2(card.X + 25f, card.Center.Y), 34f,
					unlocked ? Color.White : new Color(95, 95, 105));
				textCenterX = card.X + 49f + (card.Width - 52f) * 0.5f;
				string name = ContentSamples.ItemsByType[itemType].Name;
				DrawCenteredTextFitted(name, new Vector2(textCenterX, card.Y + 19),
					Math.Max(40f, card.Width - 56f), unlocked ? Color.LightCyan : Color.Gray, 0.49f);
			}
			string stageName = alchemy.GetStageName(requiredStage);
			string status = unlocked
				? Mod.GetLocalization("AbilityTree.Paths.Alchemy.Unlocked").Value
				: Mod.GetLocalization("AbilityTree.Locked").Value;
			DrawCenteredTextFitted(
				Mod.GetLocalization("AbilityTree.Paths.Alchemy.PillRequirement").Format(stageName, status),
				new Vector2(textCenterX, card.Bottom - 15), compact ? card.Width - 8 : Math.Max(40f, card.Width - 56f),
				unlocked ? Color.White : Color.Gray, 0.43f);
		}
	}

	private static void DrawPathItemIcon(
		int itemType,
		Vector2 center,
		float maximumSize,
		Color? drawColor = null)
	{
		Main.instance.LoadItem(itemType);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		float scale = Math.Min(maximumSize / texture.Width, maximumSize / texture.Height);
		Main.spriteBatch.Draw(texture, center, null, drawColor ?? Color.White, 0f,
			texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
		Rectangle hoverArea = new((int)(center.X - maximumSize * 0.5f),
			(int)(center.Y - maximumSize * 0.5f), (int)maximumSize, (int)maximumSize);
		if (hoverArea.Contains(Main.MouseScreen.ToPoint()))
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.HoverItem = new Item(itemType);
			Main.hoverItemName = Main.HoverItem.Name;
		}
	}

	private void DrawTechniqueLoadoutEditor(
		Texture2D pixel, Rectangle panel, Point mouse,
		CultivationPlayer cultivation)
	{
		selectedTechniqueLoadoutSlot = Math.Clamp(
			selectedTechniqueLoadoutSlot, 0,
			cultivation.TechniqueLoadoutSlotCount - 1);
		Rectangle editor = new(panel.X + 14, panel.Y + 84,
			panel.Width - 28, 74);
		Main.spriteBatch.Draw(pixel, editor,
			new Color(15, 29, 45, 245));
		DrawCenteredText(
			Mod.GetLocalization("TechniqueLoadout.Title").Value,
			new Vector2(editor.X + 108, editor.Y + 14),
			Color.LightCyan, 0.59f);

		const int presetWidth = 62;
		const int presetGap = 6;
		for (int preset = 0;
			preset < CultivationPlayer.TechniqueLoadoutPresetCount;
			preset++)
		{
			Rectangle button = new(
				editor.X + 10 + preset * (presetWidth + presetGap),
				editor.Y + 31, presetWidth, 31);
			bool available =
				preset < cultivation.AvailableTechniqueLoadoutPresets;
			bool selected =
				preset == cultivation.ActiveTechniqueLoadoutPreset;
			DrawButton(pixel, button,
				available ? $"{preset + 1}"
					: Mod.GetLocalization(
						"TechniqueLoadout.LockedPreset").Value,
				available && button.Contains(mouse),
				selected ? new Color(35, 145, 135)
					: available ? new Color(54, 48, 72)
					: new Color(38, 39, 47));
			if (available && button.Contains(mouse)
				&& Main.mouseLeft && Main.mouseLeftRelease)
			{
				Main.mouseLeftRelease = false;
				cultivation.TrySelectTechniqueLoadoutPreset(preset);
				selectedTechniqueLoadoutSlot = Math.Min(
					selectedTechniqueLoadoutSlot,
					cultivation.TechniqueLoadoutSlotCount - 1);
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
		}

		int slotsX = editor.X + 226;
		int slotsWidth = editor.Right - slotsX - 9;
		const int slotGap = 6;
		int slotWidth = (slotsWidth - slotGap
			* (CultivationPlayer.MaximumTechniqueLoadoutSlots - 1))
			/ CultivationPlayer.MaximumTechniqueLoadoutSlots;
		for (int slot = 0;
			slot < CultivationPlayer.MaximumTechniqueLoadoutSlots; slot++)
		{
			Rectangle slotBox = new(
				slotsX + slot * (slotWidth + slotGap),
				editor.Y + 10, slotWidth, 53);
			bool available =
				slot < cultivation.TechniqueLoadoutSlotCount;
			bool selected =
				available && slot == selectedTechniqueLoadoutSlot;
			Color border = !available ? new Color(65, 66, 74)
				: selected ? Color.Gold : new Color(64, 195, 200);
			Main.spriteBatch.Draw(pixel, slotBox, border);
			Rectangle inner = new(slotBox.X + 3, slotBox.Y + 3,
				slotBox.Width - 6, slotBox.Height - 6);
			Main.spriteBatch.Draw(pixel, inner,
				available ? new Color(17, 46, 55)
					: new Color(27, 29, 35));

			CultivationAbility ability =
				cultivation.GetTechniqueLoadoutAbility(
					cultivation.ActiveTechniqueLoadoutPreset, slot);
			if (available && ability != CultivationAbility.Count)
			{
				DrawTreeAbilityIcon(
					new Vector2(slotBox.X + 22, slotBox.Center.Y),
					ability, true, 26f);
				DrawCenteredTextFitted(
					Mod.GetLocalization(
						$"AbilityTree.Abilities.{ability}.Name").Value,
					new Vector2(
						slotBox.X + 43 + (slotBox.Width - 46) * 0.5f,
						slotBox.Center.Y),
					Math.Max(28, slotBox.Width - 50),
					Color.White, 0.47f);
			}
			else
			{
				DrawCenteredText(
					available ? $"{slot + 1}"
						: Mod.GetLocalization(
							"TechniqueLoadout.LockedSlot").Value,
					slotBox.Center.ToVector2(),
					available ? Color.Gray : new Color(95, 95, 105),
					0.48f);
			}

			if (available && slotBox.Contains(mouse)
				&& Main.mouseLeft && Main.mouseLeftRelease)
			{
				Main.mouseLeftRelease = false;
				selectedTechniqueLoadoutSlot = slot;
				cultivation.TrySelectActiveTechniqueSlot(slot);
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			if (available && slotBox.Contains(mouse)
				&& Main.mouseRight && Main.mouseRightRelease)
			{
				Main.mouseRightRelease = false;
				selectedTechniqueLoadoutSlot = slot;
				cultivation.TrySetTechniqueLoadoutSlot(
					cultivation.ActiveTechniqueLoadoutPreset,
					slot, CultivationAbility.Count);
				SoundEngine.PlaySound(SoundID.MenuClose);
			}
		}
	}

	private string GetAbilityTreeDetails(CultivationPlayer cultivation, CultivationAbility ability)
	{
		int level = cultivation.GetAbilityLevel(ability);
		int experience = cultivation.GetAbilityExperience(ability);
		int required = cultivation.GetAbilityExperienceRequired(ability);
		string progress = level >= CultivationAbilityInfo.MaxLevel
			? Mod.GetLocalization("AbilityTree.MaxLevel").Value
			: $"EXP {experience}/{required}";
		if (ability == CultivationAbility.QiBurning)
		{
			string state = cultivation.QiBurningEnabled
				? Mod.GetLocalization("Abilities.StateEnabled").Value
				: cultivation.HasQiDeviation
					? Mod.GetLocalization("Abilities.QiBurningDeviationState")
						.Format(cultivation.QiDeviationSecondsRemaining)
					: Mod.GetLocalization("Abilities.StateDisabled").Value;
			return Mod.GetLocalization("AbilityTree.QiBurningEffect").Format(
				level,
				MathF.Round(cultivation.QiBurningDamageBonusPercent, 1),
				MathF.Round(cultivation.QiBurningAttackSpeedBonusPercent, 1),
				MathF.Round(cultivation.BurnedQiCapacityPercent, 2),
				state, progress);
		}
		string effectKey = ability switch
		{
			CultivationAbility.Meditation => "MeditationEffect",
			CultivationAbility.QiResistance => "ResistanceEffect",
			CultivationAbility.QiProtection => "ProtectionEffect",
			CultivationAbility.QiFlight => "FlightEffect",
			CultivationAbility.NascentTeleport => "TeleportEffect",
			CultivationAbility.QiSense => "SenseEffect",
			CultivationAbility.SpiritBreathing => "SpiritBreathingEffect",
			CultivationAbility.GoldenCoreCirculation => "GoldenCoreEffect",
			CultivationAbility.NascentSoulRegeneration => "NascentSoulEffect",
			CultivationAbility.NightVision => "NightVisionEffect",
			CultivationAbility.SwordIntent => "SwordIntentEffect",
			CultivationAbility.SpiritSwordRain => "SwordRainEffect",
			CultivationAbility.SectProtectionFormation => "FormationEffect",
			CultivationAbility.SpiritualRain => "SpiritualRainEffect",
			CultivationAbility.QiBurning => "QiBurningEffect",
			_ => "CombatEffect"
		};
		return Mod.GetLocalization($"AbilityTree.{effectKey}")
			.Format(level, progress);
	}

	private string GetRootSynergyText(ElementalCultivationPlayer elemental,
		SpiritualRootPlayer root, SpiritualElement elements)
	{
		if (elements == SpiritualElement.None)
			return Mod.GetLocalization("AbilityTree.RootSynergy.Neutral").Value;
		if (!root.IsRevealed)
			return Mod.GetLocalization("AbilityTree.RootSynergy.Hidden").Value;

		float affinity = elemental.GetAffinity(elements);
		if (affinity <= 0f)
			return Mod.GetLocalization("AbilityTree.RootSynergy.NoAffinity").Value;

		float power = (elemental.GetPowerMultiplier(elements)
			* (1f + affinity * 0.0015f) - 1f) * 100f;
		float qiReduction = Math.Clamp(
			elemental.GetQiCostReductionPercent(elements) + affinity * 0.08f,
			0f, ElementalCultivationPlayer.MaximumQiCostReductionPercent);
		float mastery = (elemental.GetMasteryGainMultiplier(elements)
			* (1f + affinity * 0.001f) - 1f) * 100f;
		return Mod.GetLocalization("AbilityTree.RootSynergy.Match").Format(
			(int)MathF.Round(affinity),
			MathF.Round(power, 1),
			MathF.Round(qiReduction, 1),
			MathF.Round(mastery, 1));
	}

	private static string GetRealmLocalizationKey(int realm) => realm switch
	{
		0 => "Mortal", 1 => "QiCondensation", 2 => "FoundationEstablishment",
		3 => "CoreFormation", _ => "NascentSoul"
	};

	private static void DrawTreeAbilityIcon(Vector2 center, CultivationAbility ability, bool unlocked, float size)
	{
		(int item, string texture) = ability switch
		{
			CultivationAbility.Meditation => (ItemID.HeartreachPotion, string.Empty),
			CultivationAbility.QiSense => (ItemID.SpelunkerPotion, string.Empty),
			CultivationAbility.QiResistance => (ItemID.IronskinPotion, string.Empty),
			CultivationAbility.Fireball => (0, "Xianxia/Content/Projectiles/QiFireballProjectile"),
			CultivationAbility.QiPalm => (ItemID.FeralClaws, string.Empty),
			CultivationAbility.QiProtection => (ItemID.CobaltShield, string.Empty),
			CultivationAbility.FlameStep => (ItemID.HermesBoots, string.Empty),
			CultivationAbility.QiFlight => (ItemID.AngelWings, string.Empty),
			CultivationAbility.NascentTeleport => (ItemID.RodofDiscord, string.Empty),
			CultivationAbility.SpiritBreathing => (ItemID.ManaRegenerationPotion, string.Empty),
			CultivationAbility.GoldenCoreCirculation => (ItemID.CelestialMagnet, string.Empty),
			CultivationAbility.NascentSoulRegeneration => (ItemID.LifeFruit, string.Empty),
			CultivationAbility.NightVision => (ItemID.NightOwlPotion, string.Empty),
			CultivationAbility.SwordIntent => (ModContent.ItemType<SwordIntentManual>(), string.Empty),
			CultivationAbility.SpiritSwordRain => (ModContent.ItemType<SpiritSwordRainManual>(), string.Empty),
			CultivationAbility.SectProtectionFormation =>
				(ModContent.ItemType<SectProtectionFormationManual>(), string.Empty),
			CultivationAbility.SpiritualRain =>
				(ModContent.ItemType<SpiritualRainTechnique>(), string.Empty),
			CultivationAbility.QiBurning => (ItemID.Hellstone, string.Empty),
			_ => (ItemID.SoulCake, string.Empty)
		};
		Texture2D icon;
		if (!string.IsNullOrEmpty(texture))
			icon = ModContent.Request<Texture2D>(texture).Value;
		else
		{
			Main.instance.LoadItem(item);
			icon = TextureAssets.Item[item].Value;
		}
		float scale = Math.Min(size / icon.Width, size / icon.Height);
		Main.spriteBatch.Draw(icon, center, null, unlocked ? Color.White : new Color(75, 75, 82),
			0f, icon.Size() * 0.5f, scale, SpriteEffects.None, 0f);
	}

	private bool DrawQiBar()
	{
		if (Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
		{
			return true;
		}

		Player player = Main.LocalPlayer;
		if (!player.active)
		{
			return true;
		}

		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		CultivationClientConfig config = CultivationClientConfig.Instance;
		float uiScale = CultivationClientConfig.QiBarScale;
		int barWidth = Math.Max(180, (int)MathF.Round(BarWidth * uiScale));
		int barHeight = Math.Max(14, (int)MathF.Round(BarHeight * uiScale));
		int borderSize = Math.Max(1, (int)MathF.Round(BorderSize * uiScale));
		float horizontalPosition = MathHelper.Clamp(
			(config?.QiBarHorizontalPositionPercent ?? 50) / 100f, 0f, 1f);
		int x = (int)MathF.Round((Main.screenWidth - barWidth) * horizontalPosition);
		int y = Math.Clamp(config?.QiBarVerticalPosition ?? 18, 0,
			Math.Max(0, Main.screenHeight - barHeight - 30));

		Rectangle border = new(x, y, barWidth, barHeight);
		Rectangle background = new(
			x + borderSize,
			y + borderSize,
			barWidth - borderSize * 2,
			barHeight - borderSize * 2
		);

		float qiProgress = cultivation.BaseMaxQi > 0
			? cultivation.Qi / (float)cultivation.BaseMaxQi : 0f;
		float availableProgress = cultivation.BaseMaxQi > 0
			? cultivation.MaxQi / (float)cultivation.BaseMaxQi : 0f;
		float cultivationProgress = GetCultivationProgress(cultivation);
		Rectangle fill = new(background.X, background.Y,
			(int)(background.Width * MathHelper.Clamp(qiProgress, 0f, 1f)), background.Height);
		Rectangle burnedFill = new(
			background.X + (int)(background.Width
				* MathHelper.Clamp(availableProgress, 0f, 1f)),
			background.Y,
			(int)(background.Width
				* MathHelper.Clamp(1f - availableProgress, 0f, 1f)),
			background.Height);
		Rectangle experienceBackground = new(background.X, background.Bottom - 4, background.Width, 4);
		Rectangle experienceFill = new(experienceBackground.X, experienceBackground.Y,
			(int)(experienceBackground.Width * cultivationProgress), experienceBackground.Height);
		Texture2D pixel = TextureAssets.MagicPixel.Value;

		Main.spriteBatch.Draw(pixel, new Rectangle(x - 2, y - 2, barWidth + 4, barHeight + 4), new Color(14, 8, 28, 180));
		Main.spriteBatch.Draw(pixel, border, new Color(103, 65, 142));
		Main.spriteBatch.Draw(pixel, background, new Color(19, 23, 36));
		if (burnedFill.Width > 0)
		{
			Main.spriteBatch.Draw(pixel, burnedFill,
				new Color(92, 27, 38));
			Main.spriteBatch.Draw(pixel,
				new Rectangle(burnedFill.X, burnedFill.Y,
					burnedFill.Width, 4),
				new Color(175, 48, 48));
		}
		if (fill.Width > 0)
		{
			Main.spriteBatch.Draw(pixel, fill, new Color(55, 214, 224));
			Main.spriteBatch.Draw(pixel, new Rectangle(fill.X, fill.Y, fill.Width, 4), new Color(166, 255, 244));
		}
		Main.spriteBatch.Draw(pixel, experienceBackground, new Color(35, 18, 58));
		if (experienceFill.Width > 0)
		{
			Main.spriteBatch.Draw(pixel, experienceFill, new Color(190, 105, 255));
		}

		string realmName = cultivation.GetRealmName();
		string qiText = cultivation.IsCultivationMaxed
			? Mod.GetLocalization("Cultivation.QiStatusMax").Format(cultivation.Qi, cultivation.MaxQi)
			: Mod.GetLocalization("Cultivation.QiStatus").Format(
				cultivation.Qi, cultivation.MaxQi, cultivation.QiExp, cultivation.NextStageThreshold);
		string title = Mod.GetLocalization("Cultivation.QiBar").Format(realmName, cultivation.Stage);

		float titleScale = 0.8f * uiScale;
		float textScale = 0.75f * uiScale;
		float zoneScale = 0.62f * uiScale;
		Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * titleScale;
		Utils.DrawBorderString(Main.spriteBatch, title,
			new Vector2(x + (barWidth - titleSize.X) * 0.5f, y - 18f * uiScale), Color.White, titleScale);

		Vector2 qiSize = FontAssets.MouseText.Value.MeasureString(qiText) * textScale;
		Utils.DrawBorderString(Main.spriteBatch, qiText,
			new Vector2(x + (barWidth - qiSize.X) * 0.5f, y + 2f * uiScale), Color.White, textScale);

		bool showConcentration = config?.ShowQiConcentration ?? true;
		if (showConcentration)
		{
			string zoneText = Mod.GetLocalization("Cultivation.SpiritualQiZoneStatus").Format(
				cultivation.SpiritualQiZoneTier,
				cultivation.SpiritualQiZoneBonusPercent,
				cultivation.NearbySpiritCrystalCount);
			Vector2 zoneSize = FontAssets.MouseText.Value.MeasureString(zoneText) * zoneScale;
			Utils.DrawBorderString(Main.spriteBatch, zoneText,
				new Vector2(x + (barWidth - zoneSize.X) * 0.5f, y + barHeight + 3f * uiScale),
				cultivation.IsInSpiritualQiZone
					? new Color(190, 130, 255)
					: new Color(135, 135, 150), zoneScale);
		}

		Rectangle hoverArea = new(x - 4, (int)(y - 20f * uiScale), barWidth + 8,
			(int)(barHeight + (showConcentration ? 42f : 25f) * uiScale));
		if (hoverArea.Contains(Main.MouseScreen.ToPoint()))
		{
			player.mouseInterface = true;
			DrawBreakthroughReadiness(cultivation, border);
		}

		return true;
	}

	private void DrawBreakthroughReadiness(CultivationPlayer cultivation, Rectangle qiBar)
	{
		List<(string Text, Color Color)> lines = [];
		if (cultivation.IsCultivationMaxed)
		{
			lines.Add((Mod.GetLocalization("Cultivation.BreakthroughTooltip.MaximumReached").Value,
				Color.Gold));
		}
		else
		{
			string targetRealm = cultivation.GetRealmName(cultivation.NextBreakthroughTargetRealm);
			lines.Add((Mod.GetLocalization("Cultivation.BreakthroughTooltip.Next").Format(
				targetRealm, cultivation.NextBreakthroughTargetStage), Color.LightCyan));
			int remaining = Math.Max(0, cultivation.NextStageThreshold - cultivation.QiExp);
			lines.Add((Mod.GetLocalization("Cultivation.BreakthroughTooltip.Progress").Format(
				cultivation.QiExp, cultivation.NextStageThreshold, remaining), Color.White));
			lines.Add((Mod.GetLocalization(
				"Cultivation.BreakthroughTooltip.QiCapacity").Format(
					cultivation.Qi, cultivation.MaxQi,
					cultivation.BaseMaxQi,
					MathF.Round(cultivation.BurnedQiCapacityPercent, 2)),
				cultivation.HasBurnedQi ? Color.OrangeRed : Color.LightCyan));
			lines.Add((cultivation.GetNextStageBonusSummary(), new Color(150, 235, 205)));
			if (cultivation.NextAdvancementIsRealmBreakthrough)
			{
				lines.Add((Mod.GetLocalization(
					"Cultivation.BreakthroughTooltip.SuccessChance").Format(
						MathF.Round(cultivation.NextRealmBreakthroughChance, 1),
						MathF.Round(cultivation.NextRealmBreakthroughBaseChance, 1),
						MathF.Round(cultivation.NextRealmBreakthroughRootModifier, 1),
						MathF.Round(cultivation.NextRealmBreakthroughPillModifier, 1),
						MathF.Round(cultivation.HeartDemonBreakthroughPenalty, 1)),
					cultivation.NextRealmBreakthroughChance >= 75f
						? Color.LightGreen : Color.Gold));
				lines.Add((Mod.GetLocalization(
					"Cultivation.BreakthroughTooltip.FailurePenalty").Value,
					new Color(255, 155, 125)));
			}
			if (cultivation.NextBreakthroughRequiresHeavenlyTreasures)
			{
				bool foundation = cultivation.NextBreakthroughTargetRealm == 2;
				string requirement = foundation
					? Mod.GetLocalization(
						"Cultivation.BreakthroughTooltip.FoundationCatalyst").Format(
							cultivation.NextRealmBreakthroughPillModifier > 0f
								? Mod.GetLocalization(
									"Cultivation.BreakthroughTooltip.Ready").Value
								: Mod.GetLocalization(
									"Cultivation.BreakthroughTooltip.Absent").Value,
							cultivation.HeavenlyEyeEssenceCount,
							cultivation.HeavenlyRoyalNectarCount,
							cultivation.HeavenlyBoneMarrowCount)
					: Mod.GetLocalization(
						"Cultivation.BreakthroughTooltip.GoldenCoreTreasure").Format(
							cultivation.HeavenlyEyeEssenceCount,
							cultivation.HeavenlyRoyalNectarCount,
							cultivation.HeavenlyBoneMarrowCount);
				bool ready = foundation
					? cultivation.HasFoundationBreakthroughCatalyst
					: cultivation.HasGoldenCoreHeavenlyTreasures;
				lines.Add((requirement,
					ready ? Color.LightGreen : Color.OrangeRed));
				lines.Add((Mod.GetLocalization(
					"Cultivation.BreakthroughTooltip.PermanentImprints").Format(
						cultivation.HeavenlyEyeImprints,
						cultivation.HeavenlyRoyalNectarImprints,
						cultivation.HeavenlyBoneMarrowImprints),
					new Color(255, 220, 135)));
			}

			string nextUnlock = GetNextAbilityUnlockText(cultivation);
			if (!string.IsNullOrEmpty(nextUnlock))
			{
				lines.Add((nextUnlock, new Color(208, 165, 255)));
			}

			if (cultivation.NextBreakthroughRequiresTribulation)
			{
				int perStrike = cultivation.EstimateNextTribulationShieldCostPerStrike();
				int strikes = cultivation.NextBreakthroughTribulationStrikes;
				int totalCost = perStrike * strikes;
				lines.Add((Mod.GetLocalization("Cultivation.BreakthroughTooltip.Tribulation").Format(
					strikes, perStrike, totalCost), new Color(255, 190, 80)));
				bool ready = cultivation.CanUseQiProtection && cultivation.Qi >= totalCost;
				lines.Add((Mod.GetLocalization(ready
					? "Cultivation.BreakthroughTooltip.ShieldReady"
					: "Cultivation.BreakthroughTooltip.ShieldWarning").Value,
					ready ? Color.LightGreen : Color.OrangeRed));
				lines.Add((Mod.GetLocalization("Cultivation.BreakthroughTooltip.PenetrationWarning").Value,
					new Color(255, 145, 145)));
			}
			else
			{
				lines.Add((Mod.GetLocalization("Cultivation.BreakthroughTooltip.NoTribulation").Value,
					Color.LightGray));
			}
		}

		int panelWidth = Math.Min(620, Main.screenWidth - 20);
		const float bodyScale = 0.62f;
		const int lineHeight = 22;
		List<(string Text, Color Color)> wrappedLines = [];
		foreach ((string text, Color color) in lines)
		{
			AppendWrappedTooltipLine(wrappedLines, text, color, panelWidth - 28, bodyScale);
		}
		int panelHeight = 42 + wrappedLines.Count * lineHeight;
		int panelX = Math.Clamp(qiBar.Center.X - panelWidth / 2, 10,
			Math.Max(10, Main.screenWidth - panelWidth - 10));
		int preferredY = qiBar.Bottom + 34;
		int panelY = preferredY + panelHeight <= Main.screenHeight - 10
			? preferredY
			: Math.Max(10, qiBar.Y - panelHeight - 28);
		Rectangle panel = new(panelX, panelY, panelWidth, panelHeight);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, panel, new Color(102, 67, 145, 245));
		Main.spriteBatch.Draw(pixel,
			new Rectangle(panel.X + 3, panel.Y + 3, panel.Width - 6, panel.Height - 6),
			new Color(9, 16, 28, 248));
		DrawCenteredText(Mod.GetLocalization("Cultivation.BreakthroughTooltip.Title").Value,
			new Vector2(panel.Center.X, panel.Y + 20), Color.White, 0.8f);
		for (int i = 0; i < wrappedLines.Count; i++)
		{
			Utils.DrawBorderString(Main.spriteBatch, wrappedLines[i].Text,
				new Vector2(panel.X + 14, panel.Y + 39 + i * lineHeight),
				wrappedLines[i].Color, bodyScale);
		}
	}

	private static void AppendWrappedTooltipLine(
		List<(string Text, Color Color)> destination,
		string text,
		Color color,
		float maximumWidth,
		float scale)
	{
		string line = string.Empty;
		foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			string candidate = line.Length == 0 ? word : line + " " + word;
			if (FontAssets.MouseText.Value.MeasureString(candidate).X * scale > maximumWidth
				&& line.Length > 0)
			{
				destination.Add((line, color));
				line = word;
			}
			else
			{
				line = candidate;
			}
		}

		if (line.Length > 0)
		{
			destination.Add((line, color));
		}
	}

	private string GetNextAbilityUnlockText(CultivationPlayer cultivation)
	{
		int nextRealm = int.MaxValue;
		List<string> names = [];
		for (CultivationAbility ability = 0; ability < CultivationAbility.Count; ability++)
		{
			int requiredRealm = CultivationAbilityInfo.RequiredRealm(ability);
			if (requiredRealm <= cultivation.RealmIndex || requiredRealm > nextRealm)
			{
				continue;
			}

			if (requiredRealm < nextRealm)
			{
				nextRealm = requiredRealm;
				names.Clear();
			}
			names.Add(Mod.GetLocalization($"AbilityTree.Abilities.{ability}.Name").Value);
		}

		return names.Count == 0
			? string.Empty
			: Mod.GetLocalization("Cultivation.BreakthroughTooltip.NextAbilities").Format(
				cultivation.GetRealmName(nextRealm), string.Join(", ", names));
	}

	private static float GetCultivationProgress(CultivationPlayer cultivation)
	{
		if (cultivation.IsCultivationMaxed)
		{
			return 1f;
		}

		int stageRange = cultivation.NextStageThreshold - cultivation.CurrentStageThreshold;
		int qiInStage = cultivation.QiExp - cultivation.CurrentStageThreshold;
		return MathHelper.Clamp(qiInStage / (float)Math.Max(1, stageRange), 0f, 1f);
	}

	private bool DrawAbilityWheel()
	{
		if (Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
		{
			return true;
		}

		Player player = Main.LocalPlayer;
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		if (!player.active)
		{
			return true;
		}

		if (cultivation.IsAwaitingRealmBreakthroughConfirmation)
		{
			return DrawRealmBreakthroughConfirmation(player, cultivation);
		}

		if (cultivation.IsAwaitingTribulationConfirmation)
		{
			return DrawTribulationConfirmation(player, cultivation);
		}

		if (cultivation.IsAwaitingHeartDemonTrialConfirmation)
		{
			return DrawHeartDemonTrialConfirmation(player, cultivation);
		}

		if (!cultivation.IsAbilityWheelOpen)
			return true;

		player.mouseInterface = true;
		Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
		Vector2 center = screenSize * 0.5f;
		Vector2 mousePosition = new(Main.mouseX, Main.mouseY);
		Vector2 mouseOffset = mousePosition - center;
		float mouseDistance = mouseOffset.Length();
		AbilityWheelEntry[] entries = BuildAbilityWheelEntries(player, cultivation);
		int segmentCount = Math.Max(1, entries.Length);
		int hoveredSegment = GetHoveredSegment(
			mouseOffset, mouseDistance, entries.Length);
		AbilityWheelEntry[] toggleEntries = toggleWheelExpanded
			? BuildToggleWheelEntries(player, cultivation)
			: [];
		int hoveredToggle = toggleWheelExpanded
			? GetHoveredToggleSegment(
				mouseOffset, mouseDistance, toggleEntries.Length)
			: -1;
		Texture2D pixel = TextureAssets.MagicPixel.Value;

		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y),
			new Color(3, 5, 12, 215));

		for (int i = 0; i < entries.Length; i++)
		{
			AbilityWheelEntry entry = entries[i];
			Color segmentColor = GetSegmentColor(
				entry, hoveredSegment == i,
				cultivation.SelectedTechniqueLoadoutSlot == i);
			float centerAngle =
				WheelStartAngle + i * MathHelper.TwoPi / segmentCount;
			DrawWheelSector(
				pixel, center, centerAngle, segmentColor, segmentCount);
			DrawAbilityLabel(center, centerAngle, entry);
		}
		if (toggleWheelExpanded)
		{
			DrawToggleSubWheel(pixel, center, toggleEntries,
				hoveredToggle);
		}

		DrawFilledCircle(pixel, center, WheelInnerRadius - 5f, new Color(13, 18, 30, 245));
		DrawCircleOutline(pixel, center, WheelInnerRadius - 5f, new Color(112, 235, 245), 2.5f);
		DrawCircleOutline(pixel, center, WheelOuterRadius, new Color(94, 64, 135), 3f);

		string title = Mod.GetLocalization("AbilityWheel.Title").Value;
		float titleRadius = toggleWheelExpanded
			? ToggleWheelOuterRadius
			: WheelOuterRadius;
		Vector2 titlePosition = new(
			center.X,
			Math.Max(26f, center.Y - titleRadius - 22f));
		DrawCenteredText(title, titlePosition, Color.White, 0.9f);

		if (hoveredToggle >= 0)
		{
			AbilityWheelEntry hoveredEntry =
				toggleEntries[hoveredToggle];
			DrawAbilityIcon(center - new Vector2(0f, 13f),
				hoveredEntry, 28f);
			DrawCenteredText(hoveredEntry.Name,
				center + new Vector2(0f, 22f),
				Color.White, 0.67f);
			DrawWheelDetailPanel(pixel,
				center + new Vector2(0f, WheelOuterRadius + 38f),
				hoveredEntry);
			HandleToggleWheelClick(
				cultivation, hoveredEntry);
		}
		else if (hoveredSegment >= 0)
		{
			AbilityWheelEntry hoveredEntry = entries[hoveredSegment];
			DrawAbilityIcon(center - new Vector2(0f, 13f), hoveredEntry, 28f);
			DrawCenteredText(hoveredEntry.Name, center + new Vector2(0f, 22f), Color.White, 0.67f);
			DrawWheelDetailPanel(pixel, center + new Vector2(0f, WheelOuterRadius + 38f), hoveredEntry);
			HandleAbilityWheelClick(cultivation, hoveredSegment, hoveredEntry);
		}
		else
		{
			DrawCenteredText(Mod.GetLocalization("AbilityWheel.HoverHint").Value,
				center, Color.LightGray, 0.65f);
		}

		return true;
	}

	private bool DrawHeartDemonTrialConfirmation(
		Player player, CultivationPlayer cultivation)
	{
		player.mouseInterface = true;
		Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
		Vector2 center = screenSize * 0.5f;
		Point mouse = Main.MouseScreen.ToPoint();
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel,
			new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y),
			new Color(3, 1, 8, 220));
		Rectangle outer = new((int)center.X - 275,
			(int)center.Y - 155, 550, 310);
		Rectangle panel = new(outer.X + 3, outer.Y + 3,
			outer.Width - 6, outer.Height - 6);
		Main.spriteBatch.Draw(pixel, outer,
			new Color(105, 45, 135));
		Main.spriteBatch.Draw(pixel, panel,
			new Color(18, 12, 29, 252));
		DrawCenteredText(Mod.GetLocalization(
				"HeartDemonTrialConfirmation.Title").Value,
			center - new Vector2(0f, 112f),
			Color.MediumPurple, 1.05f);
		DrawCenteredTextFitted(Mod.GetLocalization(
				"HeartDemonTrialConfirmation.Message").Format(
					cultivation.HeartDemonPoints),
			center - new Vector2(0f, 59f),
			panel.Width - 30, Color.White, 0.75f);
		DrawCenteredTextFitted(Mod.GetLocalization(
				"HeartDemonTrialConfirmation.Warning").Value,
			center - new Vector2(0f, 14f),
			panel.Width - 36, Color.OrangeRed, 0.65f);
		DrawCenteredTextFitted(Mod.GetLocalization(
				"HeartDemonTrialConfirmation.Reward").Value,
			center + new Vector2(0f, 24f),
			panel.Width - 36, Color.LightGreen, 0.65f);
		Rectangle confirm = new((int)center.X - 225,
			(int)center.Y + 70, 200, 48);
		Rectangle cancel = new((int)center.X + 25,
			(int)center.Y + 70, 200, 48);
		bool confirmHovered = confirm.Contains(mouse);
		bool cancelHovered = cancel.Contains(mouse);
		DrawButton(pixel, confirm,
			Mod.GetLocalization(
				"HeartDemonTrialConfirmation.Confirm").Value,
			confirmHovered, new Color(95, 40, 125));
		DrawButton(pixel, cancel,
			Mod.GetLocalization(
				"HeartDemonTrialConfirmation.Cancel").Value,
			cancelHovered, new Color(105, 55, 70));
		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (confirmHovered)
			{
				Main.mouseLeftRelease = false;
				cultivation.ConfirmHeartDemonTrial();
			}
			else if (cancelHovered)
			{
				Main.mouseLeftRelease = false;
				cultivation.CancelHeartDemonTrialConfirmation();
			}
		}
		return true;
	}

	private bool DrawRealmBreakthroughConfirmation(
		Player player, CultivationPlayer cultivation)
	{
		player.mouseInterface = true;
		Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
		Vector2 center = screenSize * 0.5f;
		Point mouse = new(Main.mouseX, Main.mouseY);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel,
			new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y),
			new Color(2, 4, 10, 205));

		Rectangle outerPanel = new((int)center.X - 360,
			(int)center.Y - 285, 720, 570);
		Rectangle panel = new(outerPanel.X + 3, outerPanel.Y + 3,
			outerPanel.Width - 6, outerPanel.Height - 6);
		Main.spriteBatch.Draw(pixel, outerPanel,
			new Color(125, 78, 175, 245));
		Main.spriteBatch.Draw(pixel, panel,
			new Color(13, 17, 30, 250));
		Main.spriteBatch.Draw(pixel,
			new Rectangle(panel.X, panel.Y, panel.Width, 7),
			new Color(95, 225, 240));

		DrawCenteredText(
			Mod.GetLocalization("BreakthroughConfirmation.Title").Value,
			center - new Vector2(0f, 252f), Color.Gold, 1.08f);
		DrawCenteredTextFitted(
			Mod.GetLocalization("BreakthroughConfirmation.Message").Format(
				cultivation.PendingRealmBreakthroughTargetName),
			center - new Vector2(0f, 218f), panel.Width - 30,
			Color.White, 0.78f);

		int targetRealm = cultivation.PendingRealmBreakthroughTargetRealm;
		DrawCenteredText(
			Mod.GetLocalization("BreakthroughConfirmation.QualityTarget").Value,
			center - new Vector2(0f, 187f), Color.LightCyan, 0.67f);
		List<Rectangle> qualityButtons = [];
		if (targetRealm == 2)
		{
			FoundationQuality[] qualities =
			[
				FoundationQuality.Inferior,
				FoundationQuality.Stable,
				FoundationQuality.Perfect,
				FoundationQuality.Heavenly
			];
			const int qualityWidth = 156;
			const int qualityGap = 8;
			int qualityStart = (int)center.X
				- (qualityWidth * qualities.Length
					+ qualityGap * (qualities.Length - 1)) / 2;
			for (int i = 0; i < qualities.Length; i++)
			{
				Rectangle card = new(
					qualityStart + i * (qualityWidth + qualityGap),
					(int)center.Y - 169, qualityWidth, 42);
				qualityButtons.Add(card);
				bool selected =
					cultivation.PendingFoundationQuality == qualities[i];
				DrawButton(pixel, card,
					Mod.GetLocalization(
						$"BreakthroughGrades.Foundation.{qualities[i]}")
						.Value,
					card.Contains(mouse),
					selected ? new Color(45, 145, 130)
						: new Color(54, 48, 72));
			}
		}
		else if (targetRealm == 3)
		{
			const int tierWidth = 63;
			const int tierGap = 6;
			int start = (int)center.X
				- (tierWidth * 9 + tierGap * 8) / 2;
			for (int i = 0; i < 9; i++)
			{
				int tier = 9 - i;
				Rectangle card = new(start + i * (tierWidth + tierGap),
					(int)center.Y - 169, tierWidth, 42);
				qualityButtons.Add(card);
				bool selected =
					cultivation.PendingGoldenCoreTier == tier;
				DrawButton(pixel, card, $"Tier {tier}",
					card.Contains(mouse),
					selected ? new Color(142, 101, 35)
						: new Color(54, 48, 72));
			}
		}

		float chance = cultivation.PendingRealmBreakthroughChance;
		Color chanceColor = chance >= 75f
			? Color.LightGreen : chance >= 50f ? Color.Gold : Color.OrangeRed;
		DrawCenteredText(
			Mod.GetLocalization("BreakthroughConfirmation.Chance").Format(
				MathF.Round(chance, 1)),
			center - new Vector2(0f, 102f), chanceColor, 1.12f);
		DrawCenteredTextFitted(
			Mod.GetLocalization("BreakthroughConfirmation.Breakdown").Format(
				MathF.Round(cultivation.PendingRealmBreakthroughBaseChance, 1),
				MathF.Round(cultivation.PendingRealmBreakthroughRootModifier, 1),
				MathF.Round(cultivation.PendingRealmBreakthroughPillModifier, 1),
				MathF.Round(cultivation.HeartDemonBreakthroughPenalty, 1),
				MathF.Round(
					cultivation.PendingBreakthroughGradeChanceModifier, 1)),
			center - new Vector2(0f, 73f), panel.Width - 36,
			Color.LightCyan, 0.67f);
		DrawCenteredTextFitted(
			Mod.GetLocalization("BreakthroughConfirmation.QualityBonuses")
				.Format(cultivation.GetPendingBreakthroughGradeName(),
					MathF.Round(
						cultivation.PendingBreakthroughStatMultiplier, 2),
					MathF.Round(
						cultivation.PendingBreakthroughQiGatheringBonusPercent,
						1)),
			center - new Vector2(0f, 45f), panel.Width - 36,
			Color.LightGreen, 0.65f);

		Rectangle treasureSlot = new((int)center.X - 116,
			(int)center.Y - 11, 74, 74);
		Rectangle pillSlot = new((int)center.X + 42,
			(int)center.Y - 11, 74, 74);
		DrawBreakthroughItemSlot(pixel, treasureSlot,
			cultivation.SelectedBreakthroughTreasureType,
			Mod.GetLocalization(
				"BreakthroughConfirmation.TreasureSlot").Value,
			mouse);
		DrawBreakthroughItemSlot(pixel, pillSlot,
			cultivation.SelectedBreakthroughPillType,
			Mod.GetLocalization(
				"BreakthroughConfirmation.PillSlot").Value,
			mouse);
		DrawCenteredTextFitted(
			Mod.GetLocalization("BreakthroughConfirmation.SlotHint").Value,
			center + new Vector2(0f, 82f), panel.Width - 40,
			Color.Gray, 0.54f);

		string requirementKey = targetRealm switch
		{
			2 when cultivation.PendingFoundationQuality
				is FoundationQuality.Inferior
				or FoundationQuality.Stable =>
				"InferiorStableRequirement",
			2 when cultivation.PendingFoundationQuality
				== FoundationQuality.Perfect =>
				"PerfectRequirement",
			2 => "HeavenlyRequirement",
			3 when cultivation.PendingGoldenCoreTier == 1 =>
				"GoldenTierOneRequirement",
			3 => "GoldenRequirement",
			_ => "NoCatalystRequired"
		};
		string catalyst = cultivation.HasBurnedQi
			? Mod.GetLocalization(
				"BreakthroughConfirmation.BurnedCapacity").Format(
					MathF.Round(cultivation.BurnedQiCapacityPercent, 2))
			: Mod.GetLocalization(
				$"BreakthroughConfirmation.{requirementKey}").Value;
		DrawCenteredTextFitted(catalyst,
			center + new Vector2(0f, 111f), panel.Width - 36,
			cultivation.CanConfirmRealmBreakthrough
				? new Color(255, 220, 135) : Color.OrangeRed,
			0.68f);
		DrawCenteredTextFitted(
			Mod.GetLocalization("BreakthroughConfirmation.Failure").Value,
			center + new Vector2(0f, 140f), panel.Width - 36,
			new Color(255, 145, 125), 0.64f);

		Rectangle confirmButton = new((int)center.X - 238,
			(int)center.Y + 185, 215, 52);
		Rectangle cancelButton = new((int)center.X + 23,
			(int)center.Y + 185, 215, 52);
		bool confirmHovered = confirmButton.Contains(mouse);
		bool cancelHovered = cancelButton.Contains(mouse);
		DrawButton(pixel, confirmButton,
			cultivation.CanConfirmRealmBreakthrough
				? Mod.GetLocalization(
					"BreakthroughConfirmation.Confirm").Value
				: Mod.GetLocalization(
					"BreakthroughConfirmation.NotReady").Value,
			confirmHovered && cultivation.CanConfirmRealmBreakthrough,
			cultivation.CanConfirmRealmBreakthrough
				? new Color(35, 145, 135)
				: new Color(75, 75, 82));
		DrawButton(pixel, cancelButton,
			Mod.GetLocalization("BreakthroughConfirmation.Cancel").Value,
			cancelHovered, new Color(135, 55, 75));

		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			bool handled = false;
			for (int i = 0; i < qualityButtons.Count; i++)
			{
				if (!qualityButtons[i].Contains(mouse))
					continue;
				if (targetRealm == 2)
					cultivation.SelectPendingFoundationQuality(
						(FoundationQuality)i);
				else if (targetRealm == 3)
					cultivation.SelectPendingGoldenCoreTier(9 - i);
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
				handled = true;
				break;
			}
			if (!handled && treasureSlot.Contains(mouse))
			{
				cultivation.CycleSelectedBreakthroughTreasure();
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (!handled && pillSlot.Contains(mouse))
			{
				cultivation.ToggleSelectedBreakthroughPill();
				Main.mouseLeftRelease = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (!handled && confirmHovered
				&& cultivation.CanConfirmRealmBreakthrough)
			{
				Main.mouseLeftRelease = false;
				cultivation.ConfirmRealmBreakthrough();
			}
			else if (cancelHovered)
			{
				Main.mouseLeftRelease = false;
				cultivation.CancelRealmBreakthrough();
			}
		}
		if (Main.mouseRight && Main.mouseRightRelease)
		{
			if (treasureSlot.Contains(mouse))
			{
				cultivation.ClearSelectedBreakthroughTreasure();
				Main.mouseRightRelease = false;
			}
			else if (pillSlot.Contains(mouse))
			{
				cultivation.ClearSelectedBreakthroughPill();
				Main.mouseRightRelease = false;
			}
		}
		return true;
	}

	private void DrawBreakthroughItemSlot(Texture2D pixel,
		Rectangle slot, int itemType, string label, Point mouse)
	{
		bool hovered = slot.Contains(mouse);
		Main.spriteBatch.Draw(pixel, slot,
			hovered ? Color.White : new Color(112, 81, 151));
		Main.spriteBatch.Draw(pixel,
			new Rectangle(slot.X + 3, slot.Y + 3,
				slot.Width - 6, slot.Height - 6),
			new Color(24, 28, 44, 250));
		if (itemType > 0)
			DrawPathItemIcon(itemType, slot.Center.ToVector2(), 46f);
		else
			DrawCenteredText(
				Mod.GetLocalization(
					"BreakthroughConfirmation.EmptySlot").Value,
				slot.Center.ToVector2(), Color.Gray, 0.48f);
		DrawCenteredTextFitted(label,
			new Vector2(slot.Center.X, slot.Y - 12),
			130f, Color.LightCyan, 0.55f);
	}

	private bool DrawTribulationConfirmation(Player player, CultivationPlayer cultivation)
	{
		player.mouseInterface = true;
		Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
		Vector2 center = screenSize * 0.5f;
		Vector2 mousePosition = new(Main.mouseX, Main.mouseY);
		Texture2D pixel = TextureAssets.MagicPixel.Value;

		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y),
			new Color(2, 4, 10, 190));

		Rectangle outerPanel = new((int)center.X - 252,
			(int)center.Y - 160, 504, 320);
		Rectangle panel = new(outerPanel.X + 3, outerPanel.Y + 3, outerPanel.Width - 6, outerPanel.Height - 6);
		Main.spriteBatch.Draw(pixel, outerPanel, new Color(125, 78, 175, 245));
		Main.spriteBatch.Draw(pixel, panel, new Color(13, 17, 30, 250));
		Main.spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 6), new Color(95, 225, 240));

		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Title").Value,
			center - new Vector2(0f, 117f), Color.Gold, 1.05f);
		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Message").Format(
			cultivation.PendingTribulationRealmName),
			center - new Vector2(0f, 68f), Color.White, 0.78f);
		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Strikes").Format(
			cultivation.PendingTribulationStrikeCount),
			center - new Vector2(0f, 38f), Color.OrangeRed, 0.75f);
		DrawCenteredTextFitted(
			Mod.GetLocalization("TribulationConfirmation.Difficulty").Format(
				MathF.Round(
					cultivation.PendingTribulationPowerBonusPercent, 1),
				cultivation.GetFoundationQualityName(),
				cultivation.PendingTribulationGoldenCoreTier),
			center - new Vector2(0f, 8f), panel.Width - 28,
			new Color(255, 195, 95), 0.64f);
		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Warning").Value,
			center + new Vector2(0f, 24f), Color.LightGray, 0.65f);

		Rectangle confirmButton = new((int)center.X - 210,
			(int)center.Y + 76, 190, 48);
		Rectangle cancelButton = new((int)center.X + 20,
			(int)center.Y + 76, 190, 48);
		bool confirmHovered = confirmButton.Contains(mousePosition.ToPoint());
		bool cancelHovered = cancelButton.Contains(mousePosition.ToPoint());
		DrawButton(pixel, confirmButton,
			Mod.GetLocalization("TribulationConfirmation.Confirm").Value,
			confirmHovered, new Color(35, 145, 135));
		DrawButton(pixel, cancelButton,
			Mod.GetLocalization("TribulationConfirmation.Cancel").Value,
			cancelHovered, new Color(135, 55, 75));

		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (confirmHovered)
			{
				Main.mouseLeftRelease = false;
				cultivation.ConfirmTribulation();
			}
			else if (cancelHovered)
			{
				Main.mouseLeftRelease = false;
				cultivation.CancelTribulation();
			}
		}

		return true;
	}

	private static void DrawButton(
		Texture2D pixel,
		Rectangle rectangle,
		string text,
		bool hovered,
		Color baseColor)
	{
		Color borderColor = hovered ? Color.White : Color.Lerp(baseColor, Color.White, 0.25f);
		Color fillColor = hovered ? Color.Lerp(baseColor, Color.White, 0.22f) : baseColor;
		Main.spriteBatch.Draw(pixel, rectangle, borderColor);
		Rectangle inner = new(rectangle.X + 3, rectangle.Y + 3, rectangle.Width - 6, rectangle.Height - 6);
		Main.spriteBatch.Draw(pixel, inner, fillColor);
		DrawCenteredText(text, rectangle.Center.ToVector2(), Color.White, 0.8f);
	}

	private AbilityWheelEntry[] BuildAbilityWheelEntries(
		Player player, CultivationPlayer cultivation)
	{
		AbilityWheelEntry[] available =
			BuildAllAbilityWheelEntries(player, cultivation);
		AbilityWheelEntry[] equipped = new AbilityWheelEntry[
			cultivation.TechniqueLoadoutSlotCount + 1];
		for (int slot = 0;
			slot < cultivation.TechniqueLoadoutSlotCount; slot++)
		{
			CultivationAbility ability =
				cultivation.GetActiveTechniqueLoadoutAbility(slot);
			bool found = false;
			for (int i = 0; i < available.Length; i++)
			{
				if (GetWheelAbility(available[i].Id) != ability)
					continue;
				bool selected =
					slot == cultivation.SelectedTechniqueLoadoutSlot;
				equipped[slot] = available[i] with
				{
					BadgeText = selected
						? FormatKeyBadge(Xianxia.FireballKeybind)
						: Mod.GetLocalization(
							"TechniqueLoadout.Select").Value,
					Information = available[i].Information + " | "
						+ Mod.GetLocalization(selected
							? "TechniqueLoadout.SelectedHint"
							: "TechniqueLoadout.SelectionHint").Value
				};
				found = true;
				break;
			}
			if (!found)
			{
				equipped[slot] = new AbilityWheelEntry(
					AbilityWheelId.Empty,
					Mod.GetLocalization("TechniqueLoadout.Empty").Value,
					Mod.GetLocalization(
						"TechniqueLoadout.EmptyHint").Value,
					$"{slot + 1}",
					false, false, false, 0, string.Empty);
			}
		}
		equipped[^1] = new AbilityWheelEntry(
			AbilityWheelId.ToggleMenu,
			Mod.GetLocalization("TechniqueLoadout.ToggleMenu").Value,
			Mod.GetLocalization(toggleWheelExpanded
				? "TechniqueLoadout.ToggleMenuClose"
				: "TechniqueLoadout.ToggleMenuHint").Value,
			Mod.GetLocalization("TechniqueLoadout.ToggleBadge").Value,
			true, true, toggleWheelExpanded,
			ItemID.Lever, string.Empty);
		return equipped;
	}

	private AbilityWheelEntry[] BuildToggleWheelEntries(
		Player player, CultivationPlayer cultivation)
	{
		AbilityWheelEntry[] available =
			BuildAllAbilityWheelEntries(player, cultivation);
		List<AbilityWheelEntry> toggles = [];
		for (int i = 0; i < available.Length; i++)
		{
			CultivationAbility ability =
				GetWheelAbility(available[i].Id);
			if (!CultivationAbilityInfo.IsToggleTechnique(ability)
				|| !available[i].IsUnlocked)
			{
				continue;
			}
			toggles.Add(available[i] with
			{
				BadgeText = available[i].IsEnabled
					? Mod.GetLocalization(
						"Abilities.StateEnabled").Value
					: Mod.GetLocalization(
						"Abilities.StateDisabled").Value,
				Information = available[i].Information + " | "
					+ Mod.GetLocalization(
						"TechniqueLoadout.ToggleClickHint").Value
			});
		}
		return [.. toggles];
	}

	private static CultivationAbility GetWheelAbility(
		AbilityWheelId id) => id switch
	{
		AbilityWheelId.QiProtection =>
			CultivationAbility.QiProtection,
		AbilityWheelId.QiBurning => CultivationAbility.QiBurning,
		AbilityWheelId.QiSense => CultivationAbility.QiSense,
		AbilityWheelId.QiFlight => CultivationAbility.QiFlight,
		AbilityWheelId.NascentTeleport =>
			CultivationAbility.NascentTeleport,
		AbilityWheelId.SpiritualPressure =>
			CultivationAbility.SpiritualPressure,
		AbilityWheelId.FlameStep => CultivationAbility.FlameStep,
		AbilityWheelId.NightVision => CultivationAbility.NightVision,
		AbilityWheelId.Fireball => CultivationAbility.Fireball,
		AbilityWheelId.QiPalm => CultivationAbility.QiPalm,
		AbilityWheelId.QiResistance =>
			CultivationAbility.QiResistance,
		AbilityWheelId.SpiritualRain =>
			CultivationAbility.SpiritualRain,
		AbilityWheelId.SpiritSwordRain =>
			CultivationAbility.SpiritSwordRain,
		AbilityWheelId.SectProtectionFormation =>
			CultivationAbility.SectProtectionFormation,
		_ => CultivationAbility.Count
	};

	private AbilityWheelEntry[] BuildAllAbilityWheelEntries(
		Player player, CultivationPlayer cultivation)
	{
		string locked = Mod.GetLocalization("AbilityWheel.Locked").Value;
		string protectionState = Mod.GetLocalization(cultivation.QiProtectionEnabled
			? "Abilities.StateEnabled"
			: "Abilities.StateDisabled").Value;
		string protectionInfo = cultivation.HasUnlockedQiProtection
			? Mod.GetLocalization("AbilityWheel.PassiveToggle").Format(protectionState)
			: Mod.GetLocalization("AbilityWheel.RequiresFoundation").Value;
		string senseState = Mod.GetLocalization(cultivation.QiSenseEnabled
			? "Abilities.StateEnabled"
			: "Abilities.StateDisabled").Value;
		string senseInfo = cultivation.HasUnlockedQiSense
			? Mod.GetLocalization("AbilityWheel.QiSenseToggle").Format(senseState)
			: Mod.GetLocalization("AbilityWheel.RequiresQiGathering").Value;
		bool resistanceActive = player.HasBuff(ModContent.BuffType<QiResistanceBuff>());
		bool formationActive =
			player.HasBuff<SectProtectionFormationBuff>();
		bool spiritualRainUnlocked = cultivation.IsAbilityUnlocked(
			CultivationAbility.SpiritualRain);
		bool swordRainUnlocked = cultivation.IsAbilityUnlocked(
			CultivationAbility.SpiritSwordRain);
		bool formationUnlocked = cultivation.IsAbilityUnlocked(
			CultivationAbility.SectProtectionFormation);

		return
		[
			new(
				AbilityWheelId.QiProtection,
				Mod.GetLocalization("AbilityWheel.QiProtection").Value,
				protectionInfo,
				cultivation.HasUnlockedQiProtection ? protectionState : locked,
				true,
				cultivation.HasUnlockedQiProtection,
				cultivation.QiProtectionEnabled,
				ItemID.CobaltShield,
				string.Empty
			),
			new(
				AbilityWheelId.QiBurning,
				Mod.GetLocalization("AbilityWheel.QiBurning").Value,
				cultivation.RealmIndex >= 2
					? Mod.GetLocalization(
						"AbilityWheel.QiBurningDescription").Format(
							MathF.Round(cultivation.BurnedQiCapacityPercent, 2),
							cultivation.MaximumBurnedQiBps / 100f,
							cultivation.HasQiDeviation
								? Mod.GetLocalization(
									"Abilities.QiBurningDeviationState").Format(
										cultivation.QiDeviationSecondsRemaining)
								: cultivation.QiBurningEnabled
									? Mod.GetLocalization(
										"Abilities.StateEnabled").Value
									: Mod.GetLocalization(
										"Abilities.StateDisabled").Value)
					: Mod.GetLocalization(
						"AbilityWheel.RequiresFoundation").Value,
				cultivation.RealmIndex >= 2
					? FormatKeyBadge(Xianxia.QiBurningKeybind,
						cultivation.QiBurningEnabled)
					: locked,
				true,
				cultivation.RealmIndex >= 2,
				cultivation.QiBurningEnabled,
				ItemID.Hellstone,
				string.Empty
			),
			new(
				AbilityWheelId.QiSense,
				Mod.GetLocalization("AbilityWheel.QiSense").Value,
				senseInfo,
				cultivation.HasUnlockedQiSense ? senseState : locked,
				true,
				cultivation.HasUnlockedQiSense,
				cultivation.QiSenseEnabled,
				ItemID.SpelunkerPotion,
				string.Empty
			),
			new(
				AbilityWheelId.QiFlight,
				Mod.GetLocalization("AbilityWheel.QiFlight").Value,
				cultivation.RealmIndex >= 3
					? FormatActiveInformation("AbilityWheel.QiFlightDescription",
						Xianxia.QiFlightKeybind, cultivation.QiFlightEnabled)
					: locked,
				cultivation.RealmIndex >= 3
					? FormatKeyBadge(Xianxia.QiFlightKeybind, cultivation.QiFlightEnabled)
					: locked,
				false,
				cultivation.RealmIndex >= 3,
				cultivation.QiFlightEnabled,
				ItemID.AngelWings,
				string.Empty
			),
			new(
				AbilityWheelId.NascentTeleport,
				Mod.GetLocalization("AbilityWheel.NascentTeleport").Value,
				cultivation.RealmIndex >= 4
					? FormatActiveInformation("AbilityWheel.NascentTeleportDescription",
						Xianxia.NascentTeleportKeybind)
					: Mod.GetLocalization("AbilityWheel.RequiresNascentSoul").Value,
				cultivation.RealmIndex >= 4
					? FormatKeyBadge(Xianxia.NascentTeleportKeybind)
					: locked,
				false,
				cultivation.RealmIndex >= 4,
				false,
				ItemID.RodofDiscord,
				string.Empty
			),
			new(
				AbilityWheelId.SpiritualPressure,
				Mod.GetLocalization("AbilityWheel.SpiritualPressure").Value,
				cultivation.RealmIndex >= 4
					? FormatActiveInformation("AbilityWheel.SpiritualPressureDescription",
						Xianxia.SpiritualPressureKeybind, cultivation.SpiritualPressureEnabled)
					: Mod.GetLocalization("AbilityWheel.RequiresNascentSoul").Value,
				cultivation.RealmIndex >= 4
					? FormatKeyBadge(Xianxia.SpiritualPressureKeybind, cultivation.SpiritualPressureEnabled)
					: locked,
				false,
				cultivation.RealmIndex >= 4,
				cultivation.SpiritualPressureEnabled,
				ItemID.SoulCake,
				string.Empty
			),
			new(
				AbilityWheelId.FlameStep,
				Mod.GetLocalization("AbilityWheel.FlameStep").Value,
				cultivation.RealmIndex >= 2
					? FormatActiveInformation("AbilityWheel.FlameStepDescription", Xianxia.FlameStepKeybind)
					: Mod.GetLocalization("AbilityWheel.RequiresFoundation").Value,
				cultivation.RealmIndex >= 2 ? FormatKeyBadge(Xianxia.FlameStepKeybind) : locked,
				false,
				cultivation.RealmIndex >= 2,
				false,
				ItemID.HermesBoots,
				string.Empty
			),
			new(
				AbilityWheelId.NightVision,
				Mod.GetLocalization("AbilityWheel.NightVision").Value,
				cultivation.RealmIndex >= 2
					? FormatActiveInformation("AbilityWheel.NightVisionDescription",
						Xianxia.NightVisionKeybind, cultivation.NightVisionEnabled)
					: Mod.GetLocalization("AbilityWheel.RequiresFoundation").Value,
				cultivation.RealmIndex >= 2
					? FormatKeyBadge(Xianxia.NightVisionKeybind, cultivation.NightVisionEnabled)
					: locked,
				false,
				cultivation.RealmIndex >= 2,
				cultivation.NightVisionEnabled,
				ItemID.NightOwlPotion,
				string.Empty
			),
			new(
				AbilityWheelId.Fireball,
				Mod.GetLocalization("AbilityWheel.Fireball").Value,
				cultivation.RealmIndex >= 1
					? FormatActiveInformation("AbilityWheel.FireballDescription", Xianxia.FireballKeybind)
					: locked,
				cultivation.RealmIndex >= 1 ? FormatKeyBadge(Xianxia.FireballKeybind) : locked,
				false,
				cultivation.RealmIndex >= 1,
				false,
				0,
				"Xianxia/Content/Projectiles/QiFireballProjectile"
			),
			new(
				AbilityWheelId.QiPalm,
				Mod.GetLocalization("AbilityWheel.QiPalm").Value,
				cultivation.RealmIndex >= 1
					? FormatActiveInformation("AbilityWheel.QiPalmDescription", Xianxia.QiPalmKeybind)
					: locked,
				cultivation.RealmIndex >= 1 ? FormatKeyBadge(Xianxia.QiPalmKeybind) : locked,
				false,
				cultivation.RealmIndex >= 1,
				false,
				ItemID.FeralClaws,
				string.Empty
			),
			new(
				AbilityWheelId.QiResistance,
				Mod.GetLocalization("AbilityWheel.QiResistance").Value,
				cultivation.RealmIndex >= 1
					? FormatActiveInformation("AbilityWheel.QiResistanceDescription",
						Xianxia.QiResistanceKeybind, resistanceActive)
					: locked,
				cultivation.RealmIndex >= 1
					? FormatKeyBadge(Xianxia.QiResistanceKeybind, resistanceActive)
					: locked,
				false,
				cultivation.RealmIndex >= 1,
				resistanceActive,
				ItemID.IronskinPotion,
				string.Empty
			),
			new(
				AbilityWheelId.SpiritualRain,
				Mod.GetLocalization(
					"AbilityWheel.SpiritualRain").Value,
				Mod.GetLocalization(
					"AbilityWheel.SpiritualRainDescription").Value,
				spiritualRainUnlocked
					? Mod.GetLocalization(
						"TechniqueLoadout.Select").Value
					: locked,
				false,
				spiritualRainUnlocked,
				false,
				ModContent.ItemType<SpiritualRainTechnique>(),
				string.Empty
			),
			new(
				AbilityWheelId.SpiritSwordRain,
				Mod.GetLocalization(
					"AbilityWheel.SpiritSwordRain").Value,
				FormatActiveInformation(
					"AbilityWheel.SpiritSwordRainDescription",
					Xianxia.SpiritSwordRainKeybind),
				swordRainUnlocked
					? FormatKeyBadge(
						Xianxia.SpiritSwordRainKeybind)
					: locked,
				false,
				swordRainUnlocked,
				false,
				ModContent.ItemType<SpiritSwordRainManual>(),
				string.Empty
			),
			new(
				AbilityWheelId.SectProtectionFormation,
				Mod.GetLocalization(
					"AbilityWheel.SectProtectionFormation").Value,
				FormatActiveInformation(
					"AbilityWheel.SectProtectionFormationDescription",
					Xianxia.SectFormationKeybind,
					formationActive),
				formationUnlocked
					? FormatKeyBadge(
						Xianxia.SectFormationKeybind,
						formationActive)
					: locked,
				false,
				formationUnlocked,
				formationActive,
				ModContent.ItemType<
					SectProtectionFormationManual>(),
				string.Empty
			)
		];
	}

	private string FormatActiveInformation(string descriptionKey, ModKeybind keybind, bool showState = false)
	{
		return Mod.GetLocalization(descriptionKey).Value + " | " + FormatActiveKey(keybind, showState);
	}

	private string FormatActiveKey(ModKeybind keybind, bool showState = false)
	{
		List<string> assignedKeys = keybind.GetAssignedKeys();
		string keyText = assignedKeys.Count > 0
			? string.Join(" / ", assignedKeys)
			: Mod.GetLocalization("AbilityWheel.Unbound").Value;
		string result = Mod.GetLocalization("AbilityWheel.ActiveKey").Format(keyText);
		if (showState)
		{
			result += " - " + Mod.GetLocalization("Abilities.StateEnabled").Value;
		}

		return result;
	}

	private string FormatKeyBadge(ModKeybind keybind, bool enabled = false)
	{
		List<string> assignedKeys = keybind.GetAssignedKeys();
		string keyText = assignedKeys.Count > 0
			? string.Join("/", assignedKeys)
			: Mod.GetLocalization("AbilityWheel.Unbound").Value;
		return enabled ? $"ON  [{keyText}]" : $"[{keyText}]";
	}

	private void HandleAbilityWheelClick(
		CultivationPlayer cultivation,
		int hoveredSegment,
		AbilityWheelEntry entry)
	{
		if (!Main.mouseLeft || !Main.mouseLeftRelease)
		{
			return;
		}

		Main.mouseLeftRelease = false;
		if (entry.Id == AbilityWheelId.ToggleMenu)
		{
			toggleWheelExpanded = !toggleWheelExpanded;
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}
		if (!entry.IsUnlocked
			|| !cultivation.TrySelectActiveTechniqueSlot(
				hoveredSegment))
		{
			SoundEngine.PlaySound(SoundID.MenuClose);
			return;
		}
		SoundEngine.PlaySound(SoundID.MenuTick);
		Main.NewText(Mod.GetLocalization(
			"TechniqueLoadout.SelectedMessage").Format(
				entry.Name), Color.Cyan);
	}

	private void HandleToggleWheelClick(
		CultivationPlayer cultivation, AbilityWheelEntry entry)
	{
		if (!Main.mouseLeft || !Main.mouseLeftRelease)
			return;
		Main.mouseLeftRelease = false;
		CultivationAbility ability = GetWheelAbility(entry.Id);
		bool toggled =
			cultivation.TryToggleTechniqueFromWheel(ability);
		SoundEngine.PlaySound(toggled
			? SoundID.MenuTick : SoundID.MenuClose);
	}

	private static int GetHoveredSegment(
		Vector2 mouseOffset, float mouseDistance, int segmentCount)
	{
		if (mouseDistance < WheelInnerRadius || mouseDistance > WheelOuterRadius)
		{
			return -1;
		}

		if (segmentCount <= 0)
			return -1;
		float segmentAngle = MathHelper.TwoPi / segmentCount;
		float angle = MathF.Atan2(mouseOffset.Y, mouseOffset.X);
		float normalizedAngle = (angle - WheelStartAngle + segmentAngle * 0.5f + MathHelper.TwoPi)
			% MathHelper.TwoPi;
		return (int)(normalizedAngle / segmentAngle) % segmentCount;
	}

	private static int GetHoveredToggleSegment(
		Vector2 mouseOffset, float mouseDistance, int segmentCount)
	{
		if (segmentCount <= 0
			|| mouseDistance < ToggleWheelInnerRadius
			|| mouseDistance > ToggleWheelOuterRadius)
		{
			return -1;
		}
		float angle = MathF.Atan2(mouseOffset.Y, mouseOffset.X);
		if (angle < 0f)
			angle += MathHelper.TwoPi;
		if (angle < MathHelper.Pi)
			return -1;
		float segmentAngle = MathHelper.Pi / segmentCount;
		return Math.Clamp(
			(int)((angle - MathHelper.Pi) / segmentAngle),
			0, segmentCount - 1);
	}

	private void DrawToggleSubWheel(
		Texture2D pixel, Vector2 center,
		AbilityWheelEntry[] entries, int hovered)
	{
		if (entries.Length == 0)
		{
			DrawCenteredText(
				Mod.GetLocalization(
					"TechniqueLoadout.NoToggles").Value,
				center - new Vector2(0f, WheelOuterRadius + 35f),
				Color.Gray, 0.58f);
			return;
		}
		float segmentAngle = MathHelper.Pi / entries.Length;
		for (int i = 0; i < entries.Length; i++)
		{
			float centerAngle = MathHelper.Pi
				+ (i + 0.5f) * segmentAngle;
			Color color = GetSegmentColor(
				entries[i], hovered == i, entries[i].IsEnabled);
			DrawAnnularSector(pixel, center, centerAngle,
				segmentAngle * 0.5f - 0.012f,
				ToggleWheelInnerRadius, ToggleWheelOuterRadius,
				color);
			Vector2 labelPosition = center
				+ centerAngle.ToRotationVector2() * 267f;
			DrawAbilityIcon(labelPosition - new Vector2(0f, 20f),
				entries[i], 27f);
			DrawCenteredTextFitted(entries[i].Name,
				labelPosition + new Vector2(0f, 7f),
				92f, Color.White, 0.53f);
			DrawBadge(labelPosition + new Vector2(0f, 27f),
				entries[i].BadgeText, true, entries[i].IsEnabled);
		}
	}

	private static void DrawAnnularSector(
		Texture2D pixel, Vector2 center, float centerAngle,
		float halfAngle, float innerRadius, float outerRadius,
		Color color)
	{
		const int rays = 20;
		float radialLength = outerRadius - innerRadius;
		float middleRadius = (outerRadius + innerRadius) * 0.5f;
		float rayThickness =
			outerRadius * halfAngle * 2f / rays + 2f;
		for (int i = 0; i <= rays; i++)
		{
			float angle = MathHelper.Lerp(
				centerAngle - halfAngle,
				centerAngle + halfAngle, i / (float)rays);
			Vector2 position =
				center + angle.ToRotationVector2() * middleRadius;
			Main.spriteBatch.Draw(pixel, position,
				new Rectangle(0, 0, 1, 1), color, angle,
				new Vector2(0.5f, 0.5f),
				new Vector2(radialLength, rayThickness),
				SpriteEffects.None, 0f);
		}
	}

	private static Color GetSegmentColor(
		AbilityWheelEntry entry, bool hovered, bool selected)
	{
		if (!entry.IsUnlocked)
		{
			return hovered ? new Color(75, 75, 85, 235) : new Color(40, 42, 50, 220);
		}

		Color baseColor = entry.IsPassive
			? (entry.IsEnabled ? new Color(35, 155, 145) : new Color(105, 55, 135))
			: (entry.IsEnabled ? new Color(40, 150, 205) : new Color(45, 85, 125));
		if (selected)
			baseColor = Color.Lerp(baseColor, Color.Gold, 0.48f);
		return hovered ? Color.Lerp(baseColor, Color.White, 0.28f) : baseColor;
	}

	private static void DrawWheelSector(
		Texture2D pixel, Vector2 center, float centerAngle,
		Color color, int segmentCount)
	{
		const int rays = 24;
		float halfSegmentAngle = MathHelper.Pi / Math.Max(1, segmentCount);
		float startAngle = centerAngle - halfSegmentAngle + 0.018f;
		float endAngle = centerAngle + halfSegmentAngle - 0.018f;
		float radialLength = WheelOuterRadius - WheelInnerRadius;
		float middleRadius = (WheelOuterRadius + WheelInnerRadius) * 0.5f;
		float rayThickness = WheelOuterRadius * (endAngle - startAngle) / rays + 2f;

		for (int i = 0; i <= rays; i++)
		{
			float angle = MathHelper.Lerp(startAngle, endAngle, i / (float)rays);
			Vector2 position = center + angle.ToRotationVector2() * middleRadius;
			Main.spriteBatch.Draw(pixel, position, new Rectangle(0, 0, 1, 1), color, angle,
				new Vector2(0.5f, 0.5f), new Vector2(radialLength, rayThickness), SpriteEffects.None, 0f);
		}
	}

	private static void DrawAbilityLabel(Vector2 center, float angle, AbilityWheelEntry entry)
	{
		Vector2 position = center + angle.ToRotationVector2() * 142f;
		Color nameColor = entry.IsUnlocked ? Color.White : Color.Gray;
		DrawAbilityIcon(position - new Vector2(0f, 27f), entry, 31f);
		DrawCenteredText(entry.Name, position + new Vector2(0f, 3f), nameColor, 0.68f);
		DrawBadge(position + new Vector2(0f, 25f), entry.BadgeText,
			entry.IsUnlocked, entry.IsEnabled);
	}

	private static void DrawAbilityIcon(Vector2 center, AbilityWheelEntry entry, float maximumSize)
	{
		if (entry.Id == AbilityWheelId.Empty)
			return;
		Texture2D texture;
		if (!string.IsNullOrEmpty(entry.IconTexturePath))
		{
			texture = ModContent.Request<Texture2D>(entry.IconTexturePath).Value;
		}
		else
		{
			Main.instance.LoadItem(entry.IconItemType);
			texture = TextureAssets.Item[entry.IconItemType].Value;
		}

		float scale = Math.Min(maximumSize / texture.Width, maximumSize / texture.Height);
		Color color = entry.IsUnlocked ? Color.White : new Color(90, 90, 100, 190);
		Main.spriteBatch.Draw(texture, center, null, color, 0f,
			texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
	}

	private static void DrawBadge(Vector2 center, string text, bool unlocked, bool enabled)
	{
		float textScale = 0.52f;
		Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * textScale;
		int width = Math.Max(46, (int)textSize.X + 14);
		Rectangle outer = new((int)(center.X - width * 0.5f), (int)(center.Y - 10f), width, 20);
		Color border = !unlocked
			? new Color(80, 80, 90, 220)
			: (enabled ? new Color(95, 245, 218) : new Color(130, 105, 175));
		Color fill = !unlocked
			? new Color(28, 30, 37, 235)
			: (enabled ? new Color(18, 85, 80, 240) : new Color(25, 28, 43, 240));
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, outer, border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(outer.X + 2, outer.Y + 2, outer.Width - 4, outer.Height - 4), fill);
		DrawCenteredText(text, center, unlocked ? Color.White : Color.Gray, textScale);
	}

	private static void DrawWheelDetailPanel(Texture2D pixel, Vector2 center, AbilityWheelEntry entry)
	{
		const int width = 390;
		const int height = 42;
		Rectangle outer = new((int)center.X - width / 2, (int)center.Y - height / 2, width, height);
		Color border = entry.IsUnlocked ? new Color(91, 214, 226) : new Color(85, 85, 95);
		Main.spriteBatch.Draw(pixel, outer, border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(outer.X + 2, outer.Y + 2, outer.Width - 4, outer.Height - 4),
			new Color(11, 15, 27, 245));
		DrawCenteredText(entry.Information, center,
			entry.IsUnlocked ? Color.LightCyan : Color.Gray, 0.62f);
	}

	private static void DrawFilledCircle(Texture2D pixel, Vector2 center, float radius, Color color)
	{
		const int rays = 48;
		float thickness = MathHelper.TwoPi * radius / rays + 2f;
		for (int i = 0; i < rays; i++)
		{
			float angle = MathHelper.TwoPi * i / rays;
			Vector2 position = center + angle.ToRotationVector2() * radius * 0.5f;
			Main.spriteBatch.Draw(pixel, position, new Rectangle(0, 0, 1, 1), color, angle,
				new Vector2(0.5f, 0.5f), new Vector2(radius, thickness), SpriteEffects.None, 0f);
		}
	}

	private static void DrawCircleOutline(Texture2D pixel, Vector2 center, float radius, Color color, float thickness)
	{
		const int segments = 72;
		Vector2 previous = center + new Vector2(radius, 0f);
		for (int i = 1; i <= segments; i++)
		{
			float angle = MathHelper.TwoPi * i / segments;
			Vector2 next = center + angle.ToRotationVector2() * radius;
			DrawLine(pixel, previous, next, color, thickness);
			previous = next;
		}
	}

	private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
	{
		Vector2 difference = end - start;
		Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), color, difference.ToRotation(),
			new Vector2(0f, 0.5f), new Vector2(difference.Length(), thickness), SpriteEffects.None, 0f);
	}

	private static void DrawCenteredText(string text, Vector2 center, Color color, float scale)
	{
		Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
		Utils.DrawBorderString(Main.spriteBatch, text, center - size * 0.5f, color, scale);
	}

	private static void DrawCenteredTextFitted(
		string text,
		Vector2 center,
		float maximumWidth,
		Color color,
		float desiredScale)
	{
		float textWidth = FontAssets.MouseText.Value.MeasureString(text).X;
		float scale = textWidth > 0f
			? Math.Min(desiredScale, maximumWidth / textWidth)
			: desiredScale;
		DrawCenteredText(text, center, color, scale);
	}
}
