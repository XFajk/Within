using Godot;
using System;
using Godot.Collections;

public partial class Global : Node {
    public static Global Instance { get; private set; }

    public override void _EnterTree() {
        Instance = this;
    }

    public string LastSavedScenePath = "res://scenes/levels/spawn_building.tscn";

    public Transform3D? PlayerLastSavedTransform = null;
    public Transform3D? PlayerCameraLastSavedTransform = null;

    public bool PlayerHasTakenTransform = false;

    public bool PlayerHasDashAbility = false;
    public bool PlayerHasWallJumpAbility = false;
    public bool PlayerHasDoubleJumpAbility = false;

    public Array<string> PlayerInventory = new Array<string>();


    public Vector3? MiniCheckPointSavedPosition = null;
    public Vector3? MiniCheckPointCameraSpawnPoint = null;

    public Vector3? TransitionExitPosition = null;

    public int PlayerLastSavedHealth = 3;
    public bool RespawningInProgress = false;


    public bool IsGamePaused = false;
    public bool IsInMainMenu = true;

    public bool ImpactFrameEnabled = true;

    public AudioStreamPlayer MusicPlayer = GD.Load<PackedScene>("res://scenes/music_player.tscn").Instantiate<AudioStreamPlayer>();
    private string _currentMusic = "GroundZero";

    public override void _Ready() {
        AddChild(MusicPlayer);
    }

    public void SwitchMusic(string musicName) {
        if (_currentMusic == musicName) {
            return;
        }
        _currentMusic = musicName;
        if (MusicPlayer.GetStreamPlayback() is AudioStreamPlaybackInteractive audioStreamPlayback)
            audioStreamPlayback.SwitchToClipByName(musicName);
    }

    public void SaveProgressData() {
        var data = new Dictionary<string, Variant> { };

        var file = FileAccess.Open($"user://progress.global.dat", FileAccess.ModeFlags.Write);
        if (file != null) {
            // Process data to save here
            if (LastSavedScenePath != null) {
                data["last_scene"] = LastSavedScenePath;
            }

            if (PlayerLastSavedTransform is Transform3D transform) {
                GD.Print("Saving player transform: " + transform);
                data["player_last_saved_transform"] = transform;
            }
            if (PlayerCameraLastSavedTransform is Transform3D cameraTransform) {
                GD.Print("Saving player camera transform: " + cameraTransform);
                data["player_camera_last_saved_transform"] = cameraTransform;
            }
            data["player_has_dash_ability"] = PlayerHasDashAbility;
            data["player_has_wall_jump_ability"] = PlayerHasWallJumpAbility;
            data["player_has_double_jump_ability"] = PlayerHasDoubleJumpAbility;
            data["player_inventory"] = PlayerInventory;

            file.StoreString(GD.VarToStr(data));
            file.Close();
        }

    }

    public void LoadProgressData() {
        var file = FileAccess.Open($"user://progress.global.dat", FileAccess.ModeFlags.Read);
        if (file != null) {
            string savedData = file.GetAsText();
            file.Close();

            var loadedData = (Dictionary<string, Variant>)GD.StrToVar(savedData);

            // Process loaded data here
            if (loadedData.ContainsKey("last_scene")) {
                LastSavedScenePath = (string)loadedData["last_scene"];
            }
            if (loadedData.ContainsKey("player_last_saved_transform")) {
                PlayerLastSavedTransform = (Transform3D)loadedData["player_last_saved_transform"];
            } else {
                SaveSystem.Instance.ResetGame();
            }
            if (loadedData.ContainsKey("player_camera_last_saved_transform")) {
                PlayerCameraLastSavedTransform = (Transform3D)loadedData["player_camera_last_saved_transform"];
            }
            if (loadedData.ContainsKey("player_has_dash_ability")) {
                PlayerHasDashAbility = (bool)loadedData["player_has_dash_ability"];
            }
            if (loadedData.ContainsKey("player_has_wall_jump_ability")) {
                PlayerHasWallJumpAbility = (bool)loadedData["player_has_wall_jump_ability"];
            }
            if (loadedData.ContainsKey("player_has_double_jump_ability")) {
                PlayerHasDoubleJumpAbility = (bool)loadedData["player_has_double_jump_ability"];
            }
            if (loadedData.ContainsKey("player_inventory")) {
                PlayerInventory = (Array<string>)loadedData["player_inventory"];
                GD.Print("Loaded player inventory with " + PlayerInventory + " items.");
            }
        } else {
            SaveSystem.Instance.ResetGame();
        }

        GetTree().CallDeferred("change_scene_to_file", LastSavedScenePath);
    }

    public void ResetProgressData() {
        LastSavedScenePath = "res://scenes/levels/spawn_building.tscn";
        PlayerLastSavedTransform = null;
        PlayerCameraLastSavedTransform = null;
        PlayerHasTakenTransform = false;
        PlayerHasDashAbility = false;
        PlayerHasWallJumpAbility = false;
        PlayerHasDoubleJumpAbility = false;
        PlayerInventory.Clear();
        MiniCheckPointSavedPosition = null;
        MiniCheckPointCameraSpawnPoint = null;
        TransitionExitPosition = null;
        PlayerLastSavedHealth = 3;
        RespawningInProgress = false;
        IsGamePaused = false;
        IsInMainMenu = true;
        ImpactFrameEnabled = true;
        _currentMusic = "GroundZero";

        var file = FileAccess.Open($"user://progress.global.dat", FileAccess.ModeFlags.Write);
        if (file != null) {
            file.StoreString("");
            file.Close();
        }
    }
}