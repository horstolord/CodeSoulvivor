using Sandbox.Code.Systems;

namespace Sandbox.Code.Actors;

public sealed class EnemyRagdollHandler : Component, IRagdollHandler
{
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public NavMeshAgent Agent { get; set; }
	[Property] public Actor Enemy { get; set; }

	protected override void OnStart()
	{
		Renderer ??= Components.GetInAncestorsOrSelf<SkinnedModelRenderer>() ?? Components.GetInChildren<SkinnedModelRenderer>();
		Physics ??= Components.GetInAncestorsOrSelf<ModelPhysics>() ?? Components.GetInChildren<ModelPhysics>();
		Agent ??= Components.GetInAncestorsOrSelf<NavMeshAgent>() ?? Components.GetInChildren<NavMeshAgent>();
		Enemy ??= Components.GetInAncestorsOrSelf<Actor>() ?? Components.GetInChildren<Actor>();

		if ( Physics != null )
		{
			if ( Renderer != null )
			{
				Physics.Renderer = Renderer;
				Physics.Model = Renderer.Model;
			}
			Physics.IgnoreRoot = true;
			Physics.Enabled = false;
		}	
	}

	public void EnterRagdoll()
	{
		if ( Agent != null ) Agent.Enabled = false;
		if ( Renderer != null ) Renderer.UseAnimGraph = false;
		if ( Enemy != null ) Enemy.Enabled = false;

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
		if ( Renderer != null ) Renderer.UseAnimGraph = true;

		var cc = Components.GetInAncestorsOrSelf<CharacterController>() ?? Components.GetInChildren<CharacterController>();
		if ( cc != null ) cc.Enabled = true;

		var rb = Components.GetInAncestorsOrSelf<Rigidbody>() ?? Components.GetInChildren<Rigidbody>();
		if ( rb != null ) rb.Enabled = true;

		if ( Enemy != null ) Enemy.Enabled = true;
		if ( Agent != null ) Agent.Enabled = true;
	}
}
