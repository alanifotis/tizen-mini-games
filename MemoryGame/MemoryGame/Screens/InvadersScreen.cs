// Screens/InvadersScreen.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;
using MemoryGame.Utils;

namespace MemoryGame.Screens
{
    public class InvadersScreen
    {
        public Action OnExitToWelcome { get; set; }

        private View _rootView;
        private View _gameBoard;
        
        // Upgraded HUD
        private TextLabel _scoreLabel;
        private TextLabel _waveLabel;
        private TextLabel _livesLabel;
        
        private Timer _gameTimer; 
        private Timer _alienTimer; 

        private ImageView _player;
        
        private class Alien
        {
            public ImageView View { get; set; }
            public int Hp { get; set; }
            public int ScoreValue { get; set; }
            public int FireChance { get; set; }
            public Color ExplodeColor { get; set; }
        }
        
        private List<Alien> _aliens = new List<Alien>();
        private List<View> _playerBullets = new List<View>();
        private List<View> _enemyBullets = new List<View>();
        private List<View> _stars = new List<View>(); 

        // Progression State
        private int _score = 0;
        private int _wave = 1;
        private int _lives = 3;
        
        private int _alienDirection = 1; 
        private float _alienSpeed = 25.0f;
        private bool _isGameOver = false;
        private bool _isRespawning = false; // Blocks input while player is blowing up
        private Random _rand = new Random();

        // Popup System
        private View _popupOverlay;
        private View[] _popupButtons = new View[2];
        private TextLabel[] _popupLabels = new TextLabel[2];
        private int _popupSelectedIndex = 0;
        private bool _isPopupActive = false;

        private string _resPath => Application.Current.DirectoryInfo.Resource;
        private readonly string ArcadeFont = "Press Start 2P";
        
        private const float BOARD_W = 1200;
        private const float BOARD_H = 900;
        private const float PLAYER_SPEED = 45.0f;
        private const float BULLET_SPEED = 30.0f;

        // Image Pools for rotating enemy factions
        private readonly string[] _gruntImages = { "1.png", "2.png", "4.png", "5.png", "10.png" };
        private readonly string[] _hunterImages = { "3.png", "7.png", "8.png", "11.png" };
        private readonly string[] _dreadImages = { "9.png", "12.png", "13.png" };

        public View CreateScreen()
        {
            FontClient.Instance.AddCustomFontDirectory(_resPath + "fonts/");

            _rootView = new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0.05f, 0.05f, 0.08f, 1.0f) };

