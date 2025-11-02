using Godot;
using System;

public partial class LaserMachine : Node3D {

    private Player _player;

    private CollisionShape3D _laserAttackShape;
    private Area3D _laserAttackArea;

    private RayCast3D _laserRayCast;
    private MeshInstance3D _laserBeam;

    private Node3D _sparks;

    private StandardMaterial3D _laserActiveMaterial = GD.Load<StandardMaterial3D>("res://assets/materials/laser_active.tres");
    private StandardMaterial3D _laserInactiveMaterial = GD.Load<StandardMaterial3D>("res://assets/materials/laser_inactive.tres");

    [Export]
    public bool Enabled = false;

    [Export]
    public float MaxLaserLength = 25f;
    private float _laserLength = 0f;
    public float LaserLength {
        get { return _laserLength; }
        set {
            _laserLength = value;
            _laserAttackShape.Shape.Set("length", _laserLength);
            _laserBeam.Position = new Vector3(0f, 0f, -_laserLength / 2f);
            _laserBeam.Mesh.Set("height", _laserLength);
            _sparks.Position = new Vector3(0f, 0f, -_laserLength);
        }
    }

    [Export]
    public float LockingTime = 5.0f;

    [Export]
    public float FiringTime = 1.0f;

    [Export]
    public float ActiveTime = 2.0f;

    private Timer LockingTimer = new();
    private Timer FiringTimer = new();
    private Timer ActiveTimer = new();

    private AudioStreamPlayer3D _laserSound;
    private RayCast3D _playerRay;

    public override void _Ready() {
        _laserSound = GetNode<AudioStreamPlayer3D>("LaserSound");
        _playerRay = GetNode<RayCast3D>("PlayerRay");


        GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, 0f);
        _player = GetTree().GetNodesInGroup("Player")[0] as Player;

        _laserAttackShape = GetNode<CollisionShape3D>("AttackBox/CollisionShape3D");
        _laserAttackArea = GetNode<Area3D>("AttackBox");
        _laserAttackArea.GlobalPosition = new Vector3(0f, 0f, 1000f);

        _sparks = GetNode<Node3D>("Sparks");

        _laserRayCast = GetNode<RayCast3D>("Ray");
        _laserRayCast.TargetPosition = new Vector3(0f, 0f, -MaxLaserLength);

        _laserBeam = GetNode<MeshInstance3D>("LaserBeam");
        _laserBeam.SetSurfaceOverrideMaterial(0, _laserInactiveMaterial);

        LaserLength = 0.01f;

        LockingTimer.OneShot = true;
        LockingTimer.WaitTime = LockingTime;
        LockingTimer.Timeout += () => {
            FiringTimer.Start();
        };
        AddChild(LockingTimer);        

        FiringTimer.OneShot = true;
        FiringTimer.WaitTime = FiringTime;
        FiringTimer.Timeout += () => {
            ActiveTimer.Start();
            _laserBeam.SetSurfaceOverrideMaterial(0, _laserActiveMaterial);
            _laserAttackArea.Position = Vector3.Zero;
        };
        AddChild(FiringTimer);

        ActiveTimer.OneShot = true;
        ActiveTimer.WaitTime = ActiveTime;
        ActiveTimer.Timeout += () => {
            _laserSound.Play();
            LockingTimer.Start();
            _laserBeam.SetSurfaceOverrideMaterial(0, _laserInactiveMaterial);
            _laserAttackArea.GlobalPosition = new Vector3(0f, 0f, 1000f);
        };
        AddChild(ActiveTimer);
    }

    public override void _Process(double delta) {

        _playerRay.LookAt(_player.GlobalPosition, Vector3.Up);
        if (_playerRay.IsColliding()) {
            var collider = _playerRay.GetCollider();
            if (collider is Player) {
                _laserSound.VolumeDb = -15f;
            } else {
                _laserSound.VolumeDb = -80f;
            }
        } else {
            _laserSound.VolumeDb = -80f;
        }

        if (Global.Instance.IsGamePaused || Global.Instance.IsInMainMenu || _player == null) return;

        if (LockingTimer.IsStopped() && FiringTimer.IsStopped() && ActiveTimer.IsStopped() && Enabled) {
            _laserSound.Play();
            LockingTimer.Start();
            _laserRayCast.Enabled = true;
            _sparks.Visible = true;
        }
        if (!Enabled) {
            LockingTimer.Stop();
            FiringTimer.Stop();
            ActiveTimer.Stop();
            _laserSound.Stop();
            _laserBeam.SetSurfaceOverrideMaterial(0, _laserInactiveMaterial);
            _laserAttackArea.GlobalPosition = new Vector3(0f, 0f, 1000f);
            _laserRayCast.Enabled = false;
            _sparks.Visible = false;
            LaserLength = 0.01f;
            return;
        }

        if (!LockingTimer.IsStopped()) {
            LookAt(_player.GlobalPosition, Vector3.Up);
            RotationDegrees = new Vector3(RotationDegrees.X, Mathf.Clamp(RotationDegrees.Y, -180.0f, 0), RotationDegrees.Z);
        }

        if (_laserRayCast.IsColliding()) {
            var collisionPoint = _laserRayCast.GetCollisionPoint();
            LaserLength = GlobalPosition.DistanceTo(collisionPoint);
        } else {
            LaserLength = 0.01f;
        }
        
    }

}
