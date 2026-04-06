using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

using MouseMoveEventEvent = System.Action<Godot.Vector3>;
public partial class Game3d : Node3D
{
	[Export]
	PackedScene ChunkScene;

	[Export]
	PackedScene ControlUiScene;

	public static event MouseMoveEventEvent OnMouseMove;
	public static event System.Action OnMouseClicked;
	public static event System.Action OnMouseRightClicked;

	private Vector3 chunkSpawnPoint;
	private AnimatedSprite3D chunkSlicer;
	private Camera3D camera;
	private Vector3 normalCameraPosition;
	private Vector3 normalCameraRotation;
	public ObjectiveArea Obj;
	private ControlUi controlUi;
	private Godot.Timer timer;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		StaticBody3D conveyer = GetNode<StaticBody3D>("Conveyer");
		Obj = GetNode<ObjectiveArea>("ObjectiveArea");
		timer = GetNode<Godot.Timer>("Timer");
		chunkSlicer = GetNode<AnimatedSprite3D>("ChunkSlicer");
		chunkSlicer.SpriteFrames = Globals.Settings.GetSpriteFrames("placeholder");
		chunkSlicer.Play("idle");
		chunkSpawnPoint = new Vector3(-25f, conveyer.Position.Y + 1, conveyer.Position.Z);
		GetNode<Area3D>("DeletionArea").Position = new Vector3(35, conveyer.Position.Y, conveyer.Position.Z);
		this.camera = GetNode<Camera3D>("Camera3D");
		this.normalCameraPosition = this.camera.Position;
		this.normalCameraRotation = this.camera.Rotation;

		this.controlUi = ControlUiScene.Instantiate() as ControlUi;
		GetNode<SubViewport>("UiViewport").AddChild(this.controlUi);

		List<Vector2> objectiveVertices = new List<Vector2>()
		{
			new Vector2(-3, -4),
			new Vector2(4, 0),
			new Vector2(-3, 4)
		};
		this.Obj.InitializeArea(objectiveVertices);
		CreateNewChunk();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			broadcastMouseMove(mouseMotion.GlobalPosition);
		} 
		else if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed) // only fire on mouse down
			{
				broadcastMouseMove(mouseButton.GlobalPosition);
				OnMouseClicked.Invoke();
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
			{
				OnMouseRightClicked.Invoke();
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

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept")){
			this.sellChunks();
		}

		if (Input.IsActionPressed("ui_open_hud"))
		{
			this.camera.Position = this.camera.Position.Lerp(new Vector3(this.normalCameraPosition.X, this.normalCameraPosition.Y - 14f, this.normalCameraPosition.Z - 2f), 2f * (float)delta);
			this.camera.Rotation = this.camera.Rotation.Lerp(Vector3.Zero, 2f * (float)delta);
		} 
		else 
		{
			this.camera.Position = this.camera.Position.Lerp(this.normalCameraPosition, 5.0f * (float)delta);
			this.camera.Rotation = this.camera.Rotation.Lerp(this.normalCameraRotation, 5.0f * (float)delta);
		}

		this.controlUi.SetTimeRemaining(timer.TimeLeft);
	}

	public void AddChunkToScene(List<Vector2> topFaceVertices, Vector3 position)
	{
		Chunk chunk = ChunkScene.Instantiate() as Chunk;
		this.AddChild(chunk);
		chunk.InitializeFromPoints(topFaceVertices, chunkSpawnPoint);
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

	private void _on_timer_timeout()
	{
		CreateNewChunk();
	}

	private void CreateNewChunk()
	{
		GD.Print("spawning new chunk");
		List<Vector2> topFaceVertices = new List<Vector2>()
		{
			new Vector2(-2, -2),
			new Vector2(2, -2),
			new Vector2(3f, 0),
			new Vector2(2, 2),
			new Vector2(-2, 2),
			//new Vector2(-3f, 0),
		};

		this.AddChunkToScene(topFaceVertices, this.chunkSpawnPoint);
	}

	private void sellChunks()
	{
		ObjectiveChunkWorthInfo ocwi = Obj.SellChunks();
		this.controlUi.AddCash(ocwi.SellWorth);
	}


	private void _on_conveyer_area_body_entered(Node3D body)
	{
		if (body is Chunk c)
		{
			c.setOnConveyer(true);
		}
	}

	private void _on_conveyer_area_body_exited(Node3D body)
	{
		if (body is Chunk c)
		{
			c.setOnConveyer(false);
		}
	}

	private void _on_deletion_area_body_entered(Node3D body)
	{
		if (body is Chunk c)
		{
			GD.Print("lost chunk!");
			c.QueueFree();
		}
	}
}
