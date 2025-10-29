using Godot;
using Godot.Collections;
using System;

public partial class LazerDroneManager : Node3D {

    private bool _isActive = false;

    private PackedScene _laserScene = GD.Load<PackedScene>("res://scenes/environmental_hazards/laser.tscn");

    private Array<LazerDrone> _lazerDrones = new();
    private Node3D _doorHandle;
    private Array<Array<Vector2>> _dronePositions = new();


    private Area3D _activationArea;
    private Area3D _deactivationArea;


    public override void _Ready() {

        foreach (var formation in GetNode("Formations").GetChildren()) {
            var positions = new Array<Vector2>();
            foreach (var posNode in formation.GetChildren()) {
                if (posNode is Node3D pos3D) {
                    positions.Add(new Vector2(pos3D.GlobalPosition.X, pos3D.GlobalPosition.Y));
                }
            }
            _dronePositions.Add(positions);
        }


        float depthOffset = -0.25f;
        foreach (var child in GetNode("Drones").GetChildren()) {
            if (child is LazerDrone drone) {
                _lazerDrones.Add(drone);
                drone.DepthLayer = depthOffset;
                depthOffset -= 0.25f;
            }
        }

        _doorHandle = GetNode<Node3D>("Doors");

        _activationArea = GetNode<Area3D>("ActivationArea");
        _activationArea.BodyEntered += OnActivationAreaBodyEntered;

        _deactivationArea = GetNode<Area3D>("DeactivationArea");
        _deactivationArea.BodyExited += (Node3D _) => {
            _isActive = false;
        };
    }

    private void OnActivationAreaBodyEntered(Node3D body) {
        if (!_isActive && body is Player player) {
            _isActive = true;

            var tween = GetTree().CreateTween();
            tween.TweenProperty(_doorHandle, "position", _doorHandle.Position + Vector3.Down * 0.61f, 0.3f);

            for (int i = 0; i < 4; i++) {
                DroneCycle(tween, i);
            }
            tween.TweenCallback(Callable.From(() => {
                foreach (var drone in _lazerDrones) {
                    drone.ReturnToStart(0.5f, 1.0f);
                }
            })).SetDelay(2.0f);
            tween.TweenProperty(_doorHandle, "position", _doorHandle.Position + Vector3.Zero, 0.3f);

        }
    }

    private void DroneCycle(Tween tween, int index) {
        tween.TweenCallback(Callable.From(() => {
            var formation = _dronePositions[(int)GD.RandRange(0, _dronePositions.Count - 1)];
            int i = 0;
            foreach (var drone in _lazerDrones) {
                drone.Goto(formation[i], 0.5f, 1.0f);
                drone.Connections.Clear();
                i += 1;
            }
        })).SetDelay(index == 0 ? 0.01f : 1.0f);
        tween.TweenCallback(Callable.From(() => {
            foreach (var idrone in _lazerDrones) {
                foreach (var jdrone in _lazerDrones) {
                    if (idrone.ConnectToNotherDrone(jdrone)) {
                        var laserInstance = _laserScene.Instantiate<Laser>();
                        AddChild(laserInstance);
                        laserInstance.ConnectTwoPoints(idrone.GlobalPosition, jdrone.GlobalPosition);
                    }
                }
            }
        })).SetDelay(2.0f);

    }
}


