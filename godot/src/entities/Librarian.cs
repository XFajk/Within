using Godot;
using System;

public partial class Librarian : PersonEnemy, ISavable{
    private BasicPhysicsObject _medicineDrop = (BasicPhysicsObject)GD.Load<PackedScene>("res://scenes/misc/medicine.tscn").Instantiate();

    public override void _Ready() {
        BasicEnemy = false;
        base._Ready();
        AddToGroup("Savable");
    }

    protected override void Die() {
        Vector3 HorizontalDirectionToPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
        _medicineDrop.InitialVelocity = new Vector3(HorizontalDirectionToPlayer.X / Math.Abs(HorizontalDirectionToPlayer.X) * -7f, 3f, 0f);
        AddSibling(_medicineDrop);
        _medicineDrop.GlobalPosition = GlobalPosition + new Vector3(HorizontalDirectionToPlayer.X / Math.Abs(HorizontalDirectionToPlayer.X) * -0.15f, 0.1f, 0f);
        base.Die();
    }

    public string GetSaveID() {
        return GetPath();
    }

    public Godot.Collections.Dictionary<string, Variant> SaveState() {
        if (CurrentState == PersonEnemyState.Dead) {
            GD.Print("Saving Librarian state at position: " + GlobalPosition);
            var state = new Godot.Collections.Dictionary<string, Variant>();
            state["GlobalPosition"] = GlobalPosition;
            return state;
        } else {
            GD.Print("Not saving Librarian state, as it is not dead.");
            return new Godot.Collections.Dictionary<string, Variant>();
        }
    }

    public void LoadState(Godot.Collections.Dictionary<string, Variant> state) {
        if (state.ContainsKey("GlobalPosition")) {
            GlobalPosition = (Vector3)state["GlobalPosition"];
            base.Die();
        }
    }

}