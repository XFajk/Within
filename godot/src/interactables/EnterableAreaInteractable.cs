using Godot;
using System;

[GlobalClass]
public partial class EnterableAreaInteractable : Interactable {

    [Export]
    public StringName EnterableAreaScene;
    
    protected override void Interact() {
        _player.CurrentState = Player.PlayerState.EnteringArea;
        _player.PlayerAreaToEnter = GD.Load<PackedScene>(EnterableAreaScene);
        _player = null;
    }
}