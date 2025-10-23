using Godot;
using System;

[GlobalClass]
public abstract partial class Interactable : Area3D {
    protected Player _player;

    protected Label3D _interactionTitleLabel;

    [Export]
    public string InteractionTitle = "Interact";

    public override void _Ready() {
        AddToGroup("Interactable");
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;

        _interactionTitleLabel = GetNode<Label3D>("Title");
        _interactionTitleLabel.Text = InteractionTitle;
        _interactionTitleLabel.Visible = false;
    }

    public override void _Process(double delta) {
        if (_player != null && _player.CurrentState == Player.PlayerState.Idle && Mathf.Abs(0.0f -_player.GetInputDirection().X ) < 0.1f) {
            _interactionTitleLabel.Visible = true;
            if (Input.IsActionJustPressed("interact"))
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
