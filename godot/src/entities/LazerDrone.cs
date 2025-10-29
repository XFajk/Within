using Godot;
using System;
using System.Collections.Generic;

public partial class LazerDrone : Node3D {
    public Vector3 StartPosition = Vector3.Zero;
    public float DepthLayer = 0f;

    public HashSet<string> Connections = new();

    public override void _Ready() {
        StartPosition = GlobalPosition;
    }

    public void Goto(Vector2 targetPosition, float depthAdjustmentTime = 0.5f, float travelTime = 1.0f) {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "global_position", new Vector3(GlobalPosition.X, GlobalPosition.Y, DepthLayer), depthAdjustmentTime);
        tween.TweenProperty(this, "global_position", new Vector3(targetPosition.X, targetPosition.Y, DepthLayer), travelTime);
        tween.TweenProperty(this, "global_position", new Vector3(targetPosition.X, targetPosition.Y, 0.0f), depthAdjustmentTime);
    }

    public void ReturnToStart(float depthAdjustmentTime = 0.5f, float travelTime = 1.0f) {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "global_position", new Vector3(GlobalPosition.X, GlobalPosition.Y, DepthLayer), depthAdjustmentTime);
        tween.TweenProperty(this, "global_position", new Vector3(StartPosition.X, StartPosition.Y, DepthLayer), travelTime);
        tween.TweenProperty(this, "global_position", new Vector3(StartPosition.X, StartPosition.Y, 0.0f), depthAdjustmentTime);
    }

    public bool ConnectToNotherDrone(LazerDrone anotherDorne) {
        if (Name == anotherDorne.Name) return false; // cannot connect to self
        if (Connections.Contains(anotherDorne.Name) && anotherDorne.Connections.Contains(Name)) return false; // already connected

        // add connection both ways
        if (!Connections.Contains(anotherDorne.Name)) Connections.Add(anotherDorne.Name);
        if (!anotherDorne.Connections.Contains(Name)) anotherDorne.Connections.Add(Name);

        return true;
    }
}
