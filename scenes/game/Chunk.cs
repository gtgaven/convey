using Godot;
using Steamworks.ServerList;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

public partial class Chunk : RigidBody3D
{
	public static float CHUNK_HEIGHT = 1f;
	public static float MOUSE_WHEEL_SENSITIVITY = 5f;
	public static float MAX_AREA = 25f;
	public static float MIN_AREA = 0.5f;
	private MeshInstance3D chunkMesh;
	private CollisionShape3D collisionShape;
	private bool pickedUp = false;
	private bool hoveredOver = false;
	private Vector3 TargetPosition;
	private bool onConveyer;
	private float area;
	private int numExpansions;
	enum VisualState
	{
		NONE,
		HOVERED,
		PICKED_UP,
		IN_OBJ
	}

	private List<Vector2> topFaceVertices;
	// Called when the node enters the scene tree for the first time.
	public Chunk()
	{
		area = 0f;
		topFaceVertices = new List<Vector2>();
	}
	
	public override void _Ready()
	{
		Game3d.OnMouseMove += OnMouseMoveCallback;
		Game3d.OnMouseClicked += OnMouseClickedCallback;
		Game3d.OnMouseRightClicked += OnMouseRightClickedCallback;
		this.chunkMesh = GetNode<MeshInstance3D>("ChunkMesh");
		this.collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
	}

	public override void _ExitTree()
	{
		Game3d.OnMouseMove -= OnMouseMoveCallback;
		Game3d.OnMouseClicked -= OnMouseClickedCallback;
		Game3d.OnMouseRightClicked -= OnMouseRightClickedCallback;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_slice_chunk"))
		{
			if (this.onConveyer)
			{
				float sliceX = GetParent().GetNode<AnimatedSprite3D>("ChunkSlicer").Position.X - this.Position.X;
				this.sliceInTwain(sliceX);
			}
		}

		if (this.pickedUp)
		{
			//Position = Position.Lerp(TargetPosition, (float)(delta * 2));
			Position = this.TargetPosition;
			DetermineAndSetVisualState();
			

			if (Input.IsActionJustPressed("ui_zoom_in"))
			{
				this.expand(false);
			} 
			else if (Input.IsActionJustPressed("ui_zoom_out"))
			{
				this.expand(true);
			}
			else
			{
				// need these inside else because zoom is also a rotate due to ctrl + wheel
				if (Input.IsActionJustPressed("ui_rotate_up"))
				{
					GD.Print("rotating up");
					this.RotateY(MOUSE_WHEEL_SENSITIVITY * (float)delta);
				}

				if (Input.IsActionJustPressed("ui_rotate_down"))
				{
					GD.Print("rotating down");
					this.RotateY(-1f * MOUSE_WHEEL_SENSITIVITY * (float)delta);
				}
			}
		}
	}

	private void _on_mouse_entered()
	{
		this.setHoveredOver(true);
	}


	private void _on_mouse_exited()
	{
		this.setHoveredOver(false);
	}


	public void setOnConveyer(bool onConveyer)
	{
		this.onConveyer = onConveyer;
	}

	private void setVisualState(VisualState vs)
	{
		StandardMaterial3D material = new StandardMaterial3D();
		switch (vs)
		{
		case VisualState.NONE:
			material.AlbedoColor = new Color(0.9f, 0.1f, 0.1f);
			break;
		case VisualState.HOVERED:
			material.AlbedoColor = new Color(0.9f, 0.1f, 0.3f);
			break;
		case VisualState.PICKED_UP:
			material.AlbedoColor = new Color(0.9f, 0.9f, 0.3f);
			break;
		case VisualState.IN_OBJ:
			material.AlbedoColor = new Color(0.1f, 0.1f, 0.9f);
			break;
		default:
			GD.Print("unhandled visual state " + vs);
			break;
		}
	
		Texture2D texture = Globals.Settings.GetSpriteFrames("placeholder").GetFrameTexture("idle", 0);
		material.AlbedoTexture = texture;
		this.chunkMesh.Mesh.SurfaceSetMaterial(0, material); // Set material for the first surface
	}

	private void setHoveredOver(bool hovered)
	{
		this.hoveredOver = hovered;
		if (hovered)
		{
			this.setVisualState(VisualState.HOVERED);
		}
		else
		{
			// cant simply go back to 'none', might be in OBJ
			DetermineAndSetVisualState();
		}
		
	}

	private void DetermineAndSetVisualState()
	{
		bool isChunkWithinArea = GetParent<Game3d>().Obj.IsChunkWithinArea(this);
		if (isChunkWithinArea)
		{
			this.setVisualState(VisualState.IN_OBJ);
		}
		else if (pickedUp)
		{
			this.setVisualState(VisualState.PICKED_UP);
		}
		else if (this.hoveredOver)
		{
			this.setVisualState(VisualState.HOVERED);
		}
		else
		{
			this.setVisualState(VisualState.NONE);
		}
	}

	
	private void OnMouseMoveCallback(Godot.Vector3 pos)
	{
		this.TargetPosition = pos;
	}

