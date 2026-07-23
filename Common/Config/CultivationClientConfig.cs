using System;
using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace Xianxia.Common.Config;

public class CultivationClientConfig : ModConfig
{
	public static CultivationClientConfig Instance;

	public override ConfigScope Mode => ConfigScope.ClientSide;

	[Header("MeditationControls")]
	[DefaultValue(true)]
	public bool ToggleMeditation;

	[Header("QiHud")]
	[DefaultValue(50)]
	[Range(0, 100)]
	[Increment(1)]
	[Slider]
	public int QiBarHorizontalPositionPercent;

	[DefaultValue(18)]
	[Range(0, 300)]
	[Increment(2)]
	[Slider]
	public int QiBarVerticalPosition;

	[DefaultValue(100)]
	[Range(60, 150)]
	[Increment(5)]
	[Slider]
	public int QiBarScalePercent;

	[DefaultValue(true)]
	public bool ShowQiConcentration;

	[Header("SpiritBeasts")]
	[DefaultValue(true)]
	public bool ShowSpiritBeastNameplates;

	[Header("VisualEffects")]
	[DefaultValue(100)]
	[Range(0, 100)]
	[Increment(10)]
	[Slider]
	public int VisualEffectIntensityPercent;

	public static float QiBarScale => Math.Clamp((Instance?.QiBarScalePercent ?? 100) / 100f, 0.6f, 1.5f);
	public static float VisualEffectIntensity => Math.Clamp(
		(Instance?.VisualEffectIntensityPercent ?? 100) / 100f, 0f, 1f);

	public static int ScaleParticleCount(int normalCount, int minimumWhenEnabled = 1)
	{
		float intensity = VisualEffectIntensity;
		if (intensity <= 0f)
		{
			return 0;
		}

		return Math.Max(minimumWhenEnabled, (int)MathF.Round(normalCount * intensity));
	}

	public static bool ShouldSpawnParticle()
	{
		float intensity = VisualEffectIntensity;
		return intensity >= 1f || (intensity > 0f && Main.rand.NextFloat() < intensity);
	}
}
