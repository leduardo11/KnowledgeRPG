using Microsoft.Xna.Framework;

namespace KnowledgeRPG.Core
{
    /// <summary>
    /// A tiny axis aligned rectangle helper for the world collision queries.
    /// </summary>
    public readonly struct RectF(float x, float y, float width, float height)
	{
		public float X { get; } = x;
		public float Y { get; } = y;
		public float Width { get; } = width;
		public float Height { get; } = height;
		public float Right => X + Width;
        public float Bottom => Y + Height;
        public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

        public bool Intersects(RectF other) =>
            X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

        public RectF Offset(Vector2 delta) =>
            new(X + delta.X, Y + delta.Y, Width, Height);

        public static RectF FromCenter(Vector2 center, float width, float height) =>
            new(center.X - width / 2f, center.Y - height / 2f, width, height);
    }
}
