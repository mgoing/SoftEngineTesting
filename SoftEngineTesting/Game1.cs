using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace SoftEngineTesting
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private BasicEffect _basicEffect;

        // Map and player setup
        private int[,] map = new int[,]
        {
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            { 1, 0, 0, 0, 1, 0, 0, 0, 0, 1 },
            { 1, 0, 1, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 1, 1, 0, 0, 0, 1 },
            { 1, 1, 0, 1, 1, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 1, 1, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            { 1, 0, 0, 1, 1, 1, 0, 0, 0, 1 },
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        private Vector2 playerPos = new Vector2(2.5f, 2.5f);
        private Vector2 playerDir = new Vector2(1, 0);
        private Vector2 plane = new Vector2(0, 0.66f);
        private float moveSpeed = 0.1f;
        private float rotSpeed = 0.05f;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            var state = Keyboard.GetState();

            // Handle rotation
            if (state.IsKeyDown(Keys.A)) RotatePlayer(-rotSpeed);
            if (state.IsKeyDown(Keys.D)) RotatePlayer(rotSpeed);

            // Handle movement
            Vector2 newPlayerPos = playerPos;

            if (state.IsKeyDown(Keys.W))
            {
                newPlayerPos.X += playerDir.X * moveSpeed;
                newPlayerPos.Y += playerDir.Y * moveSpeed;
            }
            if (state.IsKeyDown(Keys.S))
            {
                newPlayerPos.X -= playerDir.X * moveSpeed;
                newPlayerPos.Y -= playerDir.Y * moveSpeed;
            }

            // Check for collisions
            int mapX = (int)newPlayerPos.X;
            int mapY = (int)newPlayerPos.Y;

            if (mapX >= 0 && mapX < map.GetLength(1) && mapY >= 0 && mapY < map.GetLength(0) && map[mapX, mapY] == 0)
            {
                playerPos = newPlayerPos; // Update position only if no wall is in the way
            }

            System.Diagnostics.Debug.WriteLine($"PlayerPos: {playerPos}, PlayerDir: {playerDir}");

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Floor and ceiling colors
            _spriteBatch.Begin();
            Texture2D pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            for (int y = 0; y < _graphics.PreferredBackBufferHeight; y++)
            {
                Color color = (y < _graphics.PreferredBackBufferHeight / 2) ? Color.DarkSlateGray : Color.Gray;
                _spriteBatch.Draw(pixel, new Rectangle(0, y, _graphics.PreferredBackBufferWidth, 1), color);
            }

            // Draw walls
            for (int x = 0; x < _graphics.PreferredBackBufferWidth; x++)
            {
                float cameraX = 2 * x / (float)_graphics.PreferredBackBufferWidth - 1;
                Vector2 rayDir = playerDir + plane * cameraX;

                int mapX = (int)playerPos.X;
                int mapY = (int)playerPos.Y;

                Vector2 sideDist;
                Vector2 deltaDist = new Vector2(
                    rayDir.X == 0 ? float.MaxValue : Math.Abs(1 / rayDir.X),
                    rayDir.Y == 0 ? float.MaxValue : Math.Abs(1 / rayDir.Y)
                );
                Point step = new Point();

                if (rayDir.X < 0)
                {
                    step.X = -1;
                    sideDist.X = (playerPos.X - mapX) * deltaDist.X;
                }
                else
                {
                    step.X = 1;
                    sideDist.X = (mapX + 1.0f - playerPos.X) * deltaDist.X;
                }

                if (rayDir.Y < 0)
                {
                    step.Y = -1;
                    sideDist.Y = (playerPos.Y - mapY) * deltaDist.Y;
                }
                else
                {
                    step.Y = 1;
                    sideDist.Y = (mapY + 1.0f - playerPos.Y) * deltaDist.Y;
                }

                bool hit = false;
                int side = 0;

                while (!hit)
                {
                    if (sideDist.X < sideDist.Y)
                    {
                        sideDist.X += deltaDist.X;
                        mapX += step.X;
                        side = 0;
                    }
                    else
                    {
                        sideDist.Y += deltaDist.Y;
                        mapY += step.Y;
                        side = 1;
                    }

                    if (map[mapX, mapY] > 0) hit = true;
                }

                float perpWallDist = (side == 0)
                    ? (mapX - playerPos.X + (1 - step.X) / 2) / rayDir.X
                    : (mapY - playerPos.Y + (1 - step.Y) / 2) / rayDir.Y;

                int lineHeight = (int)(_graphics.PreferredBackBufferHeight / perpWallDist);
                int drawStart = -lineHeight / 2 + _graphics.PreferredBackBufferHeight / 2;
                int drawEnd = lineHeight / 2 + _graphics.PreferredBackBufferHeight / 2;

                Color wallColor = (side == 0) ? Color.DarkRed : Color.Red;
                _spriteBatch.Draw(pixel, new Rectangle(x, drawStart, 1, drawEnd - drawStart), wallColor);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void RotatePlayer(float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            // Rotate direction vector
            float oldDirX = playerDir.X;
            playerDir.X = playerDir.X * cos - playerDir.Y * sin;
            playerDir.Y = oldDirX * sin + playerDir.Y * cos;

            // Rotate camera plane
            float oldPlaneX = plane.X;
            plane.X = plane.X * cos - plane.Y * sin;
            plane.Y = oldPlaneX * sin + plane.Y * cos;
        }
    }
}
