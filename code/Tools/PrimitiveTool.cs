using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace WorldCraft
{
	[Library( "tool_primitive", Title = "primitive", Icon = "CubeTool.png" )]
	public partial class PrimitiveTool : BaseTool
	{
		public enum StateEnum
		{
			Select,
			Drag,
			Extrude
		};

		public enum PrimitiveType
		{
			Box,
			Cylinder,
			Sphere
		}

		public StateEnum State = StateEnum.Select;

		public Vector3 Start;
		public Vector3 End;
		public Plane Plane;
		public Line ExtrudeLine;

		Grid Grid;
		float NextInputDelay;

		static PrimitiveType SelectedType = PrimitiveType.Box;
		static Panel SelectedTypeBtn;

		~PrimitiveTool()
		{
			// shit
			Grid?.Delete();
		}

		public override void FrameSimulate( Client cl )
		{
			// todo: need to delete grid when tool is deactivated
			if ( !Grid.IsValid() )
				Grid = new Grid();

			if( NextInputDelay > 0 )
			{
				NextInputDelay -= Time.Delta;
				return;
			}

			switch ( State )
			{
				case StateEnum.Select:
					Select();
					break;
				case StateEnum.Drag:
					Drag();
					break;
				case StateEnum.Extrude:
					Extrude();
					break;
			}
		}

		public override void BuildOptionsSheet( Panel panel )
		{
			base.BuildOptionsSheet( panel );

			panel.Add.Label( "Primitive Type", "heading" );

			// todo : wrap up active button state somewhere nice?
			foreach(PrimitiveType t in Enum.GetValues(typeof(PrimitiveType)))
			{
				var btn = panel.Add.Button( t.ToString() );
				btn.AddEventListener( "onclick", () =>
				{
					btn.AddClass( "active" );
					SelectedTypeBtn?.RemoveClass( "active" );
					SelectedTypeBtn = btn;
					SelectedType = t;
				} );

				if( t == SelectedType )
				{
					btn.AddClass( "active" );
					SelectedTypeBtn = btn;
				}
			}
		}

		void Select()
		{
			var trace = Trace.Ray( Input.Cursor, 5000.0f ).Run();
			if ( !trace.Hit ) return;

			if ( Grid.IsValid() )
			{
				Grid.Position = trace.EndPosition.SnapToGrid( Game.GridSize ) + trace.Normal * 0.05f;
				Grid.Rotation = Rotation.From( 0, 0, 0 );
				Grid.Origin = trace.EndPosition.SnapToGrid( Game.GridSize );
				Grid.Normal = trace.Normal;
			}

			Start = trace.EndPosition.SnapToGrid( Game.GridSize );
			End = Start;

			DebugOverlay.Sphere( Start, 2, Color.Blue );

			Plane = new Plane( Start, trace.Normal );

			if ( Input.Pressed( InputButton.Attack1 ) )
			{
				Sound.FromScreen( "start" );
				State = StateEnum.Drag;
			}
		}

		void Drag()
		{
			var end = Plane.Trace( Input.Cursor );
			if ( end == null ) return;

			End = end.Value.SnapToGrid( Game.GridSize );

			Draw();

			// Make sure LMB is still held down
			if ( Input.Down( InputButton.Attack1 ) ) return;

			// Sanity check
			if ( Start.IsNearlyEqual( End ) )
			{
				Sound.FromScreen( "fail" );
				State = StateEnum.Select;
				NextInputDelay = .1f;
				return;
			}

			ExtrudeLine = new Line( End - Plane.Normal * 10000, End + Plane.Normal * 10000 );

			Sound.FromScreen( "stage" );
			State = StateEnum.Extrude;
		}

		void Extrude()
		{
			End = ExtrudeLine.ClosestPoint( Input.Cursor ).SnapToGrid( Game.GridSize );

			Draw();

			if ( Input.Down( InputButton.Attack1 ) )
			{
				Sound.FromScreen( "finish" );
				State = StateEnum.Select;
				FinalizeBuild();
				NextInputDelay = .1f;
			}
		}

		void Draw()
		{
			var bounds = new BBox( Vector3.Min( Start, End ), Vector3.Max( Start, End ) );

			DebugOverlay.Box( Start, End, Color.Yellow, 0, false );
			DebugOverlay.Text( bounds.Center + Vector3.OneY * bounds.Size.y / 2, $"X: { bounds.Size.x }", Color.Yellow, 0, 5000 );
			DebugOverlay.Text( bounds.Center + Vector3.OneX * bounds.Size.x / 2, $"Y: { bounds.Size.y }", Color.Yellow, 0, 5000 );

			if ( State == StateEnum.Extrude )
			{
				DebugOverlay.Line( ExtrudeLine.a, ExtrudeLine.b, Color.Red );
				DebugOverlay.Text( ExtrudeLine.a.WithZ( Start.z + bounds.Size.z ), $"Z: { bounds.Size.z }", Color.Yellow, 0, 5000 );
			}
		}

		public void FinalizeBuild()
		{
			var bounds = new BBox( Vector3.Min( Start, End ), Vector3.Max( Start, End ) );

			if ( bounds.Volume.AlmostEqual( 0 ) )
			{
				Sandbox.UI.ChatBox.AddInformation( "Invalid block twat" );
				return;
			}

			PlaceServerside( SelectedType, bounds.Center, bounds.Size );
		}

		[ServerCmd]
		public static void PlaceServerside( PrimitiveType type, Vector3 position, Vector3 size )
		{
			var entity = new PrimitiveEntity();
			entity.Position = position;

			BasePrimitive primitive = null;

			// bet we can do something better here
			switch ( type )
			{
				case PrimitiveType.Box:
					primitive = new PrimitiveBox();
					break;
				case PrimitiveType.Cylinder:
					primitive = new PrimitiveCylinder();
					break;
				case PrimitiveType.Sphere:
					primitive = new PrimitiveSphere();
					break;
			}

			if ( primitive == null )
			{
				// eh
				return;
			}

			primitive.Origin = position;
			primitive.Size = size;
			primitive.Entity = entity; // Circular dependency bad

			entity.Primitive = primitive;
			entity.CreateMesh();
		}
	}
}
