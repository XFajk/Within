using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody3D, ISavable {

    private const float UnitTransformer = 1.0f/60.0f;

    public enum PlayerState {
        Idle,
        Jumping,
        Falling,
        OnWall,
        Dashing,
        EnteringArea,
        ExitingArea,
        MiniDeath,
        Death,
        NoControl,
    }

    private enum HorizontalDirection {
        Left = -1,
        Right = 1,
    }

    public PlayerState CurrentState = PlayerState.ExitingArea;

    public PackedScene PlayerAreaToEnter = null;

    private AnimationTree _animationTree;

    private float _runningAnimationBlend = 0.0f;
    private float _jumpAnimationBlend = 0.0f;
    private float _wallHugAnimationBlend = 0.0f;
    private float _dashAnimationBlend = 0.0f;

    private Tween _animationTransitionTween = null;

    [ExportGroup("Combat Settings")]
    [Export]
    public int MaxHealth = 3;

    [Export]
    public int MaxDemage = 5;

    public int Health = 3;

    [ExportGroup("Camera Settings")]
    public Camera3D Camera;

    public ColorRect PostProcessRect;

    public ShaderMaterial TransitionMaterial = GD.Load<ShaderMaterial>("res://src/shaders/transition/transition_material.tres");

    [Export]
    public float CameraDistance = 2.0f;

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
    private bool _canWallJump = false;

    [Export]
    public float WallJumpHorizontalVelocity = 200.0f;

    [Export]
    public float WallJumpVelocityModifier = 2.0f;

    [Export]
    public float WallSlideVelocity = -70.0f;

    [Export]
    public uint ClimbableWallLayer = 5; // Layer 5 will be for climbable walls

    [ExportGroup("Dash Settings")]
    private bool _canDash = false;

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
    private bool _canDoubleJump = false;

    [Export]
    public int SecondJumpFrames = 10;

    private bool _doubleJumpReady = true;

    public override void _Ready() {

        _canWallJump = Global.Instance.PlayerHasWallJumpAbility;
        _canDash = Global.Instance.PlayerHasDashAbility;
        _canDoubleJump = Global.Instance.PlayerHasDoubleJumpAbility;

        AddChild(_dashRecoverTimer);
        AddChild(_dashDurationTimer);

        _dashRecoverTimer.OneShot = true;
        _dashRecoverTimer.WaitTime = DashCooldown;
        _dashRecoverTimer.Timeout += () => {
            _dashReadyToUse = true;
        };

        _dashDurationTimer.OneShot = true;
        _dashDurationTimer.WaitTime = DashTime;

        if (Camera == null) {
            Camera = GetNode<Camera3D>("Camera");
            PostProcessRect = Camera.GetNode<ColorRect>("PostProcessRect");
            PostProcessRect.Visible = false;
            Camera.TopLevel = true;
        }

        Camera.Fov = CameraFov;
        Camera.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, CameraDistance);

        _animationTree = GetNode<AnimationTree>("AnimationTree");

        HandleExitingAreaState(1.0);

        // this so that the player when first loading into the game from the menu takes the position
        // that was last saved manualy at a save point if this wasent here the player would at a radom auto save position
        CallDeferred(nameof(OverrideTransformOnFirstLoadIn));
    }

    public override void _Process(double delta) {   
        Position = new Vector3(Position.X, Position.Y, 0);

        UpdateAnimationTree();

        UpdateCameraMovementBuffer(delta);
        MoveCamera(delta);
        FigureOutHorizontalDirection();
    }

    private void MoveCamera(double delta) {
        if (Camera != null) {
            // Calculate the forward position based on the player's velocity and direction
            Vector3 targetPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, CameraDistance);

            if (_cameraMovementBuffer > CameraDurationForMovement && CurrentState == PlayerState.Idle) {
                targetPosition += new Vector3(0, GetCameraMovementDirection() * CameraMovementAmount, 0);
            }

            Camera.GlobalPosition = Camera.GlobalPosition.Lerp(targetPosition, CameraLerpSpeed * (float)delta);
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
            case PlayerState.EnteringArea:
                HandleEnteringAreaState(delta);
                break;
            case PlayerState.ExitingArea:
                HandleExitingAreaState(delta);
                break;
            case PlayerState.MiniDeath:
                HandleMiniDeathState(delta);
                break;
            case PlayerState.NoControl:
                _velocity = Vector3.Zero;
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
            TransitionAnimationTo(PlayerState.Jumping);
            CurrentState = PlayerState.Jumping;
        } else if (!IsOnFloor()) {
            if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6;
            }
            TransitionAnimationTo(PlayerState.Jumping);
            CurrentState = PlayerState.Falling;
        }

        CheckDash();
    }

    private void HandleJumpingState(double delta) {
        Move(delta);

        if (IsOnFloor() || _numberOfFramesInJump > 0) {
            _velocity.Y = JumpVelocity * _jumpModifier * UnitTransformer;

            if (IsOnCeiling()) {
                _numberOfFramesInJump = 0;
                _velocity.Y = -Gravity * UnitTransformer;
                TransitionAnimationTo(PlayerState.Falling);
                CurrentState = PlayerState.Falling;
            }

            if (Input.IsActionPressed("jump")) {
                if (_numberOfFramesInJump < _amountJumpFrames) {
                    _numberOfFramesInJump++;
                } else {
                    _numberOfFramesInJump = 0;
                    TransitionAnimationTo(PlayerState.Falling);
                    CurrentState = PlayerState.Falling;
                }
            } else {
                _numberOfFramesInJump = 0;
                TransitionAnimationTo(PlayerState.Falling);
                CurrentState = PlayerState.Falling;
            }
        } else {
            _numberOfFramesInJump = 0;
            TransitionAnimationTo(PlayerState.Falling);
            CurrentState = PlayerState.Falling;
        }

        CheckDash();
    }

    private void HandleFallingState(double delta) {
        Move(delta);

        if (IsOnFloor()) {
            TransitionAnimationTo(PlayerState.Idle);
            CurrentState = PlayerState.Idle;
            _velocity.Y = 0;
        } else if (IsOnWall() && _canWallJump && IsWallClimbable()) {
            TransitionAnimationTo(PlayerState.OnWall);
            CurrentState = PlayerState.OnWall;
            _velocity.Y = WallSlideVelocity * UnitTransformer;
        } else {
            _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * UnitTransformer, Gravity * UnitTransformer);
            if (Input.IsActionJustPressed("jump") && _canDoubleJump && _doubleJumpReady) {
                _jumpBufferFrames = 0;
                _jumpModifier = 1.0f;
                _doubleJumpReady = false;
                _numberOfFramesInJump = 1;
                _amountJumpFrames = SecondJumpFrames;
                TransitionAnimationTo(PlayerState.Jumping);
                CurrentState = PlayerState.Jumping;
            } else if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6;
            }
        }

        CheckDash();
    }

    private bool IsWallClimbable() {
        var collision = GetLastSlideCollision();
        var collider = collision.GetCollider() as CollisionObject3D;
        
        if (collider != null) {
            return (collider.CollisionLayer & (1u << ((int)ClimbableWallLayer - 1))) != 0;
        }
        return false;
    }

    private void HandleOnWallState(double delta) {
        Move(delta);
        if (IsOnFloor()) {
            TransitionAnimationTo(PlayerState.Idle);
            CurrentState = PlayerState.Idle;
            _velocity.Y = 0;
        } else if (IsOnWall()) {
            _velocity.Y = WallSlideVelocity * UnitTransformer;
            var collision = GetLastSlideCollision();

            if (WantsToJump()) {
                _jumpModifier = WallJumpVelocityModifier;
                Vector3 wallNormal = collision.GetNormal();
                _velocity.X = (wallNormal * WallJumpHorizontalVelocity * UnitTransformer).X;
                _velocity.Y = JumpVelocity * _jumpModifier * UnitTransformer;
                _amountJumpFrames = FirstJumpFrames;
                TransitionAnimationTo(PlayerState.Jumping);
                CurrentState = PlayerState.Jumping;
            }
        } else {
            TransitionAnimationTo(PlayerState.Falling);
            CurrentState = PlayerState.Falling;
        }
        CheckDash();
    }

    private void HandleDashingState(double delta) {
        Vector3 dashDirection = new Vector3((float)_horizontalDirection, 0, 0).Normalized();
        _velocity = dashDirection * DashVelocity * UnitTransformer;
        if (_dashDurationTimer.IsStopped()) {
            if (IsOnFloor()) {
                TransitionAnimationTo(PlayerState.Idle);
                CurrentState = PlayerState.Idle;
            } else if (IsOnWall() && IsWallClimbable()) {
                TransitionAnimationTo(PlayerState.OnWall); 
                CurrentState = PlayerState.OnWall;
            } else {
                TransitionAnimationTo(PlayerState.Falling);
                CurrentState = PlayerState.Falling;
            }
        }
    }

    private void HandleEnteringAreaState(double delta) {
        var tween = GetTree().CreateTween();

        PostProcessRect.Visible = true;
        PostProcessRect.Material = TransitionMaterial;

        var noiseTexture = (NoiseTexture2D)TransitionMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        TransitionMaterial.SetShaderParameter("progress", 0.0f);

        tween.TweenProperty(TransitionMaterial, "shader_parameter/progress", 1.0f, 0.5f).SetDelay(0.1);
        tween.TweenCallback(Callable.From(() => {
            if (PlayerAreaToEnter != null) {
                CurrentState = PlayerState.NoControl;
                if (GetTree().CurrentScene is Level level) {
                    level.SaveState();
                }
                GetTree().ChangeSceneToPacked(PlayerAreaToEnter);
            }
        })).SetDelay(0.5f);

        CurrentState = PlayerState.NoControl;

    }

    private void HandleExitingAreaState(double delta) {

        var tween = GetTree().CreateTween();

        PostProcessRect.Visible = true;
        PostProcessRect.Material = TransitionMaterial;

        var noiseTexture = (NoiseTexture2D)TransitionMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        TransitionMaterial.SetShaderParameter("progress", 1.0f);

        tween.TweenProperty(TransitionMaterial, "shader_parameter/progress", 0.0f, 0.5f).SetDelay(0.4);
        tween.TweenCallback(Callable.From(() => {
            PostProcessRect.Visible = false;
        })).SetDelay(0.5f);
        TransitionAnimationTo(PlayerState.Idle);
        CurrentState = PlayerState.Idle;
    }
    
    private void HandleMiniDeathState(double delta) {
        var tween = GetTree().CreateTween();

        PostProcessRect.Visible = true;
        PostProcessRect.Material = TransitionMaterial;

        var noiseTexture = (NoiseTexture2D)TransitionMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        TransitionMaterial.SetShaderParameter("progress", 0.0f);

        tween.TweenProperty(TransitionMaterial, "shader_parameter/progress", 1.0f, 0.5f).SetDelay(0.1);
        tween.TweenCallback(Callable.From(() => {
            if (Global.Instance.MiniCheckPointSavedPosition is Vector3 savedPosition) {
                GlobalPosition = savedPosition;
            } else {
                GlobalPosition = Global.Instance.PlayerLastSavedTransform is Transform3D lastTransform ? lastTransform.Origin : Vector3.Zero;
            }
            TransitionAnimationTo(PlayerState.Idle);
            CurrentState = PlayerState.ExitingArea;
            HandleExitingAreaState(1.0);
        })).SetDelay(0.5f);

        CurrentState = PlayerState.NoControl;
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
        _velocity = _velocity.MoveToward(GetInputDirection() * MovementSpeed * UnitTransformer, MovementSpeed / 4 * UnitTransformer);
        _velocity.Y = yVelocity;
    }

    private void CheckDash() {
        if (Input.IsActionJustPressed("dash") && _canDash && _dashReadyToUse) {
            TransitionAnimationTo(PlayerState.Dashing);
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


    private void UpdateAnimationTree() {
        if (CurrentState == PlayerState.Idle) {
            _runningAnimationBlend = Mathf.Clamp(Mathf.Abs(_velocity.X) / MovementSpeed / (float)GetPhysicsProcessDeltaTime(), 0.0f, 1.0f);
        } else {
            _runningAnimationBlend = 0.0f;
        }
        _animationTree.Set("parameters/Run/blend_amount", _runningAnimationBlend);
        _animationTree.Set("parameters/Jump/blend_amount", _jumpAnimationBlend);
        _animationTree.Set("parameters/WallHug/blend_amount", _wallHugAnimationBlend);
        _animationTree.Set("parameters/Dash/blend_amount", _dashAnimationBlend);
    }

    private void TransitionAnimationTo(PlayerState targetState) {
        if (_animationTransitionTween != null) {
            _animationTransitionTween.Kill();
        }
        var tween = GetTree().CreateTween().SetParallel(true);
        _animationTransitionTween = tween;

        switch (targetState) {
            case PlayerState.Idle:
                tween.TweenProperty(this, nameof(_jumpAnimationBlend), 0.0f, 0.2f);
                tween.TweenProperty(this, nameof(_wallHugAnimationBlend), 0.0f, 0.2f);
                tween.TweenProperty(this, nameof(_dashAnimationBlend), 0.0f, 0.1f);
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                tween.TweenProperty(this, nameof(_jumpAnimationBlend), 1.0f, 0.2f);
                tween.TweenProperty(this, nameof(_runningAnimationBlend), 0.0f, 0.2f);
                tween.TweenProperty(this, nameof(_wallHugAnimationBlend), 0.0f, 0.2f);
                tween.TweenProperty(this, nameof(_dashAnimationBlend), 0.0f, 0.1f);
                break;
            case PlayerState.OnWall:
                tween.TweenProperty(this, nameof(_wallHugAnimationBlend), 1.0f, 0.1f);
                tween.TweenProperty(this, nameof(_runningAnimationBlend), 0.0f, 0.1f);
                tween.TweenProperty(this, nameof(_jumpAnimationBlend), 0.0f, 0.1f);
                tween.TweenProperty(this, nameof(_dashAnimationBlend), 0.0f, 0.1f);
                break;
            case PlayerState.Dashing:
                tween.TweenProperty(this, nameof(_dashAnimationBlend), 1.0f, 0.01f);
                tween.TweenProperty(this, nameof(_runningAnimationBlend), 0.0f, 0.01f);
                tween.TweenProperty(this, nameof(_jumpAnimationBlend), 0.0f, 0.01f);
                tween.TweenProperty(this, nameof(_wallHugAnimationBlend), 0.0f, 0.01f);
                break;
        }
    }

    public string GetSaveID() {
        return GetPath();
    }

    public Dictionary<string, Variant> SaveState() {
        var state = new Dictionary<string, Variant> { };

        state["health"] = Health;
        state["global_position"] = GlobalPosition;
        state["can_dash"] = _canDash;
        state["can_wall_jump"] = _canWallJump;
        state["can_double_jump"] = _canDoubleJump;

        return state;
    }

    public void LoadState(Dictionary<string, Variant> state) {
        if (state.ContainsKey("health")) {
            Health = (int)state["health"];
        }
        if (state.ContainsKey("global_position")) {
            GlobalPosition = (Vector3)state["global_position"];
        }
        if (state.ContainsKey("can_dash")) {
            _canDash = (bool)state["can_dash"];
        }
        if (state.ContainsKey("can_wall_jump")) {
            _canWallJump = (bool)state["can_wall_jump"];
        }
        if (state.ContainsKey("can_double_jump")) {
            _canDoubleJump = (bool)state["can_double_jump"];
        }
    }

    private void OverrideTransformOnFirstLoadIn() { 
        if (!Global.Instance.PlayerHasTakenTransform && Global.Instance.PlayerLastSavedTransform is Transform3D newTransform) {
            GlobalTransform = newTransform;
            Global.Instance.PlayerHasTakenTransform = true;
        }
    }

}


