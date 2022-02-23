using Sandbox;

namespace WorldCraft
{
	/// <summary>
	/// Display any type of Primitive as a networked entity.
	/// This can also be created clientside as a ghost entity.
	/// </summary>
	public partial class PrimitiveEntity : ModelEntity
	{
		[Net, Change( nameof( CreateMesh ) )]
		public BasePrimitive Primitive { get; set; }

		public override void Spawn()
		{
			CreateMesh();
		}

		public override void ClientSpawn()
		{
			CreateMesh();
		}

		public void CreateMesh()
		{
			if ( Primitive == null ) return;

			Model = Primitive?.BuildModel();
			SetupPhysicsFromModel( PhysicsMotionType.Static );
			EnableAllCollisions = true;
		}
	}
}
