using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;

namespace KnowledgeRPG.Maps
{
    /// <summary>
    /// Minimal loader for Tiled JSON maps (.tmj). Supports orthogonal tile layers,
    /// embedded tilesets, per-layer boolean properties, and horizontal/vertical
    /// tile flips. Editing the map in Tiled is the source of truth.
    /// </summary>
    public sealed class TiledMap
    {
        private const uint FlipHorizontal = 0x80000000;
        private const uint FlipVertical = 0x40000000;
        private const uint FlipDiagonal = 0x20000000;
        private const uint RotateHex120 = 0x10000000;
        private const uint GidMask = 0x0FFFFFFF;

        private readonly List<TiledLayer> _layers = new();
        private readonly List<TiledTileset> _tilesets = new();

        private TiledMap()
        {
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
        public string Name { get; private set; } = "";
        public string QuizPackId { get; private set; } = "";

        public IReadOnlyList<TiledLayer> Layers => _layers;
        public IReadOnlyList<TiledTileset> Tilesets => _tilesets;

        public static TiledMap Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Cannot open Tiled map '{path}'.", path);

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(
                    File.ReadAllText(path),
                    new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    });
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Invalid JSON in Tiled map '{path}': {ex.Message}", ex);
            }

            using (document)
            {
                JsonElement root = document.RootElement;
                var map = new TiledMap
                {
                    Width = RequiredInt(root, "width", path),
                    Height = RequiredInt(root, "height", path),
                    TileWidth = RequiredInt(root, "tilewidth", path),
                    TileHeight = RequiredInt(root, "tileheight", path),
                };

                string orientation = root.TryGetProperty("orientation", out JsonElement orientationElement)
                    ? orientationElement.GetString() ?? "orthogonal"
                    : "orthogonal";

                if (!string.Equals(orientation, "orthogonal", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        $"Tiled map '{path}' uses '{orientation}' orientation; only orthogonal maps are supported.");

                map.ReadProperties(root, path, out string name, out string quizPackId);
                map.Name = name;
                map.QuizPackId = quizPackId;

                if (!root.TryGetProperty("tilesets", out JsonElement tilesets) ||
                    tilesets.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException($"Tiled map '{path}' has no tilesets array.");
                }

                foreach (JsonElement element in tilesets.EnumerateArray())
                {
                    if (element.TryGetProperty("source", out _))
                    {
                        throw new InvalidDataException(
                            $"Tiled map '{path}' uses an external tileset. " +
                            "Embed the tileset in the map so the game can load it.");
                    }

                    map._tilesets.Add(TiledTileset.FromJson(element, path));
                }

                map._tilesets.Sort((a, b) => a.FirstGid.CompareTo(b.FirstGid));

                if (!root.TryGetProperty("layers", out JsonElement layers) ||
                    layers.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException($"Tiled map '{path}' has no layers array.");
                }

                foreach (JsonElement element in layers.EnumerateArray())
                {
                    string type = element.TryGetProperty("type", out JsonElement typeElement)
                        ? typeElement.GetString() ?? ""
                        : "";

                    if (type != "tilelayer")
                        continue;

                    map._layers.Add(TiledLayer.FromJson(element, map.Width, map.Height, path));
                }

                if (map._layers.Count == 0)
                    throw new InvalidDataException($"Tiled map '{path}' has no tile layers.");

                if (map._tilesets.Count == 0)
                    throw new InvalidDataException($"Tiled map '{path}' has no embedded tilesets.");

                return map;
            }
        }

        public bool TryResolve(uint rawGid, out TiledTileRef tile)
        {
            tile = default;

            uint gid = rawGid & GidMask;
            if (gid == 0)
                return false;

            TiledTileset tileset = null;
            foreach (TiledTileset candidate in _tilesets)
            {
                if (candidate.FirstGid > gid)
                    break;

                if (candidate.Contains(gid))
                    tileset = candidate;
            }

            if (tileset is null)
                return false;

            int localId = (int)(gid - tileset.FirstGid);
            if (tileset.Columns <= 0)
                return false;
            if (tileset.TileCount > 0 && localId >= tileset.TileCount)
                return false;

            int atlasX = localId % tileset.Columns;
            int atlasY = localId / tileset.Columns;

            SpriteEffects effects = SpriteEffects.None;
            if ((rawGid & FlipHorizontal) != 0)
                effects |= SpriteEffects.FlipHorizontally;
            if ((rawGid & FlipVertical) != 0)
                effects |= SpriteEffects.FlipVertically;

            // Diagonal flips and hexagonal rotation are not meaningful for this
            // tileset; clear them rather than drawing a subtly wrong tile.
            _ = (rawGid & (FlipDiagonal | RotateHex120)) != 0;

            tile = new TiledTileRef(tileset, atlasX, atlasY, effects);
            return true;
        }

