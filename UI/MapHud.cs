using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KnowledgeRPG.UI;

namespace KnowledgeRPG.UI
{
    /// <summary>Persistent map HUD plus the "level cleared" summary card.</summary>
    public sealed class MapHud
    {
        private int _remaining;
        private int _total;
        private string _timeText = "00:00";
        private float _pulse;

        public bool ClearVisible { get; private set; }
        public string MapName { get; private set; } = "";
        public string ClearTime { get; private set; } = "";
        public int DamageDealt { get; private set; }
        public int DamageTaken { get; private set; }

        public void SetEnemyCount(int remaining, int total)
        {
            _remaining = remaining;
            _total = total;
        }

        public void SetElapsed(TimeSpan elapsed)
        {
            _timeText = FormatDuration(elapsed);
        }

        public static string FormatDuration(TimeSpan elapsed)
        {
            int totalSeconds = (int)elapsed.TotalSeconds;
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds % 3600 / 60;
            int seconds = totalSeconds % 60;

            return hours > 0
                ? $"{hours}:{minutes:00}:{seconds:00}"
                : $"{minutes:00}:{seconds:00}";
        }

        public void ShowClear(string mapName, string time, int damageDealt, int damageTaken)
        {
            MapName = mapName;
            ClearTime = time;
            DamageDealt = damageDealt;
            DamageTaken = damageTaken;
            ClearVisible = true;
        }

        public void HideClear() => ClearVisible = false;

        public void Update(float deltaSeconds) => _pulse += deltaSeconds;

        public void Draw(SpriteBatch spriteBatch, UiPainter ui, Viewport viewport)
        {
            DrawStatus(spriteBatch, ui);

            if (ClearVisible)
                DrawClearCard(spriteBatch, ui, viewport);
        }

        private void DrawStatus(SpriteBatch spriteBatch, UiPainter ui)
        {
            var panel = new Rectangle(480, 20, 320, 46);
            ui.DrawPanel(spriteBatch, panel, new Color(20, 18, 30, 220), new Color(120, 104, 168), 2);

            ui.DrawText(spriteBatch, ui.Small,
                $"Enemies  {_remaining}/{_total}",
                new Vector2(panel.X + 16, panel.Y + 14),
                new Color(255, 226, 140));

            Vector2 timeSize = ui.Small.MeasureString(_timeText);
            ui.DrawText(spriteBatch, ui.Small, _timeText,
                new Vector2(panel.Right - 16 - timeSize.X, panel.Y + 14),
                Color.White);
        }

        private void DrawClearCard(SpriteBatch spriteBatch, UiPainter ui, Viewport viewport)
        {
            ui.FillRect(spriteBatch, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(6, 5, 12, 190));

            var card = new Rectangle(viewport.Width / 2 - 300, viewport.Height / 2 - 190, 600, 380);
            ui.DrawPanel(spriteBatch, card, new Color(24, 22, 38, 250), new Color(255, 216, 102), 3);

            ui.DrawCenteredShadow(spriteBatch, ui.Huge, "Level Cleared!", new Rectangle(card.X, card.Y + 28, card.Width, 60),
                Color.White, Color.Black);

            ui.DrawCentered(spriteBatch, ui.Body, MapName,
                new Rectangle(card.X, card.Y + 94, card.Width, 32), new Color(255, 226, 140));

            int rowY = card.Y + 150;
            DrawRow(spriteBatch, ui, card, rowY, "Time", ClearTime);
            DrawRow(spriteBatch, ui, card, rowY + 46, "Damage dealt", DamageDealt.ToString());
            DrawRow(spriteBatch, ui, card, rowY + 92, "Damage taken", DamageTaken.ToString());

            float alpha = 0.6f + 0.4f * (float)Math.Sin(_pulse * 4f);
            ui.DrawCentered(spriteBatch, ui.Body, "Press Enter to continue",
                new Rectangle(card.X, card.Bottom - 72, card.Width, 40),
                new Color(230, 226, 255) * alpha);
        }

        private static void DrawRow(SpriteBatch spriteBatch, UiPainter ui, Rectangle card, int y, string label, string value)
        {
            ui.DrawText(spriteBatch, ui.Body, label, new Vector2(card.X + 80, y), new Color(190, 186, 210));

            Vector2 size = ui.Body.MeasureString(value);
            ui.DrawText(spriteBatch, ui.Body, value, new Vector2(card.Right - 80 - size.X, y), Color.White);
        }
    }
}
