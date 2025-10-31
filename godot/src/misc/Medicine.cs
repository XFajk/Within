using Godot;
using System;

public partial class Medicine : BasicPhysicsObject {
    private PickupInteractable pickupInteractable;

    public override void _Ready() {
        base._Ready();
        pickupInteractable = GetNode<PickupInteractable>("PickupInteractable");
    }

    public override void _Process(double delta) {
        if (!IsInstanceValid(pickupInteractable)) {
            QueueFree();
        } 
    }

}
