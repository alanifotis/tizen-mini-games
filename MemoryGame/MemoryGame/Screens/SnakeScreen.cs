// Screens/SnakeScreen.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;
using MemoryGame.Utils;

namespace MemoryGame.Screens
{
    public class SnakeScreen
    {
        public Action OnExitToWelcome { get; set; }

        private View _rootView;
        private View _gameBoard;
        
        private TextLabel _scoreLabel;
        private TextLabel _lengthLabel;
        
        private Timer _gameTimer;

        // Grid Math
        private const int CELL_SIZE = 30;
        private const int COLS = 40; 
        private const int ROWS = 30; 
        
        private enum Direction { Up, Down, Left, Right }
        private Direction _currentDir;
        private Direction _nextDir; 

        private class SnakeSegment
        {
            public int X { get; set; }
            public int Y { get; set; }
            public View BodyView { get; set; }
        }

        private List<SnakeSegment> _snake = new List<SnakeSegment>();
        private View _foodView;
        private int _foodX, _foodY;

        private int _score = 0;
        private bool _isGameOver = false;
        private Random _rand = new Random();

        // Specific Neon Colors Requested
        private readonly Color SnakeBodyColor = new Color(0.22f, 0.96f, 0.15f, 1.0f); // #38F527
        private readonly Color SnakeHeadColor = Color.White;
        private readonly Color AppleColor = new Color(1.0f, 0.05f, 0.15f, 1.0f); // Neon Red

        // Popup System
        private View _popupOverlay;
        private View[] _popupButtons = new View[2];
        private TextLabel[] _popupLabels = new TextLabel[2];
        private int _popupSelectedIndex = 0;
        private bool _isPopupActive = false;

        private string _resPath => Application.Current.DirectoryInfo.Resource;
        private readonly string ArcadeFont = "Press Start 2P";

        public View CreateScreen()
        {
            FontClient.Instance.AddCustomFontDirectory(_resPath + "fonts/");

            _rootView = new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0.05f, 0.05f, 0.08f, 1.0f) };

