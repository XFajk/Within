using Godot;
using System;

public partial class PersonEnemy : Enemy {

    public enum PersonEnemyState {
        Idle,
        Chasing,
        Damaged,
        Dead,
    }

    public PersonEnemyState CurrentState = PersonEnemyState.Idle;
    protected Player _player = null;
    private Vector3 _velocity = Vector3.Zero;


    [Export]
    public float MoveSpeed = 1.0f;
    [Export]
    public float Gravity = 15.8f;
    [Export]
    public float TerminalVelocity = -400.0f;

    public bool BasicEnemy = true;

    private RayCast3D _playerDetectionRay;
    private AnimationPlayer _animationPlayer;
    private VisibleOnScreenNotifier3D _visibleOnScreenNotifier3D;

    private PackedScene _bloodEffectScene = GD.Load<PackedScene>("res://scenes/VFX/enemy_hit_particles.tscn");

    protected AudioStreamPlayer3D _streamPlayer;
    protected AudioStream _chargingSound = GD.Load<AudioStream>("res://assets/sounds/infected_charging.wav");   


    public override void _Ready() {
        base._Ready();
        if (BasicEnemy) {
            _streamPlayer = GetNode<AudioStreamPlayer3D>("ZombieSounds");
        }

        _playerDetectionRay = GetNode<RayCast3D>("PlayerDetectionRay");
        _animationPlayer = GetNode<AnimationPlayer>("enemy1_v1/AnimationPlayer");
        if (BasicEnemy) {
            _visibleOnScreenNotifier3D = GetNode<VisibleOnScreenNotifier3D>("VisibleOnScreenNotifier3D");
            _visibleOnScreenNotifier3D.ScreenEntered += () => { ProcessMode = ProcessModeEnum.Inherit; };
            _visibleOnScreenNotifier3D.ScreenExited += () => { ProcessMode = ProcessModeEnum.Disabled; };
        }
    }

    public override void _PhysicsProcess(double delta) {


        switch (CurrentState) {
            case PersonEnemyState.Idle:
                HandleIdleState(delta);
                break;
            case PersonEnemyState.Chasing:
                HandleChasingState(delta);
                break;
            case PersonEnemyState.Damaged:
                HandleDamagedState(delta);
                break;
            case PersonEnemyState.Dead:
                HandleDeadState(delta);
                break;
        }

        if (IsOnFloor()) {
            _velocity.Y = 0f;
        } else {
            _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * Player.UnitTransformer, Gravity * Player.UnitTransformer);
        }

        Velocity = _velocity;
        MoveAndSlide();
    }

    private void HandleIdleState(double delta) {
        if (_playerDetectionRay.IsColliding()) {
            var collider = _playerDetectionRay.GetCollider();
            if (collider is Player player) {
                CurrentState = PersonEnemyState.Chasing;
                _player = player;
                _streamPlayer.Stream = _chargingSound;
                _streamPlayer.MaxDistance = 10f;
                _streamPlayer.UnitSize = 10f;
                _streamPlayer.Play();
            }
        }
    }

    private void HandleChasingState(double delta) {
        if (_player != null) {
            Vector3 HorizontalDirectionToPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
            _velocity.X = HorizontalDirectionToPlayer.X * MoveSpeed;
            if (HorizontalDirectionToPlayer.X != 0) {
                if (HorizontalDirectionToPlayer.X < 0) {
                    RotationDegrees = new Vector3(0f, 0f, 0f);
                } else {
                    RotationDegrees = new Vector3(0f, 180f, 0f);
                }
            }
            _animationPlayer.Play("npc_run");
        } else {
            _velocity.X = 0f;
            CurrentState = PersonEnemyState.Idle;
            _animationPlayer.Play("npc_idle");
        }
    }

    private void HandleDamagedState(double delta) {
        _velocity.X = Mathf.MoveToward(_velocity.X, 0f, 0.1f);
        if (Math.Abs(_velocity.X) < 0.1f) {
            _velocity.X = 0f;
            CurrentState = PersonEnemyState.Chasing;
        }
    }

    private void HandleDeadState(double delta) {
        _velocity.X = Mathf.MoveToward(_velocity.X, 0f, 0.1f);
        if (Math.Abs(_velocity.X) < 0.1f) {
            _velocity.X = 0f;
        }
    }

    protected override void OnHit() {
        _animationPlayer.Play("npc_damage");
        CurrentState = PersonEnemyState.Damaged;
        var bloodEffect = _bloodEffectScene.Instantiate<Node3D>();
        AddSibling(bloodEffect);
        bloodEffect.GlobalPosition = GlobalPosition;

        if (_player == null) return;
        Vector3 HorizontalDirectionToPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
        _velocity = new Vector3(HorizontalDirectionToPlayer.X / Math.Abs(HorizontalDirectionToPlayer.X) * -2f, 0f, 0f);
    }

    protected override void OnHitEnd() {
    }

    protected override void Die() {
        _animationPlayer.Play("npc_death");
        _streamPlayer.Stop();
        CurrentState = PersonEnemyState.Dead;
        _player = null;
        GetNode("HitBox").QueueFree();
        GetNode("AttackBox").QueueFree();
        _playerDetectionRay.QueueFree();
    }
}
