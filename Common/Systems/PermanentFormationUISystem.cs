using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Xianxia.Common.Players;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.TileEntities;

namespace Xianxia.Common.Systems;

public sealed class PermanentFormationUISystem : ModSystem
{
	private static int openCoreId = -1;
	private static bool showUpgradePage;
	private static bool confirmTierUpgrade;

	private readonly record struct ModeCard(
		PermanentFormationKind Kind,
		string NameKey,
		string DescriptionKey,
		int UnlockTier,
		int Upkeep,
		Color Color);

	private static readonly ModeCard[] Modes =
	[
		new(PermanentFormationKind.Protection, "Protection", "ProtectionDescription",
			0, 1, new Color(80, 235, 225)),
		new(PermanentFormationKind.SpiritGathering, "SpiritGathering",
			"SpiritGatheringDescription", 1, 2, new Color(100, 255, 145)),
		new(PermanentFormationKind.Suppression, "Suppression",
			"SuppressionDescription", 2, 2, new Color(195, 105, 255)),
		new(PermanentFormationKind.Restoration, "Restoration",
			"RestorationDescription", 3, 3, new Color(255, 210, 95))
	];

	public static void Open(int entityId)
	{
		openCoreId = entityId;
		showUpgradePage = false;
		confirmTierUpgrade = false;
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close()
	{
		if (openCoreId < 0)
			return;
		openCoreId = -1;
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	public override void OnWorldUnload()
	{
		openCoreId = -1;
		showUpgradePage = false;
		confirmTierUpgrade = false;
	}

	public override void PostUpdateInput()
	{
		if (openCoreId >= 0
			&& Main.keyState.IsKeyDown(Keys.Escape)
			&& Main.oldKeyState.IsKeyUp(Keys.Escape))
			Close();
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		LegacyGameInterfaceLayer layer = new(
			"Xianxia: Permanent Formation Control",
			DrawPanel,
			InterfaceScaleType.UI);
		if (mouseTextIndex >= 0)
			layers.Insert(mouseTextIndex, layer);
		else
			layers.Add(layer);
	}

	private bool DrawPanel()
	{
		if (openCoreId < 0 || Main.gameMenu
			|| Main.LocalPlayer is not { active: true } player
			|| !TileEntity.ByID.TryGetValue(openCoreId, out TileEntity entity)
			|| entity is not PermanentFormationCoreEntity core
			|| Vector2.DistanceSquared(player.Center, core.WorldCenter)
				> 14f * 16f * 14f * 16f)
		{
			openCoreId = -1;
			return true;
		}

		int panelWidth = Math.Min(760, Main.screenWidth - 30);
		int panelHeight = Math.Min(600, Main.screenHeight - 30);
		Rectangle panel = new(
			(Main.screenWidth - panelWidth) / 2,
			(Main.screenHeight - panelHeight) / 2,
			panelWidth,
			panelHeight);
		Point mouse = Main.MouseScreen.ToPoint();
		if (panel.Contains(mouse))
			player.mouseInterface = true;

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
			new Color(3, 7, 17, 180));
		DrawFrame(pixel, panel, new Color(8, 17, 31, 248), new Color(70, 45, 105), 4);

		DrawCentered(Local("Title"), panel.Center.X, panel.Y + 15,
			new Color(235, 245, 255), 1.05f);
		Rectangle closeButton = new(panel.Right - 45, panel.Y + 12, 28, 28);
		DrawButton(pixel, closeButton, "X", closeButton.Contains(mouse),
			new Color(115, 45, 65));

		Rectangle formationsTab = new(panel.Center.X - 210, panel.Y + 50, 200, 34);
		Rectangle upgradeTab = new(panel.Center.X + 10, panel.Y + 50, 200, 34);
		DrawButton(pixel, formationsTab, Local("FormationsTab"),
			formationsTab.Contains(mouse), showUpgradePage
				? new Color(28, 42, 62) : new Color(24, 115, 108));
		DrawButton(pixel, upgradeTab, Local("UpgradeTab"),
			upgradeTab.Contains(mouse), showUpgradePage
				? new Color(125, 88, 32) : new Color(28, 42, 62));

		int contentX = panel.X + 22;
		int contentWidth = panel.Width - 44;
		Rectangle summary = new(contentX, panel.Y + 94, contentWidth, 92);
		DrawSummary(pixel, summary, core);

		if (showUpgradePage)
			DrawUpgradePage(pixel, panel, summary, core, player, mouse);
		else
			DrawFormationPage(pixel, panel, summary, core, player, mouse);

		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (closeButton.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				Close();
			}
			else if (formationsTab.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				showUpgradePage = false;
				confirmTierUpgrade = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (upgradeTab.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				showUpgradePage = true;
				confirmTierUpgrade = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
		}
		return true;
	}

	private static void DrawSummary(Texture2D pixel, Rectangle summary,
		PermanentFormationCoreEntity core)
	{
		DrawFrame(pixel, summary, new Color(15, 31, 49, 245),
			core.Active ? new Color(50, 195, 175) : new Color(95, 95, 115), 2);
		string powerState = Local(core.Active ? "Active" : "Inactive");
		Color powerColor = core.Active ? new Color(95, 255, 190) : new Color(180, 185, 200);
		Utils.DrawBorderString(Main.spriteBatch,
			Local("CoreState", powerState, core.Tier, core.Stage),
			new Vector2(summary.X + 14, summary.Y + 10), powerColor, 0.85f);
		Utils.DrawBorderString(Main.spriteBatch,
			Local("Slots", core.ActiveFormationModeCount, core.MaxActiveFormationModes,
				core.LinkedFlagCount, core.MaximumRelayFlags),
			new Vector2(summary.Right - 185, summary.Y + 10),
			new Color(220, 225, 245), 0.8f);
		DrawMeter(pixel, new Rectangle(summary.X + 14, summary.Y + 42,
			(summary.Width - 42) / 2, 16), core.StoredQi, core.MaximumStoredQi,
			new Color(45, 215, 235),
			Local("Qi", core.StoredQi, core.MaximumStoredQi));
		DrawMeter(pixel, new Rectangle(summary.Center.X + 7, summary.Y + 42,
			(summary.Width - 42) / 2, 16), core.Integrity, core.MaximumIntegrity,
			new Color(115, 235, 155),
			Local("Integrity", core.Integrity, core.MaximumIntegrity));
		string supplyText = core.ConnectedToSpiritVein
			? Local("VeinSupply", core.SpiritualQiConcentrationLevel,
				core.VeinQiGenerationPerSecond, core.QiUpkeepPerSecond,
				core.RepairQiPerSecond, core.NetworkSpiritCrystalCount)
			: Local("StoredSupply", core.QiUpkeepPerSecond,
				core.RepairQiPerSecond);
		Utils.DrawBorderString(Main.spriteBatch, supplyText,
			new Vector2(summary.X + 14, summary.Bottom - 23),
			core.ConnectedToSpiritVein
				? new Color(105, 255, 175) : new Color(235, 215, 155), 0.62f);
		string owner = string.IsNullOrWhiteSpace(core.OwnerName)
			? Local("Unclaimed") : core.OwnerName;
		string ownerText = Local("Owner", owner);
		Vector2 ownerSize = FontAssets.MouseText.Value.MeasureString(ownerText) * 0.62f;
		Utils.DrawBorderString(Main.spriteBatch, ownerText,
			new Vector2(summary.Right - ownerSize.X - 14, summary.Bottom - 21),
			new Color(185, 200, 225), 0.62f);
	}

	private static void DrawFormationPage(Texture2D pixel, Rectangle panel,
		Rectangle summary, PermanentFormationCoreEntity core, Player player, Point mouse)
	{
		int contentX = panel.X + 22;
		int contentWidth = panel.Width - 44;
		int cardsTop = summary.Bottom + 10;
		int gap = 8;
		int cardWidth = (contentWidth - gap) / 2;
		int cardHeight = 108;
		Rectangle[] cards = new Rectangle[Modes.Length];
		for (int index = 0; index < Modes.Length; index++)
		{
			ModeCard mode = Modes[index];
			int column = index % 2;
			int row = index / 2;
			Rectangle card = new(contentX + column * (cardWidth + gap),
				cardsTop + row * (cardHeight + gap), cardWidth, cardHeight);
			cards[index] = card;
			bool unlocked = core.Tier >= mode.UnlockTier;
			bool enabled = core.IsModeEnabled(mode.Kind);
			bool hovered = card.Contains(mouse) && unlocked;
			Color border = enabled ? mode.Color
				: hovered ? Color.Lerp(mode.Color, Color.White, 0.25f)
				: new Color(68, 76, 94);
			Color background = enabled
				? Color.Lerp(new Color(14, 27, 43), mode.Color, 0.12f)
				: new Color(14, 25, 40);
			if (!unlocked)
				background = new Color(19, 21, 29);
			DrawFrame(pixel, card, background, border, enabled ? 3 : 2);
			Utils.DrawBorderString(Main.spriteBatch,
				Language.GetTextValue($"Mods.Xianxia.PermanentFormation.Types.{mode.NameKey}"),
				new Vector2(card.X + 12, card.Y + 9),
				unlocked ? mode.Color : new Color(105, 108, 120), 0.84f);
			string state = !unlocked
				? Local("LockedTier", mode.UnlockTier)
				: Local(enabled ? "Enabled" : "Disabled");
			Vector2 stateSize = FontAssets.MouseText.Value.MeasureString(state) * 0.68f;
			Utils.DrawBorderString(Main.spriteBatch, state,
				new Vector2(card.Right - stateSize.X - 12, card.Y + 11),
				enabled ? mode.Color : new Color(165, 170, 185), 0.68f);
			Utils.DrawBorderString(Main.spriteBatch, Local(mode.DescriptionKey),
				new Vector2(card.X + 12, card.Y + 40),
				unlocked ? new Color(220, 225, 238) : new Color(100, 103, 112),
				0.66f);
			Utils.DrawBorderString(Main.spriteBatch, Local("ModeCost", mode.Upkeep),
				new Vector2(card.X + 12, card.Bottom - 26),
				unlocked ? new Color(245, 210, 120) : new Color(100, 103, 112),
				0.66f);
		}

		Rectangle accessButton = new(panel.Center.X - 230, panel.Bottom - 48, 210, 32);
		Rectangle powerButton = new(panel.Center.X + 20, panel.Bottom - 48, 210, 32);
		DrawButton(pixel, accessButton,
			Local("AccessButton",
				Language.GetTextValue(
					$"Mods.Xianxia.PermanentFormation.Access.{core.AccessMode}")),
			accessButton.Contains(mouse), core.OwnerName == player.name
				? new Color(44, 82, 118) : new Color(48, 52, 64));
		DrawButton(pixel, powerButton, Local(core.Active ? "TurnOff" : "TurnOn"),
			powerButton.Contains(mouse),
			core.Active ? new Color(105, 48, 70) : new Color(30, 125, 105));
		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (accessButton.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				SendAction(core, cycleAccess: true);
			}
			else if (powerButton.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				SendAction(core, togglePower: true);
			}
			else
			{
				for (int index = 0; index < cards.Length; index++)
				{
					if (!cards[index].Contains(mouse) || core.Tier < Modes[index].UnlockTier)
						continue;
					Main.mouseLeftRelease = false;
					SendAction(core, toggleMode: true, kind: Modes[index].Kind);
					break;
				}
			}
		}
	}

	private static void DrawUpgradePage(Texture2D pixel, Rectangle panel,
		Rectangle summary, PermanentFormationCoreEntity core, Player player, Point mouse)
	{
		Rectangle area = new(panel.X + 22, summary.Bottom + 10,
			panel.Width - 44, panel.Bottom - summary.Bottom - 26);
		DrawFrame(pixel, area, new Color(12, 25, 40, 245),
			new Color(125, 91, 42), 2);
		if (core.IsMaximumRank)
		{
			DrawCentered(Local("MaximumRank"), area.Center.X, area.Y + 45,
				new Color(255, 220, 115), 0.95f);
			DrawCentered(Local("MaximumRankDescription"), area.Center.X, area.Y + 88,
				new Color(210, 220, 235), 0.72f);
			return;
		}

		FormationCoreUpgradeCost cost = core.GetNextUpgradeCost();
		AlchemyPlayer names = player.GetModPlayer<AlchemyPlayer>();
		string currentRank = Local("Rank", core.Tier, names.GetStageName(core.Stage));
		string nextRank = Local("Rank", core.NextTier, names.GetStageName(core.NextStage));
		DrawCentered(Local("UpgradeTransition", currentRank, nextRank),
			area.Center.X, area.Y + 16, new Color(255, 220, 125), 0.9f);
		int nextQi = 10000 + core.NextTier * 10000 + core.NextStage * 2500;
		int nextIntegrity = 5000 + core.NextTier * 4000 + core.NextStage * 1000;
		int nextRadius = (int)Math.Min(80f,
			40f + core.NextTier * 15f + core.NextStage * 5f);
		int currentRadius = (int)MathF.Round(core.RadiusPixels / 16f);
		int nextRepair = 25 + core.NextTier * 20 + core.NextStage * 5;
		int nextSlots = core.NextTier switch
		{
			0 => 1,
			1 or 2 => 2,
			3 => 3,
			_ => 4
		};
		int nextRelays = core.NextTier switch
		{
			0 => 0,
			1 => 1,
			2 => 2,
			3 => 4,
			_ => 6
		};
		int currentProtection = (int)MathF.Round(MathHelper.Clamp(
			0.55f + core.Tier * 0.04f + core.Stage * 0.01f, 0.55f, 0.80f) * 100f);
		int nextProtection = (int)MathF.Round(MathHelper.Clamp(
			0.55f + core.NextTier * 0.04f + core.NextStage * 0.01f,
			0.55f, 0.80f) * 100f);
		DrawCentered(Local("UpgradeStatsPrimary",
				core.MaximumStoredQi, nextQi, core.MaximumIntegrity, nextIntegrity),
			area.Center.X, area.Y + 49, new Color(125, 235, 215), 0.67f);
		DrawCentered(Local("UpgradeStatsSecondary",
				currentRadius, nextRadius, core.MaximumRepairPerSecond, nextRepair),
			area.Center.X, area.Y + 72, new Color(150, 220, 245), 0.63f);
		DrawCentered(Local("UpgradeStatsUnlocks",
				core.MaxActiveFormationModes, nextSlots,
				core.MaximumRelayFlags, nextRelays,
				currentProtection, nextProtection),
			area.Center.X, area.Y + 94, new Color(205, 185, 255), 0.61f);
		DrawCentered(Local("RequiredMaterials"), area.Center.X, area.Y + 121,
			Color.White, 0.78f);

		int costY = area.Y + 146;
		int costCount = cost.SpecialItemCount > 0 ? 4 : 3;
		int costWidth = costCount == 4 ? 155 : 190;
		int costGap = costCount == 4 ? 10 : 18;
		int firstX = area.Center.X
			- (costWidth * costCount + costGap * (costCount - 1)) / 2;
		DrawMaterialCost(pixel, new Rectangle(firstX, costY, costWidth, 74),
			ModContent.ItemType<SpiritStone>(), cost.SpiritStones, player, mouse);
		DrawMaterialCost(pixel,
			new Rectangle(firstX + costWidth + costGap, costY, costWidth, 74),
			ModContent.ItemType<ProfoundIronBar>(), cost.ProfoundIronBars, player, mouse);
		DrawMaterialCost(pixel,
			new Rectangle(firstX + (costWidth + costGap) * 2, costY, costWidth, 74),
			ModContent.ItemType<SpiritJadeBar>(), cost.SpiritJadeBars, player, mouse);
		if (cost.SpecialItemCount > 0)
			DrawMaterialCost(pixel,
				new Rectangle(firstX + (costWidth + costGap) * 3,
					costY, costWidth, 74),
				cost.SpecialItemType, cost.SpecialItemCount, player, mouse);

		FormationPathPlayer path = player.GetModPlayer<FormationPathPlayer>();
		bool pathReady = path.RankIndex >= core.NextRankIndex;
		bool owner = string.IsNullOrWhiteSpace(core.OwnerName)
			|| core.OwnerName == player.name;
		bool materialsReady =
			PermanentFormationCoreEntity.CountInventoryItem(player,
				ModContent.ItemType<SpiritStone>()) >= cost.SpiritStones
			&& PermanentFormationCoreEntity.CountInventoryItem(player,
				ModContent.ItemType<ProfoundIronBar>()) >= cost.ProfoundIronBars
			&& PermanentFormationCoreEntity.CountInventoryItem(player,
				ModContent.ItemType<SpiritJadeBar>()) >= cost.SpiritJadeBars
			&& (cost.SpecialItemCount <= 0
				|| PermanentFormationCoreEntity.CountInventoryItem(player,
					cost.SpecialItemType) >= cost.SpecialItemCount);
		string requirement = !owner
			? Local("OwnerRequired")
			: !pathReady
				? Local("PathRequired", core.NextTier, names.GetStageName(core.NextStage))
				: !materialsReady ? Local("MaterialsMissing") : Local("Ready");
		DrawCentered(requirement, area.Center.X, costY + 82,
			owner && pathReady && materialsReady
				? new Color(105, 255, 165) : new Color(255, 145, 105), 0.72f);

		Rectangle upgradeButton = new(area.Center.X - 125, area.Bottom - 52, 250, 36);
		DrawButton(pixel, upgradeButton, Local("UpgradeButton"),
			upgradeButton.Contains(mouse), owner && pathReady && materialsReady
				? new Color(135, 92, 28) : new Color(55, 55, 66));
		if (!confirmTierUpgrade && Main.mouseLeft && Main.mouseLeftRelease
			&& upgradeButton.Contains(mouse))
		{
			Main.mouseLeftRelease = false;
			if (core.NextStage == 0 && core.NextTier > core.Tier)
				confirmTierUpgrade = true;
			else
				SendAction(core, upgrade: true);
		}
		if (confirmTierUpgrade)
			DrawTierConfirmation(pixel, area, core, mouse);
	}

	private static void DrawTierConfirmation(Texture2D pixel, Rectangle area,
		PermanentFormationCoreEntity core, Point mouse)
	{
		Rectangle modal = new(area.Center.X - 265, area.Center.Y - 92, 530, 184);
		DrawFrame(pixel, modal, new Color(12, 18, 32, 252),
			new Color(255, 185, 70), 3);
		DrawCentered(Local("TierConfirmationTitle", core.NextTier),
			modal.Center.X, modal.Y + 25, new Color(255, 220, 125), 0.95f);
		DrawCentered(Local("TierConfirmationWarning"),
			modal.Center.X, modal.Y + 67, new Color(230, 225, 235), 0.68f);
		Rectangle confirm = new(modal.Center.X - 180, modal.Bottom - 55, 160, 36);
		Rectangle cancel = new(modal.Center.X + 20, modal.Bottom - 55, 160, 36);
		DrawButton(pixel, confirm, Local("ConfirmUpgrade"), confirm.Contains(mouse),
			new Color(135, 92, 28));
		DrawButton(pixel, cancel, Local("CancelUpgrade"), cancel.Contains(mouse),
			new Color(85, 55, 70));
		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (confirm.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				confirmTierUpgrade = false;
				SendAction(core, upgrade: true);
			}
			else if (cancel.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				confirmTierUpgrade = false;
				SoundEngine.PlaySound(SoundID.MenuClose);
			}
		}
	}

