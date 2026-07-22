using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Xianxia.Common.Players;
using Xianxia.Common.Config;
using Xianxia.Content.Buffs;
using Xianxia.Common.Abilities;
using Xianxia.Content.Items.Alchemy;

namespace Xianxia.Common.Systems;

public class CultivationUISystem : ModSystem
{
	private enum AbilityMenuPage
	{
		Abilities,
		Paths
	}

	private static AbilityMenuPage abilityMenuPage;
	private const int BarWidth = 300;
	private const int BarHeight = 22;
	private const int BorderSize = 2;
	private const float WheelInnerRadius = 72f;
	private const float WheelOuterRadius = 205f;
	private const int WheelSegmentCount = 10;
	private const float WheelStartAngle = -MathHelper.PiOver2;

	private enum AbilityWheelId
	{
		QiProtection,
		QiSense,
		QiFlight,
		NascentTeleport,
		SpiritualPressure,
		FlameStep,
		Fireball,
		QiPalm,
		QiResistance,
		NightVision
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
		Rectangle abilitiesTab = new(panel.Center.X - 190, panel.Y + 47, 180, 31);
		Rectangle pathsTab = new(panel.Center.X + 10, panel.Y + 47, 180, 31);
		Point mouse = Main.MouseScreen.ToPoint();
		DrawAbilityMenuTab(pixel, abilitiesTab,
			Mod.GetLocalization("AbilityTree.Tabs.Abilities").Value,
			abilityMenuPage == AbilityMenuPage.Abilities, abilitiesTab.Contains(mouse));
		DrawAbilityMenuTab(pixel, pathsTab,
			Mod.GetLocalization("AbilityTree.Tabs.Paths").Value,
			abilityMenuPage == AbilityMenuPage.Paths, pathsTab.Contains(mouse));
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
		}

		if (abilityMenuPage == AbilityMenuPage.Paths)
		{
			DrawPathPage(pixel, panel, mouse);
			return true;
		}

		CultivationAbility[][] abilityGroups =
		[
			[CultivationAbility.SpiritBreathing, CultivationAbility.Meditation],
			[CultivationAbility.QiSense, CultivationAbility.QiResistance,
				CultivationAbility.Fireball, CultivationAbility.QiPalm],
			[CultivationAbility.QiProtection, CultivationAbility.FlameStep,
				CultivationAbility.NightVision],
			[CultivationAbility.GoldenCoreCirculation, CultivationAbility.QiFlight],
			[CultivationAbility.NascentSoulRegeneration,
				CultivationAbility.NascentTeleport, CultivationAbility.SpiritualPressure]
		];

