using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace SoftEngineTesting
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private BasicEffect _basicEffect;
        private VertexPositionColor[] _vertices;
        private Texture2D _pixelTexture; //1x1 pixel texture

        private Texture2D[] _wallTextures;
        private Color[][] _wallTextureData; // Cache texture data
        private const int TEXTURE_SIZE = 64;

        //constants for more control

        private const float MOVE_SPEED = 3.0f; // Units per second
        private const float ROTATE_SPEED = 2.0f; // Radians per second
        private const float FOV_PLANE = 0.66f;
        private const int SCREEN_WIDTH = 800;
        private const int SCREEN_HEIGHT = 600;



        // Map and player setup
        private int[,] map;

        private int mapWidth = 40;  // Number of columns
        private int mapHeight = 40; // Number of rows

        private Vector2 playerPos = new Vector2(1.5f, 1.5f);
        private Vector2 playerDir = new Vector2(1, 0);
        private Vector2 plane = new Vector2(0, FOV_PLANE);

        

        private bool isColliding = false;
       
        private Vector2 collisionPoint; // Location of the collision
        private float collisionTimer = 0.0f; // Timer for displaying collision feedback
        private const float collisionDisplayDuration = 0.5f; // Duration to show squares in seconds

        private const int MINIMAP_SIZE = 150;
        private const int MINIMAP_MARGIN = 10;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            
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

            // Create textures 
            CreateWallTextures();

            // Then generate map 
            GenerateMap();

        }












        private void GenerateMap()
        {
            Random rand = new Random();
            map = new int[mapHeight, mapWidth];

            // Initialize all as walls
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    map[y, x] = 1;
                }
            }

            // Create solid border
            for (int y = 0; y < mapHeight; y++)
            {
                map[y, 0] = 1;
                map[y, mapWidth - 1] = 1;
            }
            for (int x = 0; x < mapWidth; x++)
            {
                map[0, x] = 1;
                map[mapHeight - 1, x] = 1;
            }

            // Generate rooms
            List<Rectangle> rooms = new List<Rectangle>();
            int attempts = 0;
            int maxAttempts = 50; //  Prevent infinite loops

            while (rooms.Count < 8 && attempts < maxAttempts)
            {
                attempts++;

                int roomWidth = rand.Next(4, 8);
                int roomHeight = rand.Next(4, 8);
                int roomX = rand.Next(2, mapWidth - roomWidth - 2);
                int roomY = rand.Next(2, mapHeight - roomHeight - 2);

                Rectangle newRoom = new Rectangle(roomX, roomY, roomWidth, roomHeight);

                // Check overlaps with padding
                bool overlaps = false;
                foreach (var room in rooms)
                {
                    Rectangle expandedRoom = new Rectangle(
                        room.X - 2, room.Y - 2,
                        room.Width + 4, room.Height + 4
                    );
                    if (newRoom.Intersects(expandedRoom))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    // Carve out the room
                    for (int y = roomY; y < roomY + roomHeight; y++)
                    {
                        for (int x = roomX; x < roomX + roomWidth; x++)
                        {
                            // FIXED: Bounds check
                            if (x > 0 && x < mapWidth - 1 && y > 0 && y < mapHeight - 1)
                            {
                                map[y, x] = 0;
                            }
                        }
                    }

                    // Connect to previous room
                    if (rooms.Count > 0)
                    {
                        Rectangle prevRoom = rooms[rooms.Count - 1];
                        int prevCenterX = prevRoom.X + prevRoom.Width / 2;
                        int prevCenterY = prevRoom.Y + prevRoom.Height / 2;
                        int newCenterX = newRoom.X + newRoom.Width / 2;
                        int newCenterY = newRoom.Y + newRoom.Height / 2;

                        if (rand.Next(0, 2) == 0)
                        {
                            CreateHorizontalCorridor(prevCenterX, newCenterX, prevCenterY);
                            CreateVerticalCorridor(prevCenterY, newCenterY, newCenterX);
                        }
                        else
                        {
                            CreateVerticalCorridor(prevCenterY, newCenterY, prevCenterX);
                            CreateHorizontalCorridor(prevCenterX, newCenterX, newCenterY);
                        }
                    }

                    rooms.Add(newRoom);
                }
            }

            // Ensure valid spawn point
            if (rooms.Count > 0)
            {
                Rectangle firstRoom = rooms[0];
                playerPos = new Vector2(
                    firstRoom.X + firstRoom.Width / 2.0f,
                    firstRoom.Y + firstRoom.Height / 2.0f
                );
            }
            else
            {
                // create a safe starting area
                for (int y = 1; y < 4; y++)
                {
                    for (int x = 1; x < 4; x++)
                    {
                        map[y, x] = 0;
                    }
                }
                playerPos = new Vector2(2.5f, 2.5f);
            }

            // Add wall variety AFTER rooms are created
            for (int y = 1; y < mapHeight - 1; y++)
            {
                for (int x = 1; x < mapWidth - 1; x++)
                {
                    if (map[y, x] > 0 && rand.Next(0, 100) < 30)
                    {
                        map[y, x] = rand.Next(1, 4); // Wall types 1, 2, or 3
                    }
                }
            }
        }

        private void CreateHorizontalCorridor(int x1, int x2, int y)
        {
            int startX = Math.Min(x1, x2);
            int endX = Math.Max(x1, x2);

            for (int x = startX; x <= endX; x++)
            {
                if (x > 0 && x < mapWidth - 1 && y > 0 && y < mapHeight - 1)
                {
                    map[y, x] = 0;
                }
            }
        }

        private void CreateVerticalCorridor(int y1, int y2, int x)
        {
            int startY = Math.Min(y1, y2);
            int endY = Math.Max(y1, y2);

            for (int y = startY; y <= endY; y++)
            {
                if (x > 0 && x < mapWidth - 1 && y > 0 && y < mapHeight - 1)
                {
                    map[y, x] = 0;
                }
            }
        }
       
        
        private void CreateWallTextures()
        {
            _wallTextures = new Texture2D[3];
            _wallTextureData = new Color[3][]; // Cache the data!

            // Texture 1: Red Brick
            _wallTextures[0] = new Texture2D(GraphicsDevice, TEXTURE_SIZE, TEXTURE_SIZE);
            _wallTextureData[0] = new Color[TEXTURE_SIZE * TEXTURE_SIZE];

            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    int index = y * TEXTURE_SIZE + x;
                    bool isHorizontalMortar = (y % 16) == 0;
                    bool isVerticalMortar = ((x + (y / 16) * 8) % 16) == 0;

                    if (isHorizontalMortar || isVerticalMortar)
                    {
                        _wallTextureData[0][index] = new Color(80, 80, 80);
                    }
                    else
                    {
                        int variation = (x + y) % 20 - 10;
                        _wallTextureData[0][index] = new Color(150 + variation, 50, 50);
                    }
                }
            }
            _wallTextures[0].SetData(_wallTextureData[0]);

            // Texture 2: Blue Stone
            _wallTextures[1] = new Texture2D(GraphicsDevice, TEXTURE_SIZE, TEXTURE_SIZE);
            _wallTextureData[1] = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Random rand = new Random(42);

            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    int index = y * TEXTURE_SIZE + x;
                    int noise = rand.Next(-20, 20);
                    _wallTextureData[1][index] = new Color(50 + noise, 50 + noise, 120 + noise);
                }
            }
            _wallTextures[1].SetData(_wallTextureData[1]);

            // Texture 3: Green Metal
            _wallTextures[2] = new Texture2D(GraphicsDevice, TEXTURE_SIZE, TEXTURE_SIZE);
            _wallTextureData[2] = new Color[TEXTURE_SIZE * TEXTURE_SIZE];

            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    int index = y * TEXTURE_SIZE + x;
                    bool isPanel = ((x / 20) + (y / 20)) % 2 == 0;
                    bool isRivet = (x % 20 == 0 || x % 20 == 19) && (y % 20 == 0 || y % 20 == 19);

                    if (isRivet)
                    {
                        _wallTextureData[2][index] = new Color(40, 40, 40);
                    }
                    else if (isPanel)
                    {
                        _wallTextureData[2][index] = new Color(40, 100, 40);
                    }
                    else
                    {
                        _wallTextureData[2][index] = new Color(50, 120, 50);
                    }
                }
            }
            _wallTextures[2].SetData(_wallTextureData[2]);
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

            // Regenerate map with R key
            if (state.IsKeyDown(Keys.R))
            {
                GenerateMap();
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
            return map[mapY, mapX] == 0; // correct indexing [row, col] = [Y, X]
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
            DrawMinimap();

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
                int hitWallType = 1;

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

                    // Check bounds BEFORE accessing array
                    if (mapX < 0 || mapX >= mapWidth || mapY < 0 || mapY >= mapHeight)
                    {
                        hit = true; // Hit boundary
                        hitWallType = 1;
                        break;
                    }

                    // Check if ray hit a wall
                    if (map[mapY, mapX] > 0)
                    {
                        hit = true;
                        hitWallType = map[mapY, mapX];
                    }

                    steps++;
                }

                // Calculate distance and render wall slice
                if (hit)
                {
                    float perpWallDist;
                    float wallX;


                    if (side == 0)
                    {
                        perpWallDist = (mapX - playerPos.X + (1 - step.X) / 2) / rayDir.X;
                        wallX = playerPos.Y + perpWallDist * rayDir.Y;
                    }


                    else
                    {
                        perpWallDist = (mapY - playerPos.Y + (1 - step.Y) / 2) / rayDir.Y;
                        wallX = playerPos.X + perpWallDist * rayDir.X;
                    }
                    wallX -= (float)Math.Floor(wallX);

                    // Prevent division by zero or extremely close walls
                    perpWallDist = Math.Max(perpWallDist, 0.1f);

                    int lineHeight = (int)(SCREEN_HEIGHT / perpWallDist);

                    int drawStart = Math.Max(0, -lineHeight / 2 + SCREEN_HEIGHT / 2);
                    int drawEnd = Math.Min(SCREEN_HEIGHT - 1, lineHeight / 2 + SCREEN_HEIGHT / 2);

                    int textureIndex = Math.Min(Math.Max(hitWallType - 1, 0), _wallTextureData.Length - 1);
                    int texX = (int)(wallX * TEXTURE_SIZE);
                    texX = Math.Clamp(texX, 0, TEXTURE_SIZE - 1);

                    if ((side == 0 && rayDir.X > 0) || (side == 1 && rayDir.Y < 0))
                    {
                        texX = TEXTURE_SIZE - texX - 1;
                    }

                    // Use cached texture data instead of GetData()!
                    Color[] textureData = _wallTextureData[textureIndex];

                    for (int y = drawStart; y < drawEnd; y++)
                    {
                        int d = y * 256 - SCREEN_HEIGHT * 128 + lineHeight * 128;
                        int texY = ((d * TEXTURE_SIZE) / lineHeight) / 256;
                        texY = Math.Clamp(texY, 0, TEXTURE_SIZE - 1);

                        //  Direct array access instead of GetData()
                        int texIndex = texY * TEXTURE_SIZE + texX;
                        Color pixel = textureData[texIndex];

                        float brightness = Math.Max(0.3f, 1.0f - perpWallDist * 0.1f);
                        if (side == 1) brightness *= 0.8f;

                        Color shadedColor = new Color(
                            (byte)(pixel.R * brightness),
                            (byte)(pixel.G * brightness),
                            (byte)(pixel.B * brightness)
                        );

                        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, 1, 1), shadedColor);
                    }
                }
            }
        }


        private void DrawMinimap()
        {
            int minimapX = SCREEN_WIDTH - MINIMAP_SIZE - MINIMAP_MARGIN;
            int minimapY = MINIMAP_MARGIN;

            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(minimapX - 2, minimapY - 2, MINIMAP_SIZE + 4, MINIMAP_SIZE + 4),
                Color.Black * 0.7f
            );

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int tileX = minimapX + (x * MINIMAP_SIZE / mapWidth);
                    int tileY = minimapY + (y * MINIMAP_SIZE / mapHeight);
                    int tileSize = Math.Max(1, MINIMAP_SIZE / mapWidth);

                    Color tileColor;
                    if (map[y, x] == 0)
                    {
                        tileColor = Color.White * 0.3f;
                    }
                    else if (map[y, x] == 1)
                    {
                        tileColor = Color.Red * 0.6f;
                    }
                    else if (map[y, x] == 2)
                    {
                        tileColor = Color.Blue * 0.6f;
                    }
                    else
                    {
                        tileColor = Color.Green * 0.6f;
                    }

                    _spriteBatch.Draw(
                        _pixelTexture,
                        new Rectangle(tileX, tileY, tileSize, tileSize),
                        tileColor
                    );
                }
            }

            int playerMinimapX = minimapX + (int)(playerPos.X * MINIMAP_SIZE / mapWidth);
            int playerMinimapY = minimapY + (int)(playerPos.Y * MINIMAP_SIZE / mapHeight);
            int playerDotSize = 4;

            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    playerMinimapX - playerDotSize / 2,
                    playerMinimapY - playerDotSize / 2,
                    playerDotSize,
                    playerDotSize
                ),
                Color.Yellow
            );

            int dirLineLength = 8;
            Vector2 dirEnd = new Vector2(
                playerMinimapX + playerDir.X * dirLineLength,
                playerMinimapY + playerDir.Y * dirLineLength
            );

            DrawLine(
                _spriteBatch,
                new Vector2(playerMinimapX, playerMinimapY),
                dirEnd,
                Color.Yellow
            );
        }



        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            sb.Draw(_pixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), 2),
                null,
                color,
                angle,
                new Vector2(0, 0),
                SpriteEffects.None,
                0);
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

                if (_wallTextures != null)
                {
                    foreach (var texture in _wallTextures)
                    {
                        texture?.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }



    }
}
