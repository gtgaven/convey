using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

using MouseMoveEventEvent = System.Action<Godot.Vector3>;
public partial class Game3d : Node3D
{
	[Export]
	PackedScene ChunkScene;

	public static event MouseMoveEventEvent OnMouseMove;
	public static event System.Action OnMouseReleased;

	private Vector3 chunkSpawnPoint;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		StaticBody3D conveyer = GetNode<StaticBody3D>("Conveyer");
		chunkSpawnPoint = new Vector3(-25f, conveyer.Position.Y + 1, conveyer.Position.Z);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			broadcastMouseMove(mouseMotion.GlobalPosition);
		} 
		else if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.Pressed)
			{
				broadcastMouseMove(mouseButton.GlobalPosition);
			}
			else
			{
				OnMouseReleased.Invoke();
			}
		}
	}

	private void broadcastMouseMove(Vector2 globalPosition)
	{
		Godot.Vector3? pos = GetMousePosition(globalPosition);
		if (pos is not null)
		{
			Godot.Vector3 p = (Godot.Vector3)pos;
			Godot.Vector3 clickAndDragPosition = new Godot.Vector3(p.X, p.Y + 2, p.Z);
			OnMouseMove.Invoke(clickAndDragPosition);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
		if (Input.IsActionJustPressed("ui_accept")){
			GD.Print("spawning new chunk");

			Chunk chunk = ChunkScene.Instantiate() as Chunk;
			this.AddChild(chunk);
			List<Vector2> topFaceVertices = new List<Vector2>()
			{
				new Vector2(-1, -1),
				new Vector2(1, -1),
				new Vector2(1.5f, 0),
				new Vector2(1, 1),
				new Vector2(-1, 1),
				new Vector2(-1.5f, 0),
			};
			chunk.InitializeFromPoints(topFaceVertices, chunkSpawnPoint);
		}
	}

	public Vector3? GetMousePosition(Vector2 mouseCoordinate)
	{
		
		Vector3 start = GetViewport().GetCamera3D().ProjectRayOrigin(mouseCoordinate);
		Vector3 end = GetViewport().GetCamera3D().ProjectPosition(mouseCoordinate, 1000);
		PhysicsRayQueryParameters3D prq = PhysicsRayQueryParameters3D.Create(start, end, 1, null);
		Godot.Collections.Dictionary ret = GetWorld3D().DirectSpaceState.IntersectRay(prq);
		if (ret.ContainsKey("position"))
		{
			return (Vector3)ret["position"];
		}

		return null;
	}
}
