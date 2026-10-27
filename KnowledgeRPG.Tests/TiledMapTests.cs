using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using KnowledgeRPG.Maps;
using Xunit;

namespace KnowledgeRPG.Tests;

public class TiledMapTests
{
    private static string MapPath =>
        Path.Combine(TestPaths.ContentDirectory, "Maps", "forest.tmj");

    [Fact]
    public void ForestMapLoadsFromTiled()
    {
        TiledMap map = TiledMap.Load(MapPath);

        Assert.Equal(30, map.Width);
        Assert.Equal(30, map.Height);
        Assert.Equal(16, map.TileWidth);
        Assert.Equal(16, map.TileHeight);
        Assert.Equal("Forest", map.Name);
        Assert.Equal("forest_level1", map.QuizPackId);
        Assert.Equal(2, map.Layers.Count);
        Assert.Equal(2, map.Tilesets.Count);
    }

    [Fact]
    public void GroundLayerCoversTheWholeMap()
    {
        TiledMap map = TiledMap.Load(MapPath);
        TiledLayer ground = Assert.Single(map.Layers, layer => layer.Name == "Ground");

        Assert.Equal(30, ground.Width);
        Assert.Equal(30, ground.Height);
        Assert.All(
            Enumerable.Range(0, ground.Width * ground.Height),
            index => Assert.NotEqual(0u, ground[index % ground.Width, index / ground.Width]));
    }

    [Fact]
    public void TreesLayerIsSolidAndNonEmpty()
    {
        TiledMap map = TiledMap.Load(MapPath);
        TiledLayer trees = Assert.Single(map.Layers, layer => layer.Name == "Trees");

        Assert.True(trees.Solid);
        Assert.Equal(172, CountTiles(trees));
    }

    [Fact]
    public void EveryPlacedTileResolvesToATilesetCell()
    {
        TiledMap map = TiledMap.Load(MapPath);

        foreach (TiledLayer layer in map.Layers)
        {
            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    uint gid = layer[x, y];
                    if (gid == 0)
                        continue;

                    Assert.True(map.TryResolve(gid, out TiledTileRef tile));
                    Assert.InRange(tile.AtlasX, 0, tile.Tileset.Columns - 1);
                    Assert.True(tile.AtlasY >= 0);

                    int localId = tile.AtlasY * tile.Tileset.Columns + tile.AtlasX;
                    Assert.True(localId < tile.Tileset.TileCount);
                }
            }
        }
    }

    private static int CountTiles(TiledLayer layer)
    {
        int count = 0;
        for (int y = 0; y < layer.Height; y++)
        {
            for (int x = 0; x < layer.Width; x++)
            {
                if (layer[x, y] != 0)
                    count++;
            }
        }

        return count;
    }

    [Theory]
    [InlineData("zlib")]
    [InlineData("gzip")]
    public void Base64CompressedLayersAreSupported(string compression)
    {
        string path = CreateCompressedMap(compression);

        try
        {
            TiledMap map = TiledMap.Load(path);
            TiledLayer layer = Assert.Single(map.Layers);

            Assert.Equal(1u, layer[0, 0]);
            Assert.Equal(2u, layer[1, 0]);
            Assert.Equal(3u, layer[0, 1]);
            Assert.Equal(0u, layer[1, 1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateCompressedMap(string compression)
    {
        var gids = new uint[] { 1, 2, 3, 0 };
        var raw = new byte[gids.Length * sizeof(uint)];

        for (int i = 0; i < gids.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(i * sizeof(uint)), gids[i]);

        byte[] compressed;
        using (var output = new MemoryStream())
        {
            using (Stream encoder = compression == "gzip"
                       ? new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true)
                       : new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                encoder.Write(raw, 0, raw.Length);
            }

            compressed = output.ToArray();
        }

        string base64 = Convert.ToBase64String(compressed);
        string json = $$"""
        {
          "compressionlevel": -1,
          "height": 2,
          "infinite": false,
          "layers": [
            {
              "compression": "{{compression}}",
              "data": "{{base64}}",
              "encoding": "base64",
              "height": 2,
              "id": 1,
              "name": "Ground",
              "opacity": 1,
              "type": "tilelayer",
              "visible": true,
              "width": 2,
              "x": 0,
              "y": 0
            }
          ],
          "nextlayerid": 2,
          "nextobjectid": 1,
          "orientation": "orthogonal",
          "renderorder": "right-down",
          "tileheight": 16,
          "tilesets": [
            {
              "columns": 6,
              "firstgid": 1,
              "image": "plains.png",
              "imageheight": 192,
              "imagewidth": 96,
              "margin": 0,
              "name": "plains",
              "spacing": 0,
              "tilecount": 72,
              "tileheight": 16,
              "tilewidth": 16
            }
          ],
          "tilewidth": 16,
          "type": "map",
          "version": "1.10",
          "width": 2
        }
        """;

        string path = Path.Combine(
            Path.GetTempPath(), $"krpg-map-{Guid.NewGuid():N}.tmj");
        File.WriteAllText(path, json);
        return path;
    }
}
