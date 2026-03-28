using Godot;
using Steamworks.ServerList;
using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

public partial class Chunk : RigidBody3D
{
	public static float CHUNK_HEIGHT = 1f;
	private MeshInstance3D chunkMesh;
	private CollisionShape3D collisionShape;
	private bool pickedUp = false;
	private Vector3 TargetPosition;

	private List<Vector2> topFaceVertices;
	// Called when the node enters the scene tree for the first time.
	public Chunk()
	{
		topFaceVertices = new List<Vector2>();
	}
	
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
		this.TargetPosition = pos;
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
		this.LinearVelocity = new Vector3(0, 0, 0);
		this.AngularVelocity = new Vector3(0, 0, 0);
		//this.collisionShape.Disabled = true;
		this.pickedUp = true;
		
		
	}

	private void putDown()
	{
		this.pickedUp = false;
		//this.collisionShape.Disabled = false;
		this.GravityScale = 1;
	}

	public Chunk sliceInTwain(Vector2 start, Vector2 end)
	{
		
		return this;
	}


	public void InitializeFromPoints(List<Vector2> topFaceVertices, Vector3 position)
	{
		SurfaceTool surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		this.topFaceVertices = topFaceVertices;

		// top face vertices + bottom face vertices + top middle vertex + bottom middle vertex
		int numberOf3dVertices = 2 * topFaceVertices.Count + 2;
		Span<Vector3> vertices = stackalloc Vector3[numberOf3dVertices];
		//List<Vector3> shapeVertices = new List<Vector3>(); // used for the polygon shape, doesn't include central points on top or bottom

		float avgX = 0f;
		float avgZ = 0f;
		for (int i = 0; i < topFaceVertices.Count; i++)
		{
			// topFaceVertices is really the x and z coords, not x and y.
			Vector3 currentTopFaceVector = new Vector3(topFaceVertices[i].X, CHUNK_HEIGHT, topFaceVertices[i].Y);
			Vector3 currentBottomFaceVector = new Vector3(topFaceVertices[i].X, 0f, topFaceVertices[i].Y);
			avgX += currentTopFaceVector.X;
			avgZ += currentTopFaceVector.Z;
			vertices[i + 1] = currentTopFaceVector;
			vertices[topFaceVertices.Count + 1 + i + 1] = currentBottomFaceVector;

			// shapeVertices.Add(currentTopFaceVector);
			// shapeVertices.Add(currentBottomFaceVector);
		}

		avgX /= topFaceVertices.Count;
		avgZ /= topFaceVertices.Count;
		Vector3 TOP_MIDDLE = new Vector3(avgX, CHUNK_HEIGHT, avgZ);
		Vector3 BOTTOM_MIDDLE = new Vector3(avgX, 0f, avgZ);

		vertices[0] = TOP_MIDDLE;
		vertices[topFaceVertices.Count + 1] = BOTTOM_MIDDLE;

		// example box vertices
		// #0, 1, 0
		// #-1, 1, -1
		// #1, 1, -1
		// #1, 1, 1
		// #-1, 1, 1
		// #0, 0, 0
		// #-1, 0, -1
		// #1, 0, -1
		// #1, 0, 1
		// #-1, 0, 1

		int numberOfTriangles = 2 * topFaceVertices.Count; // top and bottom faces
		numberOfTriangles += 2 * topFaceVertices.Count; // each side wall consists of 2 triangles
		int numberOfIndices = numberOfTriangles * 3; // 3 points for each triangle
		Span<int> indices = stackalloc int[numberOfIndices];
		int indicesPerVertex = 4 * 3;
		for (int i = 0; i < topFaceVertices.Count; i++)
		{
			// top triangle
			indices[indicesPerVertex * i] = 0;
			indices[indicesPerVertex * i + 1] = i + 1;
			if (i + 2 > topFaceVertices.Count)
			{
				indices[indicesPerVertex * i + 2] = 1;
			}
			else
			{
				indices[indicesPerVertex * i + 2] = i + 2;
			}

			// bottom triangle
			indices[indicesPerVertex * i + 3] = topFaceVertices.Count + 1;
			indices[indicesPerVertex * i + 5] = i + topFaceVertices.Count + 1 + 1;
			if (i + 2 > topFaceVertices.Count)
			{
				indices[indicesPerVertex * i + 4] = topFaceVertices.Count + 1 + 1;
			}
			else
			{
				indices[indicesPerVertex * i + 4] = i + topFaceVertices.Count + 1 + 2;
			}

			// side wall upper
			indices[indicesPerVertex * i + 6] = indices[indicesPerVertex * i + 4];
			indices[indicesPerVertex * i + 7] = indices[indicesPerVertex * i + 2];
			indices[indicesPerVertex * i + 8] = indices[indicesPerVertex * i + 1];
			

			// side wall lower
			indices[indicesPerVertex * i + 9] = indices[indicesPerVertex * i + 5];
			indices[indicesPerVertex * i + 10] = indices[indicesPerVertex * i + 4];
			indices[indicesPerVertex * i + 11] = indices[indicesPerVertex * i + 1];
		}
		

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

		// TODO collision shape will bound the mesh, even if concave, just stick with convex shapes for now,
		// performing the 'create multiple convex siblings' functionality here is probably going to be some
		// annoying math..
		ConvexPolygonShape3D polygonShape = new ConvexPolygonShape3D();
		polygonShape.Points = vertices.ToArray();
		this.collisionShape.Shape = polygonShape;
	}
}
