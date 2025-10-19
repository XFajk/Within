using Godot;
using System;

[GlobalClass]
public abstract partial class Interactable : Area3D {
    protected Player _player;

    protected Label _interactionTitleLabel;

    [Export]
    public string InteractionTitle = "Interact";

    public override void _Ready() {
        AddToGroup("Interactable");
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;

        _interactionTitleLabel = GetNode<Label>("Title/SubViewport/TitleLabel");
        _interactionTitleLabel.Text = InteractionTitle;
        _interactionTitleLabel.Visible = false;
    }

    public override void _Process(double delta) {
        if (_player != null) {
            _interactionTitleLabel.Visible = true;
            if (Input.IsActionJustPressed("look_up")
                && _player.CurrentState != Player.PlayerState.NoControl
                && _player.CurrentState != Player.PlayerState.EnteringArea
                && _player.CurrentState != Player.PlayerState.ExitingArea)
                Interact();
        } else {
            _interactionTitleLabel.Visible = false;
        }
    }

    protected void OnAreaEntered(Area3D area) {
        if (area.IsInGroup("InteractableCollider")) {
            _player = area.GetParent<Player>();
        }
    }

    protected void OnAreaExited(Area3D area) {
        if (area.IsInGroup("InteractableCollider")) {
            _player = null;
        }
    }

    protected abstract void Interact();
}
