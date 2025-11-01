using Godot;
using System;
using Godot.Collections;


[GlobalClass]
public partial class PickupInteractable : Interactable, ISavable {

    [Export]
    public Texture2D IconTexture;

    [Export]
    public string itemName = "";

    public bool alreadyPickedUp = false;

    public override void _Ready() {
        base._Ready();
        AddToGroup("Savable");
    }

    protected override void Interact() {
        _player.PickupItem(itemName, IconTexture);
        if (GetTree().CurrentScene is Level level) {
            alreadyPickedUp = true;
            SaveSystem.Instance.SaveGame();
            Global.Instance.LastSavedScenePath = GetTree().CurrentScene.SceneFilePath;
            Global.Instance.PlayerLastSavedTransform = _player.GlobalTransform;
            Global.Instance.PlayerCameraLastSavedTransform = _player.Camera.GlobalTransform;
            Global.Instance.PlayerHasTakenTransform = true;
            Global.Instance.SaveProgressData();
        }
        QueueFree();
    }

    public String GetSaveID() {
        return GetPath();
    }

    public Dictionary<string, Variant> SaveState() {
        var data = new Dictionary<string, Variant> { };
        data["already_picked_up"] = alreadyPickedUp;
        return data;
    }

    public void LoadState(Dictionary<string, Variant> data) {
        if (data.ContainsKey("already_picked_up")) {
            alreadyPickedUp = (bool)data["already_picked_up"];
            if (alreadyPickedUp) {
                QueueFree();
            }
        }
    }
}