	private static void DrawMaterialCost(Texture2D pixel, Rectangle rectangle,
		int itemType, int required, Player player, Point mouse)
	{
		int owned = PermanentFormationCoreEntity.CountInventoryItem(player, itemType);
		bool enough = owned >= required;
		DrawFrame(pixel, rectangle, new Color(15, 30, 47),
			enough ? new Color(75, 190, 135) : new Color(175, 75, 75), 2);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = Main.itemAnimations[itemType]?.GetFrame(texture) ?? texture.Frame();
		float scale = Math.Min(40f / frame.Width, 40f / frame.Height);
		Main.spriteBatch.Draw(texture, new Vector2(rectangle.X + 34, rectangle.Center.Y),
			frame, Color.White, 0f, frame.Size() * 0.5f, scale,
			SpriteEffects.None, 0f);
		Item sample = ContentSamples.ItemsByType[itemType];
		Utils.DrawBorderString(Main.spriteBatch, sample.Name,
			new Vector2(rectangle.X + 62, rectangle.Y + 12), Color.White, 0.62f);
		Utils.DrawBorderString(Main.spriteBatch, $"{owned}/{required}",
			new Vector2(rectangle.X + 62, rectangle.Y + 39),
			enough ? new Color(115, 255, 170) : new Color(255, 130, 120), 0.68f);
		if (rectangle.Contains(mouse))
			Main.instance.MouseText(sample.Name);
	}

