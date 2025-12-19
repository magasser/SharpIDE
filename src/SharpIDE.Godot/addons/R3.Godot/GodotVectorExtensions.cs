using Godot;

namespace R3;

public static class GodotVectorExtensions
{
    public static Vector2I ToVector2I(this Vector2 vector)
    {
        return new Vector2I((int)vector.X, (int)vector.Y);
    }
}