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
	public string ProjectilePrefabPath;
	public float BeamRange;
	public float BeamVisualLength;
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

		var prefabPath = payload.ProjectilePrefabPath;
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
		{
			Log.Warning( "[ProjectileDelivery] Method rune has no ProjectilePrefabPath set." );
			return;
		}

		if ( !ResourceLibrary.TryGet<PrefabFile>( prefabPath, out var prefabFile ) )
		{
			Log.Warning( $"[ProjectileDelivery] Could not find '{prefabPath}' in ResourceLibrary!" );
			return;
		}

		// Clone directly into the scene root (Parent = null) so it moves independently of the Caster
		// Clone DISABLED — CloneConfig.Transform's rotation isn't reliable through
// SceneUtility.GetPrefabScene(...).Clone() while the object is already live,
// so we set it explicitly before waking it up (same pattern as SpawnDirector).
		var config = new CloneConfig
		{
			Transform = spawnTransform,
			Parent = null,
			StartEnabled = false
		};

		var projGo = SceneUtility.GetPrefabScene( prefabFile ).Clone( config );
		projGo.Name = "SpellProjectile";
		projGo.WorldPosition = ctx.Origin;
		projGo.WorldRotation = rot;

// NOTE: object is still disabled here — must use EverythingInSelfAndDescendants,
// not EnabledInSelfAndDescendants, or this returns null (SpawnDirector hits the
// same requirement for the same reason).
		var projComp = projGo.Components.Get<Projectile>( FindMode.EverythingInSelfAndDescendants );
		if ( projComp == null )
		{
			Log.Warning( $"[ProjectileDelivery] '{prefabPath}' has no Projectile component — check the prefab." );
			projGo.Destroy();
			return;
		}

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

		if ( ctx.VisualMaterial != null )
		{
			var renderer = projGo.Components.GetInChildren<ModelRenderer>( true ); // include disabled
			if ( renderer != null )
				renderer.MaterialOverride = ctx.VisualMaterial;
		}

// Everything is configured — wake it up last.
		projGo.Enabled = true;
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
		// ResourceLibrary.Get throws if not found; TryGet returns false silently.
		if ( ResourceLibrary.TryGet<PrefabFile>( "beamblue.prefab", out var prefabFile ) )
		{
			var beamInstance = SceneUtility.GetPrefabScene( prefabFile ).Clone( config );
			beamInstance.WorldPosition = ctx.Origin;
			

			float hitDistance = (targetPos - ctx.Origin).Length;
			float refLength = payload.BeamVisualLength > 0f ? payload.BeamVisualLength : 100f;
			float scale = MathF.Max( 0.01f, hitDistance / refLength );
			beamInstance.LocalScale = beamInstance.LocalScale.WithX( scale );

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
			CombatMath.ApplyKnockback( tr.GameObject, ctx.AimDirection, damageDef.KnockbackForce );
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

				var radialDirection = hit.GameObject.WorldPosition - ctx.Origin;
				CombatMath.ApplyKnockback( hit.GameObject, radialDirection, damageDef.KnockbackForce );
			}
		}

		if ( ctx.TriggerPayloadRunes != null && ctx.TriggerPayloadRunes.Count > 0 )
		{
			RuneEvaluator.ExecuteTriggerPayload( ctx, ctx.Origin, Vector3.Up, null );
		}
	}
}
