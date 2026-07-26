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
using Xianxia.Content.TileEntities;

namespace Xianxia.Common.Systems;

public sealed class FormationRelayUISystem : ModSystem
{
	private static int openRelayId = -1;
	private readonly record struct RelayMode(
		PermanentFormationKind Kind, string NameKey, string BoostKey, Color Color);
	private static readonly RelayMode[] Modes =
	[
		new(PermanentFormationKind.Protection, "Protection", "BoostProtection",
			new Color(80, 235, 225)),
		new(PermanentFormationKind.SpiritGathering, "SpiritGathering",
			"BoostGathering", new Color(100, 255, 145)),
		new(PermanentFormationKind.Suppression, "Suppression", "BoostSuppression",
			new Color(195, 105, 255)),
		new(PermanentFormationKind.Restoration, "Restoration", "BoostRestoration",
			new Color(255, 210, 95))
	];

	public static void Open(int entityId)
	{
		openRelayId = entityId;
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close()
	{
		if (openRelayId < 0)
			return;
		openRelayId = -1;
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	public override void OnWorldUnload() => openRelayId = -1;

	public override void PostUpdateInput()
	{
		if (openRelayId >= 0
			&& Main.keyState.IsKeyDown(Keys.Escape)
			&& Main.oldKeyState.IsKeyUp(Keys.Escape))
			Close();
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		LegacyGameInterfaceLayer layer = new(
			"Xianxia: Formation Relay Status", DrawPanel, InterfaceScaleType.UI);
		if (index >= 0)
			layers.Insert(index, layer);
		else
			layers.Add(layer);
	}

	private bool DrawPanel()
	{
		if (openRelayId < 0 || Main.gameMenu
			|| Main.LocalPlayer is not { active: true } player
			|| !TileEntity.ByID.TryGetValue(openRelayId, out TileEntity entity)
			|| entity is not FormationRelayFlagEntity relay
			|| Vector2.DistanceSquared(player.Center, relay.WorldCenter)
				> 14f * 16f * 14f * 16f)
		{
			openRelayId = -1;
			return true;
		}

		int width = Math.Min(620, Main.screenWidth - 30);
		int height = Math.Min(570, Main.screenHeight - 30);
		Rectangle panel = new((Main.screenWidth - width) / 2,
			(Main.screenHeight - height) / 2, width, height);
		Point mouse = Main.MouseScreen.ToPoint();
		if (panel.Contains(mouse))
			player.mouseInterface = true;
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
			new Color(3, 7, 17, 175));
		DrawFrame(pixel, panel, new Color(8, 17, 31, 248),
			new Color(55, 145, 170), 4);
		DrawCentered(Local("Title"), panel.Center.X, panel.Y + 18,
			new Color(135, 245, 255), 1.02f);
		Rectangle close = new(panel.Right - 43, panel.Y + 12, 28, 28);
		DrawButton(pixel, close, "X", close.Contains(mouse));

		bool linked = relay.TryGetLinkedCore(out PermanentFormationCoreEntity core);
		Rectangle linkCard = new(panel.X + 24, panel.Y + 62, panel.Width - 48, 86);
		DrawFrame(pixel, linkCard, new Color(14, 29, 45),
			linked ? new Color(70, 220, 205) : new Color(150, 75, 75), 2);
		DrawCentered(Local(linked ? "Linked" : "Unlinked"), linkCard.Center.X,
			linkCard.Y + 12, linked ? new Color(100, 255, 205)
				: new Color(255, 135, 125), 0.88f);
		if (linked)
		{
			float distance = Vector2.Distance(relay.WorldCenter, core.WorldCenter) / 16f;
			DrawCentered(Local("CoreLink", core.OwnerName, core.Tier, core.Stage),
				linkCard.Center.X, linkCard.Y + 40, Color.White, 0.68f);
			DrawCentered(Local("Distance", (int)MathF.Round(distance),
					(int)MathF.Round(core.RelayLinkRangePixels / 16f)),
				linkCard.Center.X, linkCard.Y + 61,
				new Color(180, 205, 235), 0.64f);
		}
		else
		{
			DrawCentered(Local("UnlinkedHint"), linkCard.Center.X,
				linkCard.Y + 48, new Color(205, 190, 195), 0.66f);
		}

		Rectangle veinCard = new(panel.X + 24, linkCard.Bottom + 12,
			(panel.Width - 60) / 2, 132);
		Rectangle networkCard = new(veinCard.Right + 12, veinCard.Y,
			veinCard.Width, veinCard.Height);
		DrawFrame(pixel, veinCard, new Color(14, 29, 45),
			relay.HasLocalSpiritVein ? new Color(120, 105, 230)
				: new Color(65, 75, 92), 2);
		DrawCentered(Local("LocalVein"), veinCard.Center.X, veinCard.Y + 13,
			new Color(190, 160, 255), 0.78f);
		DrawCentered(Local("Concentration",
				relay.SpiritualQiConcentrationLevel,
				relay.NearbySpiritCrystalCount),
			veinCard.Center.X, veinCard.Y + 47, Color.White, 0.68f);
		DrawCentered(Local("Production", relay.VeinQiGenerationPerSecond),
			veinCard.Center.X, veinCard.Y + 75,
			relay.HasLocalSpiritVein ? new Color(105, 255, 180)
				: new Color(150, 155, 170), 0.72f);
		DrawCentered(Local("ProductionLimit"), veinCard.Center.X,
			veinCard.Y + 102, new Color(170, 180, 205), 0.58f);

		DrawFrame(pixel, networkCard, new Color(14, 29, 45),
			linked ? new Color(70, 180, 165) : new Color(65, 75, 92), 2);
		DrawCentered(Local("Network"), networkCard.Center.X, networkCard.Y + 13,
			new Color(110, 235, 225), 0.78f);
		if (linked)
		{
			DrawMeter(pixel, new Rectangle(networkCard.X + 14,
					networkCard.Y + 44, networkCard.Width - 28, 15),
				core.StoredQi, core.MaximumStoredQi,
				new Color(45, 215, 235),
				Local("SharedQi", core.StoredQi, core.MaximumStoredQi));
			DrawMeter(pixel, new Rectangle(networkCard.X + 14,
					networkCard.Y + 75, networkCard.Width - 28, 15),
				core.Integrity, core.MaximumIntegrity,
				new Color(115, 235, 155),
				Local("SharedIntegrity", core.Integrity, core.MaximumIntegrity));
			DrawCentered(Local("Upkeep", relay.CurrentUpkeepPerSecond,
					relay.TerritoryInUse ? Local("ActiveTerritory")
						: Local("IdleTerritory")),
				networkCard.Center.X, networkCard.Y + 103,
				new Color(245, 210, 120), 0.61f);
		}
		else
			DrawCentered(Local("NoNetwork"), networkCard.Center.X,
				networkCard.Y + 65, new Color(150, 155, 170), 0.65f);

		int modesTop = networkCard.Bottom + 13;
		DrawCentered(Local("SpecializationTitle"), panel.Center.X, modesTop,
			new Color(235, 220, 255), 0.79f);
		Rectangle normalButton = new(panel.X + 24, modesTop + 28,
			panel.Width - 48, 42);
		bool normalSelected = !relay.HasSpecialization;
		DrawFrame(pixel, normalButton,
			normalSelected ? new Color(23, 62, 72) : new Color(14, 26, 40),
			normalButton.Contains(mouse) ? Color.White
				: normalSelected ? new Color(85, 225, 235)
				: new Color(65, 75, 92),
			normalSelected ? 3 : 2);
		DrawCentered(Local("NormalMode"), normalButton.Center.X,
			normalButton.Y + 5, new Color(125, 240, 245), 0.62f);
		DrawCentered(Local(normalSelected ? "Selected" : "SelectNormal"),
			normalButton.Center.X, normalButton.Y + 23,
			normalSelected ? new Color(100, 255, 215)
				: new Color(185, 200, 215), 0.46f);
		int modeGap = 7;
		int modeWidth = (panel.Width - 48 - modeGap * 3) / 4;
		Rectangle[] modeButtons = new Rectangle[Modes.Length];
		for (int i = 0; i < Modes.Length; i++)
		{
			RelayMode mode = Modes[i];
			Rectangle button = new(panel.X + 24 + i * (modeWidth + modeGap),
				modesTop + 80, modeWidth, 78);
			modeButtons[i] = button;
			bool unlocked = linked && core.Tier >= (int)mode.Kind;
			bool coreEnabled = unlocked && core.IsModeEnabled(mode.Kind);
			bool selected = relay.HasSpecialization
				&& relay.SpecializedMode == mode.Kind;
			Color border = selected ? mode.Color : new Color(65, 75, 92);
			Color fill = selected
				? Color.Lerp(new Color(14, 29, 45), mode.Color, 0.16f)
				: new Color(14, 26, 40);
			if (!unlocked)
				fill = new Color(20, 21, 29);
			DrawFrame(pixel, button, fill,
				button.Contains(mouse) && unlocked ? Color.White : border,
				selected ? 3 : 2);
			DrawCentered(Language.GetTextValue(
					$"Mods.Xianxia.PermanentFormation.Types.{mode.NameKey}"),
				button.Center.X, button.Y + 9,
				unlocked ? mode.Color : new Color(105, 108, 120), 0.61f);
			DrawCentered(Local(mode.BoostKey), button.Center.X, button.Y + 36,
				coreEnabled ? new Color(220, 230, 238)
					: new Color(115, 118, 130), 0.48f);
			DrawCentered(Local(!unlocked ? "ModeLocked"
					: coreEnabled ? selected ? "Selected" : "Select"
					: "CoreModeDisabled"),
				button.Center.X, button.Bottom - 19,
				coreEnabled ? mode.Color : new Color(150, 150, 160), 0.48f);
		}

		DrawCentered(Local("Benefit"), panel.Center.X, panel.Bottom - 35,
			new Color(130, 230, 205), 0.65f);
		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			if (close.Contains(mouse))
			{
				Main.mouseLeftRelease = false;
				Close();
			}
			else if (linked)
			{
				if (normalButton.Contains(mouse))
				{
					Main.mouseLeftRelease = false;
					SendSpecialization(relay, -1);
					return true;
				}
				for (int i = 0; i < modeButtons.Length; i++)
				{
					if (!modeButtons[i].Contains(mouse)
						|| core.Tier < (int)Modes[i].Kind)
						continue;
					Main.mouseLeftRelease = false;
					SendSpecialization(relay, (int)Modes[i].Kind);
					break;
				}
			}
		}
		return true;
	}

