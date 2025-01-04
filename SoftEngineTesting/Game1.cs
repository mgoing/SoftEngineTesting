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
        private VertexPositionColor[] _vertices;



        // Map and player setup
        private int[,] map = new int[,]
        {
        { 1, 1, 1, 1, 1 },
        { 1, 0, 0, 0, 1 },
        { 1, 0, 1, 0, 1 },
        { 1, 0, 0, 0, 1 },
        { 1, 1, 1, 1, 1 }
        };

        private Vector2 playerPos = new Vector2(2.5f, 2.5f);
        private Vector2 playerDir = new Vector2(1, 0);
        private Vector2 plane = new Vector2(0, 0.66f);
        private float moveSpeed = 0.1f;
        private float rotSpeed = 0.05f;
        private bool isColliding = false;
        private float collisionFlashDuration = 0.5f; // Half a second
        private float collisionFlashTimer = 0.0f;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();

            base.Initialize();

           
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            var state = Keyboard.GetState();

            if (state.IsKeyDown(Keys.W)) playerPos += playerDir * moveSpeed;
            if (state.IsKeyDown(Keys.S)) playerPos -= playerDir * moveSpeed;
            if (state.IsKeyDown(Keys.A)) playerDir = Rotate(playerDir, -rotSpeed);
            if (state.IsKeyDown(Keys.D)) playerDir = Rotate(playerDir, rotSpeed);

            Vector2 newPlayerPos = playerPos;

            // Update position based on input
            if (state.IsKeyDown(Keys.W)) newPlayerPos += playerDir * moveSpeed;
            if (state.IsKeyDown(Keys.S)) newPlayerPos -= playerDir * moveSpeed;

            // Check collision with walls
            int newMapX = (int)newPlayerPos.X;
            int newMapY = (int)newPlayerPos.Y;

            if (map[newMapX, newMapY] == 0) // Not a wall, move is valid
            {
                playerPos = newPlayerPos;
                isColliding = false;
            }
            else // Collision detected
            {
                isColliding = true;
                collisionFlashTimer = collisionFlashDuration; // Reset flash timer
            }

            // Handle rotation
            if (state.IsKeyDown(Keys.A)) playerDir = Rotate(playerDir, -rotSpeed);
            if (state.IsKeyDown(Keys.D)) playerDir = Rotate(playerDir, rotSpeed);

            // Update collision flash timer
            if (collisionFlashTimer > 0)
            {
                collisionFlashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (collisionFlashTimer <= 0)
                {
                    isColliding = false; // Stop flashing
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Create a reusable 1x1 pixel texture
            Texture2D pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            _spriteBatch.Begin();

            // Draw the collision feedback
            if (isColliding)
            {
                int squareSize = 20;
                int numSquares = 10;
                int spacing = 5;

                for (int i = 0; i < numSquares; i++)
                {
                    int x = spacing + (squareSize + spacing) * i;
                    int y = spacing;

                    _spriteBatch.Draw(pixel, new Rectangle(x, y, squareSize, squareSize), Color.White);
                }
            }

            // Draw walls using raycasting (existing logic)
            for (int x = 0; x < _graphics.PreferredBackBufferWidth; x++)
            {
                float cameraX = 2 * x / (float)_graphics.PreferredBackBufferWidth - 1; // [-1, 1]
                Vector2 rayDir = playerDir + plane * cameraX;

                int mapX = (int)playerPos.X;
                int mapY = (int)playerPos.Y;

                Vector2 sideDist;

                Vector2 deltaDist = new Vector2(
                    (rayDir.X == 0) ? 1e30f : Math.Abs(1 / rayDir.X),
                    (rayDir.Y == 0) ? 1e30f : Math.Abs(1 / rayDir.Y)
                );
                float perpWallDist;

                Point step = new Point();
                bool hit = false;
                int side = 0;

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

                if (side == 0)
                    perpWallDist = (mapX - playerPos.X + (1 - step.X) / 2) / rayDir.X;
                else
                    perpWallDist = (mapY - playerPos.Y + (1 - step.Y) / 2) / rayDir.Y;

                int lineHeight = (int)(_graphics.PreferredBackBufferHeight / perpWallDist);

                int drawStart = -lineHeight / 2 + _graphics.PreferredBackBufferHeight / 2;
                if (drawStart < 0) drawStart = 0;
                int drawEnd = lineHeight / 2 + _graphics.PreferredBackBufferHeight / 2;
                if (drawEnd >= _graphics.PreferredBackBufferHeight) drawEnd = _graphics.PreferredBackBufferHeight - 1;

                Color color = (side == 1) ? Color.Red : Color.DarkRed;

                _spriteBatch.Draw(pixel, new Rectangle(x, drawStart, 1, drawEnd - drawStart), color);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }



        private Vector2 Rotate(Vector2 v, float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }
    }
}
