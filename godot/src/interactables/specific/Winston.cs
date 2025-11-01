using Godot;
using System;

public partial class Winston : DialogInteractable {

    public override void _Ready() {
        base._Ready();
        AddToGroup("Savable");
    }

    protected override void Interact() {
        if (_player.HasItem("Medicine")) {
            CurrentDialogIndex = 1;
        } else if (_player.HasItem("Laboratory Password") && CurrentDialogIndex == 1) {
            CurrentDialogIndex = 2;
        } else if (_player.HasItem("Laboratory Password") && CurrentDialogIndex == 0) {
            CurrentDialogIndex = 3;
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