	private void OnMouseClickedCallback()
	{
		if (this.hoveredOver && !this.pickedUp)
		{
			this.pickUp();
		}
		else
		{
			this.putDown();
		}
	}

	private void OnMouseRightClickedCallback()
	{
		if (this.pickedUp)
		{
			//this.Rotation = new Vector3(this.Rotation.X, this.Rotation.Y + (Mathf.Pi / 2), this.Rotation.Z);
			this.RotateY(Mathf.Pi / 2);
		}
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("position x = " + this.Position.X + " this rotationY = " + this.RotationDegrees.Y + "\n");
		foreach (Vector2 v in this.topFaceVertices)
		{
			sb.Append("" + v.X + ", " + v.Y + "\n");
		}

		return sb.ToString();
	}

	private void pickUp()
	{
		this.GravityScale = 0;
		this.LinearVelocity = new Vector3(0, 0, 0);
		//this.AngularVelocity = new Vector3(0, 0, 0);
		//this.collisionShape.Disabled = true;
		this.pickedUp = true;
		this.DetermineAndSetVisualState();
	}

	private void putDown()
	{
		this.pickedUp = false;
		//this.collisionShape.Disabled = false;
		this.GravityScale = 1;
		this.LinearVelocity = new Vector3(0,0,0);
		this.DetermineAndSetVisualState();
	}

	private Vector2? getIntersectionPointBetween(Vector2 v0, Vector2 v1, float xOffset)
	{
		if (v0.X == v1.X)
		{
			// cant intersect with a vertical segment
			return null;
		}

		if (v0.X == xOffset)
		{
			return v0;
		}

		bool startIsLessThanOffset = false;
		if (v0.X < xOffset)
		{
			startIsLessThanOffset = true;	
		}

		if (startIsLessThanOffset && (v1.X <= xOffset))
		{
			return null;
		}

		if (!startIsLessThanOffset && (v1.X >= xOffset))
		{
			return null;
		}
		
		float slope = (v1.Y - v0.Y) / (v1.X - v0.X);
		// y = mx + b
		// b = y - mx
		float yIntercept = v0.Y - slope * v0.X;
		return new Vector2(xOffset, (slope * xOffset) + yIntercept);
	}

	public List<Vector2> GetRotatedTopFaceVertices()
	{
		double angle = this.Rotation.Y * -1f;
		List<Vector2> rotatedVertices = new List<Vector2>();
		foreach (Vector2 v in this.topFaceVertices)
		{
			double rotatedX = ((double)v.X * Math.Cos(angle)) - ((double)v.Y * Math.Sin(angle));
			double rotatedY = ((double)v.X * Math.Sin(angle)) + ((double)v.Y * Math.Cos(angle));
			rotatedVertices.Add(new Vector2((float)rotatedX, (float)rotatedY));
		}

		return rotatedVertices;
	}

	public void sliceInTwain(float xOffset)
	{
		List<Vector2> rotatedVertices = new List<Vector2>();
		rotatedVertices = GetRotatedTopFaceVertices();
		int numberOfIntersections = 0;
		List<Vector2> leftVertices = new List<Vector2>();
		List<Vector2> rightVertices = new List<Vector2>();
		bool addingToLeft = this.RotationDegrees.Y < 90f && this.RotationDegrees.Y > -90f;

		for (int i = 0; i < rotatedVertices.Count; i++)
		{
			Vector2 v0 = rotatedVertices[i];
			Vector2 v1;
			if (i == rotatedVertices.Count - 1)
			{
				// calculate intersect from last vertex to the first one,
				// obviously only works because the vertices are in order.
				v1 = rotatedVertices[0];
			}
			else
			{
				v1 = rotatedVertices[i + 1];
			}

			Vector2? possible = getIntersectionPointBetween(v0, v1, xOffset);
			if (possible is null)
			{
				if (addingToLeft)
				{
					leftVertices.Add(v0);
				}
				else
				{
					rightVertices.Add(v0);
				}
			}
			else
			{
				Vector2 intersection = (Vector2)possible;
				numberOfIntersections++;
				if (intersection.X == v0.X)
				{
					// special case, intersection is on a vertex
					leftVertices.Add(v0);
					rightVertices.Add(v0);
				}
				else
				{
					if (addingToLeft)
					{
						leftVertices.Add(v0);
						leftVertices.Add(intersection);
						rightVertices.Add(intersection);
					}
					else
					{
						rightVertices.Add(v0);
						rightVertices.Add(intersection);
						leftVertices.Add(intersection);
					}
				}

				addingToLeft = !addingToLeft;
			}
		}

		if (numberOfIntersections != 2)
		{
			GD.Print("sliceInTwain, but intersections not equal to 2, was " + numberOfIntersections);
			return;
		}

		// TODO this needs to change from this.Position to take into account new center, no?
		this.InitializeFromPoints(leftVertices, this.Position);
		PackedScene ps = GD.Load<PackedScene>("res://scenes/game/Chunk.tscn");
		Chunk rightChunk = ps.Instantiate() as Chunk;
		GetParent().AddChild(rightChunk);
		rightChunk.InitializeFromPoints(rightVertices, this.Position, 0.3f, this.numExpansions);
	}

