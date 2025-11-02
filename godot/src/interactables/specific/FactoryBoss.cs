using Godot;
using System;

public partial class FactoryBoss : DialogInteractable {
    Player _seperatePlayerFeild;

    public override void _Ready() {
        _seperatePlayerFeild = GetTree().GetNodesInGroup("Player")[0] as Player;
        if (_seperatePlayerFeild.HasItem("Medicine") || _seperatePlayerFeild.HasItem("Laboratory Password")) {
            Node3D body = GetNode<Node3D>("boss4");
            body.GetNode<AnimationPlayer>("AnimationPlayer").Stop();
            body.RotationDegrees = new Vector3(-90, 0, 0);
            CurrentDialogIndex = 1;
            InteractionTitle = "Interact";
        }

        DialogEnded += () => {
            if (CurrentDialogIndex == 1) {
                _player.Inventory.Add("BossDeath");
            }
        };
        base._Ready();
    }


}
