using Godot;

[GlobalClass]
public partial class MiniCheckPoint : Area3D {

    public override void _Ready() {
        CollisionLayer = 0;
        CollisionMask = 0;
        SetCollisionMaskValue(7, true);
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area3D player_hitbox) {
        var body = player_hitbox.GetParent<Node3D>();
        if (body is Player player) {
            Global.Instance.MiniCheckPointSavedPosition = player.GlobalPosition;
        }
    } 

}