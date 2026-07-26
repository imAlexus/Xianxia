namespace Xianxia.Common.Abilities;

public enum CultivationAbility
{
	Meditation,
	QiSense,
	QiResistance,
	Fireball,
	QiPalm,
	QiProtection,
	FlameStep,
	QiFlight,
	NascentTeleport,
	SpiritualPressure,
	SpiritBreathing,
	GoldenCoreCirculation,
	NascentSoulRegeneration,
	// Keep new abilities at the end so existing saved ability EXP remains mapped
	// to the same enum values.
	NightVision,
	SwordIntent,
	SpiritSwordRain,
	SectProtectionFormation,
	Count
}

public static class CultivationAbilityInfo
{
	public const int MaxLevel = 20;

	public static int RequiredRealm(CultivationAbility ability) => ability switch
	{
		CultivationAbility.Meditation or CultivationAbility.SpiritBreathing => 0,
		CultivationAbility.QiSense or CultivationAbility.QiResistance
			or CultivationAbility.Fireball or CultivationAbility.QiPalm => 1,
		CultivationAbility.QiProtection or CultivationAbility.FlameStep
			or CultivationAbility.NightVision => 2,
		CultivationAbility.QiFlight or CultivationAbility.GoldenCoreCirculation => 3,
		CultivationAbility.NascentTeleport or CultivationAbility.SpiritualPressure
			or CultivationAbility.NascentSoulRegeneration => 4,
		CultivationAbility.SwordIntent => 1,
		CultivationAbility.SpiritSwordRain => 2,
		CultivationAbility.SectProtectionFormation => 3,
		_ => 99
	};

	public static int ExperienceForNextLevel(int level) => 50 * level * level;
}
