﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightsOut2
{
    class GameManager
    {
        public int score;
        public bool gameOver;

        private Texture2D lavaBackground;
        private Texture2D brickBackground;
        private Texture2D overheatTexture;
        private SpriteFont spriteFont;

        public Player player;
        private HeatBar heatBar;
        private EnemyManager enemyManager;
        public static Camera camera;
        private ParticleEngine particleEngine;
        
        public GameManager()
        {
            score = 0;
            gameOver = false;

            lavaBackground = ContentManager.Get<Texture2D>("lavaBackground");
            brickBackground = ContentManager.Get<Texture2D>("brickSeamlessBackground");
            overheatTexture = ContentManager.Get<Texture2D>("overheatTex");
            spriteFont = ContentManager.Get<SpriteFont>("spriteFont");

            Viewport view = ContentManager.TransferGraphicsDevice().Viewport;
            camera = new Camera(view);
            player = new Player(new Vector2(800, 800), Constants.StandardSize);
            heatBar = new HeatBar(new Vector2(Constants.GameWindow.Width / 2 - 100, Constants.GameWindow.Height - 44), 1);
            enemyManager = new EnemyManager();
            Game1.penumbra.AmbientColor = Color.Black;
            particleEngine = new ParticleEngine();
        }

        public void Initialize()
        {
            player.extraLife = 3;
            enemyManager.enemyList.Clear();
            enemyManager.removeList.Clear();

            Game1.penumbra.Lights.Clear();
            Game1.penumbra.Hulls.Clear();
            Game1.penumbra.Lights.Add(player.viscinity);
            Game1.penumbra.Lights.Add(player.view);
            Game1.penumbra.Initialize();
            Sfx.Play.BGMStart();
        }

        public void Update(GameTime gameTime)
        {
            particleEngine.Update();
            player.Update();
            CheckMoving();
            heatBar.Update();
            enemyManager.Update(gameTime, player.position);
            CheckCollision();
            camera.SetPosition(player.position);
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            //Ritar ut objekt som skall följa med kameran/skärmen
            Game1.penumbra.BeginDraw();
            Game1.penumbra.Transform = camera.GetTransform();

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, camera.GetTransform());
                spriteBatch.Draw(lavaBackground, new Vector2(-800, -800), Color.White);
                spriteBatch.Draw(brickBackground, Vector2.Zero, Color.White);
                particleEngine.Draw(spriteBatch);
                enemyManager.Draw(spriteBatch);
                player.Draw(spriteBatch);
            spriteBatch.End();

            Game1.penumbra.Draw(gameTime);

            //Måste ritas ut separat efter Penumbra för att synas genom skuggorna (Vår GUI, HUD)
            spriteBatch.Begin();
            if (player.overheated)
                spriteBatch.Draw(overheatTexture, Vector2.Zero, Color.White);    

                heatBar.Draw(spriteBatch);

                for (int i = 0; i < player.extraLife; i++)
                {
                    spriteBatch.Draw(ContentManager.Get<Texture2D>("playerTex"), new Vector2(10 + (30 * i), 60), Color.White);
                }

                spriteBatch.DrawString(spriteFont, "Score: " + score, new Vector2(10, 100), Color.White);
            spriteBatch.End();
        }

        //----------------------------------------------------------------------------------------------------

        public void CheckCollision()
        {
            foreach (Enemy tempEnemy in enemyManager.enemyList)
            {
                foreach (Bullet tempBullet in player.bulletList)
                {
                    if (tempEnemy.GetType() == typeof(Crawler))
                    {
                        CrawlerCollision(tempEnemy, tempBullet);
                    }
                    else
                    {
                        EnemyCollision(tempEnemy, tempBullet);
                    }
                }

                if (player.screenClear != null)
                {
                    ScreenClearCollision(tempEnemy);
                }

                PlayerCollisionWithEnemy(tempEnemy);
            }

            PlayerCollisionWithBullet();
        }

        private void CrawlerCollision(Enemy tempEnemy, Bullet tempBullet)
        {
            bool dead = false;
            Crawler tempCrawler = (Crawler)tempEnemy;

            foreach (CrawlerPiece x in tempCrawler.BodyPieces)
            {
                if (x.PieceHitpoints > 0)
                {
                    if (x.hitbox.Intersects(tempBullet.hitbox))
                    {
                        x.TakeDamage();
                        dead = tempCrawler.TakeDamage();
                        particleEngine.CreateBloodSplatter(tempEnemy.position, tempBullet.direction);
                        Sfx.Play.EnemyDamage();
                        player.removeList.Add(tempBullet);
                    }
                }
            }

            if (dead)
            {
                enemyManager.removeList.Add(tempEnemy);
                particleEngine.CreateBloodSplatter(tempEnemy.position, tempBullet.direction);
                score += 100;
            }
        }

        private void EnemyCollision(Enemy tempEnemy, Bullet tempBullet)
        {
            if (tempEnemy.hitbox.Intersects(tempBullet.hitbox))
            {
                bool dead = tempEnemy.TakeDamage();
                Vector2 direction = tempEnemy.position - player.position;
                direction.Normalize();
                particleEngine.CreateBloodSplatter(tempEnemy.position, direction);
                Sfx.Play.EnemyDamage();
                player.removeList.Add(tempBullet);

                if (dead)
                {
                    enemyManager.removeList.Add(tempEnemy);
                    particleEngine.CreateBloodSplatter(tempEnemy.position, tempBullet.direction);
                    score += 100;
                }
            }
        }

        private void ScreenClearCollision(Enemy tempEnemy)
        {
            if (tempEnemy.hitbox.Intersects(player.screenClear.destinationRectangle))
            {
                enemyManager.removeList.Add(tempEnemy);
                Vector2 direction = tempEnemy.position - player.position;
                direction.Normalize();
                particleEngine.CreateBloodSplatter(tempEnemy.position, direction);
                score += 100;
            }
        }

        private void PlayerCollisionWithEnemy(Enemy tempEnemy)
        {
            if (tempEnemy.GetType() == typeof(Crawler))
            {
                Crawler tempCrawler = (Crawler)tempEnemy;

                foreach (CrawlerPiece x in tempCrawler.BodyPieces)
                {
                    if (x.hitbox.Intersects(player.hitbox) || tempEnemy.hitbox.Intersects(player.hitbox))
                    {
                        enemyManager.removeList.Add(tempEnemy);

                        if (player.extraLife >= 0)
                            player.TakeDamage();
                        else
                        {
                            gameOver = true;
                        }
                    }
                }
            }
            else if (tempEnemy.hitbox.Intersects(player.hitbox))
            {
                enemyManager.removeList.Add(tempEnemy);

                if (player.extraLife > 0)
                    player.TakeDamage();
                else
                {
                    gameOver = true;
                }
            }
        }

        private void PlayerCollisionWithBullet()
        {
            foreach (Shooter tempShooter in enemyManager.enemyList.OfType<Shooter>())
            {
                foreach (Bullet tempEnemyBullet in tempShooter.enemyBulletList)
                {
                    if (tempEnemyBullet.hitbox.Intersects(player.hitbox))
                    {
                        tempShooter.enemyRemoveList.Add(tempEnemyBullet);

                        if (player.extraLife > 0)
                            player.TakeDamage();
                        else
                        {
                            gameOver = true;
                        }
                    }

                    if (player.screenClear != null)
                    {
                        if (tempEnemyBullet.hitbox.Intersects(player.screenClear.destinationRectangle))
                        {
                            tempShooter.enemyRemoveList.Add(tempEnemyBullet);
                        }
                    }
                }
            }
        }

        private void CheckMoving()
        {
            if (player.moving)
                particleEngine.CreateRunParticle(player.position);
        }
    }
}