		CultivationAbility? hovered = null;
		Rectangle listArea = new(panel.X + 14, panel.Y + 84, panel.Width - 28, panel.Height - 168);
		const int realmColumnWidth = 170;
		int rowHeight = listArea.Height / abilityGroups.Length;
		for (int realm = 0; realm < abilityGroups.Length; realm++)
		{
			Rectangle row = new(listArea.X, listArea.Y + realm * rowHeight,
				listArea.Width, rowHeight - 3);
			bool realmUnlocked = cultivation.RealmIndex >= realm;
			Main.spriteBatch.Draw(pixel, row, realm % 2 == 0
				? new Color(18, 29, 45, 245)
				: new Color(14, 24, 39, 245));
			Rectangle realmPanel = new(row.X + 3, row.Y + 3, realmColumnWidth - 7, row.Height - 6);
			Main.spriteBatch.Draw(pixel, realmPanel, realmUnlocked
				? new Color(38, 104, 103, 235)
				: new Color(31, 34, 43, 235));
			string realmName = Mod.GetLocalization(
				$"Cultivation.Realms.{GetRealmLocalizationKey(realm)}").Value;
			DrawCenteredText(realmName, realmPanel.Center.ToVector2(),
				realmUnlocked ? Color.White : Color.Gray, 0.68f);

			CultivationAbility[] abilities = abilityGroups[realm];
			int cardsAreaX = row.X + realmColumnWidth + 6;
			int cardsAreaWidth = row.Right - cardsAreaX - 6;
			int cardGap = 8;
			int cardWidth = Math.Min(184,
				(cardsAreaWidth - cardGap * (abilities.Length - 1)) / abilities.Length);
			for (int i = 0; i < abilities.Length; i++)
			{
				CultivationAbility ability = abilities[i];
				Rectangle card = new(cardsAreaX + i * (cardWidth + cardGap), row.Y + 7,
					cardWidth, row.Height - 14);
				bool unlocked = cultivation.IsAbilityUnlocked(ability);
				bool isHovered = card.Contains(mouse);
				if (isHovered)
					hovered = ability;
				Color border = unlocked
					? (isHovered ? Color.White : new Color(68, 211, 210))
					: (isHovered ? new Color(120, 120, 132) : new Color(67, 69, 79));
				Main.spriteBatch.Draw(pixel, card, border);
				Main.spriteBatch.Draw(pixel,
					new Rectangle(card.X + 3, card.Y + 3, card.Width - 6, card.Height - 6),
					unlocked ? new Color(21, 67, 73) : new Color(24, 27, 34));

				Vector2 iconCenter = new(card.X + 25f, card.Center.Y - 2f);
				DrawTreeAbilityIcon(iconCenter, ability, unlocked, 32f);
				float textCenterX = card.X + 48f + (card.Width - 51f) * 0.5f;
				string name = Mod.GetLocalization($"AbilityTree.Abilities.{ability}.Name").Value;
				DrawCenteredTextFitted(name, new Vector2(textCenterX, card.Y + 18f),
					Math.Max(40f, card.Width - 55f), unlocked ? Color.LightCyan : Color.Gray, 0.56f);
				bool passive = ability is CultivationAbility.QiSense
					or CultivationAbility.QiProtection
					or CultivationAbility.SpiritBreathing
					or CultivationAbility.GoldenCoreCirculation
					or CultivationAbility.NascentSoulRegeneration;
				string abilityType = Mod.GetLocalization(passive
					? "AbilityTree.Passive"
					: "AbilityTree.Active").Value;
				string level = unlocked
					? $"{abilityType}  |  Lv.{cultivation.GetAbilityLevel(ability)}"
					: $"{abilityType}  |  {Mod.GetLocalization("AbilityTree.Locked").Value}";
				DrawCenteredText(level, new Vector2(textCenterX, card.Y + 39f),
					unlocked ? Color.White : Color.Gray, 0.53f);

				Rectangle experienceBar = new(card.X + 48, card.Bottom - 10,
					Math.Max(8, card.Width - 56), 5);
				Main.spriteBatch.Draw(pixel, experienceBar, new Color(10, 15, 24));
				if (unlocked)
				{
					int required = cultivation.GetAbilityExperienceRequired(ability);
					float progress = required <= 0 ? 1f
						: cultivation.GetAbilityExperience(ability) / (float)required;
					Rectangle fill = new(experienceBar.X, experienceBar.Y,
						(int)(experienceBar.Width * MathHelper.Clamp(progress, 0f, 1f)), experienceBar.Height);
					Main.spriteBatch.Draw(pixel, fill, new Color(174, 92, 238));
				}
			}
		}

