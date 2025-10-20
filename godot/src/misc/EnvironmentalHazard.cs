using Godot;

[GlobalClass]
public partial class EnvironmentalHazard : Area3D {

    public override void _Ready() {
        CollisionLayer = 0;
        CollisionMask = 0;
        SetCollisionMaskValue(6, true);
        AreaEntered+= OnAreaEntered;
    }
    
    private void OnAreaEntered(Area3D player_hitbox) {
        var body = player_hitbox.GetParent<Node3D>();
        if (body is Player player) {
            player.CurrentState = Player.PlayerState.MiniDeath;
        }
    } 

}