// Screens/WelcomeScreen.cs
using System;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;

namespace MemoryGame.Screens
{
    public class WelcomeScreen
    {
        public Action<bool> OnStartGame { get; set; }
        public Action OnExit { get; set; }

        private View _rootView;
        private ImageView _background;
        private View _ohanaButton;
        private View _alexButton;
        private bool _isOhanaSelected = true;

        // Base Styling Colors
        private readonly Color _ohanaBaseColor = new Color(1.0f, 0.4f, 0.7f, 0.4f); // Transparent Pink
        private readonly Color _alexBaseColor = new Color(0.2f, 0.5f, 1.0f, 0.4f);  // Transparent Blue
        private readonly Color _focusedColor = new Color(1.0f, 1.0f, 1.0f, 0.65f);  // White with 0.65 Alpha
        
        // Updated Border Colors
        private readonly Color _focusedBorderColor = new Color(1.0f, 1.0f, 1.0f, 0.70f);  // White with 0.70 Alpha
        private readonly Color _unfocusedBorderColor = new Color(1.0f, 1.0f, 1.0f, 0.20f);  // Dim White

        // Local File Paths
        private string _resPath => Application.Current.DirectoryInfo.Resource;
        private string _ohanaBgUrl => _resPath + "ohana_bg.jpg";
        private string _alexBgUrl => _resPath + "alex_bg.jpg";

        public View CreateScreen()
        {
            _rootView = new View() { Size = new Size(1920, 1080) };

            // 1. Dynamic Full-Screen Background
            _background = new ImageView()
            {
                Size = new Size(1920, 1080),
                FittingMode = FittingModeType.ScaleToFill
            };
            _rootView.Add(_background);

            // Dark overlay to maintain text readability against complex wallpapers
            _rootView.Add(new View() { Size = new Size(1920, 1080), BackgroundColor = new Color(0, 0, 0, 0.4f) });

            // 2. Buttons Split Bottom-Left and Bottom-Right
            // Screen width is 1920, Screen height is 1080.
            // Y=800 leaves an 80px margin at the bottom (800 + 200 height = 1000).
            _ohanaButton = CreateButton("Ohana", new Position(180, 800), _ohanaBaseColor);
            _alexButton = CreateButton("Alex", new Position(1140, 800), _alexBaseColor);

            _rootView.Add(_ohanaButton);
            _rootView.Add(_alexButton);

            UpdateSelection();
            return _rootView;
        }

        private View CreateButton(string text, Position position, Color defaultBgColor)
        {
            View btn = new View()
            {
                Size = new Size(600, 200),
                Position = position,
                BackgroundColor = defaultBgColor,
                CornerRadius = 16.0f,
                BorderlineWidth = 6.0f
            };

            TextLabel label = new TextLabel(text)
            {
                Size = new Size(600, 200),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = Color.White,
                PointSize = 64.0f
            };
            btn.Add(label);
            return btn;
        }

        public void HandleKeyEvent(string key)
        {
            if (key == "Left" || key == "Right")
            {
                _isOhanaSelected = !_isOhanaSelected;
                UpdateSelection();
            }
            else if (key == "Return" || key == "Select" || key == "space" || key == "DPAD_CENTER")
            {
                OnStartGame?.Invoke(_isOhanaSelected);
            }
            else if (key == "XF86Back" || key == "Escape")
            {
                OnExit?.Invoke();
            }
        }

        private void UpdateSelection()
        {
            // Update local background image
            _background.ResourceUrl = _isOhanaSelected ? _ohanaBgUrl : _alexBgUrl;

            // Apply focus states
            if (_isOhanaSelected)
            {
                _ohanaButton.BackgroundColor = _focusedColor;
                _ohanaButton.BorderlineColor = _focusedBorderColor;

                _alexButton.BackgroundColor = _alexBaseColor;
                _alexButton.BorderlineColor = _unfocusedBorderColor;
            }
            else
            {
                _alexButton.BackgroundColor = _focusedColor;
                _alexButton.BorderlineColor = _focusedBorderColor;

                _ohanaButton.BackgroundColor = _ohanaBaseColor;
                _ohanaButton.BorderlineColor = _unfocusedBorderColor;
            }
        }
    }
}