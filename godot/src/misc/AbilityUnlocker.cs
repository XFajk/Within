using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class AbilityUnlocker : Area3D, ISavable {

    public enum Ability {
        Dash,
        DoubleJump,
        WallJump,
        All,
    }

    [Export]
    public Ability AbilityToUnlock;

    public bool alreadyPickedUp = false;

    public override void _Ready() {
        CollisionLayer = 0;
        CollisionMask = 0;
        SetCollisionMaskValue(7, true);
        BodyEntered += OnBodyEntered;
        AddToGroup("Savable");
    }

    private void OnBodyEntered(Node body) {
        if (body is Player player) {
            player.UnlockAbility(AbilityToUnlock);
            if (GetTree().CurrentScene is Level level) {
                alreadyPickedUp = true;
                SaveSystem.Instance.SaveGame();
                Global.Instance.LastSavedScenePath = GetTree().CurrentScene.SceneFilePath;
                Global.Instance.PlayerLastSavedTransform = player.GlobalTransform;
                Global.Instance.PlayerHasTakenTransform = true;
                Global.Instance.SaveProgressData();
            }
            QueueFree();
        }
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
