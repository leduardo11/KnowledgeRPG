using System;
using System.Collections.Generic;
using System.Linq;

namespace KnowledgeRPG.Quiz
{
    /// <summary>
    /// Loads packs and serves questions from a shuffled bag so a pack cycles
    /// without repeating until every question has been asked once.
    /// </summary>
    public sealed class QuizService
    {
        private readonly Dictionary<string, QuizPack> _packs = new();
        private readonly Dictionary<string, List<QuizQuestion>> _bags = new();
        private readonly Random _random;

        public QuizService(int seed = 0)
        {
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        public QuizQuestion Next(string packId)
        {
            QuizPack pack = GetOrLoad(packId);

            if (!_bags.TryGetValue(packId, out List<QuizQuestion> bag) || bag.Count == 0)
            {
                bag = pack.Questions.ToList();
                Shuffle(bag);
                _bags[packId] = bag;
            }

            QuizQuestion question = bag[^1];
            bag.RemoveAt(bag.Count - 1);
            return question;
        }

        public QuizPack GetOrLoad(string packId)
        {
            if (_packs.TryGetValue(packId, out QuizPack cached))
                return cached;

            if (!IsValidPackId(packId))
                throw new ArgumentException($"Invalid quiz pack id '{packId}'.", nameof(packId));

            QuizPack pack = QuizPackLoader.LoadPack(packId);
            _packs[packId] = pack;
            return pack;
        }

        public static bool IsValidPackId(string packId)
        {
            if (string.IsNullOrEmpty(packId))
                return false;

            foreach (char c in packId)
            {
                if (c is not (>= 'a' and <= 'z') &&
                    c is not (>= '0' and <= '9') &&
                    c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private void Shuffle(List<QuizQuestion> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
