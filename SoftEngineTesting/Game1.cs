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
        private Texture2D _pixelTexture; //1x1 pixel texture


        //constants for more control

        private const float MOVE_SPEED = 3.0f; // Units per second
        private const float ROTATE_SPEED = 2.0f; // Radians per second
        private const float FOV_PLANE = 0.66f;
        private const int SCREEN_WIDTH = 800;
        private const int SCREEN_HEIGHT = 600;



        // Map and player setup
        private int[,] map = new int[,]
        {
        { 1, 1, 1, 1, 1, 1, 1, 0, 1, 1 },
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

        private int mapWidth = 10;  // Number of columns
        private int mapHeight = 10; // Number of rows

        private Vector2 playerPos = new Vector2(1.5f, 1.5f);
        private Vector2 playerDir = new Vector2(1, 0);
        private Vector2 plane = new Vector2(0, FOV_PLANE);
        private float moveSpeed = 0.1f;
        private float rotSpeed = 0.05f;

        private bool isColliding = false;
        private float collisionFlashDuration = 0.5f; // Half a second
        private float collisionFlashTimer = 0.0f;
        private Vector2 collisionPoint; // Location of the collision
        private float collisionTimer = 0.0f; // Timer for displaying collision feedback
        private const float collisionDisplayDuration = 0.5f; // Duration to show squares in seconds
       

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            // Dynamically calculate map dimensions
            mapWidth = map.GetLength(1); // Columns (Width)
            mapHeight = map.GetLength(0); // Rows (Height)
            
            _graphics.PreferredBackBufferWidth = SCREEN_WIDTH;
            _graphics.PreferredBackBufferHeight = SCREEN_HEIGHT;
            _graphics.ApplyChanges();

            playerDir.Normalize();



            base.Initialize();

           
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
        }

        protected override void Update(GameTime gameTime)
        {
            var state = Keyboard.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (state.IsKeyDown(Keys.A))
            {
                float angle = -ROTATE_SPEED * deltaTime;
                playerDir = Rotate(playerDir, angle);
                plane = Rotate(plane, angle);


            }
            if (state.IsKeyDown(Keys.D))
            {
                float angle = ROTATE_SPEED * deltaTime;
                playerDir = Rotate(playerDir, angle);
                plane = Rotate(plane, angle);


            }


            Vector2 newPlayerPos = playerPos;


            float moveDistance = MOVE_SPEED * deltaTime;

            if (state.IsKeyDown(Keys.W))
            {
                newPlayerPos += playerDir * moveDistance;
            }
            if (state.IsKeyDown(Keys.S))
            {
                newPlayerPos -= playerDir * moveDistance;
            }

            if (IsValidPosition(newPlayerPos))
            {
                playerPos = newPlayerPos;

            }
            else
            {
                isColliding = true;
                collisionPoint = new Vector2((int)newPlayerPos.X, (int)newPlayerPos.Y);
                collisionTimer = collisionDisplayDuration;
            }

            // Update collision timer
            if (isColliding)
            {
                collisionTimer -= deltaTime;
                if (collisionTimer <= 0)
                {
                    isColliding = false; // Stop showing collision feedback
                }
            }

            base.Update(gameTime);
        }

        private bool IsValidPosition(Vector2 pos)
        {
            int mapX = (int)pos.X;
            int mapY = (int)pos.Y;

            // Boundary check first
            if (mapX < 0 || mapX >= mapWidth || mapY < 0 || mapY >= mapHeight)
            {
                return false;
            }

            // Wall check
            return map[mapY, mapX] == 0; // Note: correct indexing [row, col] = [Y, X]
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            // Create a reusable 1x1 pixel texture
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT / 2),
                Color.DarkSlateGray
            );

            //floor
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, SCREEN_HEIGHT / 2, SCREEN_WIDTH, SCREEN_HEIGHT / 2),
                Color.Gray
            );

            // Raycasting for walls
            RenderWalls();

            // Draw collision feedback
            if (isColliding)
            {
                DrawCollisionFeedback();
            }

            _spriteBatch.End();

            


            base.Draw(gameTime);
        }


        private void RenderWalls()
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                // Calculate ray direction
                float cameraX = 2 * x / (float)SCREEN_WIDTH - 1;
                Vector2 rayDir = playerDir + plane * cameraX;

                // DDA setup
                int mapX = (int)playerPos.X;
                int mapY = (int)playerPos.Y;

                Vector2 deltaDist = new Vector2(
                    rayDir.X == 0 ? float.MaxValue : Math.Abs(1 / rayDir.X),
                    rayDir.Y == 0 ? float.MaxValue : Math.Abs(1 / rayDir.Y)
                );

                Point step;
                Vector2 sideDist;

                // Calculate step and initial sideDist
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

                // Perform DDA
                bool hit = false;
                int side = 0;
                int maxSteps = Math.Max(mapWidth, mapHeight) * 2; // Safety limit
                int steps = 0;

                while (!hit && steps < maxSteps)
                {
                    // Step to next grid line
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

                    // CRITICAL: Check bounds BEFORE accessing array
                    if (mapX < 0 || mapX >= mapWidth || mapY < 0 || mapY >= mapHeight)
                    {
                        hit = true; // Hit boundary
                        break;
                    }

                    // Check if ray hit a wall
                    if (map[mapY, mapX] > 0)
                    {
                        hit = true;
                    }

                    steps++;
                }

                // Calculate distance and render wall slice
                if (hit)
                {
                    float perpWallDist;
                    if (side == 0)
                        perpWallDist = (mapX - playerPos.X + (1 - step.X) / 2) / rayDir.X;
                    else
                        perpWallDist = (mapY - playerPos.Y + (1 - step.Y) / 2) / rayDir.Y;

                    // Prevent division by zero or extremely close walls
                    perpWallDist = Math.Max(perpWallDist, 0.1f);

                    int lineHeight = (int)(SCREEN_HEIGHT / perpWallDist);

                    int drawStart = Math.Max(0, -lineHeight / 2 + SCREEN_HEIGHT / 2);
                    int drawEnd = Math.Min(SCREEN_HEIGHT - 1, lineHeight / 2 + SCREEN_HEIGHT / 2);

                    // Different colors for X and Y sides for depth perception
                    Color color = side == 1 ? Color.Red : Color.DarkRed;

                    _spriteBatch.Draw(
                        _pixelTexture,
                        new Rectangle(x, drawStart, 1, drawEnd - drawStart),
                        color
                    );
                }
            }
        }

        //visual wall collision feedback
        private void DrawCollisionFeedback()
        {
            // Draw a pulsing indicator at the top of the screen
            float alpha = collisionTimer / collisionDisplayDuration;
            Color flashColor = Color.Yellow * alpha;

            int barHeight = 10;
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, 0, SCREEN_WIDTH, barHeight),
                flashColor
            );

            // Optional: Draw collision text
            // If you have a SpriteFont loaded, you could display "COLLISION!" here
        }


        private Vector2 Rotate(Vector2 v, float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pixelTexture?.Dispose();
            }
            base.Dispose(disposing);
        }



    }
}
