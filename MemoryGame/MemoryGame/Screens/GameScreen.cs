// Screens/GameScreen.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;
using MemoryGame.Models;
using MemoryGame.Utils;

namespace MemoryGame.Screens
{
    public class GameScreen
    {
        public Action OnExitToWelcome { get; set; }
        public Action OnProceedToTetris { get; set; }

        private View _rootView;
        private ImageView _background;
        private TextLabel _titleLabel;
        private TextLabel _statusLabel;
        
        private Card[,] cards;
        private List<View> _activeCardViews = new List<View>();
        
        private Card firstSelected = null;
        private Card secondSelected = null;
        private bool isProcessing = false;
        private int currentRow = 0;
        private int currentCol = 0;
        private bool _isOhanaActive = true;

        // --- POPUP SYSTEM ---
        private View _popupOverlay;
        private View[] _popupButtons = new View[3];
        private TextLabel[] _popupLabels = new TextLabel[3];
        private int _popupSelectedIndex = 0;
        private bool _isPopupActive = false;

        private string _currentCardBackImage;
        private string _currentImageDir;

        private readonly Color CardBackBg = new Color(0.18f, 0.20f, 0.28f, 1.0f);
        private readonly Color CardFrontBg = Color.White;
        private readonly Color CardMatchedBg = new Color(0.2f, 0.8f, 0.2f, 1.0f);
        private readonly Color CardMismatchBg = new Color(0.9f, 0.2f, 0.2f, 1.0f);
        private readonly Color FocusBorderColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        private string _resPath => Application.Current.DirectoryInfo.Resource;

        private readonly string[] _ohanaImagePool = new string[] {
            "barbie.jpg", "bean.png", "cindarella.jpg", "elsa.jpeg", 
            "flowers.jpg", "heart.jpeg", "hopps.webp", "judy.webp", 
            "moana.jpg", "mr.png", "shakira-pink.webp", "shakira-yellow.webp", "snow-white.jpg"
        };
        private readonly string[] _alexImagePool = new string[] {
            "chase.webp", "everest.jpg", "jerry.webp", "liberty.webp",
            "marshall.webp", "PAW_Patrol_Zuma_Running.webp", "rocky.webp",
            "rubble.webp", "ryder.jpg", "sky.webp", "tom.png"
        };

        public View CreateScreen()
        {
            _rootView = new View() { Size = new Size(1920, 1080) };
            _background = new ImageView() { Size = new Size(1920, 1080), FittingMode = FittingModeType.ScaleToFill };
            _rootView.Add(_background);
            _rootView.Add(new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0, 0, 0, 0.6f) });

