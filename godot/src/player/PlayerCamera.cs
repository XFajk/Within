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


    public void ShowAbilityTutorial(AbilityUnlocker.Ability ability) {
        var tutorialUi = GetNode<Control>("TutorialUi");
        tutorialUi.Visible = true;

        var tutorialLabel = tutorialUi.GetNode<Label>("Label");
        var tutorialImage = tutorialUi.GetNode<TextureRect>("AbilityImage");

        switch (ability) {
            case AbilityUnlocker.Ability.Dash:
                tutorialLabel.Text = "Press the [C] Button to Dash forward quickly!";
                tutorialImage.Texture = GD.Load<Texture2D>("res://assets/sprites/dash_graphic.png");
                break;
            case AbilityUnlocker.Ability.WallJump:
                tutorialLabel.Text = "Press the [Z] Button while On a Wall to perform a Wall Jump!";
                tutorialImage.Texture = GD.Load<Texture2D>("res://assets/sprites/walljump_graphic.png");
                break;
            case AbilityUnlocker.Ability.DoubleJump:
                tutorialLabel.Text = "Press the [Z] Button again while in the air to perform a double jump.";
                tutorialImage.Texture = GD.Load<Texture2D>("res://assets/sprites/doublejump_graphic.png");
                break;
            default:
                tutorialLabel.Text = "New Ability Unlocked!";
                tutorialImage.Texture = null;
                break;
        }
    }
}