	private static void SendAction(PermanentFormationCoreEntity core,
		bool togglePower = false, bool toggleMode = false, bool upgrade = false,
		bool cycleAccess = false,
		PermanentFormationKind kind = PermanentFormationKind.Protection)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			Xianxia.SendPermanentFormationAction(core.Position.X, core.Position.Y,
				false, togglePower, false, toggleMode, upgrade, cycleAccess,
				toggleMode ? (byte)kind : byte.MaxValue);
		else
			core.HandleInteraction(Main.LocalPlayer, false, togglePower, false,
				toggleMode, upgrade, cycleAccess,
				toggleMode ? (byte)kind : byte.MaxValue);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private static void DrawFrame(Texture2D pixel, Rectangle rectangle,
		Color fill, Color border, int thickness)
	{
		Main.spriteBatch.Draw(pixel, rectangle, fill);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.X, rectangle.Bottom - thickness,
				rectangle.Width, thickness), border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), border);
		Main.spriteBatch.Draw(pixel,
			new Rectangle(rectangle.Right - thickness, rectangle.Y,
				thickness, rectangle.Height), border);
	}

	private static void DrawMeter(Texture2D pixel, Rectangle rectangle,
		int value, int maximum, Color color, string label)
	{
		DrawFrame(pixel, rectangle, new Color(5, 10, 19), new Color(60, 69, 88), 2);
		float ratio = maximum <= 0 ? 0f : MathHelper.Clamp(value / (float)maximum, 0f, 1f);
		Rectangle fill = new(rectangle.X + 3, rectangle.Y + 3,
			(int)((rectangle.Width - 6) * ratio), rectangle.Height - 6);
		Main.spriteBatch.Draw(pixel, fill, color);
		DrawCentered(label, rectangle.Center.X, rectangle.Y - 1, Color.White, 0.62f);
	}

	private static void DrawButton(Texture2D pixel, Rectangle rectangle,
		string text, bool hovered, Color color)
	{
		Color fill = hovered ? Color.Lerp(color, Color.White, 0.15f) : color;
		DrawFrame(pixel, rectangle, fill,
			hovered ? Color.White : new Color(105, 115, 140), 2);
		DrawCentered(text, rectangle.Center.X, rectangle.Y + 7, Color.White, 0.78f);
	}

	private static void DrawCentered(string text, float x, float y,
		Color color, float scale)
	{
		Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
		Utils.DrawBorderString(Main.spriteBatch, text,
			new Vector2(x - size.X * 0.5f, y), color, scale);
	}

	private static string Local(string key, params object[] args)
	{
		return Language.GetTextValue(
			$"Mods.Xianxia.PermanentFormation.UI.{key}", args);
	}
}
