using Godot;
using System;
using System.Xml;

public partial class PlayerCamera : Camera3D {

    private SubViewport _inWorldUiViewport;
    private Camera3D _inWorldUiCamera;

    private ShapeCast3D _cameraCast;

    private float _oldPositionX;
    private float _oldPositionY;

    public override void _Ready() {
        _inWorldUiViewport = GetNode<SubViewport>("SubViewportContainer/InWorldUiSubViewport");
        _inWorldUiCamera = _inWorldUiViewport.GetNode<Camera3D>("InWorldUiCamera");

        _cameraCast = GetNode<ShapeCast3D>("CameraCast");

        _inWorldUiViewport.Size = new Vector2I((int)GetViewport().GetVisibleRect().Size.X, (int)GetViewport().GetVisibleRect().Size.Y);
        _inWorldUiCamera.Fov = Fov;

        GetViewport().SizeChanged += OnResizeViewport;

        GlobalPosition = Vector3.Zero;  

        _oldPositionX = GlobalPosition.X;
        _oldPositionY = GlobalPosition.Y;
    }

    public override void _Process(double delta) {
        _inWorldUiCamera.Fov = Fov;
        _inWorldUiCamera.GlobalTransform = GlobalTransform; 
    }

    private void OnResizeViewport() {
        _inWorldUiViewport.Size = new Vector2I((int)GetViewport().GetVisibleRect().Size.X, (int)GetViewport().GetVisibleRect().Size.Y);
    } 

    public void Move(Vector3 newPosition) {

        // Check X axis
        GlobalPosition = new Vector3(newPosition.X, GlobalPosition.Y, GlobalPosition.Z);
        _cameraCast.ForceShapecastUpdate();

        if (_cameraCast.GetCollisionCount() == 0) {
            GlobalPosition = new Vector3(_oldPositionX, GlobalPosition.Y, GlobalPosition.Z);
        } else {
            _oldPositionX = GlobalPosition.X;
        }

        // Check Y axis
        GlobalPosition = new Vector3(GlobalPosition.X, newPosition.Y, GlobalPosition.Z);
        _cameraCast.ForceShapecastUpdate();

        if (_cameraCast.GetCollisionCount() == 0) {
            GlobalPosition = new Vector3(GlobalPosition.X, _oldPositionY, GlobalPosition.Z);
        } else {
            _oldPositionY = GlobalPosition.Y;
        }
    }
}
