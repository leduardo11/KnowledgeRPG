using System;
using Microsoft.Xna.Framework;
using KnowledgeRPG.Core;
using KnowledgeRPG.Entities;
using KnowledgeRPG.Quiz;

namespace KnowledgeRPG.Battle
{
    /// <summary>
    /// Turn based quiz combat. The controller is a small frame driven state
    /// machine so it ports cleanly from the async/tween based Godot original.
    /// </summary>
    public sealed class BattleController
    {
        private enum Phase
        {
            Idle,
            IntroMove,
            IntroWait,
            IntroHud,
            PlayerInput,
            Quiz,
            PlayerAttackLunge,
            PlayerAttackReturn,
            PlayerAttackWait,
            EnemyDelay,
            EnemyAttackLunge,
            EnemyAttackReturn,
            EnemyAttackWait,
            EnemyRecover,
            VictoryDeath,
            VictoryFade,
            VictoryPause,
            DefeatDeath,
            DefeatFade,
            DefeatPause,
            OutroFade,
        }

        public event Action Activated;
        public event Action<bool, int> PlayerAttacked;
        public event Action<int, int> EnemyAttacked;
        public event Action Ended;
        public event Action PlayerDefeated;

        public BattleState State { get; private set; } = BattleState.Inactive;
        public Enemy CurrentEnemy { get; private set; }
        public BattleHud Hud { get; }
        public QuizPanel QuizPanel { get; }

        public int PlayerAttackDamage { get; set; } = 10;
        public int EnemyAttackDamage { get; set; } = 6;
        public float FormationDistance { get; set; } = 36f;
        public bool SpeedBonusEnabled { get; set; } = true;
        public float FastAnswerSeconds { get; set; } = 1.5f;
        public float SlowAnswerSeconds { get; set; } = 5f;
        public float PlayerMaxSpeedBonus { get; set; } = 0.5f;
        public float EnemyMaxSpeedBonus { get; set; } = 0.25f;

        public float FlashAlpha { get; private set; }

        private const float VerticalFormationBelow = 46f;
        private const float VerticalFormationAbove = -10f;
        private const float FootAlignmentOffset = 13f;

        private readonly Player _player;
        private readonly Maps.ForestMap _map;
        private readonly QuizGate _gate;
        private readonly QuizService _quizService;

        private Phase _phase = Phase.Idle;
        private float _phaseTime;
        private float _phaseDuration;
        private double _clock;
        private double _roundAnswerSeconds = -1;
        private bool _victory;
        private bool _defeat;
        private bool _gateOpenFrame;

        private Vector2 _introFrom;
        private Vector2 _introTo;
        private Vector2 _playerFacing;
        private Vector2 _enemyFacing;

        private Vector2 _attackOrigin;
        private Vector2 _attackTarget;
        private Entity _attacker;
        private Entity _attackTargetEntity;
        private Action _impact;
        private int _pendingPlayerDamage;
        private int _pendingEnemyDamage;
        private bool _pendingPlayerHit;

        public BattleController(Player player, Maps.ForestMap map, BattleHud hud, QuizPanel quizPanel)
        {
            _player = player;
            _map = map;
            Hud = hud;
            QuizPanel = quizPanel;
            _quizService = new QuizService();
            _gate = new QuizGate(_quizService, quizPanel);
        }

