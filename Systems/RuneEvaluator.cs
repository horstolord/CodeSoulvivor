using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Code.Actors;
using Sandbox.Code.World;

namespace Sandbox.Code.Systems;

public class RuneEvaluationResult
{
	public SpellContext ConsolidatedContext;
	public List<SpellPayload> Payloads = new();
	public bool Success;
	public string ErrorMessage;
}

public static class RuneEvaluator
{
	public static RuneEvaluationResult EvaluateSequence( List<RuneDef> runes, GameObject caster, Vector3 origin, Vector3 aimDir, Vector3? targetPoint = null, int currentDepth = 0 )
	{
		var result = new RuneEvaluationResult();
		if ( runes == null || runes.Count == 0 )
		{
			result.Success = false;
			result.ErrorMessage = "Empty rune sequence.";
			return result;
		}

		if ( currentDepth > SpellContext.MaxRecursionDepth )
		{
			result.Success = false;
			result.ErrorMessage = "Max spell recursion depth reached.";
			return result;
		}

		var ctx = new SpellContext
		{
			Caster = caster,
			Origin = origin,
			AimDirection = aimDir,
			TargetPoint = targetPoint,
			RecursionDepth = currentDepth
		};

		var statSheet = caster?.Components.GetInAncestorsOrSelf<Actor>()?.StatSheet;
		float will = statSheet?.Will.Value ?? 0f;
		float acuity = statSheet?.Acuity.Value ?? 0f;

		int index = 0;
		bool hasMethod = false;

		while ( index < runes.Count )
		{
			var rune = runes[index];
			if ( rune == null )
			{
				index++;
				continue;
			}

			// Aggregate costs & cast delay (Method delay scales with Acuity)
			ctx.TotalEnergyCost += rune.EnergyCost;
			ctx.TotalHealthCost += rune.HealthCost;
			ctx.TotalStaminaCost += rune.StaminaCost;

			float castDelay = rune.CastDelay;
			if ( rune.Category == RuneCategory.Method )
			{
				float acuityScale = rune.Scaling?.AcuityToCastSpeed ?? 0f;
				castDelay /= 1f + acuity * acuityScale / 100f;
			}
			ctx.TotalCastDelay += castDelay;

			switch ( rune.Category )
			{
				case RuneCategory.Modifier:
					rune.ModifierEffect?.Invoke( ctx );
					break;

				case RuneCategory.Multicast:
					ctx.MulticastCount = Math.Max( ctx.MulticastCount, rune.MulticastDrawCount );
					break;

				case RuneCategory.Trigger:
					if ( rune.TriggerNestedRunes != null && rune.TriggerNestedRunes.Count > 0 )
					{
						ctx.TriggerPayloadRunes.AddRange( rune.TriggerNestedRunes );
					}
					break;

				case RuneCategory.Force:
					ctx.AccumulatedDamage.HealthDamage += rune.BasePower
						+ will * (rune.Scaling?.WillToPower ?? 0f);
					ctx.AccumulatedDamage.StaggerDamage += rune.StaggerDamage;
					ctx.AccumulatedDamage.KnockbackForce += rune.KnockbackForce;
					if ( rune.ElementTag.HasValue ) ctx.ElementTags.Add( rune.ElementTag.Value );
					ctx.VisualMaterial ??= rune.VisualMaterial;
					foreach ( var tag in rune.SpellTags ) ctx.AttackTags.Add( tag );
					break;

				case RuneCategory.Method:
					hasMethod = true;
					CreatePayloadsForMethod( ctx, rune, result.Payloads );
					break;
			}

			index++;
		}

		// Evoke Force Alone (Fallback delivery when Force is present without a Method)
		if ( !hasMethod && ctx.AccumulatedDamage.HealthDamage > 0 )
		{
			var fallbackPayload = new SpellPayload
			{
				Context = ctx.Clone(),
				DeliveryType = RuneDeliveryType.SelfTouch,
				AoERadius = 120f
			};
			result.Payloads.Add( fallbackPayload );
		}

		result.ConsolidatedContext = ctx;
		result.Success = result.Payloads.Count > 0;
		return result;
	}

	private static void CreatePayloadsForMethod( SpellContext ctx, RuneDef methodRune, List<SpellPayload> outPayloads )
	{
		int count = Math.Max( 1, ctx.MulticastCount );
		for ( int i = 0; i < count; i++ )
		{
			var payloadCtx = ctx.Clone();
			var payload = new SpellPayload
			{
				Context = payloadCtx,
				DeliveryType = methodRune.DeliveryType,
				ProjectileTemplate = methodRune.ProjectileTemplate,
				ProjectilePrefabPath = methodRune.ProjectilePrefabPath,
				BeamRange = methodRune.Range,
				AoERadius = methodRune.AoERadius
			};
			outPayloads.Add( payload );
		}
	}

	public static void ExecuteTriggerPayload( SpellContext parentContext, Vector3 triggerOrigin, Vector3 triggerNormal, GameObject hitTarget )
	{
		if ( parentContext == null || parentContext.TriggerPayloadRunes == null || parentContext.TriggerPayloadRunes.Count == 0 )
			return;

		if ( parentContext.RecursionDepth >= SpellContext.MaxRecursionDepth )
			return;

		var aimDir = triggerNormal.LengthSquared > 0.001f ? triggerNormal : parentContext.AimDirection;
		var evalResult = EvaluateSequence(
			parentContext.TriggerPayloadRunes,
			parentContext.Caster,
			triggerOrigin,
			aimDir,
			triggerOrigin + aimDir * 100f,
			parentContext.RecursionDepth + 1
		);

		if ( evalResult.Success )
		{
			foreach ( var payload in evalResult.Payloads )
			{
				ExecuteDelivery( payload );
			}
		}
	}

	public static void ExecuteDelivery( SpellPayload payload )
	{
		ISpellDeliveryMethod deliveryMethod = payload.DeliveryType switch
		{
			RuneDeliveryType.Projectile => new ProjectileDeliveryMethod(),
			RuneDeliveryType.Beam => new BeamDeliveryMethod(),
			RuneDeliveryType.SelfTouch => new SelfTouchDeliveryMethod(),
			_ => new SelfTouchDeliveryMethod()
		};

		deliveryMethod.Deliver( payload );
		
	}
}
