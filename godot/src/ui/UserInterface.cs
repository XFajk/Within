using Godot;
using System;

public partial class UserInterface : Control {
    public override void _Ready() {
        if (Global.Instance.IsInMainMenu) {
            this.Visible = false;
        } else {
            this.Visible = true;
        }
    }
}
