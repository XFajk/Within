using Godot;
using System;

public partial class Player : CharacterBody3D {

    private enum PlayerState {
        Idle,
        Jumping,
        Falling,
        OnWall,
        Dashing,
    }

    private enum HorizontalDirection {
        Left = -1,
        Right = 1,
    }

    private PlayerState CurrentState = PlayerState.Idle;

    [ExportGroup("Combat Settings")]
    [Export]
    public int MaxHealth = 3;

    [Export]
    public int MaxDemage = 5;

    public int Health = 3;

    [ExportGroup("Camera Settings")]
    [Export]
    public Camera3D PlayerCamera;

    [Export]
    public float CameraDistance = 10.0f;

    [Export]
    public float CameraFov = 70.0f;

    [Export]
    public float CameraDurationForMovement = 1.0f;

    private float _cameraMovementBuffer = 0.0f;

    [Export]
    public float CameraLerpSpeed = 5.0f; // Speed at which the camera follows the player

    [Export]
    public float CameraMovementAmount = 2.0f;

    [ExportGroup("Movement Settings")]
    [Export]
    public float MovementSpeed = 120.0f;

    [Export]
    public float Gravity = 15.8f;

    [Export] float TerminalVelocity = -200.0f;

    [Export]
    public float JumpVelocity = 130.0f;

    [Export]
    public int FirstJumpFrames = 20;

    private int _amountJumpFrames = 20;

    private Vector3 _velocity = Vector3.Zero;

    private HorizontalDirection _horizontalDirection = HorizontalDirection.Right;

    private int _jumpBufferFrames = 0;
    private int _numberOfFramesInJump = 0;

    private float _jumpModifier = 1.0f;

    [ExportGroup("Wall Jump Settings")]
    [Export]
    public bool CanWallJump = false;

    [Export]
    public float WallJumpHorizontalVelocity = 200.0f;
    
    [Export]
    public float WallJumpVelocityModifier = 2.0f;

    [Export]
    public float WallSlideVelocity = -70.0f;

    [ExportGroup("Dash Settings")]
    [Export]
    public bool CanDash = false;

    [Export]
    public float DashVelocity = 500.0f;

    [Export]
    public float DashCooldown = 0.5f;

    [Export]
    public float DashTime = 0.07f;

    private bool _dashReadyToUse = true;

    private Timer _dashRecoverTimer = new();
    private Timer _dashDurationTimer = new();

    [ExportGroup("Double Jump Settings")]
    [Export]
    public bool CanDoubleJump = false;

    [Export]
    public int SecondJumpFrames = 10;

    private bool _doubleJumpReady = true;

