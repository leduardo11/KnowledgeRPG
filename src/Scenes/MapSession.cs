using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using KnowledgeRPG.Battle;
using KnowledgeRPG.Entities;
using KnowledgeRPG.Maps;
using KnowledgeRPG.UI;

namespace KnowledgeRPG.Scenes
{
    /// <summary>
    /// Tracks map progress (enemies, time, damage), drives the HUD and resets
    /// the map when the player is defeated.
    /// </summary>
    public sealed class MapSession
    {
        private readonly ForestMap _map;
        private readonly Player _player;
        private readonly IReadOnlyList<Enemy> _enemies;
        private readonly BattleController _battle;
        private readonly MapHud _hud;
        private readonly System.Diagnostics.Stopwatch _stopwatch = new();

        private Enemy _activeEnemy;

        public MapSession(
            ForestMap map,
            Player player,
            IReadOnlyList<Enemy> enemies,
            BattleController battle,
            MapHud hud)
        {
            _map = map;
            _player = player;
            _enemies = enemies;
            _battle = battle;
            _hud = hud;

            TotalEnemies = enemies.Count;
            _playerSpawn = player.Position;

            _battle.Activated += OnBattleActivated;
            _battle.PlayerAttacked += OnPlayerAttacked;
            _battle.EnemyAttacked += OnEnemyAttacked;
            _battle.Ended += OnBattleEnded;
            _battle.PlayerDefeated += OnPlayerDefeated;

            _stopwatch.Start();
            UpdateHud();
        }

        public int TotalEnemies { get; }
        public int DefeatedEnemies { get; private set; }
        public int RemainingEnemies => Math.Max(0, TotalEnemies - DefeatedEnemies);
        public int DamageDealt { get; private set; }
        public int DamageTaken { get; private set; }
        public bool MapCleared { get; private set; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        private Vector2 _playerSpawn;

        public void Update()
        {
            if (!MapCleared)
                _hud.SetElapsed(_stopwatch.Elapsed);
        }

        public void DismissClear()
        {
            if (!_hud.ClearVisible)
                return;

            _hud.HideClear();
            _player.InputEnabled = true;
        }

        public void ResetMap()
        {
            _activeEnemy = null;
            DefeatedEnemies = 0;
            DamageDealt = 0;
            DamageTaken = 0;
            MapCleared = false;
            _stopwatch.Restart();

            foreach (Enemy enemy in _enemies)
                enemy.ResetForMap();

            _player.Position = _playerSpawn;
            _player.Velocity = Vector2.Zero;
            _player.HealFull();
            _player.CancelOneShotAnimation();
            _player.Opacity = 1f;
            _player.SetBattleMode(false);
            _player.InputEnabled = true;

            _hud.HideClear();
            UpdateHud();
        }

        private void OnBattleActivated() => _activeEnemy = _battle.CurrentEnemy;

        private void OnPlayerAttacked(bool hit, int damage)
        {
            if (hit)
                DamageDealt += Math.Max(0, damage);
        }

        private void OnEnemyAttacked(int damage, int playerHp)
        {
            DamageTaken += Math.Max(0, damage);
        }

        private void OnBattleEnded()
        {
            bool victory = _activeEnemy is not null && _activeEnemy.IsDefeated;

            if (victory)
                DefeatedEnemies = Math.Min(TotalEnemies, DefeatedEnemies + 1);

            _activeEnemy = null;
            UpdateHud();

            if (victory && RemainingEnemies == 0)
                ShowClear();
        }

        private void OnPlayerDefeated() => ResetMap();

        private void ShowClear()
        {
            if (MapCleared)
                return;

            MapCleared = true;
            _stopwatch.Stop();

            _hud.ShowClear(
                _map.Name,
                MapHud.FormatDuration(_stopwatch.Elapsed),
                DamageDealt,
                DamageTaken);

            _player.InputEnabled = false;
        }

        private void UpdateHud()
        {
            _hud.SetEnemyCount(RemainingEnemies, TotalEnemies);
        }
    }
}
