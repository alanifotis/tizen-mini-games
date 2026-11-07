// Screens/TetrisScreen.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;

namespace MemoryGame.Screens
{
    public class TetrisScreen
    {
        public Action OnExitToWelcome { get; set; }
        public Action OnGoToMemoryGame { get; set; }

        private View _rootView;
        private View _boardContainer; 
        
        private TextLabel _scoreLabel;
        private TextLabel _levelLabel;
        private TextLabel _linesLabel;
        private Timer _gameTimer;
        
        private const int ROWS = 20;
        private const int COLS = 15; 
        private int[,] _board = new int[ROWS, COLS];
        
        private const int BLOCK_W = 56; 
        private const int BLOCK_H = 46; 

        private View[,] _gridWrappers = new View[ROWS, COLS];
        private View[,] _gridBodies = new View[ROWS, COLS];
        private View[,] _gridHighlights = new View[ROWS, COLS];
        private View[,] _gridShadows = new View[ROWS, COLS];

        private View[,] _nextPieceBodies = new View[4, 4];
        private View[,] _nextPieceHighlights = new View[4, 4];
        private View[,] _nextPieceShadows = new View[4, 4];

        private View _popupOverlay;
        private View[] _popupButtons = new View[3];
        private TextLabel[] _popupLabels = new TextLabel[3];
        private int _popupSelectedIndex = 0;
        private bool _isPopupActive = false;

        private View _pauseOverlay;
        private bool _isPaused = false;

        private int[,] _currentPiece;
        private int _pieceRow;
        private int _pieceCol;
        private int _pieceType;
        private int _nextPieceType;
        private int _score = 0;
        private int _level = 1;
        private int _lines = 0;
        private bool _isGameOver = false;
        private bool _isClearingLines = false;
        private Random _rand = new Random();

        private string _resPath => Application.Current.DirectoryInfo.Resource;
        private readonly string ArcadeFont = "Press Start 2P";

        private readonly Color[] _colors = new Color[] {
            new Color(0.0f, 0.0f, 0.0f, 0.0f),   
            new Color(0.0f, 0.9f, 1.0f, 1.0f),   // Cyan 
            new Color(0.1f, 0.4f, 1.0f, 1.0f),   // Blue
            new Color(1.0f, 0.6f, 0.0f, 1.0f),   // Orange
            new Color(1.0f, 0.9f, 0.1f, 1.0f),   // Yellow
            new Color(0.1f, 0.9f, 0.4f, 1.0f),   // Green
            new Color(0.7f, 0.1f, 0.9f, 1.0f),   // Purple
            new Color(1.0f, 0.2f, 0.3f, 1.0f)    // Red
        };

        private readonly int[][,] _shapes = {
            new int[,] {{0}}, 
            new int[,] {{1,1,1,1}}, // I
            new int[,] {{2,0,0},{2,2,2}}, // J
            new int[,] {{0,0,3},{3,3,3}}, // L
            new int[,] {{4,4},{4,4}}, // O
            new int[,] {{0,5,5},{5,5,0}}, // S
            new int[,] {{0,6,0},{6,6,6}}, // T
            new int[,] {{7,7,0},{0,7,7}}  // Z
        };

        public View CreateScreen()
        {
            FontClient.Instance.AddCustomFontDirectory(_resPath + "fonts/");

            _rootView = new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0.05f, 0.05f, 0.08f, 1.0f) };

            // 1. LEFT SIDE: GAME BOARD 
            int boardWidth = COLS * BLOCK_W; // 840px
            int boardHeight = ROWS * BLOCK_H; // 920px
            int boardX = 60; // 60px margin from the left
            
