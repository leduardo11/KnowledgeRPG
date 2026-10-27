using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KnowledgeRPG.Core;

namespace KnowledgeRPG.UI
{
    /// <summary>Small drawing helpers shared by the HUD, quiz panel and battle UI.</summary>
    public sealed class UiPainter
    {
        private readonly Texture2D _pixel;

        public UiPainter(GraphicsDevice device, SpriteFont small, SpriteFont body, SpriteFont title, SpriteFont huge)
        {
            _pixel = new Texture2D(device, 1, 1);
            _pixel.SetData(new[] { Color.White });
            Small = small;
            Body = body;
            Title = title;
            Huge = huge;
        }

        public SpriteFont Small { get; }
        public SpriteFont Body { get; }
        public SpriteFont Title { get; }
        public SpriteFont Huge { get; }

        public void FillRect(SpriteBatch spriteBatch, Rectangle rectangle, Color color) =>
            spriteBatch.Draw(_pixel, rectangle, color);

        public void FillRect(SpriteBatch spriteBatch, RectF rectangle, Color color) =>
            FillRect(spriteBatch, ToRectangle(rectangle), color);

        public void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds, Color fill, Color border, int thickness = 2)
        {
            FillRect(spriteBatch, bounds, fill);

            FillRect(spriteBatch, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), border);
            FillRect(spriteBatch, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), border);
            FillRect(spriteBatch, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), border);
            FillRect(spriteBatch, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), border);
        }

        public void DrawText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, text, position, color);
        }

        public void DrawTextShadow(
            SpriteBatch spriteBatch,
            SpriteFont font,
            string text,
            Vector2 position,
            Color color,
            Color shadow,
            int offset = 2)
        {
            spriteBatch.DrawString(font, text, position + new Vector2(offset, offset), shadow);
            spriteBatch.DrawString(font, text, position, color);
        }

        public void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, Rectangle area, Color color)
        {
            Vector2 size = font.MeasureString(text);
            var position = new Vector2(
                area.X + (area.Width - size.X) / 2f,
                area.Y + (area.Height - size.Y) / 2f);
            spriteBatch.DrawString(font, text, position, color);
        }

        public void DrawCenteredShadow(
            SpriteBatch spriteBatch,
            SpriteFont font,
            string text,
            Rectangle area,
            Color color,
            Color shadow,
            int offset = 2)
        {
            Vector2 size = font.MeasureString(text);
            var position = new Vector2(
                area.X + (area.Width - size.X) / 2f,
                area.Y + (area.Height - size.Y) / 2f);
            DrawTextShadow(spriteBatch, font, text, position, color, shadow, offset);
        }

        /// <summary>Greedy word wrap that also honours explicit newlines.</summary>
        public IReadOnlyList<string> Wrap(SpriteFont font, string text, float maxWidth)
        {
            var lines = new List<string>();

            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                string current = string.Empty;

                foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = current.Length == 0 ? word : current + " " + word;

                    if (current.Length > 0 && font.MeasureString(candidate).X > maxWidth)
                    {
                        lines.Add(current);
                        current = word;
                    }
                    else
                    {
                        current = candidate;
                    }
                }

                lines.Add(current);
            }

            return lines;
        }

        public int DrawWrapped(
            SpriteBatch spriteBatch,
            SpriteFont font,
            string text,
            Vector2 position,
            float maxWidth,
            Color color,
            float lineSpacing = 2f)
        {
            IReadOnlyList<string> lines = Wrap(font, text, maxWidth);
            float y = position.Y;

            foreach (string line in lines)
            {
                spriteBatch.DrawString(font, line, new Vector2(position.X, y), color);
                y += font.LineSpacing + lineSpacing;
            }

            return (int)(y - position.Y);
        }

        public int MeasureWrappedHeight(SpriteFont font, string text, float maxWidth, float lineSpacing = 2f)
        {
            IReadOnlyList<string> lines = Wrap(font, text, maxWidth);
            return (int)(lines.Count * (font.LineSpacing + lineSpacing));
        }

        public static Rectangle ToRectangle(RectF rect) =>
            new((int)rect.X, (int)rect.Y, (int)MathF.Ceiling(rect.Width), (int)MathF.Ceiling(rect.Height));
    }
}
