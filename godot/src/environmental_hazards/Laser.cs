using Godot;
using System;

public partial class Laser : Node3D {
    private Area3D _attackArea;
    private CollisionShape3D _attackShape;

    private MeshInstance3D _laserBeam;

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

    public override void _Ready() {
        _attackArea = GetNode<Area3D>("AttackBox");
        _attackShape = GetNode<CollisionShape3D>("AttackBox/CollisionShape3D");
        _laserBeam = GetNode<MeshInstance3D>("LaserBeam");

        _laserBeam.Mesh = (Mesh)_laserBeam.Mesh.Duplicate(); 
        _attackShape.Shape = (Shape3D)_attackShape.Shape.Duplicate();

    }

    public void ConnectTwoPoints(Vector3 pointA, Vector3 pointB) {
        GlobalPosition = pointA;
        LookAt(pointB, Vector3.Up);
        LaserLength = pointA.DistanceTo(pointB);
    }
}