        private void ReadProperties(
            JsonElement element,
            string path,
            out string name,
            out string quizPackId)
        {
            name = "";
            quizPackId = "";

            if (!element.TryGetProperty("properties", out JsonElement properties) ||
                properties.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement property in properties.EnumerateArray())
            {
                string propertyName = property.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString() ?? ""
                    : "";
                string value = property.TryGetProperty("value", out JsonElement valueElement)
                    ? ElementToString(valueElement)
                    : "";

                if (propertyName == "name")
                    name = value;
                else if (propertyName == "quizPackId")
                    quizPackId = value;
            }

            if (string.IsNullOrWhiteSpace(name))
                name = Path.GetFileNameWithoutExtension(path);
        }

        private static int RequiredInt(JsonElement element, string property, string path)
        {
            if (!element.TryGetProperty(property, out JsonElement value) ||
                value.ValueKind != JsonValueKind.Number ||
                !value.TryGetInt32(out int number))
            {
                throw new InvalidDataException(
                    $"Tiled map '{path}' is missing numeric property '{property}'.");
            }

            return number;
        }

        private static string ElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => element.GetRawText(),
                _ => "",
            };
        }
    }

    public sealed class TiledLayer
    {
        private TiledLayer()
        {
        }

        public string Name { get; private set; } = "";
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool Visible { get; private set; }
        public bool Solid { get; private set; }
        public IReadOnlyList<uint> Gids => _gids;

        private uint[] _gids = Array.Empty<uint>();

        public uint this[int x, int y] => _gids[y * Width + x];

        internal static TiledLayer FromJson(JsonElement element, int mapWidth, int mapHeight, string path)
        {
            string name = element.TryGetProperty("name", out JsonElement nameElement)
                ? nameElement.GetString() ?? ""
                : "";

            int width = element.TryGetProperty("width", out JsonElement widthElement)
                ? widthElement.GetInt32()
                : mapWidth;
            int height = element.TryGetProperty("height", out JsonElement heightElement)
                ? heightElement.GetInt32()
                : mapHeight;

            bool visible = !element.TryGetProperty("visible", out JsonElement visibleElement) ||
                           visibleElement.ValueKind != JsonValueKind.False;

            bool solid = ReadBoolProperty(element, "solid");

            if (!element.TryGetProperty("data", out JsonElement data))
            {
                throw new InvalidDataException(
                    $"Tiled layer '{name}' in '{path}' has no data. Infinite maps are not supported.");
            }

            uint[] gids = ReadData(data, element, name, width, height, path);

            return new TiledLayer
            {
                Name = name,
                Width = width,
                Height = height,
                Visible = visible,
                Solid = solid,
                _gids = gids,
            };
        }

        private static uint[] ReadData(
            JsonElement data,
            JsonElement layer,
            string layerName,
            int width,
            int height,
            string path)
        {
            var gids = new uint[width * height];

            if (data.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement cell in data.EnumerateArray())
                {
                    if (index >= gids.Length)
                        break;

                    gids[index++] = cell.GetUInt32();
                }

                if (index != gids.Length)
                {
                    throw new InvalidDataException(
                        $"Tiled layer '{layerName}' in '{path}' has {index} cells; expected {gids.Length}.");
                }

                return gids;
            }

            if (data.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"Tiled layer '{layerName}' in '{path}' has unsupported data. " +
                    "Use JSON/CSV or base64 encoding.");
            }

            string encoding = layer.TryGetProperty("encoding", out JsonElement encodingElement)
                ? encodingElement.GetString() ?? ""
                : "";
            string compression = layer.TryGetProperty("compression", out JsonElement compressionElement)
                ? compressionElement.GetString() ?? ""
                : "";

            if (!string.Equals(encoding, "base64", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Tiled layer '{layerName}' in '{path}' uses encoding '{encoding}'. " +
                    "Use base64, CSV, or JSON layer data.");
            }

            byte[] bytes = Convert.FromBase64String(data.GetString() ?? "");

            if (string.Equals(compression, "gzip", StringComparison.OrdinalIgnoreCase))
            {
                using var input = new MemoryStream(bytes);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                bytes = ReadAllBytes(gzip);
            }
            else if (string.Equals(compression, "zlib", StringComparison.OrdinalIgnoreCase))
            {
                using var input = new MemoryStream(bytes);
                using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                bytes = ReadAllBytes(zlib);
            }
            else if (!string.IsNullOrEmpty(compression))
            {
                throw new InvalidDataException(
                    $"Tiled layer '{layerName}' in '{path}' uses unsupported compression '{compression}'. " +
                    "Use none, gzip, or zlib.");
            }

            if (bytes.Length != gids.Length * sizeof(uint))
            {
                throw new InvalidDataException(
                    $"Tiled layer '{layerName}' in '{path}' decodes to {bytes.Length} bytes; " +
                    $"expected {gids.Length * sizeof(uint)}.");
            }

            for (int i = 0; i < gids.Length; i++)
                gids[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i * sizeof(uint)));

            return gids;
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }

        private static bool ReadBoolProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty("properties", out JsonElement properties) ||
                properties.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (JsonElement property in properties.EnumerateArray())
            {
                string name = property.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString() ?? ""
                    : "";

                if (name != propertyName)
                    continue;

                if (!property.TryGetProperty("value", out JsonElement valueElement))
                    return false;

                return valueElement.ValueKind == JsonValueKind.True ||
                       (valueElement.ValueKind == JsonValueKind.String &&
                        bool.TryParse(valueElement.GetString(), out bool parsed) && parsed);
            }

            return false;
        }
    }

    public sealed class TiledTileset
    {
        public string Name { get; private set; } = "";
        public string Image { get; private set; } = "";
        public string TextureName { get; private set; } = "";
        public int FirstGid { get; private set; }
        public int Columns { get; private set; }
        public int TileCount { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public int Spacing { get; private set; }
        public int Margin { get; private set; }

        public bool Contains(uint gid)
        {
            if (gid < FirstGid)
                return false;

            int localId = (int)(gid - FirstGid);
            return TileCount <= 0 || localId < TileCount;
        }

        internal static TiledTileset FromJson(JsonElement element, string path)
        {
            string image = element.TryGetProperty("image", out JsonElement imageElement)
                ? imageElement.GetString() ?? ""
                : "";

            string name = element.TryGetProperty("name", out JsonElement nameElement)
                ? nameElement.GetString() ?? ""
                : "";

            if (string.IsNullOrWhiteSpace(name))
                name = Path.GetFileNameWithoutExtension(image);

            var tileset = new TiledTileset
            {
                Name = name,
                Image = image,
                TextureName = Path.GetFileNameWithoutExtension(image),
                FirstGid = element.GetProperty("firstgid").GetInt32(),
                Columns = element.TryGetProperty("columns", out JsonElement columnsElement)
                    ? columnsElement.GetInt32()
                    : 0,
                TileCount = element.TryGetProperty("tilecount", out JsonElement countElement)
                    ? countElement.GetInt32()
                    : 0,
                TileWidth = element.TryGetProperty("tilewidth", out JsonElement tileWidthElement)
                    ? tileWidthElement.GetInt32()
                    : 0,
                TileHeight = element.TryGetProperty("tileheight", out JsonElement tileHeightElement)
                    ? tileHeightElement.GetInt32()
                    : 0,
                ImageWidth = element.TryGetProperty("imagewidth", out JsonElement imageWidthElement)
                    ? imageWidthElement.GetInt32()
                    : 0,
                ImageHeight = element.TryGetProperty("imageheight", out JsonElement imageHeightElement)
                    ? imageHeightElement.GetInt32()
                    : 0,
                Spacing = element.TryGetProperty("spacing", out JsonElement spacingElement)
                    ? spacingElement.GetInt32()
                    : 0,
                Margin = element.TryGetProperty("margin", out JsonElement marginElement)
                    ? marginElement.GetInt32()
                    : 0,
            };

            if (tileset.TileWidth <= 0 || tileset.TileHeight <= 0)
                throw new InvalidDataException(
                    $"Tileset '{tileset.Name}' in '{path}' is missing tilewidth/tileheight.");

            if (tileset.Columns <= 0)
            {
                int usableWidth = tileset.ImageWidth - tileset.Margin * 2;
                tileset.Columns = Math.Max(
                    1,
                    (usableWidth + tileset.Spacing) / (tileset.TileWidth + tileset.Spacing));
            }

            return tileset;
        }
    }

    /// <summary>A resolved GID: which tileset, its atlas cell, and flip mode.</summary>
    public readonly record struct TiledTileRef(
        TiledTileset Tileset,
        int AtlasX,
        int AtlasY,
        SpriteEffects Effects);
}
