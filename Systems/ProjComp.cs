namespace Sandbox.Code.Systems;

using Sandbox.Code.Actors;
using Sandbox.Code.Systems;

public enum ProjectileTerminationType { Timeout, FirstHit, PierceCount, Infinite }

public class ProjectileTemplate
{
    public float Lifetime = 5f;
    public ProjectileTerminationType Termination = ProjectileTerminationType.FirstHit;
    public int PierceCount = 1;
    public Vector3 CollisionBoxSize = new Vector3( 12f, 12f, 12f );
    public float Speed = 1000f; // motion components read this too
    public ProjectileTemplate Clone() => new ProjectileTemplate
    {
	    Lifetime = Lifetime,
	    Termination = Termination,
	    PierceCount = PierceCount,
	    CollisionBoxSize = CollisionBoxSize,
	    Speed = Speed
    };
}

public class ProjectilePayload
{
    public GameObject Caster;
    public DamageProfileDef Damage;
    public HashSet<AttackTag> AttackTags = new();
    public object SourceContext; // AttackContext or SpellContext, for override lookups later
}

public sealed class Projectile : Component
{
    public ProjectileTemplate Template { get; set; }
    public ProjectilePayload Payload { get; set; }

    private float _age;
    private int _hitCount;
    private Vector3 _lastPosition;

    protected override void OnStart()
    {
        base.OnStart();
        _lastPosition = GameObject.WorldPosition;
    }

    protected override void OnFixedUpdate()
    {
        if ( Template == null || Payload == null ) return;

        _age += Time.Delta;
        if ( Template.Termination != ProjectileTerminationType.Infinite && _age >= Template.Lifetime )
        {
	        GameObject.Destroy();
	        return;
        }

        Components.Get<IProjectileMotion>()?.Tick( this, Time.Delta );

        // Sweep-trace from last position to current, so fast projectiles can't tunnel
        var hits = Scene.Trace
            .Box( Template.CollisionBoxSize, _lastPosition, GameObject.WorldPosition )
            .IgnoreGameObjectHierarchy( Payload.Caster )
            .RunAll();

        foreach ( var hit in hits )
        {
            if ( hit.GameObject == null ) continue;
            OnHit( hit.GameObject );
            if ( ShouldTerminateAfterHit() )
            {
                GameObject.Destroy();
                break;
            }
        }

        _lastPosition = GameObject.WorldPosition;
    }

    private void OnHit( GameObject target )
    {
        var actor = target.Components.GetInAncestorsOrSelf<Actor>();
        actor?.ApplyDamage( Payload.Damage );
        _hitCount++;
        // TODO: MaterialData / InteractionOverrides lookup goes here later
    }

    private bool ShouldTerminateAfterHit()
    {
        return Template.Termination switch
        {
            ProjectileTerminationType.FirstHit => _hitCount >= 1,
            ProjectileTerminationType.PierceCount => _hitCount >= Template.PierceCount,
            _ => false
        };
    }
}
