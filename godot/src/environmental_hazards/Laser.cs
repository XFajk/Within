using Godot;
using System;

public partial class Laser : Node3D {
    private bool _activated = false;
    public bool Activated {
        get { return _activated; }
        set {
            _activated = value; 
            if (_activated) {
                _attackArea.Position = Vector3.Zero;
                _laserFireSound.Play();
                _laserBeam.SetSurfaceOverrideMaterial(0, _laserActiveMaterial);
            } else {
                _attackArea.Position = new Vector3(0f, 0f, 1000f);
                _laserBeam.SetSurfaceOverrideMaterial(0, _laserInactiveMaterial); 
            }
        }
    }

    private Area3D _attackArea;
    private CollisionShape3D _attackShape;

    private MeshInstance3D _laserBeam;
    private StandardMaterial3D _laserActiveMaterial = GD.Load<StandardMaterial3D>("res://assets/materials/laser_active.tres");
    private StandardMaterial3D _laserInactiveMaterial = GD.Load<StandardMaterial3D>("res://assets/materials/laser_inactive.tres");

    private float _laserLength = 0f;
    public float LaserLength {
        get { return _laserLength; }
        set {
            _laserLength = value;
            _attackShape.Shape.Set("length", _laserLength);
            _laserBeam.Position = new Vector3(0f, 0f, -_laserLength / 2f);
            _laserBeam.Mesh.Set("height", _laserLength);
        }
    }

    public float TimeToActivate = 1.0f;
    public float TimeActive = 0.5f;

    private Timer _activationTimer = new(); 

    private AudioStreamPlayer3D _laserFireSound;

    public override void _Ready() {

        AddChild(_activationTimer);
        _activationTimer.Timeout += ActivateLaser;
        _activationTimer.Start(TimeToActivate);

        _attackArea = GetNode<Area3D>("AttackBox");
        _attackShape = GetNode<CollisionShape3D>("AttackBox/CollisionShape3D");
        _laserBeam = GetNode<MeshInstance3D>("LaserBeam");

        _laserBeam.Mesh = (Mesh)_laserBeam.Mesh.Duplicate();
        _attackShape.Shape = (Shape3D)_attackShape.Shape.Duplicate();
        
        _laserFireSound = GetNode<AudioStreamPlayer3D>("LaserFireSound");
        
        Activated = false;
    }

    public void ConnectTwoPoints(Vector3 pointA, Vector3 pointB) {
        GlobalPosition = pointA;
        LookAt(pointB, Vector3.Up);
        LaserLength = pointA.DistanceTo(pointB);
    }

    private void ActivateLaser() {
        Activated = true;

        _activationTimer.Timeout -= ActivateLaser;
        _activationTimer.Timeout += DestroyLaser;
        _activationTimer.Start(TimeActive);
    }

    private void DestroyLaser() {
        QueueFree();
    }
}
