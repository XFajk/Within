using Godot;
using System;

public partial class AbilityTutorial : Control {
    [Signal]
    public delegate void TutorialCompletedEventHandler();

    private float _delay = 0.0f;

    public override void _Process(double delta) {
        if (!Visible) return;
        _delay += (float)delta;
        if (_delay < 1f) return;
        if (Input.IsAnythingPressed()) {
            EmitSignal(nameof(TutorialCompleted));
        }
    }

}
