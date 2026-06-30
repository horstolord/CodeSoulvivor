using Sandbox.MovieMaker;
using Sandbox.MovieMaker.Compiled;

namespace Sandbox.Code.Systems;

public class DamageProfileDef
{
	public float HealthDamage;
	public float StaggerDamage;
	public float StaminaDamage;
	public float KnockbackForce;
}

public class HitPhaseDef
{
	public float StartTime;
	public float EndTime;
	public List<HitShapeDef> Shapes = new();
	public bool StopAfterFirstHit;
}


public enum HitShapeType
{
	Sphere,
	Capsule,
	Box
}

public enum HitShapeCastType
{
	Overlap,
	Sweep,
}
public class HitShapeDef
{
	public HitShapeType Type;
	public HitShapeCastType CastType;
	public Vector3 LocalOffset;
	public Rotation LocalRotation;
	public Vector3 SweepOffset;
	public float Radius;
	public float Length;
	public Vector3 BoxSize;
}

public enum AttackTag
{
	Melee,
	Ranged,
	Unarmed,
	Kick,
	Strike,
	Slash,
	Pierce,
	Projectile,
	Spell,
	Fire,
	Frost,
	Air,
	Earth
}
public class AttackDef
{
	public string Id;
	public string DisplayName;
	public float StartupTime;
	public float RecoveryTime;
	public float CooldownTime;
	public float HealthCost;
	public float StaminaCost;
	public float EnergyCost;
	public DamageProfileDef Damage;
	public AttributeScalingDef Scaling;
	public List<HitPhaseDef> HitPhases = new();
	public HashSet<AttackTag> Tags = new();
	public string AnimationName;
	public MovieResource AttackAnimation {get; set;}
	public bool LockFacing;
	public bool CanMoveDuringStartup;
	public bool CanMoveDuringRecovery;
}
public class AttributeScalingDef
{
	public float MightToHealthDamage;
	public float MightToStaminaDamage;
	public float MightToStaggerDamage;
	public float MightToKnockbackForce;

	public float AgilityToHealthDamage;
	public float AcuityToEnergyDamage;
}
