using Sandbox;
using System;
using System.Linq;

namespace WorldCraft
{
	public class FreeCamera : CameraMode
	{
		Angles LookAngles;
		Vector3 MoveInput;
		float MoveSpeed;

		public override void Update()
		{
			Position += MoveInput.Normal * 300 * RealTime.Delta * Rotation * MoveSpeed;
			Rotation = Rotation.From( LookAngles );
			FieldOfView = 80;
			Viewer = Local.Pawn;

			if ( Input.Down( InputButton.Jump ) )
			{
				var editorPawn = Local.Pawn as EditorPawn;
				if( editorPawn.CurrentTool is SelectionTool selection && selection.SelectedEntity != null )
				{
					Focus( selection.SelectedEntity );
				}
			}
		}

		public override void BuildInput( InputBuilder input )
		{
			if ( !input.Down( InputButton.Attack2 ) )
			{
				MoveInput = Vector3.Zero;
				return;
			}

			MoveInput = input.AnalogMove;
			LookAngles += input.AnalogLook.WithRoll(0);

			MoveSpeed = 1.0f;
			if ( input.Down( InputButton.Run ) ) MoveSpeed = 5.0f;
			if ( input.Down( InputButton.Duck ) ) MoveSpeed = 0.2f;

			input.ViewAngles = LookAngles;
			input.Position = Position;

			// input.ClearButtons();
			input.StopProcessing = true;
		}

		public void Focus(PrimitiveEntity primitive)
		{
			var bb = primitive.WorldSpaceBounds;
			var focusDist = 2.0f;
			var maxSize = new[] { bb.Size.x, bb.Size.y, bb.Size.z }.Max();
			var cameraView = 2.0f * (float)Math.Tan( 0.5f * 0.017453292f * FieldOfView );
			var distance = focusDist * maxSize / cameraView; 
			distance += 0.5f * maxSize;
			Position = bb.Center - distance * Rotation.Forward;
		}

	}
}