		Rectangle details = new(panel.X + 18, panel.Bottom - 72, panel.Width - 36, 52);
		Main.spriteBatch.Draw(pixel, details, new Color(18, 27, 43, 245));
		if (hovered.HasValue)
		{
			CultivationAbility ability = hovered.Value;
			bool unlocked = cultivation.IsAbilityUnlocked(ability);
			string text = unlocked
				? GetAbilityTreeDetails(cultivation, ability)
				: Mod.GetLocalization("AbilityTree.RequiresRealm").Format(
					Mod.GetLocalization($"Cultivation.Realms.{GetRealmLocalizationKey(CultivationAbilityInfo.RequiredRealm(ability))}").Value);
			DrawCenteredText(text, details.Center.ToVector2(), unlocked ? Color.White : Color.Gray, 0.66f);
		}
		else
		{
			DrawCenteredText(Mod.GetLocalization("AbilityTree.Hint").Value,
				details.Center.ToVector2(), Color.LightGray, 0.66f);
		}
		return true;
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
			? new Color(24, 91, 91, 245)
			: new Color(20, 25, 39, 245);
		Main.spriteBatch.Draw(pixel, rectangle, border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4),
			background);
		DrawCenteredText(label, rectangle.Center.ToVector2(), selected ? Color.White : Color.LightGray, 0.7f);
	}

	private void DrawPathPage(Texture2D pixel, Rectangle panel, Point mouse)
	{
		AlchemyPlayer alchemy = Main.LocalPlayer.GetModPlayer<AlchemyPlayer>();
		Rectangle content = new(panel.X + 18, panel.Y + 90, panel.Width - 36, panel.Height - 112);
		const int pathListWidth = 260;
		Rectangle listPanel = new(content.X, content.Y, pathListWidth, content.Height);
		Rectangle detailPanel = new(content.X + pathListWidth + 12, content.Y,
			content.Width - pathListWidth - 12, content.Height);
		Main.spriteBatch.Draw(pixel, listPanel, new Color(15, 24, 38, 245));
		Main.spriteBatch.Draw(pixel, detailPanel, new Color(15, 24, 38, 245));

		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.Title").Value,
			new Vector2(listPanel.Center.X, listPanel.Y + 25), Color.White, 0.82f);
		Rectangle alchemyCard = new(listPanel.X + 12, listPanel.Y + 50, listPanel.Width - 24, 76);
		bool hovered = alchemyCard.Contains(mouse);
		Main.spriteBatch.Draw(pixel, alchemyCard,
			hovered ? Color.White : new Color(76, 211, 173));
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
		DrawCenteredText(Mod.GetLocalization("AbilityTree.Paths.FutureHint").Value,
			new Vector2(listPanel.Center.X, listPanel.Bottom - 35), Color.Gray, 0.6f);

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

	private string GetAbilityTreeDetails(CultivationPlayer cultivation, CultivationAbility ability)
	{
		int level = cultivation.GetAbilityLevel(ability);
		int experience = cultivation.GetAbilityExperience(ability);
		int required = cultivation.GetAbilityExperienceRequired(ability);
		string progress = level >= CultivationAbilityInfo.MaxLevel
			? Mod.GetLocalization("AbilityTree.MaxLevel").Value
			: $"EXP {experience}/{required}";
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
			_ => "CombatEffect"
		};
		return Mod.GetLocalization($"AbilityTree.{effectKey}").Format(level, progress);
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

		float qiProgress = cultivation.MaxQi > 0 ? cultivation.Qi / (float)cultivation.MaxQi : 0f;
		float cultivationProgress = GetCultivationProgress(cultivation);
		Rectangle fill = new(background.X, background.Y,
			(int)(background.Width * MathHelper.Clamp(qiProgress, 0f, 1f)), background.Height);
		Rectangle experienceBackground = new(background.X, background.Bottom - 4, background.Width, 4);
		Rectangle experienceFill = new(experienceBackground.X, experienceBackground.Y,
			(int)(experienceBackground.Width * cultivationProgress), experienceBackground.Height);
		Texture2D pixel = TextureAssets.MagicPixel.Value;

		Main.spriteBatch.Draw(pixel, new Rectangle(x - 2, y - 2, barWidth + 4, barHeight + 4), new Color(14, 8, 28, 180));
		Main.spriteBatch.Draw(pixel, border, new Color(103, 65, 142));
		Main.spriteBatch.Draw(pixel, background, new Color(19, 23, 36));
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
			lines.Add((cultivation.GetNextStageBonusSummary(), new Color(150, 235, 205)));

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

		if (cultivation.IsAwaitingTribulationConfirmation)
		{
			return DrawTribulationConfirmation(player, cultivation);
		}

		if (!cultivation.IsAbilityWheelOpen)
		{
			return true;
		}

		player.mouseInterface = true;
		Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
		Vector2 center = screenSize * 0.5f;
		Vector2 mousePosition = new(Main.mouseX, Main.mouseY);
		Vector2 mouseOffset = mousePosition - center;
		float mouseDistance = mouseOffset.Length();
		int hoveredSegment = GetHoveredSegment(mouseOffset, mouseDistance);
		AbilityWheelEntry[] entries = BuildAbilityWheelEntries(player, cultivation);
		Texture2D pixel = TextureAssets.MagicPixel.Value;

		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y),
			new Color(3, 5, 12, 215));

		for (int i = 0; i < WheelSegmentCount; i++)
		{
			AbilityWheelEntry entry = entries[i];
			Color segmentColor = GetSegmentColor(entry, hoveredSegment == i);
			float centerAngle = WheelStartAngle + i * MathHelper.TwoPi / WheelSegmentCount;
			DrawWheelSector(pixel, center, centerAngle, segmentColor);
			DrawAbilityLabel(center, centerAngle, entry);
		}

		DrawFilledCircle(pixel, center, WheelInnerRadius - 5f, new Color(13, 18, 30, 245));
		DrawCircleOutline(pixel, center, WheelInnerRadius - 5f, new Color(112, 235, 245), 2.5f);
		DrawCircleOutline(pixel, center, WheelOuterRadius, new Color(94, 64, 135), 3f);

		string title = Mod.GetLocalization("AbilityWheel.Title").Value;
		DrawCenteredText(title, center - new Vector2(0f, WheelOuterRadius + 34f), Color.White, 1f);

		if (hoveredSegment >= 0)
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

	private bool DrawTribulationConfirmation(Player player, CultivationPlayer cultivation)
	{
		player.mouseInterface = true;
		Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
		Vector2 center = screenSize * 0.5f;
		Vector2 mousePosition = new(Main.mouseX, Main.mouseY);
		Texture2D pixel = TextureAssets.MagicPixel.Value;

		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y),
			new Color(2, 4, 10, 190));

		Rectangle outerPanel = new((int)center.X - 252, (int)center.Y - 142, 504, 284);
		Rectangle panel = new(outerPanel.X + 3, outerPanel.Y + 3, outerPanel.Width - 6, outerPanel.Height - 6);
		Main.spriteBatch.Draw(pixel, outerPanel, new Color(125, 78, 175, 245));
		Main.spriteBatch.Draw(pixel, panel, new Color(13, 17, 30, 250));
		Main.spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 6), new Color(95, 225, 240));

		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Title").Value,
			center - new Vector2(0f, 99f), Color.Gold, 1.05f);
		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Message").Format(
			cultivation.PendingTribulationRealmName), center - new Vector2(0f, 48f), Color.White, 0.78f);
		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Strikes").Format(
			cultivation.PendingTribulationStrikeCount), center - new Vector2(0f, 18f), Color.OrangeRed, 0.75f);
		DrawCenteredText(Mod.GetLocalization("TribulationConfirmation.Warning").Value,
			center + new Vector2(0f, 16f), Color.LightGray, 0.65f);

		Rectangle confirmButton = new((int)center.X - 210, (int)center.Y + 64, 190, 48);
		Rectangle cancelButton = new((int)center.X + 20, (int)center.Y + 64, 190, 48);
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

	private AbilityWheelEntry[] BuildAbilityWheelEntries(Player player, CultivationPlayer cultivation)
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
		if (!entry.IsPassive)
		{
			return;
		}

		if (!entry.IsUnlocked)
		{
			SoundEngine.PlaySound(SoundID.MenuClose);
			string messageKey = entry.Id == AbilityWheelId.QiSense
				? "Abilities.QiSenseRequiresGathering"
				: "Abilities.QiProtectionRequiresFoundation";
			Main.NewText(Mod.GetLocalization(messageKey).Value, Color.OrangeRed);
			return;
		}

		bool enabled;
		string enabledKey;
		string disabledKey;
		if (entry.Id == AbilityWheelId.QiSense)
		{
			enabled = !cultivation.QiSenseEnabled;
			if (!cultivation.SetQiSenseEnabled(enabled))
			{
				SoundEngine.PlaySound(SoundID.MenuClose);
				Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(1), Color.OrangeRed);
				return;
			}

			enabledKey = "Abilities.QiSenseEnabled";
			disabledKey = "Abilities.QiSenseDisabled";
		}
		else
		{
			enabled = !cultivation.QiProtectionEnabled;
			cultivation.SetQiProtectionEnabled(enabled);
			enabledKey = "Abilities.QiProtectionEnabled";
			disabledKey = "Abilities.QiProtectionDisabled";
		}

		SoundEngine.PlaySound(SoundID.MenuTick);
		Main.NewText(Mod.GetLocalization(enabled
			? enabledKey
			: disabledKey).Value,
			enabled ? Color.Cyan : Color.LightGray);
	}

	private static int GetHoveredSegment(Vector2 mouseOffset, float mouseDistance)
	{
		if (mouseDistance < WheelInnerRadius || mouseDistance > WheelOuterRadius)
		{
			return -1;
		}

		float segmentAngle = MathHelper.TwoPi / WheelSegmentCount;
		float angle = MathF.Atan2(mouseOffset.Y, mouseOffset.X);
		float normalizedAngle = (angle - WheelStartAngle + segmentAngle * 0.5f + MathHelper.TwoPi)
			% MathHelper.TwoPi;
		return (int)(normalizedAngle / segmentAngle) % WheelSegmentCount;
	}

	private static Color GetSegmentColor(AbilityWheelEntry entry, bool hovered)
	{
		if (!entry.IsUnlocked)
		{
			return hovered ? new Color(75, 75, 85, 235) : new Color(40, 42, 50, 220);
		}

		Color baseColor = entry.IsPassive
			? (entry.IsEnabled ? new Color(35, 155, 145) : new Color(105, 55, 135))
			: (entry.IsEnabled ? new Color(40, 150, 205) : new Color(45, 85, 125));
		return hovered ? Color.Lerp(baseColor, Color.White, 0.28f) : baseColor;
	}

	private static void DrawWheelSector(Texture2D pixel, Vector2 center, float centerAngle, Color color)
	{
		const int rays = 24;
		float halfSegmentAngle = MathHelper.Pi / WheelSegmentCount;
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
