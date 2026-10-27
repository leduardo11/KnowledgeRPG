using System.IO;
using System.Linq;
using KnowledgeRPG.Quiz;
using Xunit;

namespace KnowledgeRPG.Tests;

public class ForestQuizPackTests
{
    [Fact]
    public void ForestPackLoadsAndPassesValidation()
    {
        string path = Path.Combine(TestPaths.DataDirectory, "forest_level1.json");
        QuizPack pack = QuizPackLoader.Load(path);

        Assert.Equal("forest_level1", pack.Id);
        Assert.Equal(1, pack.SchemaVersion);
        Assert.Equal(2, pack.Topics.Count);
        Assert.InRange(pack.Questions.Count, 20, 30);
        Assert.Empty(QuizPackValidator.Validate(pack, path));
    }

    [Fact]
    public void EveryForestQuestionIsWellFormed()
    {
        string path = Path.Combine(TestPaths.DataDirectory, "forest_level1.json");
        QuizPack pack = QuizPackLoader.Load(path);

        Assert.All(pack.Questions, question =>
        {
            Assert.Equal(4, question.Choices.Count);
            Assert.Contains(question.Answer, question.Choices);
            Assert.False(string.IsNullOrWhiteSpace(question.Explanation));
            Assert.Contains("___", question.Prompt);
            Assert.Contains(pack.Topics, topic => topic.Id == question.Topic);
        });
    }

    [Fact]
    public void TopicLabelResolvesForTheUi()
    {
        string path = Path.Combine(TestPaths.DataDirectory, "forest_level1.json");
        QuizPack pack = QuizPackLoader.Load(path);

        QuizQuestion first = pack.Questions[0];

        Assert.Equal("Verb to be (am / is / are)", first.TopicLabel(pack));
    }

    [Fact]
    public void DataDirectoryContainsOnlyValidPacks()
    {
        string[] files = Directory.GetFiles(TestPaths.DataDirectory, "*.json");

        Assert.NotEmpty(files);
        Assert.All(files, file =>
        {
            QuizPack pack = QuizPackLoader.Load(file);
            Assert.NotEmpty(pack.Questions);
        });
    }
}
