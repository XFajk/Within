using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class SaveInteractable : Interactable {

    public override void _Ready() {
        AreaEntered += (Area3D area) => {
            _interactionTitleLabel.Text = InteractionTitle;
        };
        base._Ready();
    }    

    protected override void Interact() {
        SaveSystem.Instance.SaveGame();
        Global.Instance.LastSavedScenePath = GetTree().CurrentScene.SceneFilePath;
        Global.Instance.PlayerLastSavedTransform = _player.GlobalTransform;
        Global.Instance.PlayerCameraLastSavedTransform = _player.Camera.GlobalTransform;
        Global.Instance.PlayerHasTakenTransform = true;
        Global.Instance.SaveProgressData();
        _player.EmitSignal(nameof(Player.Heal));
        _interactionTitleLabel.Text = "Game Saved!";
    }
}