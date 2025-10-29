using Godot;
using System;

public partial class CameraAxis : Node3D {
    private Player _player;

    public override void _Ready() {
        _player = GetTree().GetNodesInGroup("Player")[0] as Player;
    }

    public override void _Process(double delta) {
       LookAt(_player.GlobalPosition, Vector3.Up); 
    }
}
