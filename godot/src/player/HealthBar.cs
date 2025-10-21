using Godot;
using System;

public partial class HealthBar : Sprite2D {

    Player _player;

    public static PackedScene HitUiParticles = GD.Load<PackedScene>("res://scenes/player/ui_hit_particle.tscn");
    public static PackedScene LastHeartParticles = GD.Load<PackedScene>("res://scenes/player/ui_last_heart_particle.tscn");

    private bool _particlesSpawned = false;
    private Vector2 _lastHeartOriginalPosition;

    [Export]
    public int MaxHealth = 3;

    private int _health = 0;
    public int Health {
        get => _health;
        set {
            _health = Math.Clamp(value, 0, MaxHealth);
            if (value > _health) {
                _particlesSpawned = false;
                GetNode<Node>("UiLastHeartParticle").QueueFree();
            }
        }
    }

    public override void _Ready() {
        _player = GetNode<Player>("../../../");
        _player.Hit += OnPlayerHit;
        _lastHeartOriginalPosition = GetChild<Node2D>(0).Position;
    }

    public override void _Process(double delta) {
        for (int i = 0; i < MaxHealth; i++) {
            if (i < Health) {
                GetChild<Sprite2D>(i).Visible = true;
            } else {
                GetChild<Sprite2D>(i).Visible = false;
            }
        }

        if (Health == 1) {
            Node2D lastHeart = GetChild<Node2D>(0);
            lastHeart.Position = new Vector2(GD.RandRange(-2, 2), GD.RandRange(-2, 2)) + _lastHeartOriginalPosition;
            lastHeart.RotationDegrees = GD.RandRange(-10, 10);
        }

        if (Health == 1 && !_particlesSpawned) {
            Node2D particles = (Node2D)LastHeartParticles.Instantiate();
            AddChild(particles);
            particles.GlobalPosition = GetChild<Node2D>(0).GlobalPosition;
            _particlesSpawned = true;
        }

        if (Health <= 0 && _particlesSpawned) {
            GetNode<Node>("UiLastHeartParticle").QueueFree();
            _particlesSpawned = false;
        }
    }

    private void OnPlayerHit(int damage) {
        Node2D heartToDestroy = GetChild<Node2D>(Health - 1);
        Node2D particles = (Node2D)HitUiParticles.Instantiate();
        GetParent().AddChild(particles);
        particles.GlobalPosition = heartToDestroy.GlobalPosition;
        Health -= damage;
    }
}