            View boardBorder = new View() {
                Size = new Size(BOARD_W + 16, BOARD_H + 16),
                Position = new Position((1920 - BOARD_W) / 2 - 8, 80),
                BackgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.9f), 
                BorderlineWidth = 8.0f, BorderlineColor = new Color(0.0f, 0.9f, 1.0f, 1.0f), 
                CornerRadius = 16.0f
            };
            
            _gameBoard = new View() {
                Size = new Size(BOARD_W, BOARD_H), Position = new Position(8, 8),
                BackgroundColor = new Color(0.01f, 0.01f, 0.02f, 1.0f), 
                ClippingMode = ClippingModeType.ClipToBoundingBox 
            };
            boardBorder.Add(_gameBoard);
            _rootView.Add(boardBorder);

            // --- ADVANCED 3-PART HUD ---
            int hudY = 20;
            _scoreLabel = new TextLabel("SCORE: 0") {
                Size = new Size(400, 60), Position = new Position((1920 - BOARD_W) / 2, hudY),
                TextColor = Color.White, PointSize = 28.0f, FontFamily = ArcadeFont
            };
            _rootView.Add(_scoreLabel);

            _waveLabel = new TextLabel("WAVE: 1") {
                Size = new Size(400, 60), Position = new Position((1920 - 400) / 2, hudY),
                TextColor = new Color(0.0f, 0.9f, 1.0f, 1.0f), PointSize = 28.0f, 
                HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont
            };
            _rootView.Add(_waveLabel);

            _livesLabel = new TextLabel("LIVES: ♥♥♥") {
                Size = new Size(400, 60), Position = new Position((1920 + BOARD_W) / 2 - 400, hudY),
                TextColor = new Color(1.0f, 0.2f, 0.3f, 1.0f), PointSize = 28.0f, 
                HorizontalAlignment = HorizontalAlignment.End, FontFamily = ArcadeFont
            };
            _rootView.Add(_livesLabel);

            _gameTimer = new Timer(30); 
            _gameTimer.Tick += GameLoopTick;

            _alienTimer = new Timer(700);
            _alienTimer.Tick += AlienMarchTick;

            CreatePopup();
            return _rootView;
        }

        private void CreatePopup()
        {
            _popupOverlay = new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0, 0, 0, 0.85f) };
            View dialogBox = new View() { Size = new Size(800, 480), Position = new Position(560, 300), BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.95f), CornerRadius = 32.0f, BorderlineWidth = 4.0f, BorderlineColor = new Color(1.0f, 1.0f, 1.0f, 0.3f) };
            TextLabel title = new TextLabel("MISSION FAILED") { Size = new Size(800, 100), Position = new Position(0, 40), TextColor = new Color(1.0f, 0.2f, 0.3f, 1.0f), PointSize = 42.0f, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont };
            dialogBox.Add(title);

            string[] btnTexts = { "RETRY INVADERS", "EXIT TO MENU" };
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
            _isRespawning = false;
            
            // Complete Hard Reset
            _score = 0;
            _wave = 1;
            _lives = 3;
            
            UpdateHUD();
            ClearBoard();
            GenerateStarfield();

            _player = new ImageView() {
                Size = new Size(64, 64), 
                Position = new Position(BOARD_W / 2 - 32, BOARD_H - 90),
                ResourceUrl = _resPath + "example_ships/6B.png",
                FittingMode = FittingModeType.ShrinkToFit
            };
            _gameBoard.Add(_player);

            StartNextWave();

            _gameTimer.Start();
        }

        private void StartNextWave()
        {
            _alienDirection = 1;
            
            // Clean up old elements just in case
            foreach (var a in _aliens) { _gameBoard.Remove(a.View); a.View.Dispose(); }
            foreach (var b in _playerBullets) { _gameBoard.Remove(b); b.Dispose(); }
            foreach (var b in _enemyBullets) { _gameBoard.Remove(b); b.Dispose(); }
            _aliens.Clear(); _playerBullets.Clear(); _enemyBullets.Clear();

            // --- PROGRESSION MATH ---
            int waveIndex = _wave - 1;
            
            // Cycle through available images so enemies look different every level
            string gruntImg = _gruntImages[waveIndex % _gruntImages.Length];
            string hunterImg = _hunterImages[waveIndex % _hunterImages.Length];
            string dreadImg = _dreadImages[waveIndex % _dreadImages.Length];

            // Scale Stats
            int hpBoost = waveIndex / 2; // Extra HP every 2 waves
            int fireBoost = waveIndex * 5; // Base fire rate increases by 5% each wave
            int scoreMult = _wave;
            
            _alienSpeed = Math.Min(45.0f, 25.0f + (waveIndex * 3.0f)); // Cap max speed
            _alienTimer.Interval = (uint)Math.Max(150, 700 - (waveIndex * 30));

            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 10; c++)
                {
                    string imgFile; int hp, val, fire; int size; Color explodeCol;

                    if (r == 0) 
                    {
                        imgFile = dreadImg; hp = 3 + hpBoost; val = 500 * scoreMult; fire = 20 + fireBoost; size = 70;
                        explodeCol = new Color(1.0f, 0.4f, 0.0f, 1.0f); 
                    }
                    else if (r < 3)
                    {
                        imgFile = hunterImg; hp = 1 + (hpBoost / 2); val = 200 * scoreMult; fire = 60 + fireBoost; size = 56;
                        explodeCol = new Color(0.8f, 0.2f, 1.0f, 1.0f); 
                    }
                    else
                    {
                        imgFile = gruntImg; hp = 1; val = 100 * scoreMult; fire = 5 + (fireBoost / 2); size = 48;
                        explodeCol = new Color(0.2f, 1.0f, 0.3f, 1.0f); 
                    }

                    ImageView alienView = new ImageView() {
                        Size = new Size(size, size), 
                        Position = new Position(100 + (c * 80), 80 + (r * 70)),
                        ResourceUrl = _resPath + "example_ships/" + imgFile,
                        FittingMode = FittingModeType.ShrinkToFit
                    };
                    
                    _gameBoard.Add(alienView);
                    _aliens.Add(new Alien { View = alienView, Hp = hp, ScoreValue = val, FireChance = fire, ExplodeColor = explodeCol });
                }
            }

            _player.Position = new Position(BOARD_W / 2 - 32, BOARD_H - 90);
            _alienTimer.Start();
        }

        public void StopGame()
        {
            _gameTimer?.Stop();
            _alienTimer?.Stop();
        }

        private void GenerateStarfield()
        {
            for (int i = 0; i < 50; i++)
            {
                int size = _rand.Next(2, 5);
                float opacity = (float)_rand.NextDouble() * 0.8f + 0.2f;
                View star = new View() {
                    Size = new Size(size, size),
                    Position = new Position(_rand.Next(0, (int)BOARD_W), _rand.Next(0, (int)BOARD_H)),
                    BackgroundColor = new Color(1, 1, 1, opacity),
                    CornerRadius = size / 2.0f
                };
                _stars.Add(star);
                _gameBoard.Add(star);
            }
        }

        private void ClearBoard()
        {
            if (_player != null) { _gameBoard.Remove(_player); _player.Dispose(); _player = null; }
            foreach (var a in _aliens) { _gameBoard.Remove(a.View); a.View.Dispose(); }
            foreach (var b in _playerBullets) { _gameBoard.Remove(b); b.Dispose(); }
            foreach (var b in _enemyBullets) { _gameBoard.Remove(b); b.Dispose(); }
            foreach (var s in _stars) { _gameBoard.Remove(s); s.Dispose(); }
            
            _aliens.Clear(); _playerBullets.Clear(); _enemyBullets.Clear(); _stars.Clear();
        }

        private void UpdateHUD()
        {
            _scoreLabel.Text = $"SCORE: {_score}";
            _waveLabel.Text = $"WAVE: {_wave}";
            
            // Draw hearts based on remaining lives
            string hearts = "";
            for (int i = 0; i < _lives; i++) hearts += "♥";
            _livesLabel.Text = $"LIVES: {hearts}";
        }

        private bool AlienMarchTick(object source, Timer.TickEventArgs e)
        {
            if (_isGameOver || _isPopupActive || _isRespawning) return true;

            bool hitEdge = false;
            foreach (var alien in _aliens)
            {
                float newX = alien.View.Position.X + (_alienSpeed * _alienDirection);
                if (newX <= 20 || newX >= BOARD_W - 80) hitEdge = true;
            }

            if (hitEdge)
            {
                _alienDirection *= -1;
                foreach (var alien in _aliens) alien.View.Position = new Position(alien.View.Position.X, alien.View.Position.Y + 30);
            }
            else
            {
                foreach (var alien in _aliens) alien.View.Position = new Position(alien.View.Position.X + (_alienSpeed * _alienDirection), alien.View.Position.Y);
            }

            foreach (var alien in _aliens)
            {
                if (_rand.Next(0, 1000) < alien.FireChance) 
                {
                    ShootEnemyBullet(alien.View.Position.X + (alien.View.Size.Width / 2) - 4, alien.View.Position.Y + alien.View.Size.Height);
                }
            }

            // Speed up timer slightly as aliens die within the current wave
            _alienTimer.Interval = (uint)Math.Max(100, 700 - (_wave * 30) - (50 - _aliens.Count) * 12);

            return true;
        }

        private bool GameLoopTick(object source, Timer.TickEventArgs e)
        {
            if (_isGameOver || _isPopupActive || _isRespawning) return true;

            foreach (var star in _stars)
            {
                float newY = star.Position.Y + (star.Size.Height * 0.8f); 
                if (newY > BOARD_H) {
                    star.Position = new Position(_rand.Next(0, (int)BOARD_W), -10);
                } else {
                    star.Position = new Position(star.Position.X, newY);
                }
            }

            for (int i = _playerBullets.Count - 1; i >= 0; i--)
            {
                View bullet = _playerBullets[i];
                bullet.Position = new Position(bullet.Position.X, bullet.Position.Y - BULLET_SPEED);
                
                if (bullet.Position.Y < 0) { DestroyView(bullet, _playerBullets); continue; }

                for (int j = _aliens.Count - 1; j >= 0; j--)
                {
                    Alien alien = _aliens[j];
                    if (CheckCollision(bullet, alien.View))
                    {
                        DestroyView(bullet, _playerBullets);
                        
                        alien.Hp--;
                        if (alien.Hp <= 0)
                        {
                            ProceduralAudio.PlayExplosion(); 
                            CreateExplosion(alien.View.Position.X + (alien.View.Size.Width / 2), alien.View.Position.Y + (alien.View.Size.Height / 2), alien.ExplodeColor);
                            _score += alien.ScoreValue;
                            UpdateHUD();
                            
                            _gameBoard.Remove(alien.View);
                            alien.View.Dispose();
                            _aliens.RemoveAt(j);

                            if (_aliens.Count == 0) 
                            {
                                TriggerVictory();
                                return true; 
                            }
                        }
                        else
                        {
                            FlashAlienDamage(alien.View);
                        }
                        break;
                    }
                }
            }

            for (int i = _enemyBullets.Count - 1; i >= 0; i--)
            {
                View bullet = _enemyBullets[i];
                bullet.Position = new Position(bullet.Position.X, bullet.Position.Y + (BULLET_SPEED * 0.6f)); // Enemy bullets scale too
                
                if (bullet.Position.Y > BOARD_H) { DestroyView(bullet, _enemyBullets); continue; }
                
                if (CheckCollision(bullet, _player, 12)) 
                { 
                    _ = HandlePlayerDeath();
                    return true; 
                }
            }

            // Invasion check
            foreach (var alien in _aliens)
            {
                if (alien.View.Position.Y > BOARD_H - 100) 
                {
                    TriggerGameOver();
                    return true;
                }
            }

            return true;
        }

        private async Task HandlePlayerDeath()
        {
            _isRespawning = true;
            _lives--;
            UpdateHUD();

            ProceduralAudio.PlayExplosion(); 
            CreateExplosion(_player.Position.X + 32, _player.Position.Y + 32, new Color(0.0f, 0.9f, 1.0f, 1.0f));
            _player.Hide();

            if (_lives <= 0)
            {
                TriggerGameOver();
                return;
            }

            // Clear stray bullets to make respawn fair
            foreach (var b in _playerBullets) { _gameBoard.Remove(b); b.Dispose(); }
            foreach (var b in _enemyBullets) { _gameBoard.Remove(b); b.Dispose(); }
            _playerBullets.Clear(); _enemyBullets.Clear();

            // Brief pause so player can register they died
            await Task.Delay(1500);

            if (!_isPopupActive && !_isGameOver)
            {
                _player.Position = new Position(BOARD_W / 2 - 32, BOARD_H - 90);
                _player.Show();
                _isRespawning = false;
            }
        }

        private async void FlashAlienDamage(ImageView alienView)
        {
            alienView.Opacity = 0.3f;
            await Task.Delay(100);
            if (alienView != null) alienView.Opacity = 1.0f;
        }

        private void CreateExplosion(float startX, float startY, Color color)
        {
            for (int i = 0; i < 10; i++) 
            {
                View particle = new View() {
                    Size = new Size(8, 8), Position = new Position(startX, startY),
                    BackgroundColor = color, CornerRadius = 4.0f, PivotPoint = PivotPoint.Center
                };
                _gameBoard.Add(particle);

                float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                float distance = _rand.Next(40, 120);
                float endX = startX + (float)Math.Cos(angle) * distance;
                float endY = startY + (float)Math.Sin(angle) * distance;

                var anim = new Animation(500); 
                anim.AnimateTo(particle, "Position", new Position(endX, endY));
                anim.AnimateTo(particle, "Opacity", 0.0f);
                anim.Finished += (s, e) => {
                    _gameBoard.Remove(particle);
                    particle.Dispose();
                };
                anim.Play();
            }
        }

        private bool CheckCollision(View bullet, View target, float margin = 0)
        {
            if (bullet == null || target == null) return false;
            return bullet.Position.X < target.Position.X + target.Size.Width - margin && 
                   bullet.Position.X + bullet.Size.Width > target.Position.X + margin &&
                   bullet.Position.Y < target.Position.Y + target.Size.Height - margin && 
                   bullet.Position.Y + bullet.Size.Height > target.Position.Y + margin;
        }

        private void ShootPlayerBullet()
        {
            if (_playerBullets.Count >= 3) return; 

            ProceduralAudio.PlayLaser(); 

            View bulletLeft = new View() { Size = new Size(6, 24), Position = new Position(_player.Position.X + 12, _player.Position.Y - 10), BackgroundColor = new Color(0.0f, 1.0f, 0.8f, 1.0f), CornerRadius = 3.0f };
            View bulletRight = new View() { Size = new Size(6, 24), Position = new Position(_player.Position.X + 46, _player.Position.Y - 10), BackgroundColor = new Color(0.0f, 1.0f, 0.8f, 1.0f), CornerRadius = 3.0f };
            
            _gameBoard.Add(bulletLeft); _gameBoard.Add(bulletRight);
            _playerBullets.Add(bulletLeft); _playerBullets.Add(bulletRight);
        }

        private void ShootEnemyBullet(float x, float y)
        {
            View bullet = new View() {
                Size = new Size(8, 24), Position = new Position(x, y),
                BackgroundColor = new Color(1.0f, 0.1f, 0.1f, 1.0f), CornerRadius = 4.0f
            };
            _gameBoard.Add(bullet);
            _enemyBullets.Add(bullet);
        }

        private void DestroyView(View v, System.Collections.IList list)
        {
            list.Remove(v);
            _gameBoard.Remove(v);
            v.Dispose();
        }

        private void TriggerGameOver()
        {
            _isGameOver = true;
            _alienTimer?.Stop();
            _isPopupActive = true;
            _popupSelectedIndex = 0;
            UpdatePopupFocus();
            _popupOverlay.Show();
            _popupOverlay.RaiseToTop();
        }

        private async void TriggerVictory()
        {
            _isRespawning = true;
            _alienTimer?.Stop();
            _wave++;
            UpdateHUD();
            
            await Task.Delay(1500); 
            
            if (!_isPopupActive && !_isGameOver) 
            {
                StartNextWave();
                _isRespawning = false;
            }
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
            if (_isGameOver || _isRespawning) return;

            if (key == "Left") { _player.Position = new Position(Math.Max(10, _player.Position.X - PLAYER_SPEED), _player.Position.Y); }
            else if (key == "Right") { _player.Position = new Position(Math.Min(BOARD_W - 74, _player.Position.X + PLAYER_SPEED), _player.Position.Y); }
            else if (key == "Select" || key == "Return" || key == "DPAD_CENTER" || key == "space" || key == "Up") { ShootPlayerBullet(); }
        }
    }
}