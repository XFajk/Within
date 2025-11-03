using Godot;
using Godot.Collections;
using System;

public partial class FinalBossServer : Enemy {

    private MeshInstance3D _serverBody = new();

    [Export]
    public StandardMaterial3D HitMaterial;

    private Material _originalMaterial;

    private PackedScene _brokenSparks = GD.Load<PackedScene>("res://scenes/VFX/sparks.tscn");

    private AudioStreamPlayer3D _hitSound;

    public override void _Ready() {
        base._Ready();

        _serverBody = GetNode<MeshInstance3D>("Servers");
        _hitSound = GetNode<AudioStreamPlayer3D>("ServerHitSound");

        _originalMaterial = _serverBody.GetSurfaceOverrideMaterial(1);
    }

    protected override void OnHit() {
        _serverBody.SetSurfaceOverrideMaterial(1, HitMaterial);

        var sparks = _brokenSparks.Instantiate<Node3D>();
        AddSibling(sparks);
        var newHitSound = _hitSound.Duplicate() as AudioStreamPlayer3D;
        AddSibling(newHitSound);
        newHitSound.Play();

        sparks.GlobalPosition = GlobalPosition;
    }

    protected override void OnHitEnd() {
        _serverBody.SetSurfaceOverrideMaterial(1, _originalMaterial);
    }

    protected override void Die() {

        for (var i = 0; i < 6; i++) {
            var offset = new Vector3(
                (float)GD.RandRange(-0.5f, 0.5f),
                (float)GD.RandRange(-0.5f, 0.5f),
                (float)GD.RandRange(-0.5f, 0.5f)
            );

            var sparks = _brokenSparks.Instantiate<Node3D>();
            AddSibling(sparks);

            sparks.GlobalPosition = GlobalPosition + offset;
        }


        base.Die();
    }
}
