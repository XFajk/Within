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
    public bool PlayerHasTakenTransform = false;

    public bool PlayerHasDashAbility = false;
    public bool PlayerHasWallJumpAbility = false;
    public bool PlayerHasDoubleJumpAbility = false;


    public Vector3? MiniCheckPointSavedPosition = null;
    public Vector3? MiniCheckPointCameraSpawnPoint = null;

    public int PlayerLastSavedHealth = 3;
    public bool RespawningInProgress = false;


    public bool IsGamePaused = false;
    public bool IsInMainMenu = true;

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
            data["player_has_dash_ability"] = PlayerHasDashAbility;
            data["player_has_wall_jump_ability"] = PlayerHasWallJumpAbility;
            data["player_has_double_jump_ability"] = PlayerHasDoubleJumpAbility;

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
            if (loadedData.ContainsKey("player_has_dash_ability")) {
                PlayerHasDashAbility = (bool)loadedData["player_has_dash_ability"];
            }
            if (loadedData.ContainsKey("player_has_wall_jump_ability")) {
                PlayerHasWallJumpAbility = (bool)loadedData["player_has_wall_jump_ability"];
            }
            if (loadedData.ContainsKey("player_has_double_jump_ability")) {
                PlayerHasDoubleJumpAbility = (bool)loadedData["player_has_double_jump_ability"];
            }
        } else {
            SaveSystem.Instance.ResetGame();
        }

        GetTree().CallDeferred("change_scene_to_file", LastSavedScenePath);
    }

    public void ResetProgressData() {
        LastSavedScenePath = null;
        PlayerLastSavedTransform = null;
        PlayerHasTakenTransform = false;
        PlayerHasDashAbility = true;
        PlayerHasWallJumpAbility = true;
        PlayerHasDoubleJumpAbility = true;

        var file = FileAccess.Open($"user://progress.global.dat", FileAccess.ModeFlags.Write);
        if (file != null) {
            file.StoreString("");
            file.Close();
        }
    }
}