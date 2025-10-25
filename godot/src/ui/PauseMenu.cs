using Godot;
using System;

public partial class PauseMenu : Control {
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _exitButton;

    public override void _Ready() {
        _resumeButton = GetNode<Button>("VBoxContainer/Resume");
        _settingsButton = GetNode<Button>("VBoxContainer/Settings");
        _exitButton = GetNode<Button>("VBoxContainer/Exit");

        _resumeButton.Pressed += OnResumeButtonPressed;
        _settingsButton.Pressed += OnSettingsButtonPressed;
        _exitButton.Pressed += OnExitButtonPressed;
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("ui_cancel") && !Global.Instance.IsInMainMenu) {
            if (!this.Visible) {
                Visible = true;
                Global.Instance.IsGamePaused = true;
                GetTree().Paused = true;
            } else {
                OnResumeButtonPressed();
            }
        }
    }

    private void OnResumeButtonPressed() {
        Global.Instance.IsGamePaused = false;
        GetTree().Paused = false;
        this.Visible = false;
    }

    private void OnSettingsButtonPressed() {
        var settingsMenu = GetNode<Control>("../Settings");
        settingsMenu.Visible = true;
        this.Visible = false;
    }


    private void OnExitButtonPressed() {
        GetTree().Quit();
    }

}
