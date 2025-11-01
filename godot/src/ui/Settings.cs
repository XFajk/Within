using Godot;
using System;

public partial class Settings : Control {

    public enum WindowMode {
        Windowed = 0,
        Fullscreen = 1,
    }

    private DisplayServer.WindowMode _gdWindowMode() {
        return _windowMode switch {
            WindowMode.Windowed => DisplayServer.WindowMode.Windowed,
            WindowMode.Fullscreen => DisplayServer.WindowMode.ExclusiveFullscreen,
            _ => DisplayServer.WindowMode.Windowed,
        };
    }

    private Control _mainMenu;
    private Control _pauseMenu;


    private float _soundFxVolume;
    private float _musicVolume;

    private bool _vsyncEnabled;
    private int _fpsLimit;
    private WindowMode _windowMode;
    private bool _impactFrameEnabled;


    private Button _backButton;

    private HSlider _soundFxSlider;
    private HSlider _musicSlider;

    private CheckButton _vsyncCheckButton;
    private HSlider _fpsLimitSlider;
    private Label _fpsLimitLabel;

    private OptionButton _windowModeOptionButton;
    private CheckButton _impactFrameCheckButton;

    private const string SETTINGS_FILE = "user://settings.cfg";
    private ConfigFile _config;

    private void SaveSettings() {
        _config = new ConfigFile();

        _config.SetValue("sound", "fx_volume", _soundFxVolume);
        _config.SetValue("sound", "music_volume", _musicVolume);
        _config.SetValue("graphics", "vsync", _vsyncEnabled);
        _config.SetValue("graphics", "fps_limit", _fpsLimit);
        _config.SetValue("window", "mode", (int)_windowMode);
        _config.SetValue("gameplay", "impact_frame", _impactFrameEnabled);

        // Get the absolute path for debugging
        string absolutePath = ProjectSettings.GlobalizePath(SETTINGS_FILE);


        _config.Save(SETTINGS_FILE);

    }

    private void LoadSettings() {
        _config = new ConfigFile();

        // Get the absolute path for debugging
        string absolutePath = ProjectSettings.GlobalizePath(SETTINGS_FILE);

        Error error = _config.Load(SETTINGS_FILE);
        if (error == Error.Ok) {
            // Load sound settings
            _soundFxVolume = (float)_config.GetValue("sound", "fx_volume", 1.0f);
            _musicVolume = (float)_config.GetValue("sound", "music_volume", 1.0f);

            // Load graphics settings
            _vsyncEnabled = (bool)_config.GetValue("graphics", "vsync", true);
            _fpsLimit = (int)_config.GetValue("graphics", "fps_limit", 60);

            // Load window mode
            _windowMode = (WindowMode)(int)_config.GetValue("window", "mode", (int)WindowMode.Windowed);

            // Load gameplay settings
            _impactFrameEnabled = (bool)_config.GetValue("gameplay", "impact_frame", true);
        } else {
            // Set default values if file doesn't exist
            _soundFxVolume = 1.0f;
            _musicVolume = 1.0f;
            _vsyncEnabled = true;
            _fpsLimit = 60;
            _windowMode = WindowMode.Windowed;
            _impactFrameEnabled = true;
        }
    }

    public override void _Ready() {

        LoadSettings();

        _mainMenu = GetNode<Control>("../MainMenu");
        _pauseMenu = GetNode<Control>("../PauseMenu");

        _backButton = GetNode<Button>("Panel/Back");

        _soundFxSlider = GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/SoundSettings/SoundFxVolume");
        _musicSlider = GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/SoundSettings/MusicVolume");

        _vsyncCheckButton = GetNode<CheckButton>("Panel/ScrollContainer/VBoxContainer/GraphicalSettigs/VSync");
        _fpsLimitSlider = GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/GraphicalSettigs/FPSLimit");
        _fpsLimitLabel = GetNode<Label>("Panel/ScrollContainer/VBoxContainer/GraphicalSettigs/FPSLimit/Title");

        _windowModeOptionButton = GetNode<OptionButton>("Panel/ScrollContainer/VBoxContainer/WindowSettings/OptionButton");

        _backButton.Pressed += OnBackButtonPressed;

        _impactFrameCheckButton = GetNode<CheckButton>("Panel/ScrollContainer/VBoxContainer/WindowSettings/ImpactFrame");

        // Initialize settings values
        _soundFxSlider.Value = _soundFxVolume;
        _soundFxSlider.ValueChanged += (value) => {
            _soundFxVolume = (float)value;
            var soundFXBusIndex = AudioServer.GetBusIndex("SoundFX");
            AudioServer.SetBusVolumeLinear(soundFXBusIndex, _soundFxVolume);
            SaveSettings();
        };
        _musicSlider.Value = _musicVolume;
        _musicSlider.ValueChanged += (value) => {
            _musicVolume = (float)value;
            var musicBusIndex = AudioServer.GetBusIndex("Music");
            AudioServer.SetBusVolumeLinear(musicBusIndex, _musicVolume);
            SaveSettings();
        };

        _vsyncCheckButton.ButtonPressed = _vsyncEnabled;
        DisplayServer.WindowSetVsyncMode(_vsyncEnabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        _vsyncCheckButton.Toggled += (pressed) => {
            _vsyncEnabled = pressed;
            DisplayServer.WindowSetVsyncMode(_vsyncEnabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
            SaveSettings();
        };

        _impactFrameCheckButton.ButtonPressed = _impactFrameEnabled;
        Global.Instance.ImpactFrameEnabled = _impactFrameEnabled;
        _impactFrameCheckButton.Toggled += (pressed) => {
            _impactFrameEnabled = pressed;
            Global.Instance.ImpactFrameEnabled = _impactFrameEnabled;   
            SaveSettings();
        };

        _fpsLimitSlider.Value = _fpsLimit;
        Engine.MaxFps = _fpsLimit;
        _fpsLimitSlider.ValueChanged += (value) => {
            _fpsLimit = (int)value;
            _fpsLimitLabel.Text = $"FPS Limit: {(_fpsLimit == 0 ? "None" : _fpsLimit)}";
            Engine.MaxFps = _fpsLimit;
            SaveSettings();
        };

        _fpsLimitLabel.Text = $"FPS Limit: {(_fpsLimit == 0 ? "None" : _fpsLimit)}";

        int windowModeIndex = _windowModeOptionButton.GetItemIndex((int)_windowMode);
        _windowModeOptionButton.Selected = windowModeIndex;
        DisplayServer.WindowSetMode(_gdWindowMode());
        GD.Print("Initial window mode set to: " + DisplayServer.WindowGetMode() +" for mode " + _windowMode);
        _windowModeOptionButton.ItemSelected += (index) => {
            GD.Print("Selected window mode index: " + index);
            _windowMode = (WindowMode)index;
            DisplayServer.WindowSetMode(_gdWindowMode());
            SaveSettings();
        };

    }


    private void OnBackButtonPressed() {
        // Update settings values from UI
        _soundFxVolume = (float)_soundFxSlider.Value;
        _musicVolume = (float)_musicSlider.Value;

        _vsyncEnabled = _vsyncCheckButton.ButtonPressed;
        _fpsLimit = (int)_fpsLimitSlider.Value;
        _windowMode = (WindowMode)_windowModeOptionButton.Selected;

        // Save settings to file
        SaveSettings();

        // Return to main menu
        this.Visible = false;
        if (IsInstanceValid(_mainMenu))
            GetNode<Control>("../MainMenu").Visible = true;
        else if (IsInstanceValid(_pauseMenu))
            GetNode<Control>("../PauseMenu").Visible = true;
    }

}