            View boardBorder = new View()
            {
                Size = new Size(boardWidth + 16, boardHeight + 16),
                Position = new Position(boardX - 8, 70), 
                BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.9f),
                BorderlineWidth = 8.0f,
                BorderlineColor = new Color(0.35f, 1.0f, 0.0f, 1.0f), // Neon Green #59ff00
                CornerRadius = 16.0f
            };
            
            _boardContainer = new View()
            {
                Size = new Size(boardWidth, boardHeight),
                Position = new Position(8, 8),
                BackgroundColor = new Color(0.02f, 0.02f, 0.03f, 1.0f),
                ClippingMode = ClippingModeType.ClipToBoundingBox 
            };
            boardBorder.Add(_boardContainer);
            _rootView.Add(boardBorder);
            
            GenerateBlockGrid(_boardContainer, _gridBodies, _gridHighlights, _gridShadows, ROWS, COLS, BLOCK_W, BLOCK_H, _gridWrappers);

            _pauseOverlay = new View() {
                Size = new Size(boardWidth, boardHeight), Position = new Position(0,0),
                BackgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.8f)
            };
            TextLabel pauseTitle = new TextLabel("PAUSED") {
                Size = new Size(boardWidth, 100), Position = new Position(0, boardHeight / 2 - 50),
                TextColor = new Color(0.35f, 1.0f, 0.0f, 1.0f), PointSize = 64.0f, 
                HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont
            };
            _pauseOverlay.Add(pauseTitle);
            _boardContainer.Add(_pauseOverlay);
            _pauseOverlay.Hide();


            // 2. RIGHT SIDE: THE 3x3 DASHBOARD GRID
            int rightPaneX = 1020; // 1920 - 840 (width) - 60 (margin) = 1020
            int paneWidth = 840;

            // -- ROW 1: NEXT PIECE PANEL --
            View previewBoard = new View() {
                Size = new Size(paneWidth, 260), Position = new Position(rightPaneX, 70),
                BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.8f),
                CornerRadius = 24.0f, BorderlineWidth = 4.0f, BorderlineColor = new Color(1.0f, 1.0f, 1.0f, 0.2f)
            };
            TextLabel previewTitle = new TextLabel("NEXT") { 
                Size = new Size(paneWidth, 60), Position = new Position(0, 20), 
                TextColor = new Color(0.6f, 0.8f, 1.0f, 1.0f), PointSize = 28.0f, 
                HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont 
            };
            previewBoard.Add(previewTitle);
            
            int previewBlockSize = 40;
            // Center the 4x4 block grid inside the 840px panel (840 - 160) / 2 = 340
            View nextPieceContainer = new View() { Size = new Size(previewBlockSize * 4, previewBlockSize * 4), Position = new Position(340, 80), BackgroundColor = new Color(0,0,0,0) };
            GenerateBlockGrid(nextPieceContainer, _nextPieceBodies, _nextPieceHighlights, _nextPieceShadows, 4, 4, previewBlockSize, previewBlockSize);
            previewBoard.Add(nextPieceContainer);
            _rootView.Add(previewBoard);


            // -- ROW 2: MODULAR STAT BOXES (3 Columns) --
            int statY = 350;
            int statWidth = 260;
            int gap = 30; // (840 - (3 * 260)) / 2 = 30

            _rootView.Add(CreateStatBox("SCORE", "0", new Position(rightPaneX, statY), statWidth, new Color(1.0f, 0.8f, 0.2f, 1.0f), out _scoreLabel));
            _rootView.Add(CreateStatBox("LEVEL", "1", new Position(rightPaneX + statWidth + gap, statY), statWidth, new Color(0.0f, 0.9f, 1.0f, 1.0f), out _levelLabel));
            _rootView.Add(CreateStatBox("LINES", "0", new Position(rightPaneX + (statWidth * 2) + (gap * 2), statY), statWidth, new Color(0.1f, 0.9f, 0.4f, 1.0f), out _linesLabel));


            // -- ROW 3 & 4 (Merged): CONTROLS GUIDE --
            _rootView.Add(CreateControlsBox(new Position(rightPaneX, 540), new Size(paneWidth, 450)));


            _gameTimer = new Timer(600); 
            _gameTimer.Tick += (s, e) => { 
                if (!_isGameOver && !_isPopupActive && !_isClearingLines && !_isPaused) _ = MovePieceDownAsync(); 
                return true; 
            };

            CreatePopup();
            return _rootView;
        }

        private View CreateStatBox(string titleText, string initialVal, Position pos, int width, Color accentColor, out TextLabel valueLabel)
        {
            View box = new View() {
                Size = new Size(width, 160), Position = pos,
                BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.8f),
                CornerRadius = 16.0f, BorderlineWidth = 4.0f, BorderlineColor = accentColor
            };
            
            TextLabel title = new TextLabel(titleText) {
                Size = new Size(width, 50), Position = new Position(0, 25),
                TextColor = accentColor, PointSize = 20.0f,
                HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont
            };
            
            valueLabel = new TextLabel(initialVal) {
                Size = new Size(width, 70), Position = new Position(0, 75),
                TextColor = Color.White, PointSize = 36.0f, 
                HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont
            };
            
            box.Add(title);
            box.Add(valueLabel);
            return box;
        }

        private View CreateControlsBox(Position pos, Size size)
        {
            View box = new View() {
                Size = size, Position = pos,
                BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.8f),
                CornerRadius = 24.0f, BorderlineWidth = 4.0f, BorderlineColor = new Color(1.0f, 1.0f, 1.0f, 0.2f)
            };
            
            TextLabel title = new TextLabel("CONTROLS") {
                Size = new Size(size.Width, 50), Position = new Position(0, 20),
                TextColor = new Color(0.6f, 0.8f, 1.0f, 1.0f), PointSize = 28.0f,
                HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ArcadeFont
            };
            box.Add(title);

            // Left Column
            box.Add(CreateControlRow("◀ ▶", "MOVE", new Position(50, 120)));
            box.Add(CreateControlRow("▼ ", "DROP", new Position(50, 220)));
            
            // Right Column
            box.Add(CreateControlRow("OK", "ROTATE", new Position(450, 120)));
            box.Add(CreateControlRow("▲ ", "PLAY / PAUSE", new Position(450, 220)));

            // Centered Exit
            box.Add(CreateControlRow("↩ ", "EXIT GAME", new Position(250, 340)));

            return box;
        }

        private View CreateControlRow(string iconText, string desc, Position pos)
        {
            View container = new View() { Size = new Size(340, 70), Position = pos };
            
            View iconBg = new View() {
                Size = new Size(100, 60), Position = new Position(0, 5),
                BackgroundColor = new Color(0.2f, 0.25f, 0.35f, 1.0f),
                CornerRadius = 12.0f, BorderlineWidth = 2.0f, BorderlineColor = Color.White
            };
            TextLabel icon = new TextLabel(iconText) {
                Size = new Size(100, 60), TextColor = Color.White, PointSize = 20.0f,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                FontFamily = ArcadeFont
            };
            iconBg.Add(icon);
            container.Add(iconBg);

            TextLabel descLabel = new TextLabel(desc) {
                Size = new Size(220, 70), Position = new Position(120, 0),
                TextColor = new Color(0.8f, 0.8f, 0.8f, 1.0f), PointSize = 18.0f,
                VerticalAlignment = VerticalAlignment.Center, FontFamily = ArcadeFont
            };
            container.Add(descLabel);

            return container;
        }

        private void GenerateBlockGrid(View container, View[,] bodies, View[,] highlights, View[,] shadows, int rows, int cols, int blockW, int blockH, View[,] wrappers = null)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    View wrapper = new View() { Size = new Size(blockW, blockH), Position = new Position(c * blockW, r * blockH), PivotPoint = PivotPoint.Center, BackgroundColor = new Color(0,0,0,0) };
                    View body = new View() { Size = new Size(blockW - 2, blockH - 2), Position = new Position(1, 1), BackgroundColor = _colors[0], CornerRadius = 6.0f };
                    View shadow = new View() { Size = new Size(blockW - 12, blockH - 12), Position = new Position(10, 10), BackgroundColor = new Color(0, 0, 0, 0.3f), CornerRadius = 3.0f };
                    View highlight = new View() { Size = new Size(blockW - 12, blockH - 12), Position = new Position(2, 2), BackgroundColor = new Color(1, 1, 1, 0.4f), CornerRadius = 3.0f };

                    shadow.Hide(); highlight.Hide(); body.Hide();

                    body.Add(shadow); body.Add(highlight);
                    wrapper.Add(body);
                    container.Add(wrapper);

                    bodies[r, c] = body; highlights[r, c] = highlight; shadows[r, c] = shadow;
                    if (wrappers != null) wrappers[r, c] = wrapper;
                }
            }
        }

        private void CreateExplosion(int r, int c, Color color)
        {
            int startX = c * BLOCK_W + (BLOCK_W / 2);
            int startY = r * BLOCK_H + (BLOCK_H / 2);

            for (int i = 0; i < 8; i++) 
            {
                View particle = new View() {
                    Size = new Size(12, 12),
                    Position = new Position(startX, startY),
                    BackgroundColor = color,
                    CornerRadius = 2.0f,
                    PivotPoint = PivotPoint.Center
                };
                _boardContainer.Add(particle);

                int endX = startX + _rand.Next(-100, 100);
                int endY = startY + _rand.Next(-100, 100);

                var anim = new Animation(400); 
                anim.AnimateTo(particle, "Position", new Position(endX, endY));
                anim.AnimateTo(particle, "Opacity", 0.0f);
                anim.AnimateTo(particle, "Orientation", new Rotation(new Radian(new Degree(_rand.Next(180, 540))), PositionAxis.Z));
                
                anim.Finished += (s, e) => {
                    _boardContainer.Remove(particle);
                    particle.Dispose();
                };
                anim.Play();
            }
        }

        private void CreatePopup()
        {
            _popupOverlay = new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0, 0, 0, 0.85f) };
            View dialogBox = new View() { Size = new Size(800, 600), Position = new Position(560, 240), BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.95f), CornerRadius = 32.0f, BorderlineWidth = 4.0f, BorderlineColor = new Color(1.0f, 1.0f, 1.0f, 0.3f) };
            TextLabel title = new TextLabel("GAME OVER") { Size = new Size(800, 120), Position = new Position(0, 30), TextColor = new Color(0.9f, 0.2f, 0.2f, 1.0f), PointSize = 48.0f, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontFamily = ArcadeFont };
            dialogBox.Add(title);

            string[] btnTexts = { "RETRY TETRIS", "PLAY MEMORY", "EXIT MENU" };

            for (int i = 0; i < 3; i++)
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
            
            _isPaused = false;
            _pauseOverlay.Hide();

            _board = new int[ROWS, COLS];
            _score = 0;
            _lines = 0;
            _level = 1;
            _isGameOver = false;
            _isClearingLines = false;
            _nextPieceType = _rand.Next(1, 8);
            
            UpdateStats();
            SpawnPiece();
            _gameTimer.Start();
        }

        public void StopGame() { _gameTimer?.Stop(); }

        private void TogglePause()
        {
            if (_isGameOver || _isPopupActive || _isClearingLines) return;

            _isPaused = !_isPaused;
            if (_isPaused)
            {
                _gameTimer.Stop();
                _pauseOverlay.Show();
                _pauseOverlay.RaiseToTop();
            }
            else
            {
                _pauseOverlay.Hide();
                _gameTimer.Start();
            }
        }

        private void SpawnPiece()
        {
            _pieceType = _nextPieceType;
            _nextPieceType = _rand.Next(1, 8); 
            
            _currentPiece = (int[,])_shapes[_pieceType].Clone();
            _pieceRow = 0;
            _pieceCol = (COLS / 2) - (_currentPiece.GetLength(1) / 2);

            UpdateNextPieceUI();

            if (!IsValidMove(_currentPiece, _pieceRow, _pieceCol))
            {
                _isGameOver = true;
                DrawBoard(); 
                ShowGameOverPopup();
            }
            else
            {
                DrawBoard();
            }
        }

        private void UpdateNextPieceUI()
        {
            for (int r = 0; r < 4; r++) {
                for (int c = 0; c < 4; c++) {
                    _nextPieceBodies[r, c].Hide();
                    _nextPieceHighlights[r, c].Hide();
                    _nextPieceShadows[r, c].Hide();
                }
            }

            int[,] shape = _shapes[_nextPieceType];
            int h = shape.GetLength(0), w = shape.GetLength(1);
            int startR = (4 - h) / 2, startC = (4 - w) / 2;

            for (int r = 0; r < h; r++) {
                for (int c = 0; c < w; c++) {
                    if (shape[r, c] != 0) {
                        _nextPieceBodies[startR + r, startC + c].BackgroundColor = _colors[shape[r, c]];
                        _nextPieceBodies[startR + r, startC + c].Show();
                        _nextPieceHighlights[startR + r, startC + c].Show();
                        _nextPieceShadows[startR + r, startC + c].Show();
                    }
                }
            }
        }

        private void ShowGameOverPopup()
        {
            _gameTimer?.Stop();
            _isPopupActive = true;
            _popupSelectedIndex = 0;
            UpdatePopupFocus();
            _popupOverlay.Show();
            _popupOverlay.RaiseToTop();
        }

        private void UpdatePopupFocus()
        {
            for (int i = 0; i < 3; i++)
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
            else if (_popupSelectedIndex == 1) OnGoToMemoryGame?.Invoke();
            else if (_popupSelectedIndex == 2) OnExitToWelcome?.Invoke();
        }

        private bool IsValidMove(int[,] piece, int rOffset, int cOffset)
        {
            int h = piece.GetLength(0), w = piece.GetLength(1);
            for (int r = 0; r < h; r++)
            {
                for (int c = 0; c < w; c++)
                {
                    if (piece[r, c] == 0) continue; 
                    int newR = r + rOffset, newC = c + cOffset;
                    if (newC < 0 || newC >= COLS || newR >= ROWS) return false;
                    if (newR >= 0 && _board[newR, newC] != 0) return false;
                }
            }
            return true;
        }

        private async Task MovePieceDownAsync()
        {
            if (_isClearingLines || _isPaused) return; 

            if (IsValidMove(_currentPiece, _pieceRow + 1, _pieceCol)) 
            { 
                _pieceRow++; DrawBoard(); 
            }
            else 
            { 
                MergePiece(); 
                await ClearLinesAsync(); 
            }
        }

        private void MergePiece()
        {
            int h = _currentPiece.GetLength(0), w = _currentPiece.GetLength(1);
            for (int r = 0; r < h; r++)
            {
                for (int c = 0; c < w; c++)
                    if (_currentPiece[r, c] != 0 && _pieceRow + r >= 0) _board[_pieceRow + r, _pieceCol + c] = _currentPiece[r, c];
            }
        }

        private async Task ClearLinesAsync()
        {
            List<int> linesToClear = new List<int>();
            for (int r = 0; r < ROWS; r++)
            {
                bool isFull = true;
                for (int c = 0; c < COLS; c++) { if (_board[r, c] == 0) { isFull = false; break; } }
                if (isFull) linesToClear.Add(r);
            }

            if (linesToClear.Count > 0)
            {
                _isClearingLines = true;
                _gameTimer.Stop();

                foreach (int r in linesToClear)
                {
                    for (int c = 0; c < COLS; c++)
                    {
                        if (_board[r, c] != 0)
                        {
                            Color pieceColor = _colors[_board[r, c]];
                            CreateExplosion(r, c, pieceColor); 
                        }
                        
                        _gridBodies[r, c].Hide();
                        _gridHighlights[r, c].Hide();
                        _gridShadows[r, c].Hide();
                    }
                }
                
                await Task.Delay(400); 

                foreach (int r in linesToClear)
                {
                    for (int moveR = r; moveR > 0; moveR--)
                        for (int c = 0; c < COLS; c++) _board[moveR, c] = _board[moveR - 1, c];
                    for (int c = 0; c < COLS; c++) _board[0, c] = 0;
                }

                int count = linesToClear.Count;
                _score += (count * 100) * count; 
                _lines += count;
                _level = (_lines / 5) + 1; 
                
                _gameTimer.Interval = (uint)Math.Max(150, 600 - ((_level - 1) * 70));

                UpdateStats();
                DrawBoard(); 
                
                _isClearingLines = false;
                SpawnPiece();
                if (!_isPaused) _gameTimer.Start();
            }
            else
            {
                SpawnPiece();
            }
        }

        private void RotatePiece()
        {
            int h = _currentPiece.GetLength(0), w = _currentPiece.GetLength(1);
            int[,] rotated = new int[w, h];
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++) rotated[c, h - 1 - r] = _currentPiece[r, c];
            if (IsValidMove(rotated, _pieceRow, _pieceCol)) { _currentPiece = rotated; DrawBoard(); }
        }

        private void DrawBoard()
        {
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    int cellValue = _board[r, c];
                    
                    if (cellValue == 0) 
                    { 
                        _gridBodies[r, c].Hide();
                        _gridHighlights[r, c].Hide(); 
                        _gridShadows[r, c].Hide(); 
                    }
                    else 
                    { 
                        _gridBodies[r, c].BackgroundColor = _colors[cellValue];
                        _gridBodies[r, c].Show();
                        _gridHighlights[r, c].Show(); 
                        _gridShadows[r, c].Show(); 
                    }
                }
            }

            if (!_isGameOver)
            {
                int h = _currentPiece.GetLength(0), w = _currentPiece.GetLength(1);
                for (int r = 0; r < h; r++)
                {
                    for (int c = 0; c < w; c++)
                    {
                        if (_currentPiece[r, c] != 0 && _pieceRow + r >= 0)
                        {
                            _gridBodies[_pieceRow + r, _pieceCol + c].BackgroundColor = _colors[_currentPiece[r, c]];
                            _gridBodies[_pieceRow + r, _pieceCol + c].Show();
                            _gridHighlights[_pieceRow + r, _pieceCol + c].Show(); 
                            _gridShadows[_pieceRow + r, _pieceCol + c].Show(); 
                        }
                    }
                }
            }
        }

        private void UpdateStats() 
        { 
            _scoreLabel.Text = _score.ToString(); 
            _levelLabel.Text = _level.ToString();
            _linesLabel.Text = _lines.ToString();
        }

        public async Task HandleKeyEvent(string key)
        {
            if (_isPopupActive)
            {
                if (key == "Up") { _popupSelectedIndex = Math.Max(0, _popupSelectedIndex - 1); UpdatePopupFocus(); }
                else if (key == "Down") { _popupSelectedIndex = Math.Min(2, _popupSelectedIndex + 1); UpdatePopupFocus(); }
                else if (key == "space" || key == "Select" || key == "Return" || key == "DPAD_CENTER") ExecutePopupAction();
                return;
            }

            if (key == "XF86Back" || key == "Escape") { StopGame(); OnExitToWelcome?.Invoke(); return; }
            
            // --- NEW CONTROLS ---
            if (key == "Up") 
            { 
                TogglePause(); 
                return; 
            }

            if (_isGameOver || _isClearingLines || _isPaused) return; 

            if (key == "Left" && IsValidMove(_currentPiece, _pieceRow, _pieceCol - 1)) { _pieceCol--; DrawBoard(); }
            else if (key == "Right" && IsValidMove(_currentPiece, _pieceRow, _pieceCol + 1)) { _pieceCol++; DrawBoard(); }
            else if (key == "Down") await MovePieceDownAsync(); 
            else if (key == "Select" || key == "Return" || key == "DPAD_CENTER" || key == "space") RotatePiece(); // OK Button is now Rotate!
        }
    }
}