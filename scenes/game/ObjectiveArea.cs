using Godot;
using System;
using System.Collections.Generic;

public class ObjectiveChunkWorthInfo
{
	public float PercentageFilled {get; set;}
	public float SellWorth {get; set;}
	public int NumChunks {get; set;}
	public ObjectiveChunkWorthInfo()
	{
		PercentageFilled = 0f;
		SellWorth = 0f;
		NumChunks = 0;
	}
}

public partial class ObjectiveArea : Area3D
{
	private List<Vector2> topFaceVerticesAbsolute;
	private float area;

	public ObjectiveArea()
	{
		topFaceVerticesAbsolute = new List<Vector2>();
		area = 0f;
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

		// TODO calculate area
	}

	public ObjectiveChunkWorthInfo SellChunks()
	{
		return this.IterateOverChunksInObjective(true);
	}

	public ObjectiveChunkWorthInfo IterateOverChunksInObjective(bool deleteChunks)
	{
		ObjectiveChunkWorthInfo ocwi = new ObjectiveChunkWorthInfo();
		float totalAreaOfChunks = 0f;
		foreach (Node n in GetParent().GetChildren())
		{
			if (n is Chunk c)
			{
				if (this.IsChunkWithinArea(c))
				{
					totalAreaOfChunks += c.GetArea();
					ocwi.NumChunks++;
					if (deleteChunks)
					{
						c.QueueFree();
					}

				}
			}
		}

		if (totalAreaOfChunks > this.area)
		{
			GD.Print("Error, chunk area is greater than objective area..?");
		}

		ocwi.PercentageFilled = (totalAreaOfChunks / this.area);
		ocwi.SellWorth = 50;//TODO
		return ocwi;
	}
}
