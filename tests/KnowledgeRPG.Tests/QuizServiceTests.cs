using System;
using System.Collections.Generic;
using System.Linq;
using KnowledgeRPG.Quiz;
using Xunit;

namespace KnowledgeRPG.Tests;

public class QuizServiceTests
{
    public QuizServiceTests()
    {
        QuizPackLoader.DataDirectory = TestPaths.DataDirectory;
    }

    [Fact]
    public void ShuffleBagDrawsEveryQuestionOnceThenRefills()
    {
        QuizPack pack = QuizPackLoader.LoadPack("forest_level1");
        var service = new QuizService(seed: 1234);
        var drawn = new HashSet<string>();

        for (int i = 0; i < pack.Questions.Count; i++)
            drawn.Add(service.Next("forest_level1").Id);

        Assert.Equal(pack.Questions.Count, drawn.Count);

        QuizQuestion refilled = service.Next("forest_level1");
        Assert.Contains(pack.Questions, question => question.Id == refilled.Id);
    }

    [Theory]
    [InlineData("forest_level1", true)]
    [InlineData("level_2", true)]
    [InlineData("", false)]
    [InlineData("../secret", false)]
    [InlineData("Forest", false)]
    [InlineData("forest-level1", false)]
    public void PackIdsAreRestrictedToSafeCharacters(string packId, bool expected)
    {
        Assert.Equal(expected, QuizService.IsValidPackId(packId));
    }

    [Fact]
    public void InvalidPackIdIsRejectedBeforeLoading()
    {
        var service = new QuizService(seed: 1);

        Assert.Throws<ArgumentException>(() => service.GetOrLoad("../evil"));
    }

    [Fact]
    public void MissingPackFailsWithAUsefulError()
    {
        var service = new QuizService(seed: 1);

        Assert.ThrowsAny<Exception>(() => service.GetOrLoad("does_not_exist"));
    }
}
