using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KnowledgeRPG.Quiz
{
    public static class QuizPackValidator
    {
        public static List<string> Validate(QuizPack pack, string path)
        {
            var issues = new List<string>();

            if (pack is null)
            {
                issues.Add($"{path}: pack is null");
                return issues;
            }

            if (pack.SchemaVersion != 1)
                issues.Add($"{path}: schemaVersion must be 1 (got {pack.SchemaVersion})");

            if (string.IsNullOrWhiteSpace(pack.Id))
                issues.Add($"{path}: id is empty");

            if (string.IsNullOrWhiteSpace(pack.Title))
                issues.Add($"{path}: title is empty");

            if (string.IsNullOrWhiteSpace(pack.Language))
                issues.Add($"{path}: language is empty");

            if (pack.Level < 1)
                issues.Add($"{path}: level must be >= 1");

            if (pack.Topics.Count == 0)
                issues.Add($"{path}: at least one topic is required");

            var topicIds = new HashSet<string>();
            foreach (QuizTopic topic in pack.Topics)
            {
                if (string.IsNullOrWhiteSpace(topic.Id))
                {
                    issues.Add($"{path}: topic with an empty id");
                    continue;
                }

                if (!topicIds.Add(topic.Id))
                    issues.Add($"{path}: duplicate topic id '{topic.Id}'");

                if (string.IsNullOrWhiteSpace(topic.Label))
                    issues.Add($"{path}: topic '{topic.Id}' has an empty label");
            }

            if (pack.Questions.Count == 0)
                issues.Add($"{path}: at least one question is required");

            var questionIds = new HashSet<string>();
            foreach (QuizQuestion question in pack.Questions)
            {
                string id = string.IsNullOrWhiteSpace(question.Id) ? "(no id)" : question.Id;
                string prefix = $"{path}: question '{id}'";

                if (string.IsNullOrWhiteSpace(question.Id))
                    issues.Add($"{prefix}: id is empty");
                else if (!questionIds.Add(question.Id))
                    issues.Add($"{prefix}: duplicate question id");

                if (!topicIds.Contains(question.Topic))
                    issues.Add($"{prefix}: unknown topic '{question.Topic}'");

                if (question.Difficulty is < 1 or > 3)
                    issues.Add($"{prefix}: difficulty must be 1-3");

                if (question.Type != "multiple_choice")
                    issues.Add($"{prefix}: type must be 'multiple_choice'");

                if (string.IsNullOrWhiteSpace(question.Prompt))
                    issues.Add($"{prefix}: prompt is empty");
                else if (!question.Prompt.Contains("___", StringComparison.Ordinal))
                    issues.Add($"{prefix}: prompt has no '___' blank");

                if (question.Choices.Count != 4)
                    issues.Add($"{prefix}: exactly 4 choices are required (got {question.Choices.Count})");
                else if (question.Choices.Distinct().Count() != 4)
                    issues.Add($"{prefix}: choices must be unique");

                if (question.Choices.Any(string.IsNullOrWhiteSpace))
                    issues.Add($"{prefix}: choices must be non-empty");

                if (string.IsNullOrWhiteSpace(question.Answer))
                    issues.Add($"{prefix}: answer is empty");
                else if (!question.Choices.Contains(question.Answer))
                    issues.Add($"{prefix}: answer '{question.Answer}' is not in choices");

                if (string.IsNullOrWhiteSpace(question.Explanation))
                    issues.Add($"{prefix}: explanation is empty");
            }

            return issues;
        }

        public static void ThrowIfInvalid(QuizPack pack, string path)
        {
            List<string> issues = Validate(pack, path);
            if (issues.Count == 0)
                return;

            throw new InvalidOperationException(
                $"{issues.Count} quiz issue(s):\n  " + string.Join("\n  ", issues));
        }

        public static int RemoveInvalid(QuizPack pack, string path, Action<string> report = null)
        {
            if (pack is null)
                return 0;

            var valid = new List<QuizQuestion>();
            var seen = new HashSet<string>();

            foreach (QuizQuestion question in pack.Questions)
            {
                var probe = new QuizPack
                {
                    SchemaVersion = pack.SchemaVersion,
                    Id = pack.Id,
                    Title = pack.Title,
                    Language = pack.Language,
                    Level = pack.Level,
                    Topics = pack.Topics,
                    Questions = new List<QuizQuestion> { question },
                };

                List<string> issues = Validate(probe, path);

                if (issues.Count == 0 && seen.Add(question.Id))
                {
                    valid.Add(question);
                    continue;
                }

                if (report is not null)
                {
                    foreach (string issue in issues)
                        report(issue);
                }
            }

            int removed = pack.Questions.Count - valid.Count;
            pack.Questions.Clear();
            pack.Questions.AddRange(valid);
            return removed;
        }
    }
}
