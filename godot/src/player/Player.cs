using Godot;
using System;

public partial class Player : CharacterBody3D {

    private enum PlayerState {
        Idle,
        Jumping,
        Falling,
        OnWall,
    }

    private PlayerState CurrentState = PlayerState.Idle;

    [ExportGroup("Camera Settings")]
    [Export]
    public Camera3D PlayerCamera;

    [Export]
    public float CameraDistance = 10.0f;

    [Export]
    public float CameraFov = 70.0f;

    [ExportGroup("Movement Settings")]
    [Export]
    public float MovementSpeed = 120.0f;

    [Export]
    public float Gravity = 15.8f;

    [Export] float TerminalVelocity = -200.0f;

    [Export]
    public float JumpVelocity = 130.0f;

    private Vector3 _velocity = Vector3.Zero;

    private int _jumpBufferFrames = 0;
    private int _numberOfFramesInJump = 0;

    private float _jumpModifier = 1.0f;

    [ExportGroup("Wall Jump Settings")]
    [Export]
    public float WallJumpHorizontalVelocity = 500.0f;

    [Export]
    public float WallJumpVelocityModifier = 2.0f;

    [Export]
    public float WallSlideVelocity = -50.0f;

    public override void _Ready() {
        if (PlayerCamera == null) {
            PlayerCamera = new Camera3D();
        }

        PlayerCamera.Fov = CameraFov;
        PlayerCamera.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, CameraDistance);
    }

    public override void _Process(double delta) {
        MoveCamera();
    }

    public override void _PhysicsProcess(double delta) {
        ProcessJumpBuffer();

        switch (CurrentState) {
            case PlayerState.Idle:
                HandleIdleState(delta);
                break;
            case PlayerState.Jumping:
                HandleJumpingState(delta);
                break;
            case PlayerState.Falling:
                HandleFallingState(delta);
                break;
            case PlayerState.OnWall:
                HandleOnWallState(delta);
                break;
        }

        Velocity = _velocity;
        MoveAndSlide();
    }

    private void HandleIdleState(double delta) {
        Move(delta);

        if (IsOnFloor() && WantsToJump()) {
            _jumpBufferFrames = 0;
            _jumpModifier = 1.0f;
            CurrentState = PlayerState.Jumping;
        } else if (!IsOnFloor()) {
            if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6;
            }
            CurrentState = PlayerState.Falling;
        }
    } 

    private void HandleJumpingState(double delta) {
        Move(delta);

        if (IsOnFloor() || _numberOfFramesInJump > 0) {
            _velocity.Y = JumpVelocity * _jumpModifier * (float)delta;

            if (IsOnCeiling()) {
                _numberOfFramesInJump = 0;
                _velocity.Y = -Gravity * (float)delta;
                CurrentState = PlayerState.Falling;
            }

            if (Input.IsActionPressed("jump")) {
                if (_numberOfFramesInJump < 20) {
                    _numberOfFramesInJump++;
                } else {
                    _numberOfFramesInJump = 0;
                    CurrentState = PlayerState.Falling;
                }
            } else {
                _numberOfFramesInJump = 0;
                CurrentState = PlayerState.Falling;
            }
        } else {
            _numberOfFramesInJump = 0;
            CurrentState = PlayerState.Falling;
        }
    }

    private void HandleFallingState(double delta) {
        Move(delta);

        if (IsOnFloor()) {
            CurrentState = PlayerState.Idle;
            _velocity.Y = 0;
        } else if (IsOnWall()) {
            CurrentState = PlayerState.OnWall;
            _velocity.Y = WallSlideVelocity * (float)delta;
        } else {
            _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * (float)delta, Gravity * (float)delta);
            if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6;
            }
        }
    }

    private void HandleOnWallState(double delta) {
        Move(delta);
        if (IsOnWall()) {
            _velocity.Y = WallSlideVelocity * (float)delta;

            if (WantsToJump()) {
                _jumpModifier = WallJumpVelocityModifier;
                Vector3 wallNormal = GetLastSlideCollision().GetNormal();
                _velocity.X = (wallNormal * WallJumpHorizontalVelocity * (float)delta).X;
                _velocity.Y = JumpVelocity * _jumpModifier * (float)delta;
                CurrentState = PlayerState.Jumping;
            }
        } else if (IsOnFloor()) {
            CurrentState = PlayerState.Idle;
            _velocity.Y = 0;
        } else {
            CurrentState = PlayerState.Falling;
        }
    }

    private Vector3 GetInputDirection() {
        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("move_right")) {
            direction.X += 1;
        }
        if (Input.IsActionPressed("move_left")) {
            direction.X -= 1;
        }

        return direction.Normalized();
    }

    private void Move(double delta) {
        float yVelocity = _velocity.Y;
        _velocity = _velocity.MoveToward(GetInputDirection() * MovementSpeed * (float)delta, MovementSpeed/4*(float)delta);
        _velocity.Y = yVelocity;
    }

    private void ProcessJumpBuffer() {
        if (_jumpBufferFrames > 0) {
            _jumpBufferFrames--;
        }
    }

    private void MoveCamera() {
        if (PlayerCamera != null) {
            PlayerCamera.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, CameraDistance);
        }
    }

    private bool WantsToJump() {
        return (Input.IsActionJustPressed("jump") || _jumpBufferFrames > 0);
    }
}