        public void Update(float deltaSeconds, InputState input, double now, bool pointerConfirm = false)
        {
            _clock = now;
            Hud.Update(deltaSeconds);

            if (State == BattleState.Inactive)
                return;

            _phaseTime += deltaSeconds;

            if (FlashAlpha > 0f)
                FlashAlpha = Math.Max(0f, FlashAlpha - deltaSeconds * 3f);

            switch (_phase)
            {
                case Phase.IntroMove:
                    UpdateIntroMove();
                    break;
                case Phase.IntroWait:
                    WaitThen(Phase.IntroHud, 0.6f, BeginIntroHud);
                    break;
                case Phase.IntroHud:
                    Hud.Opacity = MathHelper.Clamp(_phaseTime / 0.2f, 0f, 1f);
                    if (IsPhaseDone())
                    {
                        Hud.Opacity = 1f;
                        State = BattleState.Active;
                        Activated?.Invoke();
                        BeginPlayerTurn();
                    }
                    break;
                case Phase.PlayerInput:
                    if (input.ConfirmPressed || pointerConfirm)
                        OpenQuiz();
                    break;
                case Phase.Quiz:
                    if (!_gateOpenFrame && _gate.TryConsume(out QuizAnswerResult result))
                        StartPlayerAttack(result);

                    _gateOpenFrame = false;
                    break;
                case Phase.PlayerAttackLunge:
                    UpdateLunge(_attacker, _attackOrigin, _attackTarget);
                    if (IsPhaseDone())
                    {
                        _attacker.Position = _attackTarget;
                        _impact?.Invoke();
                        SetPhase(Phase.PlayerAttackReturn, 0.14f);
                    }
                    break;
                case Phase.PlayerAttackReturn:
                    UpdateLunge(_attacker, _attackTarget, _attackOrigin);
                    if (IsPhaseDone())
                    {
                        _attacker.Position = _attackOrigin;
                        SetPhase(Phase.PlayerAttackWait, 0.57f);
                    }
                    break;
                case Phase.PlayerAttackWait:
                    if (IsPhaseDone())
                    {
                        _attacker = null;
                        if (CurrentEnemy is null || CurrentEnemy.IsDefeated)
                            StartVictory();
                        else
                            SetPhase(Phase.EnemyDelay, 0.9f);
                    }
                    break;
                case Phase.EnemyDelay:
                    if (IsPhaseDone())
                        StartEnemyAttack();
                    break;
                case Phase.EnemyAttackLunge:
                    UpdateLunge(_attacker, _attackOrigin, _attackTarget);
                    if (IsPhaseDone())
                    {
                        _attacker.Position = _attackTarget;
                        _impact?.Invoke();
                        SetPhase(Phase.EnemyAttackReturn, 0.14f);
                    }
                    break;
                case Phase.EnemyAttackReturn:
                    UpdateLunge(_attacker, _attackTarget, _attackOrigin);
                    if (IsPhaseDone())
                    {
                        _attacker.Position = _attackOrigin;
                        SetPhase(Phase.EnemyAttackWait, 1.17f);
                    }
                    break;
                case Phase.EnemyAttackWait:
                    if (IsPhaseDone())
                    {
                        _attacker = null;
                        if (_player.IsDefeated)
                            StartDefeat();
                        else
                            SetPhase(Phase.EnemyRecover, 0.8f);
                    }
                    break;
                case Phase.EnemyRecover:
                    if (IsPhaseDone())
                        BeginPlayerTurn();
                    break;
                case Phase.VictoryDeath:
                    if (IsPhaseDone())
                        SetPhase(Phase.VictoryFade, 0.45f);
                    break;
                case Phase.VictoryFade:
                    if (CurrentEnemy is not null)
                        CurrentEnemy.Opacity = 1f - MathHelper.Clamp(_phaseTime / 0.45f, 0f, 1f);

                    if (IsPhaseDone())
                    {
                        if (CurrentEnemy is not null)
                            CurrentEnemy.IsVisible = false;

                        SetPhase(Phase.VictoryPause, 0.35f);
                    }
                    break;
                case Phase.VictoryPause:
                    if (IsPhaseDone())
                        BeginOutro();
                    break;
                case Phase.DefeatDeath:
                    if (IsPhaseDone())
                        SetPhase(Phase.DefeatFade, 0.45f);
                    break;
                case Phase.DefeatFade:
                    _player.Opacity = 1f - 0.7f * MathHelper.Clamp(_phaseTime / 0.45f, 0f, 1f);
                    if (IsPhaseDone())
                        SetPhase(Phase.DefeatPause, 0.4f);
                    break;
                case Phase.DefeatPause:
                    if (IsPhaseDone())
                        BeginOutro();
                    break;
                case Phase.OutroFade:
                    Hud.Opacity = 1f - MathHelper.Clamp(_phaseTime / 0.2f, 0f, 1f);
                    if (IsPhaseDone())
                        FinishBattle();
                    break;
            }
        }

