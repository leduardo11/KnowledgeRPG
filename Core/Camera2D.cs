using Microsoft.Xna.Framework;

namespace KnowledgeRPG.Core
{
    /// <summary>
    /// Follows a target and clamps the view inside the map bounds, mirroring the
    /// Camera2D limits the Godot scene used.
    /// </summary>
    public sealed class Camera2D
    {
        public Camera2D(int viewWidth, int viewHeight, int mapWidth, int mapHeight)
        {
            ViewWidth = viewWidth;
            ViewHeight = viewHeight;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
        }

        public int ViewWidth { get; }
        public int ViewHeight { get; }
        public int MapWidth { get; }
        public int MapHeight { get; }
        public Vector2 Position { get; private set; }

        public void Follow(Vector2 target)
        {
            float halfW = ViewWidth / 2f;
            float halfH = ViewHeight / 2f;

            float x = MapWidth <= ViewWidth
                ? MapWidth / 2f
                : MathHelper.Clamp(target.X, halfW, MapWidth - halfW);

            float y = MapHeight <= ViewHeight
                ? MapHeight / 2f
                : MathHelper.Clamp(target.Y, halfH, MapHeight - halfH);

            Position = new Vector2(x, y);
        }

        public Matrix GetViewMatrix(float scale) =>
            Matrix.CreateTranslation(-Position.X, -Position.Y, 0f) *
            Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(ViewWidth * scale / 2f, ViewHeight * scale / 2f, 0f);

        public Vector2 ScreenToWorld(Vector2 screen, float scale)
        {
            Vector2 centered = screen / scale - new Vector2(ViewWidth / 2f, ViewHeight / 2f);
            return centered + Position;
        }
    }
}