	private void expand(bool zoomOut)
	{
		if (zoomOut && this.area > MAX_AREA)
		{
			GD.Print("can't expand");
			return;
		}
		else if (!zoomOut && this.area < MIN_AREA)
		{
			GD.Print("can't shrink");
			return;
		}
		List<Vector2> rotatedVertices = GetRotatedTopFaceVertices();
		List<Vector2> expandedTopFaceVertices = new List<Vector2>();
		foreach (Vector2 v in rotatedVertices)
		{
			float distanceFromOrigin = Mathf.Sqrt((v.X * v.X) + (v.Y * v.Y));
			float angle = Mathf.Atan2(v.Y, v.X);
			if (zoomOut)
			{
				distanceFromOrigin += MOUSE_WHEEL_SENSITIVITY * .02f;
			}
			else
			{
				distanceFromOrigin -= MOUSE_WHEEL_SENSITIVITY * .02f;
			}

			expandedTopFaceVertices.Add(new Vector2(distanceFromOrigin * Mathf.Cos(angle), distanceFromOrigin * Mathf.Sin(angle)));
		}

		this.InitializeFromPoints(expandedTopFaceVertices, this.Position, removeIfTooSmall:false);
		this.numExpansions++;
	}

	public void InitializeFromPoints(List<Vector2> topFaceVertices, Vector3 position, float nudgeRight = 0f, int startingNumExpansions = 0, bool removeIfTooSmall = true)
	{
		this.numExpansions = startingNumExpansions;
		this.area = 0f;
		this.Rotation = new Vector3(0,0,0);
		SurfaceTool surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		// top face vertices + bottom face vertices + top middle vertex + bottom middle vertex
		int numberOf3dVertices = 2 * topFaceVertices.Count + 2;
		Span<Vector3> vertices = stackalloc Vector3[numberOf3dVertices];

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
		}

		avgX /= topFaceVertices.Count;
		avgZ /= topFaceVertices.Count;
		Vector3 TOP_MIDDLE = new Vector3(avgX, CHUNK_HEIGHT, avgZ);
		Vector3 BOTTOM_MIDDLE = new Vector3(avgX, 0f, avgZ);

		vertices[0] = TOP_MIDDLE;
		vertices[topFaceVertices.Count + 1] = BOTTOM_MIDDLE;

		// re-zero middle as "this.Position"
		//////////////
		for (int i = 0; i < numberOf3dVertices; i++)
		{
			vertices[i].X -= TOP_MIDDLE.X;
			vertices[i].Z -= TOP_MIDDLE.Z;
		}

		AnimatedSprite3D center = GetNode<AnimatedSprite3D>("CenterPoint");
		center.Position = vertices[0];
		center.SpriteFrames = Globals.Settings.GetSpriteFrames("placeholder");
		center.Play("idle");

		this.topFaceVertices = new List<Vector2>();
		foreach (Vector2 v in topFaceVertices)
		{
			this.topFaceVertices.Add(new Vector2(v.X - TOP_MIDDLE.X, v.Y - TOP_MIDDLE.Z));
		}
		//////////////


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
			if (i != topFaceVertices.Count - 1)
			{
				this.area += GeneralUtilities.AreaOfTriangle(this.topFaceVertices[i], this.topFaceVertices[i+1]);
			}
			else
			{
				this.area += GeneralUtilities.AreaOfTriangle(this.topFaceVertices[i], this.topFaceVertices[0]);
			}
			
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

		GD.Print("######" + this.area);
		if (removeIfTooSmall && (this.area < MIN_AREA))
		{
			GD.Print("deleting chunk due to min area check");
			destroy();
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
		
		this.setVisualState(VisualState.NONE);
		this.Position = new Vector3(position.X + TOP_MIDDLE.X + nudgeRight, position.Y, position.Z + TOP_MIDDLE.Z);
		this.TargetPosition = this.Position;

		// TODO collision shape will bound the mesh, even if concave, just stick with convex shapes for now,
		// performing the 'create multiple convex siblings' functionality here is probably going to be some
		// annoying math..
		ConvexPolygonShape3D polygonShape = new ConvexPolygonShape3D();
		polygonShape.Points = vertices.ToArray();
		this.collisionShape.Shape = polygonShape;
	}

	private void destroy()
	{
		// TODO play some animation, go up in smoke etc		
		this.QueueFree();
	}

	public float GetArea()
	{
		return this.area;
	}
}
