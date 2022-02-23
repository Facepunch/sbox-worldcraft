using Sandbox;

namespace WorldCraft
{
	public partial class EditorPawn
	{
		public override void Spawn()
		{
			SetModel( "models/Ghost.vmdl" );

			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;
		}

		private void UpdatePosition( Client cl )
		{
			Position = Input.Position;
			Rotation = Input.Rotation;
			EyeRotation = Input.Rotation;
		}
	}
}
