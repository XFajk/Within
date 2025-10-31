using Godot;
using System;

public partial class MrKenstn : DialogInteractable, ISavable {
    [Export]
    public Texture2D IconTexture;

    public string itemName = "Laboratory Password";

    public override void _Ready() {
        base._Ready();
        DialogEnded += OnDialogEnded;
        AddToGroup("Savable");
    }

    private void OnDialogEnded() {
        if (CurrentDialogIndex == 1 && !_player.HasItem(itemName)) {
            _player.PickupItem(itemName, IconTexture);
            if (GetTree().CurrentScene is Level level) {
                SaveSystem.Instance.SaveGame();
                Global.Instance.LastSavedScenePath = GetTree().CurrentScene.SceneFilePath;
                Global.Instance.PlayerLastSavedTransform = _player.GlobalTransform;
                Global.Instance.PlayerHasTakenTransform = true;
                Global.Instance.SaveProgressData();
            }
        }
    }

    protected override void Interact() {
        if (_player.HasItem("Medicine") && CurrentDialogIndex == 0) {
            CurrentDialogIndex = 1;
        } else if (_player.HasItem("Medicine") && CurrentDialogIndex == 1) {
            CurrentDialogIndex = 2;
        }
        base.Interact();
    }

    public string GetSaveID() {
        return GetPath();
    }

    public Godot.Collections.Dictionary<string, Variant> SaveState() {
        var data = new Godot.Collections.Dictionary<string, Variant> { };
        data["CurrentDialogIndex"] = CurrentDialogIndex;
        return data;
    }

    public void LoadState(Godot.Collections.Dictionary<string, Variant> data) {
        if (data.ContainsKey("CurrentDialogIndex")) {
            CurrentDialogIndex = (int)data["CurrentDialogIndex"];
        }
    }
}