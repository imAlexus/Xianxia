using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Xianxia.Common.Utilities;

public static class FormationWorldBarDrawer
{
	public static void Draw(SpriteBatch spriteBatch, Vector2 worldCenter,
		int width, int qi, int maximumQi, int integrity, int maximumIntegrity,
		bool powered)
	{
		Vector2 offscreenOffset = Main.drawToScreen
			? Vector2.Zero
			: new Vector2(Main.offScreenRange);
		Vector2 screenCenter = worldCenter - Main.screenPosition + offscreenOffset;
		int x = (int)MathF.Round(screenCenter.X - width * 0.5f);
		int y = (int)MathF.Round(screenCenter.Y);
		float opacity = powered ? 1f : 0.48f;

		DrawSingleBar(spriteBatch, new Rectangle(x, y, width, 7),
			qi, maximumQi, new Color(45, 225, 240) * opacity,
			new Color(85, 245, 255) * opacity);
		DrawSingleBar(spriteBatch, new Rectangle(x, y + 9, width, 7),
			integrity, maximumIntegrity, new Color(70, 215, 125) * opacity,
			new Color(135, 255, 175) * opacity);
	}

	private static void DrawSingleBar(SpriteBatch spriteBatch,
		Rectangle rectangle, int value, int maximum, Color fillColor,
		Color highlightColor)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color border = new Color(5, 10, 18, 230);
		Color background = new Color(13, 20, 30, 215);
		spriteBatch.Draw(pixel, rectangle, border);
		Rectangle interior = new(rectangle.X + 1, rectangle.Y + 1,
			rectangle.Width - 2, rectangle.Height - 2);
		spriteBatch.Draw(pixel, interior, background);

		float ratio = maximum <= 0
			? 0f
			: MathHelper.Clamp(value / (float)maximum, 0f, 1f);
		int fillWidth = (int)MathF.Round(interior.Width * ratio);
		if (fillWidth <= 0)
			return;
		Rectangle fill = new(interior.X, interior.Y, fillWidth, interior.Height);
		spriteBatch.Draw(pixel, fill, fillColor);
		if (fill.Height >= 3)
			spriteBatch.Draw(pixel,
				new Rectangle(fill.X, fill.Y, fill.Width, 1), highlightColor);
	}
}
