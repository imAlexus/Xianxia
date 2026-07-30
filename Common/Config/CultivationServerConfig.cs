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

	[Header("Progression")]
	[DefaultValue(1)]
	[Range(1, 10)]
	[Increment(1)]
	[Slider]
	public int CultivationRequirementMultiplier;

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

	[Header("CultivationRisks")]
	[DefaultValue(true)]
	public bool EnableQiBurning;

	[DefaultValue(50)]
	[Range(20, 80)]
	[Increment(5)]
	[Slider]
	public int MaximumBurnedQiPercent;

	[DefaultValue(true)]
	public bool EnableHeartDemons;

	[DefaultValue(100)]
	[Range(0, 200)]
	[Increment(10)]
	[Slider]
	public int HeartDemonPenaltyStrengthPercent;

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
