using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KnowledgeRPG.Core;

namespace KnowledgeRPG.Maps
{
    /// <summary>
    /// The forest level: a 30x30 tile map (480x480 px) generated from the
    /// original Godot TileMapLayer data, with tree tiles acting as obstacles.
    /// </summary>
    public sealed class ForestMap
    {
        private const int Tile = ForestTileData.TileSize;
        private readonly Texture2D _groundTiles;
        private readonly Texture2D _objectTiles;
        private readonly HashSet<(int X, int Y)> _solidTiles = new();

        public ForestMap(Texture2D groundTiles, Texture2D objectTiles)
        {
            _groundTiles = groundTiles;
            _objectTiles = objectTiles;

            foreach (TileCell cell in ForestTileData.Trees)
            {
                if (cell.X >= 0 && cell.Y >= 0 &&
                    cell.X < ForestTileData.MapTiles &&
                    cell.Y < ForestTileData.MapTiles)
                {
                    _solidTiles.Add((cell.X, cell.Y));
                }
            }
        }

        public string Name => "Forest";
        public string QuizPackId => "forest_level1";
        public int Width => ForestTileData.MapSize;
        public int Height => ForestTileData.MapSize;

        public bool IsSolid(RectF bounds)
        {
            if (bounds.X < 0f || bounds.Y < 0f ||
                bounds.Right > Width || bounds.Bottom > Height)
                return true;

            int minX = (int)(bounds.X / Tile);
            int minY = (int)(bounds.Y / Tile);
            int maxX = (int)((bounds.Right - 0.01f) / Tile);
            int maxY = (int)((bounds.Bottom - 0.01f) / Tile);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (_solidTiles.Contains((x, y)))
                        return true;
                }
            }

            return false;
        }

        public void DrawGround(SpriteBatch spriteBatch)
        {
            foreach (TileCell cell in ForestTileData.Ground)
            {
                spriteBatch.Draw(
                    _groundTiles,
                    new Rectangle(cell.X * Tile, cell.Y * Tile, Tile, Tile),
                    new Rectangle(cell.AtlasX * Tile, cell.AtlasY * Tile, Tile, Tile),
                    Color.White);
            }
        }

        public void DrawTrees(SpriteBatch spriteBatch)
        {
            foreach (TileCell cell in ForestTileData.Trees)
            {
                spriteBatch.Draw(
                    _objectTiles,
                    new Rectangle(cell.X * Tile, cell.Y * Tile, Tile, Tile),
                    new Rectangle(cell.AtlasX * Tile, cell.AtlasY * Tile, Tile, Tile),
                    Color.White);
            }
        }
    }
}
