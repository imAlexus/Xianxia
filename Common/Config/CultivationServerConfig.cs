using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Xianxia.Common.Config;

public class CultivationServerConfig : ModConfig
{
	public static CultivationServerConfig Instance;

	public override ConfigScope Mode => ConfigScope.ServerSide;

	[DefaultValue(true)]
	public bool EnableTimeAcceleration;

	[DefaultValue(5)]
	[Range(1, 20)]
	[Increment(1)]
	[Slider]
	public int TimeMultiplier;

	[Header("SpiritualQiZones")]
	[DefaultValue(true)]
	public bool EnableSpiritualQiZones;

	[DefaultValue(60)]
	[Range(20, 150)]
	[Increment(5)]
	[Slider]
	public int SpiritualQiZoneRadiusBlocks;

	[DefaultValue(250)]
	[Range(50, 500)]
	[Increment(25)]
	[Slider]
	public int SpiritMineDetectorRadiusBlocks;

	[Header("SpiritBeasts")]
	[DefaultValue(true)]
	public bool EnableSpiritBeasts;

	[DefaultValue(true)]
	public bool EnableSpiritBeastDistanceScaling;

	[DefaultValue(100)]
	[Range(25, 300)]
	[Increment(25)]
	[Slider]
	public int SpiritBeastSpawnRatePercent;

	[Header("Debug")]
	[DefaultValue(false)]
	public bool EnableDebugCommandsInMultiplayer;

	[Header("AbilityTerrainDestruction")]
	[DefaultValue(true)]
	public bool EnableAbilityTerrainDestruction;

	[DefaultValue(true)]
	public bool EnableFireballTerrainDestruction;

	[DefaultValue(true)]
	public bool EnableQiPalmTerrainDestruction;

	[DefaultValue(true)]
	public bool EnableFlameStepTerrainDestruction;
}
