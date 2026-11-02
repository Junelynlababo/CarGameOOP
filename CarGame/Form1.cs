using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CarGame
{
    public partial class Form1 : Form
    {
        // ==================== GAME CLASSES ====================

        // ABSTRACTION:
        // Car is an abstract base class that defines what every car can do.
        // It provides shared data and requires child classes to define Update and Draw.
        public abstract class Car
        {
            // ENCAPSULATION:
            // These protected fields keep the car's data inside the class hierarchy.
            // Car and its child classes can use them without exposing them publicly.
            protected int x;
            protected float y;
            protected int width;
            protected int height;
            protected Image sprite;

            // ENCAPSULATION:
            // The hitbox is exposed through a read-only property instead of public fields.
            public Rectangle Hitbox
            {
                get
                {
                    return new Rectangle(
                        x + 8, (int)y + 8,
                        width - 16, height - 16);
                }
            }

            // ABSTRACTION:
            // Child classes must provide their own movement and drawing behavior.
            public abstract void Update(float speed, bool braking);
            public abstract void Draw(Graphics g);
        }

        // INHERITANCE:
        // PlayerCar inherits the shared car data and members from Car.
        public class PlayerCar : Car
        {
            // ENCAPSULATION:
            // Private fields store the player's movement settings inside this class.
            private int[] lanes;
            private int targetLane;
            private int laneSpeed = 8;
            private float normalY;
            private float targetY;
            private float forwardSpeed = 2f;
            private bool movingForward;
            private bool braking;

            // ENCAPSULATION:
            // Other classes can read CurrentLane, but only PlayerCar can change it.
            public int CurrentLane { get; private set; }

            // CONSTRUCTOR:
            // Sets up a new player car when it is created.
            public PlayerCar(Image image, int[] lanePositions, int screenHeight)
            {
                sprite = image;
                lanes = lanePositions;

                width = 55;
                height = 95;

                normalY = screenHeight - 120;
                y = normalY;
                targetY = normalY;

                CurrentLane = 1;
                targetLane = 1;
                x = lanes[CurrentLane];
            }

            public void ChangeLane(int direction)
            {
                targetLane += direction;

                if (targetLane < 0)
                    targetLane = 0;

                if (targetLane >= lanes.Length)
                    targetLane = lanes.Length - 1;
            }

            public void SetForward(bool value)
            {
                movingForward = value;
            }

            public void SetBraking(bool value)
            {
                braking = value;
            }

            // POLYMORPHISM:
            // PlayerCar provides its own version of Car.Update for player movement.
            public override void Update(float speed, bool isBraking)
            {
                int targetX = lanes[targetLane];

                if (x < targetX)
                {
                    x += laneSpeed;
                    if (x > targetX)
                        x = targetX;
                }
                else if (x > targetX)
                {
                    x -= laneSpeed;
                    if (x < targetX)
                        x = targetX;
                }

                if (x == targetX)
                    CurrentLane = targetLane;

                targetY = movingForward ? normalY - 35 : normalY;

                if (y < targetY)
                {
                    y += forwardSpeed;
                    if (y > targetY)
                        y = targetY;
                }
                else if (y > targetY)
                {
                    y -= forwardSpeed;
                    if (y < targetY)
                        y = targetY;
                }
            }

            // POLYMORPHISM:
            // PlayerCar provides its own version of Car.Draw.
            public override void Draw(Graphics g)
            {
                g.DrawImage(sprite, x, (int)y, width, height);

                if (braking)
                {
                    using (Brush brush = new SolidBrush(Color.Red))
                    {
                        g.FillEllipse(
                            brush, x + 10,
                            y + height - 10, 8, 8);

                        g.FillEllipse(
                            brush, x + width - 18,
                            y + height - 10, 8, 8);
                    }
                }
            }
        }

        // INHERITANCE:
        // EnemyCar also inherits the common features from the Car base class.
        public class EnemyCar : Car
        {
            // ENCAPSULATION:
            // Lane can be read by other classes, but changed only inside EnemyCar.
            public int Lane { get; private set; }

            // CONSTRUCTOR:
            // Sets up an enemy car with its image, lane, position, and size.
            public EnemyCar(Image image, int lane, int laneX, float startY)
            {
                sprite = image;
                Lane = lane;

                x = laneX;
                y = startY;

                width = 55;
                height = 95;
            }

            // POLYMORPHISM:
            // EnemyCar moves differently from PlayerCar, so it overrides Update.
            public override void Update(float speed, bool braking)
            {
                if (braking)
                    y -= 2;
                else
                    y += speed + 2;
            }

            // POLYMORPHISM:
            // EnemyCar has its own drawing behavior by overriding Draw.
            public override void Draw(Graphics g)
            {
                g.DrawImage(sprite, x, (int)y, width, height);
            }
        }

        // ABSTRACTION:
        // RoadManager groups the code responsible for updating and drawing the road.
        // Handles the moving road
        public class RoadManager
        {
            private Image roadImage;
            private float roadY;
            private int roadWidth;
            private int roadHeight;

            public RoadManager(Image image, int width, int height)
            {
                roadImage = image;
                roadWidth = width;
                roadHeight = height;
            }

            public void Update(float speed)
            {
                roadY += speed;

                if (roadY >= roadHeight)
                    roadY -= roadHeight;
            }

            public void Draw(Graphics g)
            {
                int y = (int)roadY;

                g.DrawImage(roadImage, 0, y, roadWidth, roadHeight);
                g.DrawImage(
                    roadImage, 0, y - roadHeight,
                    roadWidth, roadHeight);
            }

            public void Reset()
            {
                roadY = 0;
            }
        }

        // ABSTRACTION:
        // TrafficManager handles enemy spawning, movement, and cleanup in one class.
        public class TrafficManager
        {
            private List<EnemyCar> enemies = new List<EnemyCar>();
            private Image[] enemySprites;
            private int[] lanes;
            private Random random = new Random();

            private int maxEnemies = 10;
            private float spawnDistance;
            private float spawnEvery = 260f;
            private int screenHeight;

            private int[][] trafficPatterns =
            {
                new int[] { 0 },
                new int[] { 1 },
                new int[] { 2 },
                new int[] { 3 },
                new int[] { 0, 2 },
                new int[] { 1, 3 },
                new int[] { 0, 1 },
                new int[] { 2, 3 },
                new int[] { 1, 2 },
                new int[] { 0, 2, 3 },
                new int[] { 0, 1, 3 }
            };

            // ENCAPSULATION:
            // Exposes the enemy collection through a property so Form1 can read and use it.
            public List<EnemyCar> Enemies
            {
                get { return enemies; }
            }

            public TrafficManager(
                Image[] sprites, int[] lanePositions, int height)
            {
                enemySprites = sprites;
                lanes = lanePositions;
                screenHeight = height;
            }

            public void Update(float speed, bool braking, float distance)
            {
                spawnDistance += speed;

                if (spawnDistance >= spawnEvery)
                {
                    spawnDistance = 0;
                    SpawnEnemies(distance);
                }

                foreach (EnemyCar enemy in enemies)
                {
                    enemy.Update(speed, braking);
                }

                enemies.RemoveAll(
                    enemy => enemy.Hitbox.Y > screenHeight);
            }

            private void SpawnEnemies(float distance)
            {
                int km = (int)(distance / 1000);
                int[] pattern;

                if (km <= 1)
                {
                    SpawnInLane(random.Next(4));
                }
                else if (km < 3)
                {
                    pattern = trafficPatterns[random.Next(8)];

                    foreach (int lane in pattern)
                        SpawnInLane(lane);
                }
                else
                {
                    pattern = trafficPatterns[
                        random.Next(trafficPatterns.Length)];

                    foreach (int lane in pattern)
                        SpawnInLane(lane);
                }
            }

            private void SpawnInLane(int lane)
            {
                if (enemies.Count >= maxEnemies)
                    return;

                foreach (EnemyCar enemy in enemies)
                {
                    if (enemy.Lane == lane &&
                        enemy.Hitbox.Y < 220)
                        return;
                }

                Image sprite =
                    enemySprites[random.Next(enemySprites.Length)];

                enemies.Add(
                    new EnemyCar(
                        sprite,
                        lane,
                        lanes[lane],
                        -95 - random.Next(40, 95)));
            }

            // POLYMORPHISM:
            // Each enemy is handled as a Car, and its own Draw override is called.
            public void Draw(Graphics g)
            {
                foreach (Car enemy in enemies)
                    enemy.Draw(g);
            }

            public void Reset()
            {
                enemies.Clear();
                spawnDistance = 0;
            }
        }

        // ABSTRACTION:
        // GameStats keeps distance and score calculations together.
        public class GameStats
        {
            // ENCAPSULATION:
            // Other classes can read these values, but only GameStats can change them.
            public float Distance { get; private set; }
            public int Score { get; private set; }

            public void Update(float speed)
            {
                Distance += speed / 12f;
                Score = (int)(Distance * speed);
            }

            public void Reset()
            {
                Distance = 0;
                Score = 0;
            }
        }

        // ==================== FORM VARIABLES ====================

        // ENCAPSULATION:
        // Form1 keeps its game state and object references private.
        // It uses the manager classes instead of handling every task itself.
        private Random rnd = new Random();

        private bool gameOver;
        private bool choosingCar = true;

        private float speed = 3f;
        private float normalSpeed = 3f;
        private float maxSpeed = 18f;

        private float acceleration = 0.08f;
        private float deceleration = 0.05f;
        private float brakePower = 0.15f;

        private bool moveForward;
        private bool isBraking;

        private Timer timerRoad;

        private Image carSpriteSheet;
        private Image[] cars;
        private Image[] enemyCarSprites;
        private Image playerCarImage;

        private Rectangle[] carFrames;
        private int hoveredCar = -1;

        private int[] lanes = { 90, 150, 215, 278 };

        // OBJECT COMPOSITION:
        // Form1 contains and works with separate objects for the player, road,
        // traffic, and game statistics. Each object has its own responsibility.
        private PlayerCar player;
        private RoadManager road;
        private TrafficManager traffic;
        private GameStats stats;

        // ==================== GAME INITIALIZATION ====================

        // CONSTRUCTOR:
        // Creates the form and starts setting up the game objects.
        public Form1()
        {
            InitializeComponent();
            InitializeGame();
        }

        // OBJECT CREATION:
        // This method creates the manager and player objects used by the game.
        private void InitializeGame()
        {
            InitializeWindow();
            InitializeSprites();

            road = new RoadManager(
                Properties.Resources.road,
                ClientSize.Width,
                ClientSize.Height);

            player = new PlayerCar(
                playerCarImage,
                lanes,
                ClientSize.Height);

            traffic = new TrafficManager(
                enemyCarSprites,
                lanes,
                ClientSize.Height);

            stats = new GameStats();

            timerRoad = new Timer();
            timerRoad.Interval = 30;
            timerRoad.Tick += TimerRoad_Tick;
            timerRoad.Start();

            RegisterEvents();
        }

        private void InitializeWindow()
        {
            ClientSize = new Size(420, 540);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
        }

        private void InitializeSprites()
        {
            carSpriteSheet = Properties.Resources.car_sprites;

            Bitmap sheet = new Bitmap(carSpriteSheet);

            int columns = 5;
            int carWidth = 175;
            int carHeight = 270;

            cars = new Image[columns];
            enemyCarSprites = new Image[columns];

            for (int i = 0; i < columns; i++)
            {
                Rectangle playerSource = new Rectangle(
                    i * carWidth, 0, carWidth, carHeight);

                cars[i] = sheet.Clone(
                    playerSource, sheet.PixelFormat);

                Rectangle enemySource = new Rectangle(
                    i * carWidth, carHeight, carWidth, carHeight);

                enemyCarSprites[i] = sheet.Clone(
                    enemySource, sheet.PixelFormat);
            }

            carFrames = new Rectangle[]
            {
                new Rectangle(40, 120, 80, 120),
                new Rectangle(170, 120, 80, 120),
                new Rectangle(300, 120, 80, 120),
                new Rectangle(100, 280, 80, 120),
                new Rectangle(240, 280, 80, 120)
            };
        }

        private void RegisterEvents()
        {
            Paint += Form1_Paint;
            MouseClick += Form1_MouseClick;
            MouseMove += Form1_MouseMove;
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
        }

        // ==================== CAR SELECTION ====================

        private void UpdateHoveredCar(Point position)
        {
            if (!choosingCar)
                return;

            hoveredCar = -1;
            Cursor = Cursors.Default;

            for (int i = 0; i < carFrames.Length; i++)
            {
                if (carFrames[i].Contains(position))
                {
                    hoveredCar = i;
                    Cursor = Cursors.Hand;
                    break;
                }
            }
        }

        private void SelectCar(Point position)
        {
            if (!choosingCar)
                return;

            for (int i = 0; i < carFrames.Length; i++)
            {
                if (carFrames[i].Contains(position))
                {
                    playerCarImage = cars[i];

                    player = new PlayerCar(
                        playerCarImage,
                        lanes,
                        ClientSize.Height);

                    choosingCar = false;
                    Cursor = Cursors.Default;

                    Invalidate();
                    return;
                }
            }
        }

        // ==================== GAME LOGIC ====================

        private void UpdateSpeed()
        {
            if (moveForward)
            {
                speed += acceleration;

                if (speed > maxSpeed)
                    speed = maxSpeed;
            }
            else if (isBraking)
            {
                speed -= brakePower;

                if (speed < 0)
                    speed = 0;
            }
            else
            {
                if (speed > normalSpeed)
                {
                    speed -= deceleration;

                    if (speed < normalSpeed)
                        speed = normalSpeed;
                }
                else if (speed < normalSpeed)
                {
                    speed += deceleration;

                    if (speed > normalSpeed)
                        speed = normalSpeed;
                }
            }
        }

        private void CheckCollision()
        {
            foreach (EnemyCar enemy in traffic.Enemies)
            {
                if (player.Hitbox.IntersectsWith(enemy.Hitbox))
                {
                    gameOver = true;
                    timerRoad.Stop();
                    return;
                }
            }
        }

        private void RestartGame()
        {
            gameOver = false;
            speed = normalSpeed;

            road.Reset();
            traffic.Reset();
            stats.Reset();

            player = new PlayerCar(
                playerCarImage,
                lanes,
                ClientSize.Height);

            moveForward = false;
            isBraking = false;

            timerRoad.Start();
            Invalidate();
        }

        // ==================== TIMER ====================

        private void TimerRoad_Tick(object sender, EventArgs e)
        {
            if (gameOver || choosingCar)
                return;

            UpdateSpeed();

            road.Update(speed);
            player.Update(speed, isBraking);

            traffic.Update(
                speed,
                isBraking,
                stats.Distance);

            stats.Update(speed);

            CheckCollision();

            Invalidate();
        }

        // ==================== KEYBOARD EVENTS ====================

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameOver)
            {
                if (e.KeyCode == Keys.R ||
                    e.KeyCode == Keys.Enter ||
                    e.KeyCode == Keys.Escape)
                {
                    RestartGame();
                }

                return;
            }

            if (choosingCar)
                return;

            if (e.KeyCode == Keys.Left ||
                e.KeyCode == Keys.A)
            {
                player.ChangeLane(-1);
            }

            if (e.KeyCode == Keys.Right ||
                e.KeyCode == Keys.D)
            {
                player.ChangeLane(1);
            }

            if (e.KeyCode == Keys.Up ||
                e.KeyCode == Keys.W)
            {
                moveForward = true;
                player.SetForward(true);
            }

            if (e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.S)
            {
                isBraking = true;
                player.SetBraking(true);
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up ||
                e.KeyCode == Keys.W)
            {
                moveForward = false;
                player.SetForward(false);
            }

            if (e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.S)
            {
                isBraking = false;
                player.SetBraking(false);
            }
        }

        // ==================== MOUSE EVENTS ====================

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateHoveredCar(e.Location);
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            SelectCar(e.Location);
        }

        // ==================== DRAWING ====================

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            road.Draw(g);

            traffic.Draw(g);

            if (choosingCar)
                DrawCarSelection(g);
            else
            {
                player.Draw(g);
                DrawHUD(g);
            }

            if (gameOver)
                DrawGameOver(g);
        }

        private void DrawCarSelection(Graphics g)
        {
            using (Brush overlay = new SolidBrush(
                Color.FromArgb(128, 0, 0, 0)))
            {
                g.FillRectangle(overlay, ClientRectangle);
            }

            using (Font font = new Font(
                "Arial", 22, FontStyle.Bold))
            {
                string title = "Choose Your Car";

                SizeF size = g.MeasureString(title, font);

                g.DrawString(
                    title,
                    font,
                    Brushes.White,
                    (ClientSize.Width - size.Width) / 2,
                    20);
            }

            for (int i = 0; i < cars.Length; i++)
            {
                Rectangle frame = carFrames[i];

                if (i == hoveredCar)
                    frame.Inflate(10, 10);

                g.DrawImage(cars[i], frame);
            }
        }

        private void DrawHUD(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawHudPanel(g, new Rectangle(10, 10, 120, 55));

            using (Font big = new Font(
                "Segoe UI", 22, FontStyle.Bold))
            using (Font small = new Font(
                "Segoe UI", 10))
            {
                g.DrawString(
                    (speed * 10).ToString("0"),
                    big,
                    Brushes.White,
                    18, 10);

                g.DrawString(
                    "km/h",
                    small,
                    Brushes.Gainsboro,
                    74, 36);
            }

            DrawHudPanel(
                g,
                new Rectangle(ClientSize.Width - 120, 10, 110, 45));

            float km = stats.Distance / 1000f;

            string distance = km < 1
                ? $"{(int)stats.Distance} m"
                : $"{km:F2} km";

            using (Font value = new Font(
                "Segoe UI", 16, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(distance, value);

                g.DrawString(
                    distance,
                    value,
                    Brushes.White,
                    ClientSize.Width - size.Width - 18,
                    18);
            }

            DrawHudPanel(
                g,
                new Rectangle(ClientSize.Width - 110, 70, 100, 55));

            using (Font label = new Font(
                "Segoe UI", 10, FontStyle.Bold))
            using (Font scoreFont = new Font(
                "Segoe UI", 14, FontStyle.Bold))
            {
                g.DrawString(
                    stats.Score.ToString(),
                    scoreFont,
                    Brushes.Gold,
                    ClientSize.Width - 90,
                    90);

                g.DrawString(
                    "Score",
                    label,
                    Brushes.Gainsboro,
                    ClientSize.Width - 100,
                    70);
            }
        }

        private void DrawHudPanel(Graphics g, Rectangle rect)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                int radius = 10;

                path.AddArc(
                    rect.Left, rect.Top,
                    radius, radius, 180, 90);

                path.AddArc(
                    rect.Right - radius, rect.Top,
                    radius, radius, 270, 90);

                path.AddArc(
                    rect.Right - radius,
                    rect.Bottom - radius,
                    radius, radius, 0, 90);

                path.AddArc(
                    rect.Left,
                    rect.Bottom - radius,
                    radius, radius, 90, 90);

                path.CloseFigure();

                using (SolidBrush brush = new SolidBrush(
                    Color.FromArgb(160, 25, 25, 25)))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(
                    Color.FromArgb(70, Color.White)))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void DrawGameOver(Graphics g)
        {
            using (Brush overlay = new SolidBrush(
                Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(overlay, ClientRectangle);
            }

            using (Font title = new Font(
                "Arial", 28, FontStyle.Bold))
            using (Font subtitle = new Font(
                "Arial", 14))
            {
                string gameOverText = "GAME OVER";
                string retryText = "Press R to Restart";

                SizeF titleSize = g.MeasureString(
                    gameOverText, title);

                SizeF retrySize = g.MeasureString(
                    retryText, subtitle);

                g.DrawString(
                    gameOverText,
                    title,
                    Brushes.Red,
                    (ClientSize.Width - titleSize.Width) / 2,
                    220);

                g.DrawString(
                    retryText,
                    subtitle,
                    Brushes.White,
                    (ClientSize.Width - retrySize.Width) / 2,
                    280);
            }
        }
    }
}