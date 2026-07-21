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

	protected override void OnUpdate()
	{
		if ( _rechargeTimer > 0 ) _rechargeTimer -= Time.Delta;
		if ( _castDelayTimer > 0 ) _castDelayTimer -= Time.Delta;
	}

	public bool CanCast()
	{
		return _rechargeTimer <= 0 && _castDelayTimer <= 0;
	}

	public bool CastSpell( Vector3 origin, Vector3 aimDirection, Vector3? targetPoint = null )
	{
		if ( !CanCast() ) return false;

		var runesToEvaluate = RuneSlots != null && RuneSlots.Count > 0
			? RuneSlots
			: (Catalyst?.EquippedRunes ?? new List<RuneDef>());

		if ( runesToEvaluate.Count == 0 ) return false;

		var actor = Components.GetInAncestorsOrSelf<Actor>();
		var evalResult = RuneEvaluator.EvaluateSequence( runesToEvaluate, GameObject, origin, aimDirection, targetPoint );

		if ( !evalResult.Success ) return false;

		// Resource Cost Check via ICostable
		if ( actor != null && !actor.CanPayCost( evalResult.ConsolidatedContext ) )
		{
			return false;
		}

		// Pay Costs
		if ( actor != null )
		{
			actor.PayCost( evalResult.ConsolidatedContext );
		}

		// Deliver Payloads
		foreach ( var payload in evalResult.Payloads )
		{
			RuneEvaluator.ExecuteDelivery( payload );
		}

		// Apply Cooldown / Timers
		float castDelay = evalResult.ConsolidatedContext.TotalCastDelay + (Catalyst?.BaseCastDelay ?? 0.1f);
		float rechargeTime = Catalyst?.BaseRechargeTime ?? 1.0f;

		_castDelayTimer = castDelay;
		_rechargeTimer = rechargeTime;

		return true;
	}
}
