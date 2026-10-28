using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using KnowledgeRPG.Core;
using KnowledgeRPG.UI;

namespace KnowledgeRPG.Quiz
{
    /// <summary>
    /// Modal quiz window. Answers are timed so the battle can reward fast
    /// responses; callers poll <see cref="ResultReady"/> after closing.
    /// </summary>
    public sealed class QuizPanel
    {
        private static readonly Color CorrectColor = new(140, 235, 153);
        private static readonly Color WrongColor = new(248, 128, 128);
        private static readonly Color Accent = new(255, 216, 102);
        private static readonly Color PanelFill = new(24, 22, 38, 245);
        private static readonly Color PanelBorder = new(120, 104, 168);
        private static readonly Color ChoiceIdle = new(46, 42, 70);
        private static readonly Color ChoiceHover = new(66, 60, 100);
        private static readonly Color ChoiceCorrect = new(44, 102, 62);
        private static readonly Color ChoiceWrong = new(110, 48, 58);

        private QuizQuestion _question;
        private string _topic = "";
        private int _selected = -1;
        private bool _answered;
        private bool _correct;
        private double _startedAt;
        private double _answerSeconds = -1;

        public bool IsOpen { get; private set; }
        public bool ResultReady => _result is not null;
        public QuizAnswerResult Result { get; private set; } = null;
        public QuizQuestion CurrentQuestion => _question;

        private QuizAnswerResult _result;
        private Rectangle[] _choiceRects = Array.Empty<Rectangle>();
        private Rectangle _continueRect;

        public void Ask(QuizQuestion question, string topicLabel, double now)
        {
            if (question is null || question.Choices.Count != 4)
            {
                _result = new QuizAnswerResult { Correct = true, AnswerSeconds = -1 };
                IsOpen = false;
                return;
            }

            _question = question;
            _topic = string.IsNullOrWhiteSpace(topicLabel) ? question.Topic : topicLabel;
            _selected = -1;
            _answered = false;
            _correct = false;
            _answerSeconds = -1;
            _startedAt = now;
            _result = null;
            IsOpen = true;
        }

        public void Cancel()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            _question = null;
            _answered = false;
            _selected = -1;
            _result = new QuizAnswerResult { Correct = false, AnswerSeconds = -1 };
        }

        public QuizAnswerResult ConsumeResult()
        {
            QuizAnswerResult result = _result;
            _result = null;
            Result = null;
            return result;
        }

        public void Update(InputState input, MouseState mouse, bool mouseClicked, double now)
        {
            if (!IsOpen || _question is null)
                return;

            if (_answered)
            {
                if (input.ConfirmPressed || (mouseClicked && _continueRect.Contains(mouse.Position)))
                    Continue();
                return;
            }

            int choice = input.ChoicePressed();
            if (choice >= 0)
            {
                SelectChoice(choice, now);
                return;
            }

            if (input.WasPressed(Keys.Left) || input.WasPressed(Keys.Up))
                _selected = _selected <= 0 ? 3 : _selected - 1;
            else if (input.WasPressed(Keys.Right) || input.WasPressed(Keys.Down))
                _selected = _selected < 0 ? 0 : (_selected + 1) % 4;

            if (mouseClicked)
            {
                for (int i = 0; i < _choiceRects.Length; i++)
                {
                    if (_choiceRects[i].Contains(mouse.Position))
                    {
                        SelectChoice(i, now);
                        return;
                    }
                }
            }

            if (input.ConfirmPressed && _selected >= 0)
                SelectChoice(_selected, now);
        }

        private void SelectChoice(int index, double now)
        {
            if (_answered || _question is null)
                return;

            if (index < 0 || index >= _question.Choices.Count)
                return;

            _selected = index;
            _correct = _question.Choices[index] == _question.Answer;
            _answered = true;
            _answerSeconds = Math.Max(0, now - _startedAt);
        }

        private void Continue()
        {
            if (!_answered || _question is null)
                return;

            _result = new QuizAnswerResult
            {
                Correct = _correct,
                Selected = _selected >= 0 && _selected < _question.Choices.Count
                    ? _question.Choices[_selected]
                    : "",
                QuestionId = _question.Id,
                AnswerSeconds = _answerSeconds,
            };

            IsOpen = false;
            _question = null;
            _answered = false;
            _selected = -1;
        }

