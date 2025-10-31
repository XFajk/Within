using Godot;
using System;

public partial class EntrenceToMetro : EnterableAreaInteractable {
    [Export]
    public string LockedTitle = "Locked Entrance";


    [Export]
    public AbilityUnlocker.Ability Ability = AbilityUnlocker.Ability.WallJump;

    public override void _Process(double delta) {
        if (_player != null && !PlayerHasAbility(Ability)) {
            _interactionTitleLabel.Text = LockedTitle;
        } else {
            _interactionTitleLabel.Text = InteractionTitle;
        }

        base._Process(delta);
    }


    protected override void Interact() {
        if (PlayerHasAbility(Ability)) {
            base.Interact();
        }
    }

    private bool PlayerHasAbility(AbilityUnlocker.Ability ability) {
        switch (ability) {
            case AbilityUnlocker.Ability.WallJump:
                return _player.CanWallJump;
            case AbilityUnlocker.Ability.Dash:
                return _player.CanDash;
            case AbilityUnlocker.Ability.DoubleJump:
                return _player.CanDoubleJump;
            case AbilityUnlocker.Ability.All:
                return _player.CanWallJump && _player.CanDash && _player.CanDoubleJump;
            default:
                return false;
        }
    }
}
