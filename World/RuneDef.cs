using Sandbox.Code.Systems;

namespace Sandbox.Code.World;

public enum RuneCategory { Force, Method, Modifier }

// Separate vocabulary from AttackTag — melee tags don't mean anything for spells
public enum RuneElementTag { Fire, Frost, Air, Earth, Light, Dark }

public class RuneScalingDef
{
	public float WisdomToPower;
	public float AcuityToCastSpeed;
	public float WillToDuration;
	// extend as playstyles need it
}

public class RuneDef
{
	public string Id;
	public string DisplayName;
	public RuneCategory Category;
	public HashSet<AttackTag> SpellTags = new();

	public float HealthCost;
	public float StaminaCost;
	public float EnergyCost;
	public float CastTime;
	public float CooldownTime;
	public float Range;

	public RuneScalingDef Scaling;
	// Force runes: base elemental power before scaling
	public float BasePower;

	// Method runes only — delivery shape
	// public ProjectileTemplate Payload;

	// Modifier runes only — how it mutates whatever payload rune follows it
	// public RuneModifierEffect Modifier;
}
