using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KnowledgeRPG.UI;

namespace KnowledgeRPG.Battle
{
    /// <summary>Battle overlays: HP bars, message panel, command hint and damage numbers.</summary>
    public sealed class BattleHud
    {
        private sealed class FloatingNumber
        {
            public Vector2 Position;
            public string Text = "";
            public Color Color;
            public float Age;
            public float Lifetime;
        }

        private readonly List<FloatingNumber> _numbers = new();
        private int _hp;
        private int _maxHp = 1;
        private int _enemyHp;
        private int _enemyMaxHp = 1;
        private string _enemyName = "";
        private string _message = "";
        private bool _messageVisible;
        private bool _commandsVisible;
        private float _pulse;

        public float Opacity { get; set; }
        public bool Visible => Opacity > 0.01f;
        public bool CommandsVisible => _commandsVisible;

        public void Reset()
        {
            _numbers.Clear();
            _messageVisible = false;
            _commandsVisible = false;
            Opacity = 0f;
        }

        public void ShowEncounter(string enemyName)
        {
            _enemyName = enemyName;
            _message = $"{enemyName} wants to fight!";
            _messageVisible = true;
            _commandsVisible = false;
        }

        public void SetMessage(string text)
        {
            _message = text;
            _messageVisible = true;
        }

        public void HideMessage() => _messageVisible = false;

        public void SetCommandsVisible(bool visible) => _commandsVisible = visible;

        public void SetHp(int current, int max)
        {
            _maxHp = Math.Max(1, max);
            _hp = Math.Clamp(current, 0, _maxHp);
        }

        public void SetEnemyHp(string name, int current, int max)
        {
            _enemyName = name;
            _enemyMaxHp = Math.Max(1, max);
            _enemyHp = Math.Clamp(current, 0, _enemyMaxHp);
        }

        public void AddDamageNumber(Vector2 worldPosition, int damage, bool miss = false)
        {
            _numbers.Add(new FloatingNumber
            {
                Position = worldPosition,
                Text = miss ? "MISS" : damage.ToString(),
                Color = miss ? new Color(242, 102, 102) : new Color(255, 230, 90),
                Lifetime = 0.55f,
            });
        }

        public void Update(float deltaSeconds)
        {
            _pulse += deltaSeconds;

            for (int i = _numbers.Count - 1; i >= 0; i--)
            {
                FloatingNumber number = _numbers[i];
                number.Age += deltaSeconds;
                number.Position.Y -= 24f * deltaSeconds;

                if (number.Age >= number.Lifetime)
                    _numbers.RemoveAt(i);
            }
        }

        public void DrawWorld(SpriteBatch spriteBatch, UiPainter ui)
        {
            foreach (FloatingNumber number in _numbers)
            {
                float remaining = number.Lifetime - number.Age;
                float alpha = MathHelper.Clamp(remaining / 0.3f, 0f, 1f);
                Color color = number.Color * alpha;
                float scale = number.Text == "MISS" ? 0.85f : 1f;
                Vector2 size = ui.Small.MeasureString(number.Text) * scale;
                Vector2 position = number.Position - new Vector2(size.X / 2f, 0f);

                spriteBatch.DrawString(ui.Small, number.Text, position + new Vector2(1f, 1f), Color.Black * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(ui.Small, number.Text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        public void Draw(SpriteBatch spriteBatch, UiPainter ui, Viewport viewport)
        {
            if (!Visible)
                return;

            Color text = Color.White * Opacity;
            Color dim = new Color(16, 14, 26) * (0.82f * Opacity);
            Color border = new Color(120, 104, 168) * Opacity;

            DrawPlayerPanel(spriteBatch, ui, text, dim, border);
            DrawEnemyPanel(spriteBatch, ui, text, dim, border);

            if (_messageVisible)
            {
                var messageRect = new Rectangle(viewport.Width / 2 - 320, viewport.Height - 132, 640, 54);
                ui.DrawPanel(spriteBatch, messageRect, new Color(20, 18, 30) * (0.9f * Opacity), border, 2);
                ui.DrawCentered(spriteBatch, ui.Body, _message, messageRect, text);
            }

            if (_commandsVisible)
            {
                float alpha = 0.65f + 0.35f * (float)Math.Sin(_pulse * 4f);
                var hintRect = new Rectangle(viewport.Width / 2 - 300, viewport.Height - 66, 600, 40);
                ui.DrawCenteredShadow(
                    spriteBatch, ui.Body,
                    "Press Enter or Space to answer a question",
                    hintRect,
                    new Color(255, 226, 140) * (alpha * Opacity),
                    new Color(10, 8, 14) * Opacity);
            }
        }

        private void DrawPlayerPanel(SpriteBatch spriteBatch, UiPainter ui, Color text, Color fill, Color border)
        {
            var panel = new Rectangle(24, 20, 320, 78);
            ui.DrawPanel(spriteBatch, panel, fill, border, 2);

            ui.DrawTextShadow(spriteBatch, ui.Body, "Hero", new Vector2(panel.X + 14, panel.Y + 8), text, Color.Black * Opacity);

            var bar = new Rectangle(panel.X + 14, panel.Y + 36, 292, 18);
            ui.FillRect(spriteBatch, bar, new Color(48, 20, 30) * Opacity);
            float ratio = (float)_hp / _maxHp;
            ui.FillRect(spriteBatch, new Rectangle(bar.X, bar.Y, (int)(bar.Width * ratio), bar.Height),
                new Color(94, 214, 118) * Opacity);
            ui.DrawPanel(spriteBatch, bar, Color.Transparent, border, 2);

            ui.DrawCentered(spriteBatch, ui.Small, $"{_hp}/{_maxHp}", bar, text);
        }

        private void DrawEnemyPanel(SpriteBatch spriteBatch, UiPainter ui, Color text, Color fill, Color border)
        {
            var panel = new Rectangle(936, 20, 320, 78);
            ui.DrawPanel(spriteBatch, panel, fill, border, 2);

            ui.DrawTextShadow(spriteBatch, ui.Body, _enemyName, new Vector2(panel.X + 14, panel.Y + 8), text, Color.Black * Opacity);

            var bar = new Rectangle(panel.X + 14, panel.Y + 36, 292, 18);
            ui.FillRect(spriteBatch, bar, new Color(48, 20, 30) * Opacity);
            float ratio = (float)_enemyHp / _enemyMaxHp;
            ui.FillRect(spriteBatch, new Rectangle(bar.X, bar.Y, (int)(bar.Width * ratio), bar.Height),
                new Color(226, 98, 98) * Opacity);
            ui.DrawPanel(spriteBatch, bar, Color.Transparent, border, 2);

            ui.DrawCentered(spriteBatch, ui.Small, $"{_enemyHp}/{_enemyMaxHp}", bar, text);
        }
    }
}
