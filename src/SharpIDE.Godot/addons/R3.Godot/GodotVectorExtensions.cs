using Godot;

namespace R3;

public static class GodotVectorExtensions
{
    extension(Vector2 vector)
    {
        public Vector2I ToVector2I()
        {
            return new Vector2I((int)vector.X, (int)vector.Y);
        }
    }
}