        public void Draw(SpriteBatch spriteBatch, UiPainter ui, Viewport viewport)
        {
            if (!IsOpen || _question is null)
                return;

            ui.FillRect(spriteBatch, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(6, 5, 12, 190));

            int panelWidth = Math.Min(960, viewport.Width - 80);
            int panelHeight = Math.Min(620, viewport.Height - 60);
            var panel = new Rectangle(
                (viewport.Width - panelWidth) / 2,
                (viewport.Height - panelHeight) / 2,
                panelWidth,
                panelHeight);

            ui.DrawPanel(spriteBatch, panel, PanelFill, PanelBorder, 3);

            int pad = 34;
            var content = new Rectangle(panel.X + pad, panel.Y + pad, panel.Width - pad * 2, panel.Height - pad * 2);

            ui.DrawText(spriteBatch, ui.Small, _topic.ToUpperInvariant(), new Vector2(content.X, content.Y), Accent);

            var promptArea = new Rectangle(content.X, content.Y + 30, content.Width, 120);
            int promptHeight = ui.DrawWrapped(
                spriteBatch, ui.Title, _question.Prompt, new Vector2(promptArea.X, promptArea.Y),
                promptArea.Width, Color.White);

            int choicesTop = promptArea.Y + Math.Max(promptHeight, 52) + 22;
            const int choiceHeight = 58;
            const int choiceGap = 12;

            _choiceRects = new Rectangle[4];

            for (int i = 0; i < 4; i++)
            {
                var rect = new Rectangle(content.X, choicesTop + i * (choiceHeight + choiceGap), content.Width, choiceHeight);
                _choiceRects[i] = rect;

                Color fill = ChoiceIdle;

                if (_answered)
                {
                    if (i == _selected)
                        fill = _correct ? ChoiceCorrect : ChoiceWrong;
                    else if (_question.Choices[i] == _question.Answer)
                        fill = ChoiceCorrect;
                }
                else if (i == _selected)
                {
                    fill = ChoiceHover;
                }

                ui.DrawPanel(spriteBatch, rect, fill, i == _selected ? Accent : PanelBorder, 2);

                var numberBox = new Rectangle(rect.X + 12, rect.Y + 10, 38, 38);
                ui.DrawPanel(spriteBatch, numberBox, new Color(18, 16, 28), PanelBorder, 1);
                ui.DrawCentered(spriteBatch, ui.Body, (i + 1).ToString(), numberBox, Accent);

                var labelRect = new Rectangle(rect.X + 64, rect.Y, rect.Width - 80, rect.Height);
                ui.DrawCentered(spriteBatch, ui.Body, _question.Choices[i], labelRect, Color.White);
            }

            int feedbackY = choicesTop + 4 * (choiceHeight + choiceGap) + 6;
            var feedbackArea = new Rectangle(content.X, feedbackY, content.Width, 90);

            if (_answered)
            {
                string feedback = _correct
                    ? $"Correct! {_question.Explanation}"
                    : $"Not quite. The answer is \"{_question.Answer}\". {_question.Explanation}";

                ui.DrawWrapped(
                    spriteBatch, ui.Small, feedback,
                    new Vector2(feedbackArea.X, feedbackArea.Y), feedbackArea.Width,
                    _correct ? CorrectColor : WrongColor);

                _continueRect = new Rectangle(
                    content.Right - 220, panel.Bottom - pad - 52, 220, 44);
                ui.DrawPanel(spriteBatch, _continueRect, ChoiceHover, Accent, 2);
                ui.DrawCentered(spriteBatch, ui.Body, "Continue (Enter)", _continueRect, Color.White);
            }
            else
            {
                string hint = "Press 1-4 or arrows + Enter. Faster answers hit harder and soften the counterattack.";
                ui.DrawWrapped(spriteBatch, ui.Small, hint,
                    new Vector2(feedbackArea.X, feedbackArea.Y), feedbackArea.Width,
                    new Color(180, 176, 200));
                _continueRect = Rectangle.Empty;
            }
        }
    }
}