        public void BeginEncounter(Enemy enemy)
        {
            if (State != BattleState.Inactive)
                return;

            if (enemy is null || _player is null)
                return;

            CurrentEnemy = enemy;
            State = BattleState.Intro;
            _victory = false;
            _defeat = false;
            _roundAnswerSeconds = -1;
            _attacker = null;

            _player.SetBattleMode(true);
            enemy.BeginBattle();

            Hud.Reset();
            Hud.SetHp(_player.Hp, _player.MaxHp);
            Hud.SetEnemyHp(enemy.BattleName, enemy.Hp, enemy.MaxHp);
            Hud.ShowEncounter(enemy.BattleName);

            Vector2 offset = _player.Position - enemy.Position;
            bool horizontal = MathF.Abs(offset.X) >= MathF.Abs(offset.Y);

            if (horizontal)
            {
                float side = offset.X < 0f ? -1f : 1f;
                _introTo = enemy.Position + new Vector2(side * FormationDistance, FootAlignmentOffset);
                _playerFacing = new Vector2(-side, 0f);
                _enemyFacing = new Vector2(side, 0f);
            }
            else
            {
                float side = offset.Y < 0f ? -1f : 1f;
                float targetY = side < 0f ? VerticalFormationAbove : VerticalFormationBelow;
                _introTo = enemy.Position + new Vector2(0f, targetY);
                _playerFacing = new Vector2(0f, -side);
                _enemyFacing = new Vector2(0f, side);
            }

            _introFrom = _player.Position;
            _player.FaceAndWalk(_playerFacing);
            enemy.FaceDirection(_enemyFacing);

            FlashAlpha = 0.55f;
            SetPhase(Phase.IntroMove, 0.32f);
        }

        private void BeginIntroHud()
        {
            Hud.Opacity = 0f;
        }

        private void UpdateIntroMove()
        {
            float t = MathHelper.Clamp(_phaseTime / _phaseDuration, 0f, 1f);
            float ease = 1f - (1f - t) * (1f - t);
            _player.Position = Vector2.Lerp(_introFrom, _introTo, ease);

            if (IsPhaseDone())
            {
                _player.Position = _introTo;
                _player.FaceAndStop(_playerFacing);
                CurrentEnemy?.FaceAndStop(_enemyFacing);
                SetPhase(Phase.IntroWait, 0.1f);
            }
        }

        private void BeginPlayerTurn()
        {
            if (CurrentEnemy is null || _player.IsDefeated)
                return;

            _roundAnswerSeconds = -1;
            Hud.HideMessage();
            Hud.SetCommandsVisible(true);
            SetPhase(Phase.PlayerInput, 0f, keepTime: true);
        }

        private void OpenQuiz()
        {
            Hud.SetCommandsVisible(false);
            _gateOpenFrame = true;
            _gate.Ask(_map?.QuizPackId ?? "", _clock);
            SetPhase(Phase.Quiz, 0f, keepTime: true);
        }

        private void StartPlayerAttack(QuizAnswerResult result)
        {
            _roundAnswerSeconds = result.AnswerSeconds;

            int damage = 0;
            if (result.Correct)
            {
                double maxBonus = SpeedBonusEnabled ? PlayerMaxSpeedBonus : 0;
                damage = DamageCalculator.ComputePlayerDamage(
                    PlayerAttackDamage,
                    result.AnswerSeconds,
                    FastAnswerSeconds,
                    SlowAnswerSeconds,
                    maxBonus);
            }

            _pendingPlayerDamage = damage;
            _pendingPlayerHit = result.Correct;

            BeginAttackAnimation(_player, CurrentEnemy, damage, isPlayerAttacking: true);
        }

        private void StartEnemyAttack()
        {
            int damage = DamageCalculator.ComputeEnemyDamage(
                EnemyAttackDamage,
                _roundAnswerSeconds,
                FastAnswerSeconds,
                SlowAnswerSeconds,
                SpeedBonusEnabled ? EnemyMaxSpeedBonus : 0);

            _pendingEnemyDamage = damage;
            Hud.HideMessage();
            BeginAttackAnimation(CurrentEnemy, _player, damage, isPlayerAttacking: false);
        }

        private void BeginAttackAnimation(Entity attacker, Entity target, int damage, bool isPlayerAttacking)
        {
            if (attacker is null || target is null)
                return;

            Vector2 direction = target.Position - attacker.Position;
            if (direction == Vector2.Zero)
                direction = Vector2.UnitY;
            direction.Normalize();

            attacker.FaceDirection(direction);
            attacker.PlayOneShot($"{attacker.AnimationDirection}_attack");

            _attacker = attacker;
            _attackTargetEntity = target;
            _attackOrigin = attacker.Position;
            _attackTarget = _attackOrigin + direction * 8f;

            if (isPlayerAttacking)
            {
                _impact = ApplyPlayerImpact;
                SetPhase(Phase.PlayerAttackLunge, 0.09f);
            }
            else
            {
                _impact = ApplyEnemyImpact;
                SetPhase(Phase.EnemyAttackLunge, 0.09f);
            }
        }

