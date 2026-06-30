using Sandbox.MovieMaker;
using Sandbox.MovieMaker.Compiled;

namespace Sandbox.Code.Systems;
using System.Linq;
using Actors;

public sealed class CombatComponent : Component
{
	public AttackContext? CurrentAttack { get; private set; }
	private float AttackElapsed;
	private HashSet<GameObject> _hitObjects = new();
    public bool TryStartAttack( AttackRequest request )
    {
        var context = BuildContext( request );
        if ( !ValidateAttack( context ) )
            return false;
        CurrentAttack = context;
        StartAttack( context );
        
        return true;
        
    }

    protected override void OnUpdate()
    {
	    UpdateCurrentAttack();
    }
    private AttackContext BuildContext( AttackRequest request )
    {
        var attack = request.Attack;
        return new AttackContext
        {
            Request = request,
            Attack = attack,
            Attacker = request.Attacker,
            SourceItem = request.SourceItem,
            Origin = request.Origin,
            Facing = request.Facing,
            AimDirection = request.AimDirection,
            TargetPoint = request.TargetPoint,
            Charge01 = request.Charge01,
            AlternateUse = request.AlternateUse,
            TriggerType = request.TriggerType,
            StartupTime = attack.StartupTime,
            RecoveryTime = attack.RecoveryTime,
            CooldownTime = attack.CooldownTime,
            HealthCost = attack.HealthCost,
            StaminaCost = attack.StaminaCost,
            EnergyCost = attack.EnergyCost,
            Scaling = attack.Scaling,
            Damage = new DamageProfileDef
            {
                HealthDamage = attack.Damage.HealthDamage,
                StaggerDamage = attack.Damage.StaggerDamage,
                StaminaDamage = attack.Damage.StaminaDamage,
                KnockbackForce = attack.Damage.KnockbackForce * attack.Scaling.MightToKnockbackForce
            },
            HitPhases = attack.HitPhases
                .Select( phase => new HitPhaseDef
                {
                    StartTime = phase.StartTime,
                    EndTime = phase.EndTime,
                    StopAfterFirstHit = phase.StopAfterFirstHit,
                    Shapes = phase.Shapes
                        .Select( shape => new HitShapeDef
                        {
                            Type = shape.Type,
                            CastType = shape.CastType,
                            LocalOffset = shape.LocalOffset,
                            LocalRotation = shape.LocalRotation,
                            SweepOffset = shape.SweepOffset,
                            Radius = shape.Radius,
                            Length = shape.Length,
                            BoxSize = shape.BoxSize
                        } )
                        .ToList()
                } )
                .ToList(),
            Tags = new HashSet<AttackTag>( attack.Tags ),
            AnimationName = attack.AnimationName,
            AttackAnimation = attack.AttackAnimation,
            LockFacing = attack.LockFacing,
            CanMoveDuringStartup = attack.CanMoveDuringStartup,
            CanMoveDuringRecovery = attack.CanMoveDuringRecovery
        };
    }
    private bool ValidateAttack( AttackContext context )
    {
        if ( context.Attacker == null )
            return false;
        if ( context.Attack == null )
            return false;
        var attacker = ResolveActor( context.Attacker );
        if ( attacker != null && !attacker.CanPayCost( context.Attack ) )
        {
	        Log.Info( $"Not enough resources for {context.Attack.DisplayName}. Health {attacker.Runtime.Health}/{context.Attack.HealthCost}, stamina {attacker.Runtime.Stamina}/{context.Attack.StaminaCost}, energy {attacker.Runtime.Energy}/{context.Attack.EnergyCost}" );
	        return false;
        }
        return true;
    }
    private void StartAttack( AttackContext context )
    {
        CurrentAttack = context;
        AttackElapsed = 0f;
        _hitObjects.Clear();
        ResolveActor( context.Attacker )?.PayCost( context.Attack );
	    Log.Info( $"Starting attack: {context.Attack.DisplayName} (health cost={context.HealthCost}, stamina cost={context.StaminaCost}, energy cost={context.EnergyCost})" );
	   
    }

    
    

    private void UpdateCurrentAttack()
    {
	    if ( CurrentAttack == null )
		    return;
	    AttackElapsed += Time.Delta;
	    foreach ( var phase in CurrentAttack.HitPhases )
	    {
		    if ( AttackElapsed >= phase.StartTime && AttackElapsed <= phase.EndTime )
		    {
			    ExecuteHitPhase( CurrentAttack, phase );
		    }
	    }
	    var endTime = GetAttackEndTime( CurrentAttack );
	    if ( AttackElapsed >= endTime )
	    {
		    CurrentAttack = null;
		    _hitObjects.Clear();
	    } 
    }
    private void ExecuteHitPhase( AttackContext context, HitPhaseDef phase )
    {
	    foreach ( var shape in phase.Shapes )
	    {
		    ExecuteHitShape( context, shape );
	    }
    }
    private void ExecuteHitShape( AttackContext context, HitShapeDef shape )
    {
	    var rotation = context.Facing;
	    var position = context.Origin;
	    var worldOffset = rotation * shape.LocalOffset;
	    var center = position + worldOffset;
	    var sweep = rotation * shape.SweepOffset;
	    var start = shape.CastType == HitShapeCastType.Sweep ? center - sweep * 0.5f : center;
	    var end = shape.CastType == HitShapeCastType.Sweep ? center + sweep * 0.5f : center;
	    var hits = Scene.Trace
		    .Box( shape.BoxSize, start, end )
		    .IgnoreGameObjectHierarchy( context.Attacker )
		    .RunAll()
		    .ToList();
	    foreach ( var hit in hits )
	    {
		    var target = hit.GameObject;
		    if ( target == null )
			    continue;
		    if ( _hitObjects.Contains( target ) )
			    continue;
		    _hitObjects.Add( target );
		    ApplyHit( context, target );
	    }
    }
    private void ApplyHit( AttackContext context, GameObject target )
    {
	    var actor = ResolveActor( target );
	    actor?.ApplyDamage( context.Damage );

	    var body = target.Components.GetInAncestorsOrSelf<Rigidbody>();
	    if ( body != null )
	    {
		    var impulseDirection = context.Facing.Forward.Normal;
		    body.ApplyImpulse( impulseDirection * context.Damage.KnockbackForce );
	    }
	    else
	    {
		    Log.Info( $"Attack hit {target.Name}, but no Rigidbody was found." );
	    }
    }

    private Actor? ResolveActor( GameObject gameObject )
    {
	    return gameObject.Components.GetAll<Actor>()
		    .OrderByDescending( actor => actor.GetType() != typeof(Actor) )
		    .FirstOrDefault()
		    ?? gameObject.Components.GetInAncestorsOrSelf<Actor>();
    }

    private float GetAttackEndTime( AttackContext context )
    {
	    float latestPhaseEnd = 0f;
	    foreach ( var phase in context.HitPhases )
	    {
		    if ( phase.EndTime > latestPhaseEnd )
			    latestPhaseEnd = phase.EndTime;
	    }
	    return latestPhaseEnd + context.RecoveryTime;
    }
}
