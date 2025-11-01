using Godot;
using System;

public partial class PauseMenu : Control {
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _exitButton;

    private Control _settingsMenu;

    public override void _Ready() {
        _resumeButton = GetNode<Button>("VBoxContainer/Resume");
        _settingsButton = GetNode<Button>("VBoxContainer/Settings");
        _exitButton = GetNode<Button>("VBoxContainer/Exit");
        _settingsMenu = GetNode<Control>("../Settings");

        _resumeButton.Pressed += OnResumeButtonPressed;
        _settingsButton.Pressed += OnSettingsButtonPressed;
        _exitButton.Pressed += OnExitButtonPressed;
    }

    public override void _Process(double delta) {
        if (_settingsMenu.Visible) {
            return;
        }
        if (Input.IsActionJustPressed("ui_cancel") && !Global.Instance.IsInMainMenu) {
            if (!this.Visible) {
                var masterBusIndex = AudioServer.GetBusIndex("Music&SoundFX");
                var lowPassFilterEffect = AudioServer.GetBusEffect(masterBusIndex, 0) as AudioEffectLowPassFilter;

                lowPassFilterEffect.CutoffHz /= 30.0f;

                Visible = true;
                Global.Instance.IsGamePaused = true;
                GetTree().Paused = true;
            } else {
                var masterBusIndex = AudioServer.GetBusIndex("Music&SoundFX");
                var lowPassFilterEffect = AudioServer.GetBusEffect(masterBusIndex, 0) as AudioEffectLowPassFilter;

                lowPassFilterEffect.CutoffHz *= 30.0f;
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
        _settingsMenu.Visible = true;
        this.Visible = false;
    }


    private void OnExitButtonPressed() {
        GetTree().Quit();
    }

}
