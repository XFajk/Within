using Godot;
using System;

public partial class EntrenceToGroundOne : EnterableAreaInteractable {

    [Export]
    public string LockedTitle = "Locked Entrance";


    [Export]
    public string KeyItemName = "";

    public override void _Process(double delta) {
        if (_player != null && !_player.HasItem(KeyItemName)) {
            _interactionTitleLabel.Text = LockedTitle;
        } else {
            _interactionTitleLabel.Text = InteractionTitle;
        }

        base._Process(delta);
    }


    protected override void Interact() {
        if (_player.HasItem(KeyItemName)) {
            base.Interact();
        }
    }
}
