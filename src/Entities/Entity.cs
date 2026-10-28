using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KnowledgeRPG.Core;

namespace KnowledgeRPG.Entities
{
    public enum Facing
    {
        Front,
        Side,
        Back
    }

    /// <summary>
    /// Shared character behaviour: sprite animation, facing, and movement with
    /// per-axis collision resolution. <see cref="Position"/> is the character's
    /// feet (the origin used by the Godot scenes).
    /// </summary>
    public abstract class Entity(SpriteAnimator sprite, Vector2 bodySize, Vector2 bodyOffset)
	{
		public SpriteAnimator Sprite { get; } = sprite;
		public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Speed { get; set; } = 100f;
		public Vector2 BodySize { get; } = bodySize;
		public Vector2 BodyOffset { get; } = bodyOffset;
		public Facing Facing { get; protected set; } = Facing.Front;
        public bool AnimationLocked { get; protected set; }
        public bool IsVisible { get; set; } = true;
        public float Opacity { get; set; } = 1f;
        public float HitFlash { get; set; }

        public Vector2 BodyCenter => Position + BodyOffset;

        public RectF Body => RectF.FromCenter(BodyCenter, BodySize.X, BodySize.Y);

        protected Color DrawColor
        {
            get
            {
                Color tint = HitFlash > 0f ? new Color(255, 150, 130) : Color.White;
                return tint * Opacity;
            }
        }

        public virtual void Update(float deltaSeconds)
        {
            Sprite.Update(deltaSeconds);
            UpdateLockedAnimation();

            if (HitFlash > 0f)
                HitFlash = MathF.Max(0f, HitFlash - deltaSeconds);
        }

        public abstract void Draw(SpriteBatch spriteBatch);

        public void FaceDirection(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                return;

            TurnTowards(direction);
        }

        public void FaceAndWalk(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                return;

            TurnTowards(direction);
            Play($"{DirectionName}_walk");
        }

        public void FaceAndStop(Vector2 direction)
        {
            if (direction != Vector2.Zero)
                TurnTowards(direction);

            StopMoving();
        }

        protected string DirectionName => Facing switch
        {
            Facing.Side => "side",
            Facing.Back => "back",
            _ => "front"
        };

        public string AnimationDirection => DirectionName;

        protected void TurnTowards(Vector2 direction)
        {
            if (MathF.Abs(direction.X) > MathF.Abs(direction.Y))
            {
                Facing = Facing.Side;
                Sprite.FlipHorizontally = direction.X < 0;
            }
            else if (direction.Y < 0)
            {
                Facing = Facing.Back;
                Sprite.FlipHorizontally = false;
            }
            else
            {
                Facing = Facing.Front;
                Sprite.FlipHorizontally = false;
            }
        }

        public void Play(string animationName)
        {
            if (AnimationLocked || !Sprite.Has(animationName))
                return;

            Sprite.Play(animationName);
        }

        /// <summary>Plays a clip that locks animation control until it finishes.</summary>
        public void PlayOneShot(string animationName)
        {
            if (!Sprite.Has(animationName))
                return;

            AnimationLocked = true;
            Sprite.Play(animationName, restart: true);
        }

        public void PlayDeath()
        {
            if (!Sprite.Has("death"))
                return;

            AnimationLocked = true;
            Sprite.Play("death", restart: true);
        }

        public void CancelOneShotAnimation()
        {
            AnimationLocked = false;
            Play($"{DirectionName}_idle");
        }

        protected void UpdateLockedAnimation()
        {
            if (!AnimationLocked || !Sprite.Finished)
                return;

            if (Sprite.CurrentAnimation == "death")
            {
                // Hold the final frame until the battle cleans up the body.
                Sprite.SnapToEnd();
                return;
            }

            AnimationLocked = false;
            Play($"{DirectionName}_idle");
        }

        protected bool MoveInDirection(Vector2 direction, Func<RectF, bool> isSolid, float deltaSeconds)
        {
            if (direction == Vector2.Zero)
            {
                StopMoving();
                return false;
            }

            Velocity = Vector2.Normalize(direction) * Speed;
            bool collided = MoveWithCollision(isSolid, deltaSeconds);
            UpdateAnimation(direction);
            return collided;
        }

        protected bool MoveWithInput(Vector2 direction, Func<RectF, bool> isSolid, float deltaSeconds)
        {
            Velocity = direction;
            if (Velocity.LengthSquared() > 1f)
                Velocity = Vector2.Normalize(Velocity);

            Velocity *= Speed;

            bool collided = MoveWithCollision(isSolid, deltaSeconds);
            UpdateAnimation(Velocity);
            return collided;
        }

        private bool MoveWithCollision(Func<RectF, bool> isSolid, float deltaSeconds)
        {
            bool collided = false;

            Position += new Vector2(Velocity.X * deltaSeconds, 0f);
            if (isSolid(Body))
            {
                Position -= new Vector2(Velocity.X * deltaSeconds, 0f);
                Velocity = new Vector2(0f, Velocity.Y);
                collided = true;
            }

            Position += new Vector2(0f, Velocity.Y * deltaSeconds);
            if (isSolid(Body))
            {
                Position -= new Vector2(0f, Velocity.Y * deltaSeconds);
                Velocity = new Vector2(Velocity.X, 0f);
                collided = true;
            }

            return collided;
        }

        protected void StopMoving()
        {
            Velocity = Vector2.Zero;
            UpdateAnimation(Vector2.Zero);
        }

        private void UpdateAnimation(Vector2 direction)
        {
            if (AnimationLocked)
                return;

            if (direction == Vector2.Zero)
            {
                Play($"{DirectionName}_idle");
                return;
            }

            TurnTowards(direction);
            Play($"{DirectionName}_walk");
        }
    }
}
