using Godot;
using System;

[GlobalClass]
public partial class EnterableAreaInteractable : Interactable {

    [Export]
    public StringName EnterableAreaScene;

    [Export]
    public Vector3 ExitPosition;

    protected override void Interact() {
        _player.CurrentState = Player.PlayerState.EnteringArea;
        _player.PlayerAreaToEnter = GD.Load<PackedScene>(EnterableAreaScene);

        Global.Instance.TransitionExitPosition = ExitPosition;
        _player = null;
    }
}