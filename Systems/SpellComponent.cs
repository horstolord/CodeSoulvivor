using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Code.Actors;
using Sandbox.Code.Data;
using Sandbox.Code.World;

namespace Sandbox.Code.Systems;

public sealed class SpellComponent : Component
{
	public CatalystDef Catalyst { get; set; }
	public List<RuneDef> RuneSlots { get; set; } = new();

	private float _rechargeTimer = 0f;
	private float _castDelayTimer = 0f;
	private Actor _actor;

	protected override void OnStart()
	{
		_actor ??= Components.GetInAncestorsOrSelf<Actor>();
	}

	protected override void OnUpdate()
	{
		_actor ??= Components.GetInAncestorsOrSelf<Actor>();

		bool wasCasting = _castDelayTimer > 0;

		if ( _rechargeTimer > 0 ) _rechargeTimer -= Time.Delta;
		if ( _castDelayTimer > 0 ) _castDelayTimer -= Time.Delta;

		// Post-cast delay elapsed — release Casting (guarded so we don't stomp a state
		// something else set in the meantime, e.g. Staggered/Dead).
		if ( wasCasting && _castDelayTimer <= 0f && _actor?.StateComp != null && _actor.StateComp.CurrentState == ActorStateType.Casting )
		{
			_actor.StateComp.CurrentState = ActorStateType.Idle;
		}
	}

	public bool CanCast()
	{
		if ( _rechargeTimer > 0 || _castDelayTimer > 0 ) return false;
		if ( _actor?.StateComp != null && _actor.StateComp.CurrentState != ActorStateType.Idle ) return false;

		return true;
	}

	public bool CastSpell( Vector3 origin, Vector3 aimDirection, Vector3? targetPoint = null )
	{
		if ( !CanCast() ) return false;

		var runesToEvaluate = RuneSlots != null && RuneSlots.Count > 0
			? RuneSlots
			: (Catalyst?.EquippedRunes ?? new List<RuneDef>());

		if ( runesToEvaluate.Count == 0 ) return false;

		_actor ??= Components.GetInAncestorsOrSelf<Actor>();
		var evalResult = RuneEvaluator.EvaluateSequence( runesToEvaluate, GameObject, origin, aimDirection, targetPoint );

		if ( !evalResult.Success ) return false;

		// Resource Cost Check via ICostable
		if ( _actor != null && !_actor.CanPayCost( evalResult.ConsolidatedContext ) )
		{
			return false;
		}

		// Pay Costs
		if ( _actor != null )
		{
			_actor.PayCost( evalResult.ConsolidatedContext );
		}

		// Deliver Payloads
		foreach ( var payload in evalResult.Payloads )
		{
			RuneEvaluator.ExecuteDelivery( payload );
		}

		// Apply Cooldown / Timers
		float castDelay = evalResult.ConsolidatedContext.TotalCastDelay + (Catalyst?.BaseCastDelay ?? 0.1f);
		float rechargeTime = Catalyst?.BaseRechargeTime ?? 1.0f;
		var stats = _actor?.Components.Get<StatSheet>();
		float castSpeedMultiplier = MathF.Max( 0.01f, (stats?.CastSpeed?.Value ?? 100f) / 100f );
		_castDelayTimer = castDelay / castSpeedMultiplier;
		_rechargeTimer = rechargeTime;

		if ( _actor?.StateComp != null )
			_actor.StateComp.CurrentState = ActorStateType.Casting;

		return true;
	}
}
