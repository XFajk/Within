using Godot;
using System;

public partial class Player : CharacterBody3D {

    private enum PlayerState {
        Idle,
        Jumping,
        Falling
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
    public float MovementSpeed = 10.0f;

    [Export]
    public float Gravity = 98.8f;

    [Export] float TerminalVelocity = -50.0f;

    [Export]
    public float JumpVelocity = 4.5f;

    private Vector3 _velocity = Vector3.Zero;

    private int _jumpBufferFrames = 0;
    private int _numberOfFramesInJump = 0;


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
        }

        Velocity = _velocity;
        MoveAndSlide();
    }

    private void HandleIdleState(double delta) {
        Move();

        if (IsOnFloor() && (Input.IsActionPressed("jump") || _jumpBufferFrames > 0)) {
            _jumpBufferFrames = 0;
            CurrentState = PlayerState.Jumping;
        } else {
            if (Input.IsActionPressed("jump")) {
                _jumpBufferFrames = 5;
            }
            CurrentState = PlayerState.Falling;
        }
    } 

    private void HandleJumpingState(double delta) {
        Move();

        if (IsOnFloor() || _numberOfFramesInJump > 0) {
            _velocity.Y = JumpVelocity * (float)delta;

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
        Move();

        if (IsOnFloor()) {
            CurrentState = PlayerState.Idle;
            _velocity.Y = 0;
        } else {
            _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity, Gravity * (float)delta);
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

    private void Move() {
        float yVelocity = _velocity.Y;
        _velocity = _velocity.MoveToward(GetInputDirection() * MovementSpeed, MovementSpeed);
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
}


