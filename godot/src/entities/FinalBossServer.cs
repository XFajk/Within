using Godot;
using Godot.Collections;
using System;

public partial class FinalBossServer : Enemy {

    private Array<MeshInstance3D> _serverShells = new();

    [Export]
    public StandardMaterial3D HitMaterial;

    private Material _originalMaterial;


    private PackedScene _brokenSparks = GD.Load<PackedScene>("res://scenes/VFX/sparks.tscn");

    public override void _Ready() {
        base._Ready();

        var bodyPartsChildren = GetNode("Body").GetChildren();
        foreach (var child in bodyPartsChildren) {
            if (child is MeshInstance3D mesh) {
                _serverShells.Add(mesh);
            }
        }

        _originalMaterial = _serverShells[0].GetSurfaceOverrideMaterial(0);
    }

    protected override void OnHit() {
        foreach (var shell in _serverShells) {
            shell.SetSurfaceOverrideMaterial(0, HitMaterial);
        }

        var sparks = _brokenSparks.Instantiate<Node3D>();
        AddSibling(sparks);

        sparks.GlobalPosition = GlobalPosition;
    }

    protected override void OnHitEnd() {
        foreach (var shell in _serverShells) {
            shell.SetSurfaceOverrideMaterial(0, _originalMaterial);
        }
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
