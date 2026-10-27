using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KnowledgeRPG.Core;

namespace KnowledgeRPG.Maps
{
    /// <summary>
    /// The forest level, loaded from a Tiled map at
    /// <c>Content/Maps/forest.tmj</c>. Tile layers tagged with the boolean
    /// property <c>solid</c> block movement.
    /// </summary>
    public sealed class ForestMap
    {
        private readonly TiledMap _map;
        private readonly Dictionary<string, Texture2D> _textures;
        private readonly HashSet<(int X, int Y)> _solidTiles = new();

        public ForestMap(Texture2D groundTiles, Texture2D objectTiles)
        {
            string path = Path.Combine(
                AppContext.BaseDirectory, "Content", "Maps", "forest.tmj");

            _map = TiledMap.Load(path);
            _textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase)
            {
                ["plains"] = groundTiles,
                ["objects"] = objectTiles,
            };

            foreach (TiledLayer layer in _map.Layers)
            {
                if (!layer.Solid)
                    continue;

                for (int y = 0; y < layer.Height; y++)
                {
                    for (int x = 0; x < layer.Width; x++)
                    {
                        if (layer[x, y] != 0)
                            _solidTiles.Add((x, y));
                    }
                }
            }
        }

        public string Name => _map.Name;
        public string QuizPackId => _map.QuizPackId;
        public int Width => _map.Width * _map.TileWidth;
        public int Height => _map.Height * _map.TileHeight;

        public bool IsSolid(RectF bounds)
        {
            if (bounds.X < 0f || bounds.Y < 0f ||
                bounds.Right > Width || bounds.Bottom > Height)
                return true;

            int minX = (int)(bounds.X / _map.TileWidth);
            int minY = (int)(bounds.Y / _map.TileHeight);
            int maxX = (int)((bounds.Right - 0.01f) / _map.TileWidth);
            int maxY = (int)((bounds.Bottom - 0.01f) / _map.TileHeight);

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

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (TiledLayer layer in _map.Layers)
            {
                if (!layer.Visible)
                    continue;

                DrawLayer(spriteBatch, layer);
            }
        }

        private void DrawLayer(SpriteBatch spriteBatch, TiledLayer layer)
        {
            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    uint gid = layer[x, y];
                    if (!_map.TryResolve(gid, out TiledTileRef tile))
                        continue;

                    Texture2D texture = TextureFor(tile.Tileset);
                    TiledTileset tileset = tile.Tileset;

                    var source = new Rectangle(
                        tileset.Margin + tile.AtlasX * (tileset.TileWidth + tileset.Spacing),
                        tileset.Margin + tile.AtlasY * (tileset.TileHeight + tileset.Spacing),
                        tileset.TileWidth,
                        tileset.TileHeight);

                    var destination = new Rectangle(
                        x * _map.TileWidth,
                        y * _map.TileHeight,
                        _map.TileWidth,
                        _map.TileHeight);

                    spriteBatch.Draw(
                        texture,
                        destination,
                        source,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        tile.Effects,
                        0f);
                }
            }
        }

        private Texture2D TextureFor(TiledTileset tileset)
        {
            if (_textures.TryGetValue(tileset.Name, out Texture2D byName))
                return byName;

            if (_textures.TryGetValue(tileset.TextureName, out Texture2D byImage))
                return byImage;

            throw new InvalidOperationException(
                $"Tileset '{tileset.Name}' (image '{tileset.Image}') has no texture in ForestMap. " +
                "Add its PNG to Content/Content.mgcb and pass it to the ForestMap constructor.");
        }
    }
}
