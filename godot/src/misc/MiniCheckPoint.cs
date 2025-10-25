using Godot;

[GlobalClass]
public partial class MiniCheckPoint : Area3D {

    public Node3D _cameraSpawnPoint;

    public override void _Ready() {
        _cameraSpawnPoint = GetNode<Node3D>("CameraPoint");

        CollisionLayer = 0;
        CollisionMask = 0;
        SetCollisionMaskValue(7, true);
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area3D player_hitbox) {
        var body = player_hitbox.GetParent<Node3D>();
        if (body is Player player) {
            Global.Instance.MiniCheckPointSavedPosition = player.GlobalPosition;
            if (_cameraSpawnPoint != null) {
                Global.Instance.MiniCheckPointCameraSpawnPoint = _cameraSpawnPoint.GlobalPosition;
            }
        }
    } 

}