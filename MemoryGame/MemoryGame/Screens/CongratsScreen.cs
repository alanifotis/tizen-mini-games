// Screens/CongratsScreen.cs
using System;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace MemoryGame.Screens
{
    public class CongratsScreen
    {
        public Action OnProceedToTetris { get; set; }
        private View _rootView;

        public View CreateScreen()
        {
            _rootView = new View()
            {
                Size = new Size(1920, 1080),
                BackgroundColor = new Color(0.1f, 0.1f, 0.15f, 1.0f)
            };

            // Success Message
            TextLabel title = new TextLabel("🎉 LEVEL 1 COMPLETE! 🎉")
            {
                Size = new Size(1920, 100),
                Position = new Position(0, 400),
                TextColor = new Color(1.0f, 0.85f, 0.2f, 1.0f), // Gold
                PointSize = 64.0f,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _rootView.Add(title);

            TextLabel subtitle = new TextLabel("You have unlocked the Secret Tetris Level.\nPress OK / Select to begin.")
            {
                Size = new Size(1920, 100),
                Position = new Position(0, 520),
                TextColor = Color.White,
                PointSize = 32.0f,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _rootView.Add(subtitle);

            return _rootView;
        }

        public void HandleKeyEvent(string key)
        {
            if (key == "Return" || key == "Select" || key == "space" || key == "DPAD_CENTER")
            {
                OnProceedToTetris?.Invoke();
            }
        }
    }
}