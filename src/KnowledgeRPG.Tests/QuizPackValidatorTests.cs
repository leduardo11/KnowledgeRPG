using System.Collections.Generic;
using System.Linq;
using KnowledgeRPG.Quiz;
using Xunit;

namespace KnowledgeRPG.Tests;

public class QuizPackValidatorTests
{
    [Fact]
    public void ValidPackHasNoIssues()
    {
        QuizPack pack = GoodPack();

        Assert.Empty(QuizPackValidator.Validate(pack, "good.json"));
    }

    [Fact]
    public void EmptyPackReportsEveryStructuralProblem()
    {
        var pack = new QuizPack
        {
            SchemaVersion = 2,
            Topics = new List<QuizTopic>(),
            Questions = new List<QuizQuestion>
            {
                new()
                {
                    Id = "q1",
                    Topic = "missing_topic",
                    Difficulty = 7,
                    Type = "typed",
                    Prompt = "No blank here",
                    Choices = new List<string> { "a", "b", "c" },
                    Answer = "z",
                    Explanation = "",
                },
            },
        };

        var issues = QuizPackValidator.Validate(pack, "bad.json");

        Assert.True(issues.Count >= 6, $"Expected at least 6 issues, got {issues.Count}.");
        Assert.Contains(issues, issue => issue.Contains("schemaVersion"));
        Assert.Contains(issues, issue => issue.Contains("unknown topic"));
        Assert.Contains(issues, issue => issue.Contains("difficulty"));
        Assert.Contains(issues, issue => issue.Contains("multiple_choice"));
        Assert.Contains(issues, issue => issue.Contains("blank"));
        Assert.Contains(issues, issue => issue.Contains("exactly 4 choices"));
        Assert.Contains(issues, issue => issue.Contains("answer"));
        Assert.Contains(issues, issue => issue.Contains("explanation"));
    }

    [Fact]
    public void DuplicateQuestionIdsAreRejected()
    {
        QuizPack pack = GoodPack();
        pack.Questions.Add(ValidQuestion("q1"));

        var issues = QuizPackValidator.Validate(pack, "dup.json");

        Assert.Contains(issues, issue => issue.Contains("duplicate question id"));
    }

    [Fact]
    public void NullPackIsRejected()
    {
        var issues = QuizPackValidator.Validate(null, "missing.json");

        Assert.Single(issues);
        Assert.Contains("pack is null", issues[0]);
    }

    [Fact]
    public void ThrowIfInvalidThrowsForBadContent()
    {
        QuizPack pack = GoodPack();
        pack.Questions[0].Answer = "not-a-choice";

        Assert.Throws<System.InvalidOperationException>(
            () => QuizPackValidator.ThrowIfInvalid(pack, "bad.json"));
    }

    [Fact]
    public void RemoveInvalidKeepsOnlyValidQuestions()
    {
        QuizPack pack = GoodPack();
        pack.Questions.Add(new QuizQuestion
        {
            Id = "broken",
            Topic = "t",
            Prompt = "No blank",
            Choices = new List<string> { "a", "b", "c", "d" },
            Answer = "a",
            Explanation = "x",
        });
        pack.Questions.Add(ValidQuestion("q1"));

        var reported = new List<string>();
        int removed = QuizPackValidator.RemoveInvalid(pack, "pack.json", reported.Add);

        Assert.Equal(2, removed);
        Assert.Single(pack.Questions);
        Assert.Equal("q1", pack.Questions[0].Id);
        Assert.NotEmpty(reported);
    }

    private static QuizPack GoodPack()
    {
        return new QuizPack
        {
            SchemaVersion = 1,
            Id = "good",
            Title = "Good",
            Language = "en",
            Level = 1,
            Topics = new List<QuizTopic> { new() { Id = "t", Label = "T" } },
            Questions = new List<QuizQuestion> { ValidQuestion("q1") },
        };
    }

    private static QuizQuestion ValidQuestion(string id)
    {
        return new QuizQuestion
        {
            Id = id,
            Topic = "t",
            Difficulty = 1,
            Type = "multiple_choice",
            Prompt = "She ___ here.",
            Choices = new List<string> { "am", "is", "are", "be" },
            Answer = "is",
            Explanation = "Use is with she.",
        };
    }
}
