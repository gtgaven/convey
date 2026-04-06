using Godot;

class GeneralUtilities 
 {
    public static float AreaOfTriangle(Vector2 b, Vector2 c)
	{
		// triangle made up of (0,0), (b.X,b.Y), (c.X,c.Y)
		return 0.5f * Mathf.Abs((b.X * c.Y) + (c.X * (0f - b.Y)));
	}
}