﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace LightsOut2
{
    public class ParticleEngine
    {
        public Vector2 EmitterLocation { get; set; }
        private List<Particle> particles;
        private Texture2D texture;

        public ParticleEngine()
        {
            texture = ContentManager.Get<Texture2D>("particle");

            particles = new List<Particle>();
        }

        public void Update(bool playerSprint, Vector2 playerPosition)
        {
            PlayerUpdate(playerSprint, playerPosition);
            EnemyUpdate();
            
            for (int particle = 0; particle < particles.Count; particle++)
            {
                particles[particle].Update();
                if (particles[particle].LifeSpan <= 0)
                {
                    particles.RemoveAt(particle);
                    particle--;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int index = 0; index < particles.Count; index++)
            {
                particles[index].Draw(spriteBatch);
            }
        }

        //----------------------------------------------------------------------------------------------------

        private void PlayerUpdate(bool playerSprint, Vector2 playerPosition)
        {
            EmitterLocation = playerPosition;
            if (playerSprint)
            {
                particles.Add(GenerateNewParticle("sprintEffect"));
            }
        }

        private void EnemyUpdate()
        {
            /*
            EmitterLocation = new Vector2(enemyposition);
            if (enemy.Dead)
            {
                particles.Add(GenerateNewParticle(type));
            }
            */
        }

        private Particle GenerateNewParticle(string type)
        {
            Texture2D tempTex = texture;
            Vector2 position = EmitterLocation;
            int ttl = 10 + Constants.Randomizer.Next(10);
            float shade = 0f;
            Color color = new Color();
            float size = 0f;

            switch (type)
            {
                case "sprintEffect":
                    shade = (float)Constants.Randomizer.NextDouble();
                    color = new Color(shade, shade, shade);
                    size = 5 + 5 * (float)Constants.Randomizer.NextDouble();
                    break;
            }

            return new Particle(tempTex, position, color, size, ttl);
        }
    }
}
