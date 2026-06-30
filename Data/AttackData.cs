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
			MightToKnockbackForce = 50f
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
				StartTime = 0.18f,
				EndTime = 0.28f,
				StopAfterFirstHit = true,
				Shapes = new List<HitShapeDef>
				{
					new HitShapeDef
					{
						Type = HitShapeType.Box,
						CastType = HitShapeCastType.Sweep,
						LocalOffset = new Vector3( 45f, 0f, 40f ),
						LocalRotation = Rotation.Identity,
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
		StartupTime = 0.10f,
		RecoveryTime = 0.15f,
		CooldownTime = 0.10f,
		StaminaCost = 5f,
		Damage = new DamageProfileDef
		{
			HealthDamage = 5f,
			StaminaDamage = 24f,
			StaggerDamage = 18f,
			KnockbackForce = 100f
		},
		Scaling = new AttributeScalingDef
		{
			MightToHealthDamage = 2f,
			MightToStaminaDamage = 1f,
			MightToStaggerDamage = 3f,
			MightToKnockbackForce = 50f
		},
		AnimationName = "punch_right",
		LockFacing = true,
		CanMoveDuringStartup = false,
		CanMoveDuringRecovery = false,
		Tags = new HashSet<AttackTag>
		{
			AttackTag.Melee,
			AttackTag.Unarmed,
			AttackTag.Strike
		},
		AttackAnimation = ResourceLibrary.Get<MovieResource>("punch_right.movie"),
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
						Type = HitShapeType.Box,
						CastType = HitShapeCastType.Sweep,
						LocalOffset = new Vector3( 45f, 0f, 40f ),
						LocalRotation = Rotation.Identity,
						SweepOffset = new Vector3( 30f, 0f, 0f ),
						BoxSize = new Vector3( 34f, 42f, 42f )
					}
				}
			}
		}
	};
}
