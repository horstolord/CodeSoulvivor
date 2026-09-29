using Sandbox.Code.Systems;

namespace Sandbox.Code.Actors;

public sealed class EnemyRagdollHandler : Component, IRagdollHandler
{
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public NavMeshAgent Agent { get; set; }
	[Property] public Actor Enemy { get; set; }
	[Property] public ModelCollider ModelCollider { get; set; }

	private bool _modelColliderWasEnabled;

	protected override void OnStart()
	{
		Renderer ??= Components.GetInAncestorsOrSelf<SkinnedModelRenderer>() ?? Components.GetInChildren<SkinnedModelRenderer>();
		Physics ??= Components.GetInAncestorsOrSelf<ModelPhysics>() ?? Components.GetInChildren<ModelPhysics>();
		Agent ??= Components.GetInAncestorsOrSelf<NavMeshAgent>() ?? Components.GetInChildren<NavMeshAgent>();
		Enemy ??= Components.GetInAncestorsOrSelf<Actor>() ?? Components.GetInChildren<Actor>();
		ModelCollider ??= Components.GetInAncestorsOrSelf<ModelCollider>() ?? Components.GetInChildren<ModelCollider>();
		_modelColliderWasEnabled = ModelCollider?.Enabled ?? false;

		if ( Physics != null )
		{
			if ( Renderer != null )
			{
				Physics.Renderer = Renderer;
				Physics.Model = Renderer.Model;
			}
			// A stale prefab can serialize PhysicsWereCreated=true without serializing
			// the generated body list. ModelPhysics then skips CreatePhysics on enable,
			// leaving no bodies to drive the renderer after its animation graph stops.
			if ( Physics.PhysicsWereCreated && (Physics.Bodies == null || Physics.Bodies.Count == 0) )
				Physics.PhysicsWereCreated = false;
			Physics.IgnoreRoot = false;
			Physics.Enabled = false;
		}	
	}

	public void EnterRagdoll()
	{
		// Cache the animated pose before stopping the graph. ModelPhysics applies this
		// pose to its generated bone bodies when it is enabled.
		if ( Physics != null && Renderer != null )
			Physics.CopyBonesFrom( Renderer, true );

		if ( Agent != null ) Agent.Enabled = false;
		if ( Renderer != null ) Renderer.UseAnimGraph = false;
		// ModelCollider is one whole-model collider on the renderer object; it does not
		// follow individual bones and can obstruct the bone-level ragdoll colliders.
		if ( ModelCollider != null ) ModelCollider.Enabled = false;

		// Disable any kinematic / character movement that locks position or overrides bone simulation
		var cc = Components.GetInAncestorsOrSelf<CharacterController>() ?? Components.GetInChildren<CharacterController>();
		if ( cc != null ) cc.Enabled = false;

		var rb = Components.GetInAncestorsOrSelf<Rigidbody>() ?? Components.GetInChildren<Rigidbody>();
		if ( rb != null ) rb.Enabled = false;

		if ( Physics != null )
		{
			Physics.Enabled = true;
		}
	}

	public void ExitRagdoll()
	{
		if ( Physics != null ) Physics.Enabled = false;
		if ( ModelCollider != null ) ModelCollider.Enabled = _modelColliderWasEnabled;
		if ( Renderer != null ) Renderer.UseAnimGraph = true;

		var cc = Components.GetInAncestorsOrSelf<CharacterController>() ?? Components.GetInChildren<CharacterController>();
		if ( cc != null ) cc.Enabled = true;

		var rb = Components.GetInAncestorsOrSelf<Rigidbody>() ?? Components.GetInChildren<Rigidbody>();
		if ( rb != null ) rb.Enabled = true;

		if ( Enemy != null ) Enemy.Enabled = true;
		if ( Agent != null ) Agent.Enabled = true;
	}
}
