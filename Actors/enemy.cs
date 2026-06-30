namespace Sandbox.Code.Actors;

public sealed class Enemy : Actor
{
	[Property] public GameObject Target { get; set; }
}