	private static void SendSpecialization(FormationRelayFlagEntity relay,
		int mode)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			Xianxia.SendPermanentFormationRelayAction(
				relay.Position.X, relay.Position.Y, mode);
		else
			relay.TrySetSpecializedMode(Main.LocalPlayer, mode);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private static void DrawMeter(Texture2D pixel, Rectangle rectangle,
		int value, int maximum, Color color, string label)
	{
		DrawFrame(pixel, rectangle, new Color(5, 10, 19),
			new Color(60, 69, 88), 2);
		float ratio = maximum <= 0 ? 0f
			: MathHelper.Clamp(value / (float)maximum, 0f, 1f);
		Main.spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 3,
			rectangle.Y + 3, (int)((rectangle.Width - 6) * ratio),
			rectangle.Height - 6), color);
		DrawCentered(label, rectangle.Center.X, rectangle.Y - 1,
			Color.White, 0.56f);
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

	private static void DrawButton(Texture2D pixel, Rectangle rectangle,
		string text, bool hovered)
	{
		DrawFrame(pixel, rectangle,
			hovered ? new Color(145, 65, 80) : new Color(105, 45, 65),
			hovered ? Color.White : new Color(130, 105, 125), 2);
		DrawCentered(text, rectangle.Center.X, rectangle.Y + 7,
			Color.White, 0.78f);
	}

	private static void DrawCentered(string text, float x, float y,
		Color color, float scale)
	{
		Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
		Utils.DrawBorderString(Main.spriteBatch, text,
			new Vector2(x - size.X * 0.5f, y), color, scale);
	}

	private static string Local(string key, params object[] args) =>
		Language.GetTextValue($"Mods.Xianxia.PermanentFormation.RelayUI.{key}", args);
}
