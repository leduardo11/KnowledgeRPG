using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KnowledgeRPG.Core;

namespace KnowledgeRPG.Entities
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Engaged
    }

    /// <summary>
    /// Wandering hostile that chases the player when it has line of sight and
    /// starts a quiz battle on contact.
    /// </summary>
    public class Enemy : Entity
    {
        private const float DetectionRadius = 100f;
        private const float RepathDistance = 4f;
        private Vector2 _lastTargetPosition;
        private bool _hasPathTarget;

        public Enemy(SpriteAnimator sprite, Vector2 bodySize, Vector2 bodyOffset)
            : base(sprite, bodySize, bodyOffset)
        {
            Hp = MaxHp;
        }

        public event Action Engaged;

        public string DisplayName { get; set; } = string.Empty;
        public string BattleName => string.IsNullOrWhiteSpace(DisplayName) ? GetType().Name : DisplayName;
        public int MaxHp { get; set; } = 30;
        public int Hp { get; protected set; }
        public bool IsDefeated => Hp <= 0;
        public EnemyState State { get; protected set; } = EnemyState.Idle;
        public bool BattleMode { get; private set; }
        public bool IsRemovedFromMap { get; private set; }
        public bool HealthBarVisible { get; private set; }

        public Vector2 SpawnPosition { get; private set; }

        public void SetSpawn(Vector2 position)
        {
            Position = position;
            SpawnPosition = position;
        }

        public void SetHp(int hp)
        {
            Hp = Math.Clamp(hp, 0, MaxHp);
        }

        public void TakeDamage(int amount) => SetHp(Hp - Math.Max(0, amount));

        public void UpdateHostile(
            float deltaSeconds,
            Player player,
            Func<RectF, bool> isSolid)
        {
            base.Update(deltaSeconds);

            if (IsRemovedFromMap || !IsVisible)
                return;

            if (BattleMode || State == EnemyState.Engaged)
            {
                FaceAndStop(player.BodyCenter - BodyCenter);
                return;
            }

            Vector2 toPlayer = player.BodyCenter - BodyCenter;
            float distanceSquared = toPlayer.LengthSquared();
            bool inRange = distanceSquared <= DetectionRadius * DetectionRadius;

            if (!inRange || !HasLineOfSight(player, isSolid))
            {
                EnterIdle();
                return;
            }

            if (ContactRect.Intersects(player.Body))
            {
                EnterEngaged();
                return;
            }

            State = EnemyState.Chase;

            // The original used a NavigationAgent2D. The forest is open enough
            // that steering straight at the player with collision sliding reads
            // the same, and avoids baking a navmesh per map.
            if (!_hasPathTarget ||
                Vector2.DistanceSquared(_lastTargetPosition, player.BodyCenter) >
                RepathDistance * RepathDistance)
            {
                _lastTargetPosition = player.BodyCenter;
                _hasPathTarget = true;
            }

            MoveInDirection(toPlayer, isSolid, deltaSeconds);

            if (ContactRect.Intersects(player.Body))
                EnterEngaged();
        }

        private RectF ContactRect =>
            RectF.FromCenter(BodyCenter, 20f, 17f);

        private bool HasLineOfSight(Player player, Func<RectF, bool> isSolid)
        {
            Vector2 start = BodyCenter;
            Vector2 end = player.BodyCenter;
            Vector2 delta = end - start;
            float distance = delta.Length();

            if (distance < 0.001f)
                return true;

            Vector2 step = Vector2.Normalize(delta) * 8f;
            Vector2 probe = start + step;

            for (float travelled = step.Length(); travelled < distance; travelled += step.Length())
            {
                if (isSolid(RectF.FromCenter(probe, 2f, 2f)))
                    return false;

                probe += step;
            }

            return true;
        }

        private void EnterIdle()
        {
            State = EnemyState.Idle;
            _hasPathTarget = false;
            StopMoving();
        }

        private void EnterEngaged()
        {
            if (State == EnemyState.Engaged)
                return;

            State = EnemyState.Engaged;
            _hasPathTarget = false;
            StopMoving();
            Engaged?.Invoke();
        }

        public void BeginBattle()
        {
            BattleMode = true;
            State = EnemyState.Engaged;
            _hasPathTarget = false;
            SetHp(MaxHp);
            HealthBarVisible = true;
        }

        public void EndEncounter()
        {
            BattleMode = false;
            HealthBarVisible = false;
            EnterIdle();
        }

        public void RemoveFromMap()
        {
            IsRemovedFromMap = true;
            BattleMode = false;
            State = EnemyState.Idle;
            HealthBarVisible = false;
            IsVisible = false;
            Velocity = Vector2.Zero;
        }

        public void ResetForMap()
        {
            IsRemovedFromMap = false;
            IsVisible = true;
            Position = SpawnPosition;
            Velocity = Vector2.Zero;
            Opacity = 1f;
            BattleMode = false;
            State = EnemyState.Idle;
            HealthBarVisible = false;
            _hasPathTarget = false;
            CancelOneShotAnimation();
            SetHp(MaxHp);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            Sprite.Draw(
                spriteBatch,
                Position,
                DrawColor,
                new Vector2(16f, 16f));
        }
    }
}
