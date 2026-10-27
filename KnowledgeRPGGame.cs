using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using KnowledgeRPG.Battle;
using KnowledgeRPG.Core;
using KnowledgeRPG.Entities;
using KnowledgeRPG.Maps;
using KnowledgeRPG.Quiz;
using KnowledgeRPG.Scenes;
using KnowledgeRPG.UI;

namespace KnowledgeRPG
{
    public sealed class KnowledgeRPGGame : Game
    {
        private const int WindowWidth = 1280;
        private const int WindowHeight = 720;
        private const int WorldScale = 2;
        private const int ViewWidth = WindowWidth / WorldScale;
        private const int ViewHeight = WindowHeight / WorldScale;

        private readonly GraphicsDeviceManager _graphics;
        private readonly InputState _input = new();

        private SpriteBatch _spriteBatch;
        private UiPainter _ui;
        private Camera2D _camera;

        private ForestMap _map;
        private Player _player;
        private List<Enemy> _enemies;
        private BattleHud _battleHud;
        private QuizPanel _quizPanel;
        private BattleController _battle;
        private MapHud _mapHud;
        private MapSession _session;

        private MouseState _previousMouse;
        private double _clock;
        private string _screenshotPath;
        private bool _screenshotTaken;

        public KnowledgeRPGGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = WindowWidth,
                PreferredBackBufferHeight = WindowHeight,
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);

            Window.Title = "KnowledgeRPG";
        }

        protected override void LoadContent()
        {
            _screenshotPath = Environment.GetEnvironmentVariable("KRPG_SCREENSHOT");
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Texture2D playerTexture = Content.Load<Texture2D>("characters/player");
            Texture2D slimeTexture = Content.Load<Texture2D>("characters/slime");
            Texture2D plains = Content.Load<Texture2D>("tiles/plains");
            Texture2D objects = Content.Load<Texture2D>("tiles/objects");

            _ui = new UiPainter(
                GraphicsDevice,
                Content.Load<SpriteFont>("Fonts/Small"),
                Content.Load<SpriteFont>("Fonts/Body"),
                Content.Load<SpriteFont>("Fonts/Title"),
                Content.Load<SpriteFont>("Fonts/Huge"));

            _map = new ForestMap(plains, objects);
            _camera = new Camera2D(ViewWidth, ViewHeight, _map.Width, _map.Height);

            var playerSprite = new SpriteAnimator(playerTexture, 48, 48);
            _player = new Player(playerSprite) { Position = new Vector2(104f, 112f) };

            _enemies = new List<Enemy>
            {
                CreateSlime(slimeTexture, new Vector2(222f, 129f)),
                CreateSlime(slimeTexture, new Vector2(82f, 365f)),
                CreateSlime(slimeTexture, new Vector2(296f, 318f)),
            };

            _battleHud = new BattleHud();
            _quizPanel = new QuizPanel();
            _battle = new BattleController(_player, _map, _battleHud, _quizPanel);

            foreach (Enemy enemy in _enemies)
            {
                Enemy captured = enemy;
                captured.Engaged += () => TryBeginEncounter(captured);
            }

            _mapHud = new MapHud();
            _session = new MapSession(_map, _player, _enemies, _battle, _mapHud);

            _previousMouse = Mouse.GetState();
        }

        private static Enemy CreateSlime(Texture2D texture, Vector2 position)
        {
            var slime = new Slime(new SpriteAnimator(texture, 32, 32));
            slime.SetSpawn(position);
            return slime;
        }

        private void TryBeginEncounter(Enemy enemy)
        {
            if (_battle.State != BattleState.Inactive || _session.MapCleared || _mapHud.ClearVisible)
                return;

            _battle.BeginEncounter(enemy);
        }

        protected override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _clock += delta;

            _input.Update();

            MouseState mouse = Mouse.GetState();
            bool mouseClicked = mouse.LeftButton == ButtonState.Pressed &&
                                _previousMouse.LeftButton == ButtonState.Released;

            if (_input.WasPressed(Keys.Escape))
            {
                if (_mapHud.ClearVisible)
                    _session.DismissClear();
                else if (!_quizPanel.IsOpen)
                {
                    Exit();
                    return;
                }
            }

            if (_quizPanel.IsOpen)
                _quizPanel.Update(_input, mouse, mouseClicked, _clock);

            _battle.Update(delta, _input, _clock, mouseClicked);

            if (_mapHud.ClearVisible && (_input.ConfirmPressed || mouseClicked))
                _session.DismissClear();

            _player.Update(delta);

            if (_battle.State == BattleState.Inactive && !_quizPanel.IsOpen && !_mapHud.ClearVisible)
                _player.UpdateMovement(delta, _input, _map.IsSolid);
            else if (_battle.State == BattleState.Inactive)
                _player.Velocity = Vector2.Zero;

            foreach (Enemy enemy in _enemies)
            {
                if (enemy.IsRemovedFromMap)
                    continue;

                enemy.UpdateHostile(delta, _player, _map.IsSolid);
            }

            _camera.Follow(_player.BodyCenter);
            _session.Update();

            _previousMouse = mouse;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(12, 18, 14));

            Matrix world = _camera.GetViewMatrix(WorldScale);

            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: world);

            _map.Draw(_spriteBatch);

            foreach (Enemy enemy in _enemies.OrderBy(e => e.Position.Y))
            {
                if (!enemy.IsRemovedFromMap)
                    enemy.Draw(_spriteBatch);
            }

            _player.Draw(_spriteBatch);
            _battleHud.DrawWorld(_spriteBatch, _ui);

            _spriteBatch.End();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _battleHud.Draw(_spriteBatch, _ui, GraphicsDevice.Viewport);
            _mapHud.Draw(_spriteBatch, _ui, GraphicsDevice.Viewport);

            if (_battle.FlashAlpha > 0f)
            {
                _ui.FillRect(
                    _spriteBatch,
                    new Rectangle(0, 0, WindowWidth, WindowHeight),
                    Color.White * _battle.FlashAlpha);
            }

            _quizPanel.Draw(_spriteBatch, _ui, GraphicsDevice.Viewport);

            if (_battle.State == BattleState.Inactive && !_mapHud.ClearVisible && !_quizPanel.IsOpen)
            {
                _ui.DrawTextShadow(
                    _spriteBatch,
                    _ui.Small,
                    "WASD / Arrow keys to move",
                    new Vector2(22, WindowHeight - 30),
                    new Color(230, 226, 255) * 0.75f,
                    Color.Black * 0.75f,
                    1);
            }

            _spriteBatch.End();

            if (_screenshotPath is not null && !_screenshotTaken && _clock > 1.0)
            {
                _screenshotTaken = true;
                CaptureScreenshot(_screenshotPath);
                Exit();
            }

            base.Draw(gameTime);
        }

        private void CaptureScreenshot(string path)
        {
            var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            var data = new Color[width * height];
            GraphicsDevice.GetBackBufferData(data);

            using var texture = new Texture2D(GraphicsDevice, width, height);
            texture.SetData(data);

            using var stream = System.IO.File.Create(path);
            texture.SaveAsPng(stream, width, height);
        }
    }
}
