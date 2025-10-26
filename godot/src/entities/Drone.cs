using Godot;
using System;

public partial class Drone : Enemy {
    private MeshInstance3D _body;

    private Player _player;

    [Export]
    public StandardMaterial3D HitMaterial;

    [Export]
    public float FlySpeed = 1.5f;
    [Export]
    public float KnockbackForce = 5f;

    private Material _originalMaterial;

    private PackedScene _brokenSparks = GD.Load<PackedScene>("res://scenes/VFX/sparks.tscn");
    private Node3D _brokenSmoke;

    private Timer _wobbleTimer = new();

    public override void _Ready() {
        base._Ready();

        _wobbleTimer.OneShot = false;
        _wobbleTimer.Autostart = false;
        _wobbleTimer.Timeout += () => {
            float wobbleAmount = 1.0f;
            Velocity += new Vector3(
                0.0f,
                (float)GD.RandRange(0.7f, wobbleAmount),
                0.0f
            );

            AddChild(_brokenSparks.Instantiate<Node3D>());
        };
        AddChild(_wobbleTimer);

        _brokenSmoke = GetNode<Node3D>("DroneBrokenSmoke");
        _brokenSmoke.Visible = false;

        _body = GetNode<MeshInstance3D>("Body");
        _originalMaterial = _body.GetSurfaceOverrideMaterial(0);

        _player = GetTree().GetNodesInGroup("Player")[0] as Player;
    }

    public override void _Process(double delta) {
        if (Global.Instance.IsInMainMenu) {
            return;
        }
        if (_player == null) return;

        GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, 0f);

        Vector3 directionToPlayer = (_player.GlobalPosition - (GlobalPosition - Vector3.Up * 0.3f)).Normalized();
        if (Health < MaxHealth / 2 && _wobbleTimer.IsStopped()) {
            _wobbleTimer.Start(GD.RandRange(1.5f, 3.0f));
        }

        LookAt(_player.GlobalPosition, Vector3.Up);
        Velocity = Velocity.MoveToward(directionToPlayer * FlySpeed, 2.0f * (float)delta);
        MoveAndSlide();
    }

    protected override void OnHit() {
        AddChild(_brokenSparks.Instantiate<Node3D>());
        if (Health < MaxHealth / 2) {
            _brokenSmoke.Visible = true;
        }

        _body.SetSurfaceOverrideMaterial(0, HitMaterial);
        Vector3 directionFromPlayer = (_player.GlobalPosition - (GlobalPosition - Vector3.Up * 0.3f)).Normalized() * -1;
        Velocity = directionFromPlayer * KnockbackForce;
    }

    protected override void OnHitEnd() {
        _body.SetSurfaceOverrideMaterial(0, _originalMaterial);
    }

    protected override void Die() {
        for (int i = 0; i < 3; i++) {
            var sparks = _brokenSparks.Instantiate<Node3D>();
            AddSibling(sparks);
            sparks.GlobalPosition = GlobalPosition;
        }
        QueueFree();
    }
}
