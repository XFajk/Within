using Godot;
using System;

public abstract partial class Enemy : CharacterBody3D {
    [Export]
    public int Health = 20;


    private Area3D _hitBoxArea;

    public override void _Ready() {
        _hitBoxArea = GetNode<Area3D>("HitBox");
        _hitBoxArea.AreaEntered += OnHitBoxAreaEntered;
        _hitBoxArea.AreaExited += OnHitBoxAreaExited;
    }

    private void OnHitBoxAreaEntered(Area3D area) {
        Player player = area.GetParent<Player>();
        if (player != null) {
            Health -= player.Damage;
            OnHit();
            if (Health <= 0) {
                Die();
            }
        }
    }

    private void OnHitBoxAreaExited(Area3D area) {
        Player player = area.GetParent<Player>();
        if (player != null) {
            OnHitEnd();
        }
    }


    protected virtual void Die() {
        QueueFree();
    }

    protected abstract void OnHit();

    protected abstract void OnHitEnd();

}