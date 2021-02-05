﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using GameProject.Actors;
using GameProject.Engine;
using GameProject.Engine.Components;
using System;

namespace GameProject
{
    public class Game : Microsoft.Xna.Framework.Game
    {
        public static Game Instance { get; private set; }

        public SpriteBatch SpriteBatch { get; private set; }
        public int DisplayWidth => _graphics.PreferredBackBufferWidth;
        public int DisplayHeight => _graphics.PreferredBackBufferHeight;
        public SpriteFont DebugFont { get; private set; }
        public Texture2D DebugPixel { get; private set; }

        private GraphicsDeviceManager _graphics;
        private List<Actor> actors;
        private List<IDestroyable> toDestroy;
        private List<RectangleCollider> colliders;
        private bool contentLoaded = false;
        private float timeSinceFixedUpdate = 0;

        public Game()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.IsFullScreen = true;
            //Window.IsBorderless = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Instance = this;
        }

        protected override void Initialize()
        {
            actors = new List<Actor>();
            toDestroy = new List<IDestroyable>();
            colliders = new List<RectangleCollider>();

            SpawnBlockersAroundScreen();
            Instantiate(new Player());
            SpawnEnemy();
            Instantiate(new Scoreboard(new Vector2(20, 20)));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            DebugFont = Content.Load<SpriteFont>("debug");
            DebugPixel = Content.Load<Texture2D>("1x1WhitePixel");

            foreach (Actor actor in actors)
                actor.LoadContent(Content);

            contentLoaded = true;
        }

        protected override void Update(GameTime gameTime)
        {
            Time.UpdateTime(gameTime);
            InputManager.Update();

            if (InputManager.Exit)
                Exit();

            timeSinceFixedUpdate += Time.deltaTime;
            while (timeSinceFixedUpdate > Time.fixedDeltaTime)
            {
                timeSinceFixedUpdate -= Time.fixedDeltaTime;
                for (int i = 0; i < actors.Count; i++)
                {
                    Actor actor = actors[i];
                    if (!actor.IsDestroyed)
                        actor.EngineFixedUpdate();
                }
            }

            CollisionHelper.CheckCollision(colliders);

            for (int i = 0; i < actors.Count; i++)
            {
                Actor actor = actors[i];
                if (!actor.IsDestroyed)
                    actor.EngineUpdate();
            }

            while (toDestroy.Count > 0)
            {
                IDestroyable d = toDestroy[0];

                d.FinalDestroy();

                toDestroy.RemoveAt(0);
                if (d is Actor a)
                {
                    actors.Remove(a);
                } 
                else if (d is RectangleCollider rc)
                {
                    colliders.Remove(rc);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Green);

            SpriteBatch.Begin();
            foreach (Actor actor in actors)
            {
                if (!actor.IsDestroyed)
                    actor.Draw();
            }
            SpriteBatch.End();

            base.Draw(gameTime);
        }

        public Actor Instantiate(Actor a)
        {
            actors.Add(a);

            a.Start();

            if (contentLoaded)
                a.LoadContent(Content);

            return a;
        }

        public void Destroy(IDestroyable d)
        {
            toDestroy.Add(d);

            if (!d.IsDestroyed)
                d.Destroy();
        }

        public void AddCollider(RectangleCollider collider)
        {
            colliders.Add(collider);
        }

        public void SpawnEnemy()
        {
            Vector2 playerPos;
            if (actors.Find(a => a is Player) is Player player)
                playerPos = player.Position;
            else
                playerPos = Vector2.Zero;

            Random r = new Random();
            Vector2 enemyPos;
            do
            {
                // Prevent them from spawning on the border 5% of the screen
                enemyPos.X = ((float)r.NextDouble() * 0.9f + 0.05f) * DisplayWidth;
                enemyPos.Y = ((float)r.NextDouble() * 0.9f + 0.05f) * DisplayHeight;
                enemyPos = enemyPos.ScreenToWorldspace();
            } while (Vector2.Distance(playerPos, enemyPos) < 200);

            Instantiate(new Enemy(enemyPos));
        }

        public T GetActor<T>() where T : Actor
        {
            return actors.Find(a => a is T) as T;
        }

        public void SpawnBlockersAroundScreen()
        {
            Instantiate(new PlayerBlocker(new Vector2(0, DisplayHeight / 2 + 501), Direction.Top));
            Instantiate(new PlayerBlocker(new Vector2(0, -DisplayHeight / 2 - 501), Direction.Bottom));
            Instantiate(new PlayerBlocker(new Vector2(-DisplayWidth / 2 - 501, 0), Direction.Left));
            Instantiate(new PlayerBlocker(new Vector2(DisplayWidth / 2 + 501, 0), Direction.Right));
        }
    }
}
