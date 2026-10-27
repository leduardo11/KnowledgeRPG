using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KnowledgeRPG.Core
{
    /// <summary>
    /// A single animation strip on a sprite sheet: a row of frames with equal
    /// size, played at a fixed rate. Non-looping clips hold their last frame.
    /// </summary>
    public sealed class AnimationClip
    {
        public AnimationClip(string name, int row, int frameCount, float framesPerSecond, bool loop)
        {
            Name = name;
            Row = row;
            FrameCount = frameCount;
            FramesPerSecond = framesPerSecond;
            Loop = loop;
        }

        public string Name { get; }
        public int Row { get; }
        public int FrameCount { get; }
        public float FramesPerSecond { get; }
        public bool Loop { get; }
    }

    /// <summary>Plays <see cref="AnimationClip"/>s from a grid based sprite sheet.</summary>
    public sealed class SpriteAnimator
    {
        private readonly Texture2D _texture;
        private readonly int _frameWidth;
        private readonly int _frameHeight;
        private readonly int _columns;
        private readonly Dictionary<string, AnimationClip> _clips = new();

        private AnimationClip _current;
        private float _elapsed;
        private int _frame;

        public SpriteAnimator(Texture2D texture, int frameWidth, int frameHeight)
        {
            _texture = texture;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
            _columns = texture.Width / frameWidth;
        }

        public string CurrentAnimation => _current?.Name ?? string.Empty;
        public bool Finished { get; private set; }
        public bool FlipHorizontally { get; set; }

        public void Add(AnimationClip clip) => _clips[clip.Name] = clip;

        public void Add(string name, int row, int frameCount, float fps, bool loop = true) =>
            Add(new AnimationClip(name, row, frameCount, fps, loop));

        public bool Has(string name) => _clips.ContainsKey(name);

        public void Play(string name, bool restart = false)
        {
            if (!_clips.TryGetValue(name, out AnimationClip clip))
                return;

            if (!restart && ReferenceEquals(_current, clip))
                return;

            _current = clip;
            _frame = 0;
            _elapsed = 0f;
            Finished = false;
        }

        public void Update(float deltaSeconds)
        {
            if (_current is null || _current.FrameCount <= 1)
                return;

            _elapsed += deltaSeconds;
            float frameDuration = 1f / _current.FramesPerSecond;

            while (_elapsed >= frameDuration)
            {
                _elapsed -= frameDuration;

                if (_frame + 1 < _current.FrameCount)
                {
                    _frame++;
                    continue;
                }

                if (_current.Loop)
                {
                    _frame = 0;
                }
                else
                {
                    Finished = true;
                    break;
                }
            }
        }

        /// <summary>Advances to the final frame immediately (used for death clips).</summary>
        public void SnapToEnd()
        {
            if (_current is null)
                return;

            _frame = _current.FrameCount - 1;
            _elapsed = 0f;
            Finished = true;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color, Vector2 origin)
        {
            if (_current is null)
                return;

            var source = new Rectangle(
                _frame * _frameWidth,
                _current.Row * _frameHeight,
                _frameWidth,
                _frameHeight);

            SpriteEffects effects = FlipHorizontally ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            spriteBatch.Draw(_texture, position, source, color, 0f, origin, 1f, effects, 0f);
        }

        public Rectangle GetFrameBounds(Vector2 position, Vector2 origin) =>
            new(
                (int)(position.X - origin.X),
                (int)(position.Y - origin.Y),
                _frameWidth,
                _frameHeight);
    }
}
