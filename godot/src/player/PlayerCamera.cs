using Godot;
using System;

public partial class PlayerCamera : Camera3D {

    private SubViewport _inWorldUiViewport;
    private Camera3D _inWorldUiCamera;

    public override void _Ready() {
        _inWorldUiViewport = GetNode<SubViewport>("SubViewportContainer/InWorldUiSubViewport");
        _inWorldUiCamera = _inWorldUiViewport.GetNode<Camera3D>("InWorldUiCamera");

        _inWorldUiViewport.Size = new Vector2I((int)GetViewport().GetVisibleRect().Size.X, (int)GetViewport().GetVisibleRect().Size.Y);
        _inWorldUiCamera.Fov = Fov;
        GetViewport().SizeChanged += OnResizeViewport;
    }

    public override void _Process(double delta) {
        _inWorldUiCamera.Fov = Fov;
        _inWorldUiCamera.GlobalTransform = GlobalTransform;
    }

    private void OnResizeViewport() {
        _inWorldUiViewport.Size = new Vector2I((int)GetViewport().GetVisibleRect().Size.X, (int)GetViewport().GetVisibleRect().Size.Y);
    } 
}
