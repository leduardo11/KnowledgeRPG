using System;
using KnowledgeRPG.Quiz;

namespace KnowledgeRPG.Battle
{
    /// <summary>
    /// Bridges the battle to a quiz pack. A missing or broken pack fails open so
    /// content problems can never soft-lock an encounter.
    /// </summary>
    public sealed class QuizGate
    {
        private readonly QuizService _quiz;
        private readonly QuizPanel _panel;
        private bool _busy;
        private QuizAnswerResult _pending;

        public QuizGate(QuizService quiz, QuizPanel panel)
        {
            _quiz = quiz ?? throw new ArgumentNullException(nameof(quiz));
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
        }

        public bool IsBusy => _busy;
        public bool IsPanelOpen => _panel.IsOpen;

        public void Ask(string packId, double now)
        {
            _pending = null;

            if (string.IsNullOrWhiteSpace(packId))
            {
                _pending = new QuizAnswerResult { Correct = true, AnswerSeconds = -1 };
                _busy = false;
                return;
            }

            try
            {
                QuizPack pack = _quiz.GetOrLoad(packId);
                QuizQuestion question = _quiz.Next(packId);
                _panel.Ask(question, question.TopicLabel(pack), now);
                _busy = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Quiz pack '{packId}' failed to load: {ex.Message}");
                _pending = new QuizAnswerResult { Correct = true, AnswerSeconds = -1 };
                _busy = false;
            }
        }

        public bool TryConsume(out QuizAnswerResult result)
        {
            if (_pending is not null)
            {
                result = _pending;
                _pending = null;
                return true;
            }

            if (_busy && _panel.ResultReady)
            {
                result = _panel.ConsumeResult();
                _busy = false;
                return true;
            }

            result = null;
            return false;
        }

        public void Cancel()
        {
            _busy = false;
            _pending = null;
            _panel.Cancel();
        }
    }
}
