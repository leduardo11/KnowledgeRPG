using System;
using System.IO;
using System.Text.Json;

namespace KnowledgeRPG.Quiz
{
    public static class QuizPackLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static string DataDirectory { get; set; } =
            Path.Combine(AppContext.BaseDirectory, "Data");

        public static QuizPack Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Cannot open quiz pack '{path}'.", path);

            string json = File.ReadAllText(path);

            QuizPack pack;
            try
            {
                pack = JsonSerializer.Deserialize<QuizPack>(json, Options);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Invalid JSON in '{path}': {ex.Message}", ex);
            }

            if (pack is null)
                throw new InvalidDataException($"Quiz pack '{path}' is empty.");

            // Debug builds stop on bad content; release drops bad questions and keeps going.
#if DEBUG
            QuizPackValidator.ThrowIfInvalid(pack, path);
#else
            QuizPackValidator.RemoveInvalid(pack, path, Console.Error.WriteLine);
#endif
            return pack;
        }

        public static QuizPack LoadPack(string packId)
        {
            return Load(Path.Combine(DataDirectory, $"{packId}.json"));
        }
    }
}
