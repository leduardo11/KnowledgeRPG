using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using KnowledgeRPG.Core;

namespace KnowledgeRPG.Entities
{
    public sealed class Player : Entity
    {
        public const int PlayerMaxHp = 50;

        public Player(SpriteAnimator sprite) : base(sprite, new Vector2(10f, 18f), new Vector2(0f, -16f))
        {
            Speed = 100f;
            Hp = MaxHp;

            // Sheet layout: 48x48 frames, six columns.
            sprite.Add("front_idle", 0, 6, 5f);
            sprite.Add("side_idle", 1, 6, 5f);
            sprite.Add("back_idle", 2, 6, 5f);
            sprite.Add("front_walk", 3, 6, 5f);
            sprite.Add("side_walk", 4, 6, 5f);
            sprite.Add("back_walk", 5, 6, 5f);
            sprite.Add("front_attack", 6, 4, 5f, loop: false);
            sprite.Add("side_attack", 7, 4, 5f, loop: false);
            sprite.Add("back_attack", 8, 4, 5f, loop: false);
            sprite.Add("death", 9, 3, 6f, loop: false);

            sprite.Play("front_idle");
        }

        public int MaxHp { get; } = PlayerMaxHp;
        public int Hp { get; private set; }
        public bool IsDefeated => Hp <= 0;
        public bool InputEnabled { get; set; } = true;
        public bool InBattle { get; private set; }

        public void SetBattleMode(bool enabled)
        {
            InBattle = enabled;
            InputEnabled = !enabled;

            if (enabled)
                StopMoving();
        }

        public void SetHp(int hp) => Hp = Math.Clamp(hp, 0, MaxHp);

        public void TakeDamage(int amount) => SetHp(Hp - Math.Max(0, amount));

        public void HealFull() => SetHp(MaxHp);

        public void UpdateMovement(float deltaSeconds, InputState input, Func<RectF, bool> isSolid)
        {
            if (!InputEnabled)
            {
                StopMoving();
                return;
            }

            Vector2 direction = Vector2.Zero;

            if (input.IsDown(Keys.Left) || input.IsDown(Keys.A)) direction.X -= 1f;
            if (input.IsDown(Keys.Right) || input.IsDown(Keys.D)) direction.X += 1f;
            if (input.IsDown(Keys.Up) || input.IsDown(Keys.W)) direction.Y -= 1f;
            if (input.IsDown(Keys.Down) || input.IsDown(Keys.S)) direction.Y += 1f;

            MoveWithInput(direction, isSolid, deltaSeconds);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            // The Godot AnimatedSprite2D sat at (0, -24), centred on a 48x48 frame.
            Sprite.Draw(
                spriteBatch,
                Position + new Vector2(0f, -24f),
                DrawColor,
                new Vector2(24f, 24f));
        }
    }
}
