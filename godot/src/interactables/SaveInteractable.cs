using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class SaveInteractable : Interactable {

    protected override void Interact() {
        SaveSystem.Instance.SaveGame();
        Global.Instance.LastSavedScenePath = GetTree().CurrentScene.SceneFilePath;
        Global.Instance.PlayerLastSavedTransform = _player.GlobalTransform;
        Global.Instance.PlayerHasTakenTransform = true;
        Global.Instance.SaveProgressData();
        _player.EmitSignal(nameof(Player.Heal));
        _player = null;
    }
}