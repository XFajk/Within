using Godot;
using System;

public partial class HealthBar : Sprite2D {

    Player _player;

    public static PackedScene HitUiParticles = GD.Load<PackedScene>("res://scenes/player/ui_hit_particle.tscn");
    public static PackedScene LastHeartParticles = GD.Load<PackedScene>("res://scenes/player/ui_last_heart_particle.tscn");

    private bool _particlesSpawned = false;
    private Vector2 _lastHeartOriginalPosition;

    // reference to the instantiated last-heart particle so we can free it reliably
    private Node2D _lastHeartParticleInstance = null;

    [Export]
    public int MaxHealth = 3;

    private int _health = 0;

    public bool Healing = false;

    public bool IsInvincible = false; 

    public int Health {
        get => _health;
        set {
            // keep previous value to detect healing correctly
            int previous = _health;
            _health = Math.Clamp(value, 0, MaxHealth);

            // if healed, remove last-heart particle (use stored instance)
            if (_health > previous) {
                _particlesSpawned = false;
                if (_lastHeartParticleInstance != null && IsInstanceValid(_lastHeartParticleInstance)) {
                    _lastHeartParticleInstance.QueueFree();
                    _lastHeartParticleInstance = null;
                }
            }
        }
    }

    public override void _Ready() {
        Health = Global.Instance.PlayerLastSavedHealth;

        _player = GetNode<Player>("../../../");
        _player.Hit += OnPlayerHit;
        _player.Heal += OnPlayerHealed;

        _lastHeartOriginalPosition = GetChild<Node2D>(0).Position;
    }

    public override void _ExitTree() {
        Global.Instance.PlayerLastSavedHealth = Health;
    }

    public override void _Process(double delta) {
        for (int i = 0; i < MaxHealth; i++) {
            if (i < Health) {
                Sprite2D heart = GetChild<Sprite2D>(i);
                if (!heart.Visible) {
                    heart.Scale = new Vector2(0.01f, 0.01f);
                    Tween tween = GetTree().CreateTween();
                    tween.TweenProperty(heart, "scale", new Vector2(1.184f, 1.184f), 0.3f);
                    heart.Visible = true;
                }
            } else {
                GetChild<Sprite2D>(i).Visible = false;
            }
        }

        if (Health == 1 && !Healing) {
            Node2D lastHeart = GetChild<Node2D>(0);
            lastHeart.Position = new Vector2(GD.RandRange(-2, 2), GD.RandRange(-2, 2)) + _lastHeartOriginalPosition;
            lastHeart.RotationDegrees = GD.RandRange(-10, 10);
        }

        if (Health == 1 && !_particlesSpawned) {
            Node2D particles = (Node2D)LastHeartParticles.Instantiate();
            // optional: set a name to make debugging easier
            particles.Name = "UiLastHeartParticle";
            AddChild(particles);
            particles.GlobalPosition = GetChild<Node2D>(0).GlobalPosition;
            _particlesSpawned = true;
            _lastHeartParticleInstance = particles;
        }

        if (Health <= 0 && _particlesSpawned) {
            if (_lastHeartParticleInstance != null && IsInstanceValid(_lastHeartParticleInstance)) {
                _lastHeartParticleInstance.QueueFree();
                _lastHeartParticleInstance = null;
            }
            _particlesSpawned = false;
        }

        if (IsInvincible) {
            var modulator = (Mathf.Sin(Time.GetTicksMsec() / 100f) + 1f) / 2f;

            Modulate = new Color(modulator, modulator, modulator, 1f);
        } else {
            Modulate = new Color(1, 1, 1, 1f);
        }
    }

    private void OnPlayerHit(int damage) {
        // guard against negative index
        if (IsInvincible) {
            return;
        }

        int idx = Health - 1;
        if (idx >= 0) {
            Node2D heartToDestroy = GetChild<Node2D>(idx);
            Node2D particles = (Node2D)HitUiParticles.Instantiate();
            GetParent().AddChild(particles);
            particles.GlobalPosition = heartToDestroy.GlobalPosition;
        }
        Health -= damage;
    }

    private void OnPlayerHealed() {
        // free via stored reference
        if (_lastHeartParticleInstance != null && IsInstanceValid(_lastHeartParticleInstance)) {
            _lastHeartParticleInstance.QueueFree();
            _lastHeartParticleInstance = null;
        }
        Healing = true;

        Node2D lastHeart = GetChild<Node2D>(0);
        lastHeart.Position = _lastHeartOriginalPosition;
        lastHeart.RotationDegrees = 15.2f;

        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "Health", MaxHealth, 1.0f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(() => {
            Healing = false;
            Global.Instance.RespawningInProgress = false;
        }));
    }


    public void MakeInvincible(float duration) {
        if (IsInvincible) {
            return;
        }
        IsInvincible = true;
        Tween invincibilityTween = GetTree().CreateTween();
        invincibilityTween.TweenCallback(Callable.From(() => {
            IsInvincible = false;
        })).SetDelay(duration);
    }
}
