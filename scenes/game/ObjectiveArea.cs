using Godot;
using System;
using System.Collections.Generic;

public partial class ObjectiveArea : Area3D
{
	private List<Vector2> topFaceVerticesAbsolute;

	public ObjectiveArea()
	{
		topFaceVerticesAbsolute = new List<Vector2>();
	}

	public bool IsChunkWithinArea(Chunk c)
	{
		Vector2[] topFaceAsArray = this.topFaceVerticesAbsolute.ToArray();
		foreach (Vector2 v in c.GetRotatedTopFaceVertices())
		{
			Vector2 absolute = new Vector2(c.GlobalPosition.X + v.X, c.GlobalPosition.Z + v.Y);
			if (!Geometry2D.IsPointInPolygon(absolute, topFaceAsArray))
			{
				return false;
			}
		}

		return true;
	}

	public void InitializeArea(List<Vector2> vertices)
	{
		topFaceVerticesAbsolute = new List<Vector2>();
		List<Vector3> objVertices = new List<Vector3>();
		foreach (Vector2 v in vertices)
		{
			topFaceVerticesAbsolute.Add(new Vector2(this.GlobalPosition.X + v.X, this.GlobalPosition.Z + v.Y));
			objVertices.Add(new Vector3(v.X, Chunk.CHUNK_HEIGHT, v.Y));
			objVertices.Add(new Vector3(v.X, 0f, v.Y));
		}

		ConvexPolygonShape3D polygonShape = new ConvexPolygonShape3D();
		polygonShape.Points = objVertices.ToArray();
		GetNode<CollisionShape3D>("CollisionShape3D").Shape = polygonShape;
	}
}