            View titleBox = new View()
            {
                Size = new Size(800, 80), Position = new Position(560, 40), 
                BackgroundColor = new Color(0, 0, 0, 0.7f), CornerRadius = 40.0f, 
                BorderlineWidth = 4.0f, BorderlineColor = new Color(1.0f, 1.0f, 1.0f, 0.4f)
            };
            _titleLabel = new TextLabel("MEMORY JOURNEY")
            {
                Size = new Size(800, 80), TextColor = Color.White, PointSize = 36.0f,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            titleBox.Add(_titleLabel);
            _rootView.Add(titleBox);

            View statusBox = new View()
            {
                Size = new Size(1200, 60), Position = new Position(360, 980), 
                BackgroundColor = new Color(0, 0, 0, 0.5f), CornerRadius = 30.0f
            };
            _statusLabel = new TextLabel("Awaiting your first move...")
            {
                Size = new Size(1200, 60), TextColor = new Color(0.9f, 0.9f, 0.95f, 1.0f), PointSize = 26.0f,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            statusBox.Add(_statusLabel);
            _rootView.Add(statusBox);

            CreatePopup(); 

            return _rootView;
        }

        private void CreatePopup()
        {
            _popupOverlay = new View()
            {
                Size = new Size(1920, 1080),
                BackgroundColor = new Color(0, 0, 0, 0.85f) 
            };

            View dialogBox = new View()
            {
                Size = new Size(800, 600),
                Position = new Position(560, 240), 
                BackgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.95f),
                CornerRadius = 32.0f,
                BorderlineWidth = 4.0f,
                BorderlineColor = new Color(1.0f, 1.0f, 1.0f, 0.3f)
            };

            TextLabel title = new TextLabel("🎉 LEVEL UNLOCKED! 🎉")
            {
                Size = new Size(800, 120),
                Position = new Position(0, 30),
                TextColor = new Color(1.0f, 0.85f, 0.2f, 1.0f),
                PointSize = 48.0f,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dialogBox.Add(title);

            string[] btnTexts = { "CONTINUE TO TETRIS", "REPLAY MEMORY GAME", "EXIT TO MENU" };

            for (int i = 0; i < 3; i++)
            {
                _popupButtons[i] = new View()
                {
                    Size = new Size(600, 90),
                    Position = new Position(100, 180 + (i * 120)),
                    BackgroundColor = new Color(0.2f, 0.25f, 0.35f, 1.0f),
                    CornerRadius = 24.0f
                };

                _popupLabels[i] = new TextLabel(btnTexts[i])
                {
                    Size = new Size(600, 90),
                    TextColor = Color.White,
                    PointSize = 32.0f,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                _popupButtons[i].Add(_popupLabels[i]);
                dialogBox.Add(_popupButtons[i]);
            }

            _popupOverlay.Add(dialogBox);
            _rootView.Add(_popupOverlay);
            _popupOverlay.Hide();
        }

        public void StartNewGame(bool isOhanaSelected)
        {
            _isPopupActive = false;
            _popupOverlay.Hide();
            _isOhanaActive = isOhanaSelected;
            _background.ResourceUrl = isOhanaSelected ? _resPath + "ohana_bg.jpg" : _resPath + "alex_bg.jpg";
            _titleLabel.Text = isOhanaSelected ? "OHANA'S MEMORY JOURNEY" : "ALEX'S MEMORY JOURNEY";
            SetStatus("Use the D-Pad to explore. Press OK to reveal a memory.");

            string[] currentPool = isOhanaSelected ? _ohanaImagePool : _alexImagePool;
            _currentImageDir = isOhanaSelected ? "ohana_cards/" : "alex_cards/";
            _currentCardBackImage = isOhanaSelected ? _resPath + "ohana_cards/card_bg.jpeg" : _resPath + "alex_cards/card_bg.jpg";

            foreach (var view in _activeCardViews)
            {
                _rootView.Remove(view);
                view.Dispose();
            }
            _activeCardViews.Clear();

            var rand = new Random();
            string[] selectedImages = currentPool.OrderBy(x => rand.Next()).Take(6).ToArray();
            cards = new Card[3, 4];
            string[,] generatedBoard = BoardGenerator.GenerateBoard(selectedImages, 3, 4);

            int cardW = 300, cardH = 220, gapX = 120, gapY = 60, startX = 180, startY = 160; 
            int imgW = (int)(cardW * 0.9), imgH = (int)(cardH * 0.9);
            int imgX = (cardW - imgW) / 2, imgY = (cardH - imgH) / 2;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    View cardWrapper = new View() { Size = new Size(cardW, cardH), Position = new Position(startX + c * (cardW + gapX), startY + r * (cardH + gapY)), PivotPoint = PivotPoint.Center };
                    View focusBorder = new View() { Size = new Size(cardW + 16, cardH + 16), Position = new Position(-8, -8), BackgroundColor = new Color(0, 0, 0, 0), BorderlineWidth = 8.0f, BorderlineColor = FocusBorderColor, CornerRadius = 24.0f };
                    focusBorder.Hide();
                    cardWrapper.Add(focusBorder);

                    View cardBg = new View() { Size = new Size(cardW, cardH), BackgroundColor = CardBackBg, CornerRadius = 16.0f, ClippingMode = ClippingModeType.ClipToBoundingBox };
                    ImageView frontImg = new ImageView() { Size = new Size(imgW, imgH), Position = new Position(imgX, imgY), ResourceUrl = _resPath + _currentImageDir + generatedBoard[r, c], FittingMode = FittingModeType.ShrinkToFit };
                    frontImg.Hide();
                    ImageView backImg = new ImageView() { Size = new Size(imgW, imgH), Position = new Position(imgX, imgY), ResourceUrl = _currentCardBackImage, FittingMode = FittingModeType.Fill };
                    
                    cardBg.Add(frontImg); cardBg.Add(backImg); cardWrapper.Add(cardBg);
                    _rootView.Add(cardWrapper); _activeCardViews.Add(cardWrapper);

                    cards[r, c] = new Card() { Row = r, Col = c, ImageFilename = generatedBoard[r, c], Wrapper = cardWrapper, CardBackground = cardBg, FrontImage = frontImg, BackImage = backImg, FocusBorder = focusBorder };
                }
            }

            // Fixed syntax for pushing the overlay to the top layer
            _popupOverlay.RaiseToTop();

            currentRow = currentCol = 0;
            firstSelected = secondSelected = null;
            isProcessing = false;
            UpdateGameFocusVisuals();
        }

