using Godot;
using System;

public partial class HitSoundsCleanUp : AudioStreamPlayer3D {
    public override void _Ready() {
        Finished += () => {
            QueueFree();
        };
    }
}
