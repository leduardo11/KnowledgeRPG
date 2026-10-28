using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KnowledgeRPG.Quiz
{
    public sealed class QuizPack
    {
        [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("language")] public string Language { get; set; } = "en";
        [JsonPropertyName("level")] public int Level { get; set; } = 1;
        [JsonPropertyName("topics")] public List<QuizTopic> Topics { get; set; } = new();
        [JsonPropertyName("questions")] public List<QuizQuestion> Questions { get; set; } = new();
    }

    public sealed class QuizTopic
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("label")] public string Label { get; set; } = "";
    }

    public sealed class QuizQuestion
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("topic")] public string Topic { get; set; } = "";
        [JsonPropertyName("difficulty")] public int Difficulty { get; set; } = 1;
        [JsonPropertyName("type")] public string Type { get; set; } = "multiple_choice";
        [JsonPropertyName("prompt")] public string Prompt { get; set; } = "";
        [JsonPropertyName("choices")] public List<string> Choices { get; set; } = new();
        [JsonPropertyName("answer")] public string Answer { get; set; } = "";
        [JsonPropertyName("explanation")] public string Explanation { get; set; } = "";

        public string TopicLabel(QuizPack pack)
        {
            foreach (QuizTopic topic in pack.Topics)
            {
                if (topic.Id == Topic)
                    return topic.Label;
            }

            return Topic;
        }
    }

    public sealed class QuizAnswerResult
    {
        public bool Correct { get; set; }
        public string Selected { get; set; } = "";
        public string QuestionId { get; set; } = "";
        public double AnswerSeconds { get; set; } = -1;
    }
}