        private void ApplyPlayerImpact()
        {
            Enemy enemy = CurrentEnemy;
            if (enemy is null || !enemy.IsVisible)
                return;

            int actualDamage = 0;

            if (_pendingPlayerHit)
            {
                int before = enemy.Hp;
                enemy.TakeDamage(_pendingPlayerDamage);
                actualDamage = before - enemy.Hp;
                enemy.HitFlash = 0.15f;
                Hud.AddDamageNumber(enemy.BodyCenter + new Vector2(0f, -22f), _pendingPlayerDamage);
            }
            else
            {
                Hud.AddDamageNumber(enemy.BodyCenter + new Vector2(0f, -22f), 0, miss: true);
            }

            Hud.SetEnemyHp(enemy.BattleName, enemy.Hp, enemy.MaxHp);
            PlayerAttacked?.Invoke(_pendingPlayerHit, actualDamage);
        }

        private void ApplyEnemyImpact()
        {
            int before = _player.Hp;
            _player.TakeDamage(_pendingEnemyDamage);
            int actualDamage = before - _player.Hp;
            _player.HitFlash = 0.15f;

            Hud.SetHp(_player.Hp, _player.MaxHp);
            Hud.AddDamageNumber(_player.Position + new Vector2(0f, -52f), _pendingEnemyDamage);
            EnemyAttacked?.Invoke(actualDamage, _player.Hp);
        }

        private void StartVictory()
        {
            _victory = true;
            State = BattleState.Outro;
            Hud.SetCommandsVisible(false);
            Hud.SetMessage($"{CurrentEnemy?.BattleName ?? "Enemy"} is defeated!");

            if (CurrentEnemy is not null)
            {
                CurrentEnemy.PlayDeath();
            }

            SetPhase(Phase.VictoryDeath, 0.75f);
        }

        private void StartDefeat()
        {
            _defeat = true;
            State = BattleState.Outro;
            Hud.SetCommandsVisible(false);
            Hud.SetMessage("You were defeated...");

            if (_attacker is not null)
            {
                _attacker = null;
            }

            _player.PlayDeath();
            SetPhase(Phase.DefeatDeath, 0.55f);
        }

        private void BeginOutro()
        {
            Hud.SetCommandsVisible(false);
            SetPhase(Phase.OutroFade, 0.2f);
        }

        private void FinishBattle()
        {
            if (CurrentEnemy is not null)
            {
                if (_victory)
                    CurrentEnemy.RemoveFromMap();
                else
                    CurrentEnemy.EndEncounter();
            }

            _player.CancelOneShotAnimation();
            _player.Opacity = 1f;
            _player.HitFlash = 0f;

            if (_defeat)
            {
                _player.HealFull();
                Hud.SetHp(_player.Hp, _player.MaxHp);
            }

            _player.SetBattleMode(false);

            bool wasDefeat = _defeat;
            CurrentEnemy = null;
            State = BattleState.Inactive;
            _victory = false;
            _defeat = false;
            _roundAnswerSeconds = -1;
            _attacker = null;
            _attackTargetEntity = null;
            _impact = null;
            _phase = Phase.Idle;

            Hud.Reset();
            Ended?.Invoke();

            if (wasDefeat)
                PlayerDefeated?.Invoke();
        }

        private void UpdateLunge(Entity actor, Vector2 from, Vector2 to)
        {
            if (actor is null)
                return;

            float t = MathHelper.Clamp(_phaseTime / MathF.Max(_phaseDuration, 0.0001f), 0f, 1f);
            actor.Position = Vector2.Lerp(from, to, t);
        }

        private void SetPhase(Phase phase, float duration, bool keepTime = false)
        {
            _phase = phase;
            _phaseDuration = duration;

            if (!keepTime)
                _phaseTime = 0f;
        }

        private bool IsPhaseDone() => _phaseTime >= _phaseDuration;

        private void WaitThen(Phase next, float duration, Action onStart)
        {
            if (IsPhaseDone())
            {
                onStart?.Invoke();
                SetPhase(next, duration);
            }
        }
    }
}
