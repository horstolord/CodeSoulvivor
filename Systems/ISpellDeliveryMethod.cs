using System;
using Sandbox;
using Sandbox.Code.Actors;
using Sandbox.Code.World;

namespace Sandbox.Code.Systems;

public struct SpellPayload
{
	public SpellContext Context;
	public RuneDeliveryType DeliveryType;
	public ProjectileTemplate ProjectileTemplate;
	public float BeamRange;
	public float AoERadius;
}

public interface ISpellDeliveryMethod
{
	void Deliver( SpellPayload payload );
}

public class ProjectileDeliveryMethod : ISpellDeliveryMethod
{
	public void Deliver( SpellPayload payload )
	{
		var ctx = payload.Context;
		if ( ctx == null || !ctx.Caster.IsValid() ) return;

		var template = payload.ProjectileTemplate?.Clone() ?? new ProjectileTemplate();
		template.Speed *= ctx.SpeedMultiplier;
		template.PierceCount += ctx.BonusPierce;

		var rot = Rotation.LookAt( ctx.AimDirection );
		if ( ctx.SpreadAngle > 0.01f )
		{
			var yawOffset = Random.Shared.Float( -ctx.SpreadAngle, ctx.SpreadAngle );
			var pitchOffset = Random.Shared.Float( -ctx.SpreadAngle, ctx.SpreadAngle );
			rot *= Rotation.From( pitchOffset, yawOffset, 0 );
		}

		var spawnTransform = new Transform( ctx.Origin, rot );

		// Update this path to match your project's directory structure (excluding "Assets/")
		if ( ResourceLibrary.TryGet<PrefabFile>( "fireballin'.prefab", out var prefabFile ) )
		{
			// Clone directly into the scene root (Parent = null) so it moves independently of the Caster
			var config = new CloneConfig
			{
				Transform = spawnTransform,
				Parent = null,
				StartEnabled = true
			};

			var projGo = SceneUtility.GetPrefabScene( prefabFile ).Clone( config );
			projGo.Name = "SpellProjectile";

			Log.Info( $"[Fireballin] Spawning projectile at {ctx.Origin}" );

			// Attach your motion and projectile components directly to the cloned prefab
			projGo.Components.Create<BallMotion>();
			var projComp = projGo.Components.Create<Projectile>();
			var damageDef = new DamageProfileDef
			{
				HealthDamage = ctx.AccumulatedDamage.HealthDamage * ctx.DamageMultiplier,
				StaggerDamage = ctx.AccumulatedDamage.StaggerDamage,
				StaminaDamage = ctx.AccumulatedDamage.StaminaDamage,
				KnockbackForce = ctx.AccumulatedDamage.KnockbackForce,
				Tags = ctx.AttackTags
			};
			var casterSheet = ctx.Caster.Components.GetInAncestorsOrSelf<Actor>()?.StatSheet;
			damageDef = CombatMath.RollCrit( casterSheet, damageDef );

			projComp.Template = template;
			projComp.Payload = new ProjectilePayload
			{
				Caster = ctx.Caster, Damage = damageDef, AttackTags = ctx.AttackTags, SourceContext = ctx
			};
		}
		else
		{
			Log.Warning( "[ProjectileDelivery] Could not find fireballin'.prefab in ResourceLibrary! Make sure the folder path is correct." );
		}
	}
}

public class BeamDeliveryMethod : ISpellDeliveryMethod
{
	public void Deliver( SpellPayload payload )
	{
		
		var ctx = payload.Context;
		if ( ctx == null || !ctx.Caster.IsValid() ) return;
		var spawnTransform = new Transform(ctx.Origin, Rotation.LookAt(ctx.AimDirection));
		var range = payload.BeamRange > 0 ? payload.BeamRange : 3000f;
		var endPos = ctx.Origin + ctx.AimDirection * range;
		var config = new CloneConfig( spawnTransform , ctx.Caster, false );
		var tr = ctx.Caster.Scene.Trace
			.Ray( ctx.Origin, endPos )
			.IgnoreGameObjectHierarchy( ctx.Caster )
			.Run();

		var targetPos = tr.Hit ? tr.EndPosition : endPos;

		var damageDef = new DamageProfileDef
		{
			HealthDamage = ctx.AccumulatedDamage.HealthDamage * ctx.DamageMultiplier,
			StaggerDamage = ctx.AccumulatedDamage.StaggerDamage,
			StaminaDamage = ctx.AccumulatedDamage.StaminaDamage,
			KnockbackForce = ctx.AccumulatedDamage.KnockbackForce,
			Tags = ctx.AttackTags
		};
		var casterSheet = ctx.Caster.Components.GetInAncestorsOrSelf<Actor>()?.StatSheet;
		damageDef = CombatMath.RollCrit( casterSheet, damageDef );
		// In s&box, Assets/ is the filesystem root — paths must NOT include "assets/" prefix.
		// ResourceLibrary.Get throws if not found; TryGet returns false silently.
		if ( ResourceLibrary.TryGet<PrefabFile>( "beamblue.prefab", out var prefabFile ) )
		{
			var beamInstance = SceneUtility.GetPrefabScene( prefabFile ).Clone(config);
			// Place it at the cast origin, aimed in the fire direct.
			beamInstance.WorldPosition = ctx.Origin;
			
			beamInstance.Enabled = true;

		}
		else
		{
			Log.Warning( $"[BeamDelivery] Could not find beamblue.prefab in ResourceLibrary!" );
		}

		if ( tr.Hit && tr.GameObject.IsValid() )
		{
			var actor = tr.GameObject.Components.GetInAncestorsOrSelf<Actor>();
			actor?.ApplyDamage( damageDef );
		}

		if ( ctx.TriggerPayloadRunes != null && ctx.TriggerPayloadRunes.Count > 0 )
		{
			RuneEvaluator.ExecuteTriggerPayload( ctx, targetPos, tr.Normal, tr.GameObject );
		}
	}
}

public class SelfTouchDeliveryMethod : ISpellDeliveryMethod
{
	public void Deliver( SpellPayload payload )
	{
		var ctx = payload.Context;
		if ( ctx == null || !ctx.Caster.IsValid() ) return;
		var radius = payload.AoERadius > 0 ? payload.AoERadius : 150f;
		var hits = ctx.Caster.Scene.Trace
			.Sphere( radius, ctx.Origin, ctx.Origin )
			.IgnoreGameObjectHierarchy( ctx.Caster )
			.RunAll();

		var damageDef = new DamageProfileDef
		{
			HealthDamage = ctx.AccumulatedDamage.HealthDamage * ctx.DamageMultiplier,
			StaggerDamage = ctx.AccumulatedDamage.StaggerDamage,
			StaminaDamage = ctx.AccumulatedDamage.StaminaDamage,
			KnockbackForce = ctx.AccumulatedDamage.KnockbackForce,
			Tags = ctx.AttackTags
		};
		var casterSheet = ctx.Caster.Components.GetInAncestorsOrSelf<Actor>()?.StatSheet;
		damageDef = CombatMath.RollCrit( casterSheet, damageDef );

		foreach ( var hit in hits )
		{
			if ( hit.GameObject.IsValid() )
			{
				var actor = hit.GameObject.Components.GetInAncestorsOrSelf<Actor>();
				actor?.ApplyDamage( damageDef );
			}
		}

		if ( ctx.TriggerPayloadRunes != null && ctx.TriggerPayloadRunes.Count > 0 )
		{
			RuneEvaluator.ExecuteTriggerPayload( ctx, ctx.Origin, Vector3.Up, null );
		}
	}
}
