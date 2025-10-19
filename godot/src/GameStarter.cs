using Godot;
using System;

public partial class GameStarter : Node {
    public override void _Ready() {
        
        Global.Instance.LoadProgressData();
    }
}
