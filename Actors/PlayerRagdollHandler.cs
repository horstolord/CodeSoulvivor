using Sandbox.Citizen;
using Sandbox.Code.Systems;

namespace Sandbox.Code.Actors;

public sealed class PlayerRagdollHandler : Component, IRagdollHandler
{
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public CitizenAnimationHelper AnimHelper { get; set; }
	[Property] public PlayerController Controller { get; set; }

	protected override void OnStart()
	{
		if ( Physics != null && Physics.Renderer == null )
		{
			Physics.Renderer = Renderer; // belt-and-suspenders if prefab wiring is ever missed

			Physics.Enabled = false;

		}
	}

	public void EnterRagdoll()
	{
		Controller.UseInputControls = false;
		AnimHelper.Enabled = false;
		Physics.Enabled = true;
	}

	public void ExitRagdoll()
	{
		Physics.Enabled = false;
		AnimHelper.Enabled = true;
		Controller.UseInputControls = true;
	}
}