        public async Task HandleKeyEvent(string key)
        {
            if (_isPopupActive)
            {
                if (key == "Up") { _popupSelectedIndex = Math.Max(0, _popupSelectedIndex - 1); UpdatePopupFocus(); }
                else if (key == "Down") { _popupSelectedIndex = Math.Min(2, _popupSelectedIndex + 1); UpdatePopupFocus(); }
                else if (key == "Return" || key == "Select" || key == "space" || key == "DPAD_CENTER") ExecutePopupAction();
                return;
            }

            if (key == "Left") { currentCol = Math.Max(0, currentCol - 1); UpdateGameFocusVisuals(); }
            else if (key == "Right") { currentCol = Math.Min(3, currentCol + 1); UpdateGameFocusVisuals(); }
            else if (key == "Up") { currentRow = Math.Max(0, currentRow - 1); UpdateGameFocusVisuals(); }
            else if (key == "Down") { currentRow = Math.Min(2, currentRow + 1); UpdateGameFocusVisuals(); }
            else if (key == "Return" || key == "Select" || key == "space" || key == "DPAD_CENTER") await SelectCard(currentRow, currentCol);
            else if (key == "XF86Back" || key == "Escape") OnExitToWelcome?.Invoke();
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

            if (_popupSelectedIndex == 0) OnProceedToTetris?.Invoke();
            else if (_popupSelectedIndex == 1) StartNewGame(_isOhanaActive);
            else if (_popupSelectedIndex == 2) OnExitToWelcome?.Invoke();
        }

        private void UpdateGameFocusVisuals()
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (r == currentRow && c == currentCol) cards[r, c].FocusBorder.Show();
                    else cards[r, c].FocusBorder.Hide();
                }
            }
        }

        private async Task SelectCard(int row, int col)
        {
            if (isProcessing) return;
            Card card = cards[row, col];
            if (card.IsFlipped || card.IsMatched) return;

            isProcessing = true; card.IsFlipped = true;
            SetStatus("Revealing memory...");
            
            await CardAnimator.FlipCardAsync(card.Wrapper, card.BackImage, card.FrontImage, card.CardBackground, CardFrontBg);

            if (firstSelected == null)
            {
                firstSelected = card; isProcessing = false; SetStatus("Seek the matching memory...");
            }
            else if (secondSelected == null)
            {
                secondSelected = card;
                if (firstSelected.ImageFilename == secondSelected.ImageFilename)
                {
                    firstSelected.IsMatched = secondSelected.IsMatched = true;
                    firstSelected.CardBackground.BackgroundColor = secondSelected.CardBackground.BackgroundColor = CardMatchedBg;
                    ResetSelection(); SetStatus("A beautiful connection found!");
                    CheckWinCondition(); 
                }
                else
                {
                    SetStatus("Not quite... the memories are fading.");
                    firstSelected.CardBackground.BackgroundColor = secondSelected.CardBackground.BackgroundColor = CardMismatchBg;
                    await Task.Delay(1200);
                    var flipBack1 = CardAnimator.FlipCardAsync(firstSelected.Wrapper, firstSelected.FrontImage, firstSelected.BackImage, firstSelected.CardBackground, CardBackBg);
                    var flipBack2 = CardAnimator.FlipCardAsync(secondSelected.Wrapper, secondSelected.FrontImage, secondSelected.BackImage, secondSelected.CardBackground, CardBackBg);
                    await Task.WhenAll(flipBack1, flipBack2);
                    firstSelected.IsFlipped = secondSelected.IsFlipped = false;
                    ResetSelection(); SetStatus("Continue your journey. Press OK to reveal.");
                }
            }
        }

        private void ResetSelection() { firstSelected = secondSelected = null; isProcessing = false; }
        private void SetStatus(string text) { _statusLabel.Text = text; }

        private async void CheckWinCondition()
        {
            bool allMatched = cards.Cast<Card>().All(c => c.IsMatched);
            if (allMatched)
            {
                SetStatus("✨ GLORIOUS! ALL MEMORIES UNLOCKED! ✨");
                _statusLabel.TextColor = new Color(1.0f, 0.85f, 0.2f, 1.0f); 
                
                await Task.Delay(2000);
                
                _isPopupActive = true;
                _popupSelectedIndex = 0;
                UpdatePopupFocus();
                _popupOverlay.Show();
            }
        }
    }
}