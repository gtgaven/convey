using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

public partial class Chunk : RigidBody3D
{
	private MeshInstance3D chunkMesh;
	private CollisionShape3D collisionShape;
	private bool pickedUp = false;
	private Vector3 TargetPosition;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Game3d.OnMouseMove += OnMouseMoveCallback;
		Game3d.OnMouseReleased += OnMouseReleasedCallback;
		this.chunkMesh = GetNode<MeshInstance3D>("ChunkMesh");
		this.collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
	}

	public override void _ExitTree()
	{
		Game3d.OnMouseMove -= OnMouseMoveCallback;
		Game3d.OnMouseReleased -= OnMouseReleasedCallback;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Position = Position.Lerp(TargetPosition, (float)(delta * 2));
		if (pickedUp)
		{
			Position = this.TargetPosition;
		}
		
	}

	private void _on_static_body_3d_mouse_entered()
	{
		GD.Print("here:");
	}

	
	private void OnMouseMoveCallback(Godot.Vector3 pos)
	{
		GD.Print(pos);
		if (pickedUp)
		{
			this.TargetPosition = pos;
		}
	}

	private void OnMouseReleasedCallback()
	{
		this.putDown();
	}

	private void _on_input_event(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shape_idx)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.Pressed)
			{
				pickUp();
			}
		}
	}


	private void pickUp()
	{
		this.GravityScale = 0;
		//this.collisionShape.Disabled = true;
		this.pickedUp = true;
		
		
	}

	private void putDown()
	{
		this.pickedUp = false;
		//this.collisionShape.Disabled = false;
		this.GravityScale = 1;
	}


	public void InitializeFromPoints(Vector3 position)
	{
		SurfaceTool surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		// 3. Define the vertices and associated data (UVs, normals, colors, etc.)

		// Vertices of a simple plane made of two triangles
		List<Vector3> vertices = new List<Vector3>()
		{
			new Vector3(-1, 0, -1),
			new Vector3(1, 0, -1),
			new Vector3(1, 0, 1),
			new Vector3(-1, 0, 1)
		};

		// Indices to form two triangles: (0, 1, 2) and (0, 2, 3)
		List<int> indices = new List<int>()
		{
			0, 1, 2,
			0, 2, 3
		};

		// Add the vertices and indices
		foreach (Vector3 vertex in vertices)
		{
			surfaceTool.AddVertex(vertex);
		}
		foreach (int index in indices)
		{
			surfaceTool.AddIndex(index);
		}
		
		// 4. Generate normals for correct lighting
		// This is important for 3D lighting to work correctly
		surfaceTool.GenerateNormals();

		// 5. Commit the mesh to an ArrayMesh resource
		ArrayMesh arrayMesh = surfaceTool.Commit();

		// 6. Assign the generated mesh to the MeshInstance3D
		this.chunkMesh.Mesh = arrayMesh;
		
		// Optional: Assign a material to make it visible (e.g., a standard unshaded material)
		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = new Color(0.9f, 0.1f, 0.1f);
		this.chunkMesh.Mesh.SurfaceSetMaterial(0, material); // Set material for the first surface
		this.Position = position;
		this.TargetPosition = position;

		BoxShape3D collisionBox = new BoxShape3D();
		collisionBox.Size = new Vector3(2, 2, 2);
		this.collisionShape.Shape = collisionBox;
	}
}
