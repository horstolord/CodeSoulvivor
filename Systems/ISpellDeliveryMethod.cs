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

		var projGo = new GameObject( true, "SpellProjectile" );
		projGo.WorldPosition = ctx.Origin;
		projGo.WorldRotation = rot;

		var projComp = projGo.Components.Create<Projectile>();

		var damageDef = new DamageProfileDef
		{
			HealthDamage = ctx.AccumulatedDamage.HealthDamage * ctx.DamageMultiplier,
			StaggerDamage = ctx.AccumulatedDamage.StaggerDamage,
			StaminaDamage = ctx.AccumulatedDamage.StaminaDamage,
			KnockbackForce = ctx.AccumulatedDamage.KnockbackForce
		};

		projComp.Template = template;
		projComp.Payload = new ProjectilePayload
		{
			Caster = ctx.Caster,
			Damage = damageDef,
			AttackTags = ctx.AttackTags,
			SourceContext = ctx
		};
	}
}

public class BeamDeliveryMethod : ISpellDeliveryMethod
{
	public void Deliver( SpellPayload payload )
	{
		var ctx = payload.Context;
		if ( ctx == null || !ctx.Caster.IsValid() ) return;

		var range = payload.BeamRange > 0 ? payload.BeamRange : 1000f;
		var tr = ctx.Caster.Scene.Trace
			.Ray( ctx.Origin, ctx.Origin + ctx.AimDirection * range )
			.IgnoreGameObjectHierarchy( ctx.Caster )
			.Run();

		if ( tr.Hit && tr.GameObject.IsValid() )
		{
			var damageDef = new DamageProfileDef
			{
				HealthDamage = ctx.AccumulatedDamage.HealthDamage * ctx.DamageMultiplier,
				StaggerDamage = ctx.AccumulatedDamage.StaggerDamage,
				StaminaDamage = ctx.AccumulatedDamage.StaminaDamage,
				KnockbackForce = ctx.AccumulatedDamage.KnockbackForce
			};

			var actor = tr.GameObject.Components.GetInAncestorsOrSelf<Actor>();
			actor?.ApplyDamage( damageDef );

			if ( ctx.TriggerPayloadRunes != null && ctx.TriggerPayloadRunes.Count > 0 )
			{
				RuneEvaluator.ExecuteTriggerPayload( ctx, tr.EndPosition, tr.Normal, tr.GameObject );
			}
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
			KnockbackForce = ctx.AccumulatedDamage.KnockbackForce
		};

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