            View boardBorder = new View() {
                Size = new Size((COLS * CELL_SIZE) + 16, (ROWS * CELL_SIZE) + 16),
                Position = new Position((1920 - (COLS * CELL_SIZE)) / 2 - 8, 100),
                BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.9f), 
                BorderlineWidth = 8.0f, BorderlineColor = new Color(0.9f, 0.0f, 1.0f, 1.0f), 
                CornerRadius = 16.0f
            };
            
            _gameBoard = new View() {
                Size = new Size(COLS * CELL_SIZE, ROWS * CELL_SIZE), Position = new Position(8, 8),
                BackgroundColor = new Color(0.02f, 0.02f, 0.03f, 1.0f), 
                ClippingMode = ClippingModeType.ClipToBoundingBox 
            };
            boardBorder.Add(_gameBoard);
            _rootView.Add(boardBorder);

            _scoreLabel = new TextLabel("SCORE: 0") {
                Size = new Size(600, 60), Position = new Position(360, 30),
                TextColor = Color.White, PointSize = 28.0f, FontFamily = ArcadeFont
            };
            _rootView.Add(_scoreLabel);

            _lengthLabel = new TextLabel("LENGTH: 3") {
                Size = new Size(600, 60), Position = new Position(960, 30),
                TextColor = new Color(0.0f, 0.9f, 1.0f, 1.0f), PointSize = 28.0f, 
                HorizontalAlignment = HorizontalAlignment.End, FontFamily = ArcadeFont
            };
            _rootView.Add(_lengthLabel);

            // Apple
            _foodView = new View() {
                Size = new Size(CELL_SIZE, CELL_SIZE),
                BackgroundColor = AppleColor,
                CornerRadius = CELL_SIZE / 2.0f
            };
            _gameBoard.Add(_foodView);

            _gameTimer = new Timer(150); 
            _gameTimer.Tick += GameLoopTick;

            CreatePopup();
            return _rootView;
        }

        private void CreatePopup()
        {
            _popupOverlay = new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0, 0, 0, 0.85f) };
            View dialogBox = new View() { Size = new Size(800, 480), Position = new Position(560, 300), BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.95f), CornerRadius = 32.0f, BorderlineWidth = 4.0f, BorderlineColor = new Color(1.0f, 1.0f, 1.0f, 0.3f) };
            TextLabel title = new TextLabel("SNAKE CRASHED") { Size = new Size(800, 100), Position = new Position(0, 40), TextColor = new Color(1.0f, 0.2f, 0.3f, 1.0f), PointSize = 42.0f, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont };
            dialogBox.Add(title);

            string[] btnTexts = { "RETRY SNAKE", "EXIT TO MENU" };
            for (int i = 0; i < 2; i++)
            {
                _popupButtons[i] = new View() { Size = new Size(600, 90), Position = new Position(100, 180 + (i * 120)), BackgroundColor = new Color(0.2f, 0.25f, 0.35f, 1.0f), CornerRadius = 24.0f };
                _popupLabels[i] = new TextLabel(btnTexts[i]) { Size = new Size(600, 90), TextColor = Color.White, PointSize = 24.0f, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontFamily = ArcadeFont };
                _popupButtons[i].Add(_popupLabels[i]);
                dialogBox.Add(_popupButtons[i]);
            }
            _popupOverlay.Add(dialogBox);
            _rootView.Add(_popupOverlay);
            _popupOverlay.Hide();
        }

        public void StartGame()
        {
            _isPopupActive = false;
            _popupOverlay.Hide();
            _isGameOver = false;
            _score = 0;
            
            _currentDir = Direction.Right;
            _nextDir = Direction.Right;

            foreach (var seg in _snake) { _gameBoard.Remove(seg.BodyView); seg.BodyView.Dispose(); }
            _snake.Clear();

            int startX = COLS / 2;
            int startY = ROWS / 2;
            for (int i = 0; i < 3; i++)
            {
                AddSegment(startX - i, startY, i == 0);
            }

            SpawnFood();
            UpdateHUD();

            _gameTimer.Interval = 150; 
            _gameTimer.Start();
        }

        public void StopGame()
        {
            _gameTimer?.Stop();
        }

        private void AddSegment(int x, int y, bool isHead)
        {
            // OVERLAPPING CIRCLES: Make them slightly bigger than the cell size (+6px) and offset them (-3px) 
            // so they blend together into a seamless organic tube.
            View block = new View() {
                Size = new Size(CELL_SIZE + 6, CELL_SIZE + 6),
                Position = new Position(x * CELL_SIZE - 3, y * CELL_SIZE - 3),
                BackgroundColor = isHead ? SnakeHeadColor : SnakeBodyColor, 
                CornerRadius = (CELL_SIZE + 6) / 2.0f // Perfect Circle
            };
            
            _gameBoard.Add(block);
            _snake.Add(new SnakeSegment { X = x, Y = y, BodyView = block });
        }

        private void SpawnFood()
        {
            bool valid = false;
            while (!valid)
            {
                _foodX = _rand.Next(1, COLS - 1);
                _foodY = _rand.Next(1, ROWS - 1);
                
                valid = true;
                foreach (var seg in _snake)
                {
                    if (seg.X == _foodX && seg.Y == _foodY) { valid = false; break; }
                }
            }
            _foodView.Position = new Position(_foodX * CELL_SIZE, _foodY * CELL_SIZE);
            _foodView.RaiseToTop(); // Ensure apple is never hidden
        }

        private bool GameLoopTick(object source, Timer.TickEventArgs e)
        {
            if (_isGameOver || _isPopupActive) return true;

            _currentDir = _nextDir; 
            
            int headX = _snake[0].X;
            int headY = _snake[0].Y;

            if (_currentDir == Direction.Up) headY--;
            else if (_currentDir == Direction.Down) headY++;
            else if (_currentDir == Direction.Left) headX--;
            else if (_currentDir == Direction.Right) headX++;

            // Check Wall Collision
            if (headX < 0 || headX >= COLS || headY < 0 || headY >= ROWS)
            {
                TriggerGameOver();
                return true;
            }

            // Check Self Collision
            for (int i = 0; i < _snake.Count - 1; i++) 
            {
                if (_snake[i].X == headX && _snake[i].Y == headY)
                {
                    TriggerGameOver();
                    return true;
                }
            }

            // Move the Snake
            if (headX == _foodX && headY == _foodY)
            {
                // ATE FOOD
                _snake[0].BodyView.BackgroundColor = SnakeBodyColor; 
                
                View newHeadView = new View() {
                    Size = new Size(CELL_SIZE + 6, CELL_SIZE + 6),
                    Position = new Position(headX * CELL_SIZE - 3, headY * CELL_SIZE - 3),
                    BackgroundColor = SnakeHeadColor,
                    CornerRadius = (CELL_SIZE + 6) / 2.0f
                };
                _gameBoard.Add(newHeadView);
                newHeadView.RaiseToTop(); // Keep head on top of the overlapping neck
                
                _snake.Insert(0, new SnakeSegment { X = headX, Y = headY, BodyView = newHeadView });

                _score += 50;
                UpdateHUD();
                
                ProceduralAudio.PlayLaser(); 
                CreateParticles(_foodX * CELL_SIZE + 15, _foodY * CELL_SIZE + 15, AppleColor);
                
                _gameTimer.Interval = (uint)Math.Max(50, _gameTimer.Interval - 3); 
                SpawnFood();
            }
            else
            {
                // NO FOOD: O(1) Memory trick. Move the tail to the front.
                _snake[0].BodyView.BackgroundColor = SnakeBodyColor; 
                
                SnakeSegment tail = _snake[_snake.Count - 1];
                _snake.RemoveAt(_snake.Count - 1);
                
                tail.X = headX;
                tail.Y = headY;
                tail.BodyView.Position = new Position(headX * CELL_SIZE - 3, headY * CELL_SIZE - 3);
                tail.BodyView.BackgroundColor = SnakeHeadColor;
                
                // Force the new head to overlap on top of the body correctly
                tail.BodyView.RaiseToTop();
                
                _snake.Insert(0, tail);
            }

            return true;
        }

        private void TriggerGameOver()
        {
            _isGameOver = true;
            StopGame();
            ProceduralAudio.PlayExplosion();
            
            CreateParticles(_snake[0].X * CELL_SIZE + 15, _snake[0].Y * CELL_SIZE + 15, SnakeHeadColor);
            _snake[0].BodyView.Hide();

            _isPopupActive = true;
            _popupSelectedIndex = 0;
            UpdatePopupFocus();
            _popupOverlay.Show();
            _popupOverlay.RaiseToTop();
        }

        private void CreateParticles(float startX, float startY, Color color)
        {
            for (int i = 0; i < 8; i++) 
            {
                View particle = new View() {
                    Size = new Size(8, 8), Position = new Position(startX, startY),
                    BackgroundColor = color, CornerRadius = 4.0f, PivotPoint = PivotPoint.Center
                };
                _gameBoard.Add(particle);

                float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                float distance = _rand.Next(30, 80);
                float endX = startX + (float)Math.Cos(angle) * distance;
                float endY = startY + (float)Math.Sin(angle) * distance;

                var anim = new Animation(400); 
                anim.AnimateTo(particle, "Position", new Position(endX, endY));
                anim.AnimateTo(particle, "Opacity", 0.0f);
                anim.Finished += (s, e) => {
                    _gameBoard.Remove(particle);
                    particle.Dispose();
                };
                anim.Play();
            }
        }

        private void UpdateHUD()
        {
            _scoreLabel.Text = $"SCORE: {_score}";
            _lengthLabel.Text = $"LENGTH: {_snake.Count}";
        }

        private void UpdatePopupFocus()
        {
            for (int i = 0; i < 2; i++)
            {
                if (i == _popupSelectedIndex)
                {
                    _popupButtons[i].BackgroundColor = new Color(1.0f, 1.0f, 1.0f, 0.9f);
                    _popupLabels[i].TextColor = Color.Black;
                }
                else
                {
                    _popupButtons[i].BackgroundColor = new Color(0.2f, 0.25f, 0.35f, 1.0f);
                    _popupLabels[i].TextColor = Color.White;
                }
            }
        }

        private void ExecutePopupAction()
        {
            _isPopupActive = false;
            _popupOverlay.Hide();
            if (_popupSelectedIndex == 0) StartGame();
            else if (_popupSelectedIndex == 1) OnExitToWelcome?.Invoke();
        }

        public void HandleKeyEvent(string key)
        {
            if (_isPopupActive)
            {
                if (key == "Up") { _popupSelectedIndex = Math.Max(0, _popupSelectedIndex - 1); UpdatePopupFocus(); }
                else if (key == "Down") { _popupSelectedIndex = Math.Min(1, _popupSelectedIndex + 1); UpdatePopupFocus(); }
                else if (key == "space" || key == "Select" || key == "Return" || key == "DPAD_CENTER") ExecutePopupAction();
                return;
            }

            if (key == "XF86Back" || key == "Escape") { StopGame(); OnExitToWelcome?.Invoke(); return; }
            if (_isGameOver) return;

            if (key == "Up" && _currentDir != Direction.Down) _nextDir = Direction.Up;
            else if (key == "Down" && _currentDir != Direction.Up) _nextDir = Direction.Down;
            else if (key == "Left" && _currentDir != Direction.Right) _nextDir = Direction.Left;
            else if (key == "Right" && _currentDir != Direction.Left) _nextDir = Direction.Right;
        }
    }
}