using Sandbox.MovieMaker;
using Sandbox.MovieMaker.Compiled;

namespace Sandbox.Code.Data;
using Sandbox.Code.Systems;


public static class AttackData
{
	public static AttackDef Kick => new AttackDef
	{
		Id = "kick",
		DisplayName = "Kick",
		StartupTime = 0.18f,
		RecoveryTime = 0.35f,
		CooldownTime = 0.10f,
		StaminaCost = 10f,
		Damage = new DamageProfileDef
		{
			HealthDamage = 10f,
			StaminaDamage = 24f,
			StaggerDamage = 18f,
			KnockbackForce = 100f
		},
		Scaling = new AttributeScalingDef
		{
			MightToHealthDamage = 2f,
			MightToStaminaDamage = 1f,
			MightToStaggerDamage = 3f,
			MightToKnockbackForce = 150f
		},
		AnimationName = "attack_kick",
		LockFacing = true,
		CanMoveDuringStartup = false,
		CanMoveDuringRecovery = false,
		Tags = new HashSet<AttackTag>
		{
			AttackTag.Melee,
			AttackTag.Unarmed,
			AttackTag.Kick,
			AttackTag.Strike
		},
		HitPhases = new List<HitPhaseDef>
		{
			new HitPhaseDef
			{
				StartTime = 0.3f,
				EndTime = 0.6f,
				StopAfterFirstHit = true,
				Shapes = new List<HitShapeDef>
				{
					new HitShapeDef
					{
						CastType = HitShapeCastType.Sweep,
						LocalOffset = new Vector3( 45f, 0f, 40f ),
						SweepOffset = new Vector3( 30f, 0f, 0f ),
						BoxSize = new Vector3( 60f, 60f, 60f )
					}
				}
			}
		}
	};

	public static AttackDef Punch => new AttackDef
	{
		Id = "punch",
		DisplayName = "Punch",
		StartupTime = 0.4f,
		RecoveryTime = 0.4f,
		CooldownTime = 1f,
		StaminaCost = 5f,
		Damage =
			new DamageProfileDef
			{
				HealthDamage = 5f, StaminaDamage = 5f, StaggerDamage = 18f, KnockbackForce = 100f
			},
		Scaling =
			new AttributeScalingDef
			{
				MightToHealthDamage = 2f,
				MightToStaminaDamage = 1f,
				MightToStaggerDamage = 3f,
				MightToKnockbackForce = 50f
			},
		AnimationName = "b_attack",
		LockFacing = true,
		CanMoveDuringStartup = false,
		CanMoveDuringRecovery = false,
		Tags = new HashSet<AttackTag> { AttackTag.Melee, AttackTag.Unarmed, AttackTag.Strike },
		HitPhases = new List<HitPhaseDef>
		{
			new HitPhaseDef
			{
				StartTime = 0.18f,
				EndTime = 0.28f,
				StopAfterFirstHit = true,
				Shapes = new List<HitShapeDef>
				{
					new HitShapeDef
					{
						CastType = HitShapeCastType.Sweep,
						LocalOffset = new Vector3( 40f, 0f, 40f ),
						SweepOffset = new Vector3( 90f, 0f, 0f ),
						BoxSize = new Vector3( 34f, 42f, 42f )
					}
				}
			}
		}
	};
	public static AttackDef Shoot => new AttackDef
	{
		Id = "shoot",
		DisplayName = "Shoot Arrow",
		StartupTime = 0.15f,
		RecoveryTime = 0.25f,
		CooldownTime = 0.5f,
		StaminaCost = 8f,
		Damage = new DamageProfileDef
		{
			HealthDamage = 8f,
			StaggerDamage = 5f,
			KnockbackForce = 40f
		},
		Scaling = new AttributeScalingDef { MightToHealthDamage = 1f },
		AnimationName = "attack_shoot",
		LockFacing = true,
		CanMoveDuringStartup = false,
		CanMoveDuringRecovery = false,
		Tags = new HashSet<AttackTag> { AttackTag.Ranged, AttackTag.Projectile },
		HitPhases = new List<HitPhaseDef>(),
		ProjectileTemplate = new ProjectileTemplate
		{
			Termination = ProjectileTerminationType.Infinite,
			CollisionBoxSize = new Vector3( 6f, 6f, 6f ),
			Speed = 3000f
		}
	};
}
