using Godot;
using System;


public partial class TestEnemy : Enemy {

    private MeshInstance3D _body;

    [Export]
    public Material HitMaterial;

    private Material _originalMaterial;
    
    public override void _Ready() {
        base._Ready();

        _body = GetNode<MeshInstance3D>("Body");
        _originalMaterial = _body.GetSurfaceOverrideMaterial(0);
    }


    protected override void OnHit() {
        _body.SetSurfaceOverrideMaterial(0, HitMaterial);
    }

    protected override void OnHitEnd() {
        _body.SetSurfaceOverrideMaterial(0, _originalMaterial);
    }
}