// Program.cs
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using MemoryGame.Screens;

namespace MemoryGame
{
    class Program : NUIApplication
    {
        private Window _window;
        
        // Screens
        private WelcomeScreen _welcomeScreen;
        private GameScreen _gameScreen;
        private TetrisScreen _tetrisScreen;
        private InvadersScreen _invadersScreen;
        private SnakeScreen _snakeScreen; // NEW
        
        // Views
        private View _welcomeView;
        private View _gameView;
        private View _tetrisView;
        private View _invadersView;
        private View _snakeView; // NEW

        private enum AppState { Welcome, Game, Tetris, Invaders, Snake }
        private AppState _currentState = AppState.Welcome;
        
        private string _cheatBuffer = "";
        private bool _lastProfileOhana = true; 

        protected override void OnCreate()
        {
            base.OnCreate();
            _window = GetDefaultWindow();
            _window.KeyEvent += OnKeyEvent;

            // Initialize Routers
            _welcomeScreen = new WelcomeScreen();
            _welcomeScreen.OnStartGame = SwitchToGameScreen;
            _welcomeScreen.OnExit = Exit;

            _gameScreen = new GameScreen();
            _gameScreen.OnExitToWelcome = SwitchToWelcomeScreen;
            _gameScreen.OnProceedToTetris = SwitchToTetrisScreen; 

            _tetrisScreen = new TetrisScreen();
            _tetrisScreen.OnExitToWelcome = SwitchToWelcomeScreen;
            _tetrisScreen.OnGoToMemoryGame = SwitchToGameScreenFromTetris; 

            _invadersScreen = new InvadersScreen();
            _invadersScreen.OnExitToWelcome = SwitchToWelcomeScreen;

            _snakeScreen = new SnakeScreen(); // NEW
            _snakeScreen.OnExitToWelcome = SwitchToWelcomeScreen;

            // Generate Views
            _welcomeView = _welcomeScreen.CreateScreen();
            _gameView = _gameScreen.CreateScreen();
            _tetrisView = _tetrisScreen.CreateScreen();
            _invadersView = _invadersScreen.CreateScreen();
            _snakeView = _snakeScreen.CreateScreen(); // NEW

            // Append to DOM
            _window.Add(_welcomeView);
            _window.Add(_gameView);
            _window.Add(_tetrisView);
            _window.Add(_invadersView);
            _window.Add(_snakeView); // NEW

            SwitchToWelcomeScreen();
        }

        private void HideAll()
        {
            _welcomeView.Hide();
            _gameView.Hide();
            _tetrisView.Hide();
            _invadersView.Hide();
            _snakeView.Hide(); // NEW
        }

        private void SwitchToWelcomeScreen()
        {
            _currentState = AppState.Welcome;
            HideAll();
            _welcomeView.Show();
        }

        private void SwitchToGameScreen(bool isOhanaSelected)
        {
            _lastProfileOhana = isOhanaSelected;
            _currentState = AppState.Game;
            _gameScreen.StartNewGame(isOhanaSelected); 
            HideAll();
            _gameView.Show();
        }

        private void SwitchToGameScreenFromTetris()
        {
            SwitchToGameScreen(_lastProfileOhana);
        }

        private void SwitchToTetrisScreen()
        {
            _currentState = AppState.Tetris;
            _tetrisScreen.StartGame();
            HideAll();
            _tetrisView.Show();
        }

        private void SwitchToInvadersScreen()
        {
            _currentState = AppState.Invaders;
            _invadersScreen.StartGame();
            HideAll();
            _invadersView.Show();
        }

        private void SwitchToSnakeScreen()
        {
            _currentState = AppState.Snake;
            _snakeScreen.StartGame();
            HideAll();
            _snakeView.Show();
        }

        private async void OnKeyEvent(object sender, Window.KeyEventArgs e)
        {
            if (e.Key.State != Key.StateType.Down) return;
            string key = e.Key.KeyPressedName;

            // --- THE 3 MASTER CHEAT CODES ---
            if (key == "1" || key == "2" || key == "3" || key == "4" || 
                key == "5" || key == "6" || key == "7" || key == "8" || key == "9")
            {
                _cheatBuffer += key;
                if (_cheatBuffer.EndsWith("1234"))
                {
                    _cheatBuffer = ""; SwitchToTetrisScreen(); return; 
                }
                if (_cheatBuffer.EndsWith("9876"))
                {
                    _cheatBuffer = ""; SwitchToInvadersScreen(); return; 
                }
                if (_cheatBuffer.EndsWith("5678")) // NEW SNAKE CHEAT
                {
                    _cheatBuffer = ""; SwitchToSnakeScreen(); return; 
                }
            }
            else
            {
                _cheatBuffer = ""; // Reset buffer if any normal D-Pad key is pressed
            }

            // Route standard controls
            switch (_currentState)
            {
                case AppState.Welcome:
                    _welcomeScreen.HandleKeyEvent(key);
                    break;
                case AppState.Game:
                    await _gameScreen.HandleKeyEvent(key);
                    break;
                case AppState.Tetris:
                    await _tetrisScreen.HandleKeyEvent(key);
                    break;
                case AppState.Invaders:
                    _invadersScreen.HandleKeyEvent(key);
                    break;
                case AppState.Snake:
                    _snakeScreen.HandleKeyEvent(key); // NEW
                    break;
            }
        }

        static void Main(string[] args)
        {
            IsUsingXaml = false;
            var app = new Program();
            app.Run(args);
        }
    }
}