    public override void _Ready() {
        AddChild(_dashRecoverTimer);
        AddChild(_dashDurationTimer);

        _dashRecoverTimer.OneShot = true;
        _dashRecoverTimer.WaitTime = DashCooldown;
        _dashRecoverTimer.Timeout += () => {
            _dashReadyToUse = true;
        };

        _dashDurationTimer.OneShot = true;
        _dashDurationTimer.WaitTime = DashTime;

        if (PlayerCamera == null) {
            PlayerCamera = new();
        }

        PlayerCamera.Fov = CameraFov;
        PlayerCamera.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, CameraDistance);
    }

    public override void _Process(double delta) {
        UpdateCameraMovementBuffer(delta);
        MoveCamera(delta);
        FigureOutHorizontalDirection();
    }

    private void MoveCamera(double delta) {
        if (PlayerCamera != null) {
            // Calculate the forward position based on the player's velocity and direction
            Vector3 targetPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, CameraDistance);

            if (_cameraMovementBuffer > CameraDurationForMovement && CurrentState == PlayerState.Idle) {
                targetPosition += new Vector3(0, GetCameraMovementDirection()*CameraMovementAmount, 0);
            } 

            PlayerCamera.GlobalPosition = PlayerCamera.GlobalPosition.Lerp(targetPosition, CameraLerpSpeed * (float)delta);
        }
    }

    private void UpdateCameraMovementBuffer(double delta) {
        if (Input.IsActionPressed("look_up") || Input.IsActionPressed("look_down")) {
            _cameraMovementBuffer += (float)delta;
        } else {
            _cameraMovementBuffer = 0.0f;
        }
    }

    private float GetCameraMovementDirection() {
        if (Input.IsActionPressed("look_up")) {
            return 1.0f;
        } else if (Input.IsActionPressed("look_down")) {
            return -1.0f;
        } else {
            return 0.0f;
        }
    }

    private void FigureOutHorizontalDirection() {
        if (Input.IsActionPressed("move_left") && Input.IsActionPressed("move_right")) {
            // Do nothing, conflicting inputs
        } else if (Input.IsActionPressed("move_left")) {
            _horizontalDirection = HorizontalDirection.Left;
            Rotation = new Vector3(0, Mathf.DegToRad(180), 0); 
        } else if (Input.IsActionPressed("move_right")) {
            _horizontalDirection = HorizontalDirection.Right;
            Rotation = new Vector3(0, 0, 0);
        }
    }

    public override void _PhysicsProcess(double delta) {
        ProcessJumpBuffer();
        ResetDoubleJump();

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
            case PlayerState.Dashing:
                HandleDashingState(delta);
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
            _amountJumpFrames = FirstJumpFrames;
            CurrentState = PlayerState.Jumping;
        } else if (!IsOnFloor()) {
            if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6;
            }
            CurrentState = PlayerState.Falling;
        }

        CheckDash();
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
                if (_numberOfFramesInJump < _amountJumpFrames) {
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

        CheckDash();
    }

    private void HandleFallingState(double delta) {
        Move(delta);

        if (IsOnFloor()) {
            CurrentState = PlayerState.Idle;
            _velocity.Y = 0;
        } else if (IsOnWall() && CanWallJump) {
            CurrentState = PlayerState.OnWall;
            _velocity.Y = WallSlideVelocity * (float)delta;
        } else {
            _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * (float)delta, Gravity * (float)delta);
            if (Input.IsActionJustPressed("jump") && CanDoubleJump && _doubleJumpReady) {
                _jumpBufferFrames = 0;
                _jumpModifier = 1.0f;
                _doubleJumpReady = false;
                _numberOfFramesInJump = 1;
                _amountJumpFrames = SecondJumpFrames;
                CurrentState = PlayerState.Jumping;
            } else if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6;
            }
        }

        CheckDash();
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
                _amountJumpFrames = FirstJumpFrames;
                CurrentState = PlayerState.Jumping;
            }
        } else if (IsOnFloor()) {
            CurrentState = PlayerState.Idle;
            _velocity.Y = 0;
        } else {
            CurrentState = PlayerState.Falling;
        }
        CheckDash();
    }

    private void HandleDashingState(double delta) {
        Vector3 dashDirection = new Vector3((float)_horizontalDirection, 0, 0).Normalized();
        _velocity = dashDirection * DashVelocity * (float)delta;
        if (_dashDurationTimer.IsStopped()) {
            if (IsOnFloor()) {
                CurrentState = PlayerState.Idle;
            } else if (IsOnWall()) {
                CurrentState = PlayerState.OnWall;
            } else {
                CurrentState = PlayerState.Falling;
            }
        }
    }

    private Vector3 GetInputDirection() {
        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("move_left")) {
            direction.X -= 1;
        }
        if (Input.IsActionPressed("move_right")) {
            direction.X += 1;
        }

        return direction.Normalized();
    }

    private void Move(double delta) {
        float yVelocity = _velocity.Y;
        _velocity = _velocity.MoveToward(GetInputDirection() * MovementSpeed * (float)delta, MovementSpeed / 4 * (float)delta);
        _velocity.Y = yVelocity;
    }

    private void CheckDash() {
        if (Input.IsActionJustPressed("dash") && CanDash && _dashReadyToUse) {
            CurrentState = PlayerState.Dashing;
            _dashReadyToUse = false;
            _dashDurationTimer.Start();
            _dashRecoverTimer.Start();
        }
    }

    private void ProcessJumpBuffer() {
        if (_jumpBufferFrames > 0) {
            _jumpBufferFrames--;
        }
    }

    private void ResetDoubleJump() {
        if (IsOnFloor() || IsOnWall()) _doubleJumpReady = true;
    }


    private bool WantsToJump() {
        return (Input.IsActionJustPressed("jump") || _jumpBufferFrames > 0);
    }
}


