using Godot;
using System;

public partial class MainMenu : Control {

    private Button _startButton;
    private Button _settingsButton;
    private Button _exitButton;

    private Control _settingsMenu;
    private Control _userInterface;

    private Player _player;
    private PlayerCamera _playerCamera;

    public override void _Ready() {

        _player = GetNode<Player>("../../");
        _playerCamera = GetNode<PlayerCamera>("../../Camera");

        if (!Global.Instance.IsInMainMenu) {
            QueueFree();
            return;
        }

        _startButton = GetNode<Button>("VBoxContainer/Start");
        _settingsButton = GetNode<Button>("VBoxContainer/Settings");
        _exitButton = GetNode<Button>("VBoxContainer/Exit");
        _settingsMenu = GetNode<Control>("../Settings");
        _userInterface = GetNode<Control>("../UserInterface");

        _startButton.Pressed += OnStartButtonPressed;
        _settingsButton.Pressed += OnSettingsButtonPressed;
        _exitButton.Pressed += OnExitButtonPressed;

        _settingsMenu.Visible = false;
    }

    public override void _Process(double delta) {
        if (_player.CurrentState == Player.PlayerState.Sleeping) {
            _playerCamera.GlobalPosition = _player.GlobalPosition + new Vector3(0.75f, 0.25f, 2);
        }
    }


    private void OnStartButtonPressed() {
        Global.Instance.IsInMainMenu = false;
        _userInterface.Visible = true;
        if (_player.CurrentState == Player.PlayerState.Sleeping) {
            _player.TransitionAnimationTo(Player.PlayerState.WakingUp);
            _player.CurrentState = Player.PlayerState.WakingUp;
            _player.WakeUpTimer.Start();
        } else {
            _player.TransitionAnimationTo(Player.PlayerState.Idle);
            _player.CurrentState = Player.PlayerState.Idle;
        }
        QueueFree();
    }

    private void OnSettingsButtonPressed() {
        _settingsMenu.Visible = true;
        this.Visible = false;
    }
    
    private void OnExitButtonPressed() {
        GetTree().Quit();
    }

}
