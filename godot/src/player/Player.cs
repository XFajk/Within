using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody3D, ISavable {

    private const float UnitTransformer = 1.0f / 60.0f;

    [Signal]
    public delegate void HitEventHandler(int damage);

    [Signal]
    public delegate void HealEventHandler();

    public enum PlayerState {
        Idle, // has two animations blended based on speed
        Jumping, // has a seeked animation
        Falling, // has a animation
        OnWall, // has a animation
        Dashing, // has a animation
        Damaged, // has a animation
        AttackingFront, // has a seeked animation
        AttackingUp, // has a seeked animation
        Sleeping, // has a animation
        WakingUp, // has a seeked animation
        Crazy, // has a animation
        EnteringArea,
        ExitingArea,
        MiniDeath,
        Death, // has a animation
        NoControl,
    }

    private enum HorizontalDirection {
        Left = -1,
        Right = 1,
    }

    public PlayerState CurrentState = PlayerState.ExitingArea;

    public PackedScene PlayerAreaToEnter = null;

    // Visual Effects Variables
    private PackedScene _hitUiParticles = GD.Load<PackedScene>("res://scenes/VFX/small_death_particles.tscn");

    // Animation Variables    
    private AnimationTree _animationTree;

    private PlayerAnimationBlender _animationBlender;

    private Tween _animationTransitionTween = null;

    // Transition Variables
    private Tween _transitionTween = null;

    // Dialog System Variables
    public Label3D TextBox;

    // Wrench Trail Variables
    private Skeleton3D _playerSkeleton;
    private PackedScene _wrenchTrail = GD.Load<PackedScene>("res://scenes/VFX/wrench_trail.tscn");
    private Node3D _wrenchTrailInstance;

    [ExportGroup("Combat Settings")]

    [Export]
    public int MaxDamage = 5;

    private HealthBar _healthBar;

    private Area3D _hitBoxArea;

    private Area3D _frontAttackBoxArea;
    private Area3D _aboveAttackBoxArea;

    private Timer _attackDurationTimer = new();

    [ExportGroup("Camera Settings")]
    public Camera3D Camera;

    public ColorRect PostProcessRect;

    public ShaderMaterial TransitionMaterial = GD.Load<ShaderMaterial>("res://src/shaders/transition/transition_material.tres");
    public ShaderMaterial HitMaterial = GD.Load<ShaderMaterial>("res://src/shaders/low_health/low_health_material.tres");

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

    private Node3D _dashTrailInstance;

    private RayCast3D _floorDetector;

    [ExportGroup("Double Jump Settings")]
    private bool _canDoubleJump = false;

    [Export]
    public int SecondJumpFrames = 10;

    private bool _doubleJumpReady = true;

    public override void _Ready() {

        _canWallJump = Global.Instance.PlayerHasWallJumpAbility;
        _canDash = Global.Instance.PlayerHasDashAbility;
        _canDoubleJump = Global.Instance.PlayerHasDoubleJumpAbility;

        // Create and set up floor detector raycast
        _floorDetector = new RayCast3D();
        AddChild(_floorDetector);
        _floorDetector.TargetPosition = new Vector3(0, -20, 0); // 20 units down
        _floorDetector.CollisionMask = (1 << 0) | (1 << 4); // Layer 1 and 5 (0-based index)
        
        // Force an immediate update of the raycast
        _floorDetector.ForceRaycastUpdate();

        // If we hit something, position the player above it with proper offset
        if (_floorDetector.IsColliding()) {
            Vector3 collisionPoint = _floorDetector.GetCollisionPoint();
            // Add 0.3 units (half of the player's height) to position player properly
            GlobalPosition = new Vector3(GlobalPosition.X, collisionPoint.Y + 0.3f, GlobalPosition.Z);
        }

        _hitBoxArea = GetNode<Area3D>("HitBox");
        _frontAttackBoxArea = GetNode<Area3D>("FrontAttackBox");
        _frontAttackBoxArea.Monitorable = false;
        _aboveAttackBoxArea = GetNode<Area3D>("AboveAttackBox");
        _aboveAttackBoxArea.Monitorable = false;

        AddChild(_attackDurationTimer);

        _attackDurationTimer.OneShot = true;
        _attackDurationTimer.Timeout += () => {
            FigureOutStateAfterAnimationState();
            _aboveAttackBoxArea.Monitorable = false;
            _frontAttackBoxArea.Monitorable = false;
        };

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

        _animationBlender = new PlayerAnimationBlender() { PlayerAnimationTree = _animationTree, Name = "PlayerAnimationBlender" };
        AddChild(_animationBlender);

        TextBox = GetNode<Label3D>("TextBox");

        _healthBar = Camera.GetNode<HealthBar>("UserInterface/HealthBar");

        _playerSkeleton = GetNode<Skeleton3D>("Armature/Skeleton3D");

        HandleExitingAreaState(1.0);

        if (Global.Instance.RespawningInProgress) {
            EmitSignalHeal();
        }

        // this so that the player when first loading into the game from the menu takes the position
        // that was last saved manualy at a save point if this wasent here the player would at a radom auto save position
        CallDeferred(nameof(OverrideTransformOnFirstLoadIn));
    }

    public override void _Process(double delta) {
        Position = new Vector3(Position.X, Position.Y, 0);

        if (_hitBoxArea.GetOverlappingAreas().Count > 0 && !_healthBar.IsInvincible) {
            OnDamaged();
        }

        UpdateAnimationTree();
        UpdateWrenchTransform();

        CheckForDeath();

        MoveCamera(delta);
        FigureOutHorizontalDirection();
    }

    private void MoveCamera(double delta) {
        if (Camera != null) {
            // Calculate the forward position based on the player's velocity and direction
            Vector3 targetPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y, CameraDistance);

            Camera.GlobalPosition = Camera.GlobalPosition.Lerp(targetPosition, CameraLerpSpeed * (float)delta);
        }
    }

    private void FigureOutHorizontalDirection() {
        if (CurrentState == PlayerState.Damaged) {
            return;
        }
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

    private void CheckForDeath() {
        if (_healthBar.Health <= 0 && CurrentState != PlayerState.Death) {
            TransitionAnimationTo(PlayerState.Death);
            CurrentState = PlayerState.Death;
        }
    }

    private void UpdateWrenchTransform() {
        int boneIndex = _playerSkeleton.FindBone("Bone");
        if (_wrenchTrailInstance != null && IsInstanceValid(_wrenchTrailInstance)) {
            _wrenchTrailInstance.Transform = _playerSkeleton.GetBoneGlobalPose(boneIndex);
        }
    }

    public override void _PhysicsProcess(double delta) {
        ProcessJumpBuffer();
        ResetDoubleJump();

        if (_healthBar.Health < 2 && CurrentState == PlayerState.MiniDeath) {
            TransitionAnimationTo(PlayerState.Death);
            CurrentState = PlayerState.Death;
        }

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
            case PlayerState.AttackingFront:
            case PlayerState.AttackingUp:
                Move(delta);
                if (!IsOnFloor()) {
                    _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * UnitTransformer, Gravity * UnitTransformer);
                }
                break;
            case PlayerState.Damaged:
                float yVelocity = _velocity.Y;
                _velocity = _velocity.MoveToward(Vector3.Zero, MovementSpeed / 4 * UnitTransformer);
                _velocity.Y = yVelocity;
                if (!IsOnFloor()) {
                    _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * UnitTransformer, Gravity * UnitTransformer);
                } else {
                    TransitionAnimationTo(PlayerState.Idle);
                    CurrentState = PlayerState.Idle;
                }
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
            case PlayerState.Death:
                HandleDeathState(delta);
                break;
            case PlayerState.NoControl:
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
        CheckAttack();
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

        CheckAttack();
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
        CheckAttack();
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
            FigureOutStateAfterAnimationState();
        }
    }

    private void HandleEnteringAreaState(double delta) {
        _velocity = Vector3.Zero;
        if (_transitionTween != null) {
            _transitionTween.Kill();
        }
        _transitionTween = GetTree().CreateTween();

        PostProcessRect.Visible = true;
        PostProcessRect.Material = TransitionMaterial;

        var noiseTexture = (NoiseTexture2D)TransitionMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        TransitionMaterial.SetShaderParameter("progress", 0.0f);

        _transitionTween.TweenProperty(TransitionMaterial, "shader_parameter/progress", 1.0f, 0.5f).SetDelay(0.1);
        _transitionTween.TweenCallback(Callable.From(() => {
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
        _velocity = Vector3.Zero;
        if (_transitionTween != null) {
            _transitionTween.Kill();
        }

        _transitionTween = GetTree().CreateTween();

        PostProcessRect.Visible = true;
        PostProcessRect.Material = TransitionMaterial;

        var noiseTexture = (NoiseTexture2D)TransitionMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        TransitionMaterial.SetShaderParameter("progress", 1.0f);
        _transitionTween.TweenProperty(TransitionMaterial, "shader_parameter/progress", 0.0f, 0.5f).SetDelay(0.4);
        _transitionTween.TweenCallback(Callable.From(() => {
            TransitionAnimationTo(PlayerState.Idle);
            CurrentState = PlayerState.Idle;
        }));
        _transitionTween.TweenCallback(Callable.From(() => {
            PostProcessRect.Visible = false;
        })).SetDelay(0.5f);

        CurrentState = PlayerState.NoControl;
    }

    private void HandleMiniDeathState(double delta) {
        if (_healthBar.Health == 1) {
            TransitionAnimationTo(PlayerState.Death);
            CurrentState = PlayerState.Death;
            return;
        }

        _velocity = Vector3.Zero;

        if (_transitionTween != null) {
            _transitionTween.Kill();
        }
        _transitionTween = GetTree().CreateTween().SetParallel(true);

        PostProcessRect.Visible = false;

        HitFrame(0.4f);

        Node3D particles = (Node3D)_hitUiParticles.Instantiate();
        GetParent().AddChild(particles);
        particles.GlobalPosition = GlobalPosition;

        EmitSignalHit(1);

        PostProcessRect.Visible = true;
        PostProcessRect.Material = HitMaterial;

        var noiseTexture = (NoiseTexture2D)HitMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        HitMaterial.SetShaderParameter("progress", 0.2f);
        HitMaterial.SetShaderParameter("smooth_amount", 0.3f);

        _transitionTween.TweenProperty(HitMaterial, "shader_parameter/progress", 1.0f, 0.7f);
        _transitionTween.TweenProperty(HitMaterial, "shader_parameter/smooth_amount", 0.0f, 0.7f);
        _transitionTween.TweenCallback(Callable.From(() => {
            if (Global.Instance.MiniCheckPointSavedPosition is Vector3 savedPosition) {
                GlobalPosition = savedPosition;
            } else {
                if (Global.Instance.PlayerLastSavedTransform is Transform3D lastTransform) {
                    GlobalPosition = lastTransform.Origin;
                }
            }

            // Check for floor below the current position and adjust if needed
            _floorDetector.GlobalPosition = GlobalPosition;
            _floorDetector.ForceRaycastUpdate();
            
            if (_floorDetector.IsColliding())
            {
                Vector3 collisionPoint = _floorDetector.GetCollisionPoint();
                // Add 0.3 units (half of the player's height) to position player properly
                GlobalPosition = new Vector3(GlobalPosition.X, collisionPoint.Y + 0.3f, GlobalPosition.Z);
            }

            PostProcessRect.Visible = false;
            TransitionAnimationTo(PlayerState.Idle);
            CurrentState = PlayerState.ExitingArea;
        })).SetDelay(1.3f);

        CurrentState = PlayerState.NoControl;
    }

    private void HandleDeathState(double delta) {
        _velocity = Vector3.Zero;
        if (Global.Instance.RespawningInProgress) {
            return;
        }

        if (_transitionTween != null) {
            _transitionTween.Kill();
        }
        _transitionTween = GetTree().CreateTween().SetParallel(true);

        HitFrame(0.4f);

        Node3D particles = (Node3D)_hitUiParticles.Instantiate();
        GetParent().AddChild(particles);
        particles.GlobalPosition = GlobalPosition;

        EmitSignalHit(1);

        PostProcessRect.Visible = true;
        PostProcessRect.Material = HitMaterial;

        var noiseTexture = (NoiseTexture2D)HitMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        HitMaterial.SetShaderParameter("progress", 0.0f);
        HitMaterial.SetShaderParameter("smooth_amount", 1.0f);

        _transitionTween.TweenProperty(HitMaterial, "shader_parameter/progress", 1.0f, 1.0f);
        _transitionTween.TweenProperty(HitMaterial, "shader_parameter/smooth_amount", 0.0f, 1.0f);
        _transitionTween.TweenCallback(Callable.From(() => {
            Global.Instance.LoadProgressData();
            Global.Instance.PlayerHasTakenTransform = false;
        })).SetDelay(1.6f);

        Global.Instance.RespawningInProgress = true;
    }

    public Vector3 GetInputDirection() {
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

    private void CheckAttack() {
        bool isAttackAlreadyInProgress = !_attackDurationTimer.IsStopped();
        if (isAttackAlreadyInProgress) {
            return;
        }

        if (Input.IsActionJustPressed("hit") && Input.IsActionPressed("look_up")) {
            TransitionAnimationTo(PlayerState.AttackingUp);
            CurrentState = PlayerState.AttackingUp;

            _numberOfFramesInJump = 0;

            _aboveAttackBoxArea.Monitorable = true;

            if (!IsInstanceValid(_wrenchTrailInstance)) {
                _wrenchTrailInstance = (Node3D)_wrenchTrail.Instantiate();
                _playerSkeleton.AddChild(_wrenchTrailInstance);
            }

            _attackDurationTimer.Start(0.2f);
        } else if (Input.IsActionJustPressed("hit")) {
            TransitionAnimationTo(PlayerState.AttackingFront);
            CurrentState = PlayerState.AttackingFront;

            _numberOfFramesInJump = 0;

            _frontAttackBoxArea.Monitorable = true;

            if (!IsInstanceValid(_wrenchTrailInstance)) {
                _wrenchTrailInstance = (Node3D)_wrenchTrail.Instantiate();
                _playerSkeleton.AddChild(_wrenchTrailInstance);
            }
        
            _attackDurationTimer.Start(0.2f);
        }
    }

    private void ProcessJumpBuffer() {
        if (_jumpBufferFrames > 0) {
            _jumpBufferFrames--;
        }
    }

    private void ResetDoubleJump() {
        if (IsOnFloor() || (IsOnWall() && _canWallJump && IsWallClimbable())) _doubleJumpReady = true;
    }

    private bool WantsToJump() {
        return (Input.IsActionJustPressed("jump") || _jumpBufferFrames > 0);
    }

    private void OnDamaged() {
        if (_healthBar.IsInvincible || CurrentState == PlayerState.Death) return;
        if (_healthBar.Health == 1) {
            TransitionAnimationTo(PlayerState.Death);
            CurrentState = PlayerState.Death;
            return;
        }


        TransitionAnimationTo(PlayerState.Damaged);
        CurrentState = PlayerState.Damaged;
        _velocity = new Vector3((float)_horizontalDirection * -5.0f, 3.0f, 0);

         Node3D particles = (Node3D)_hitUiParticles.Instantiate();
        GetParent().AddChild(particles);
        particles.GlobalPosition = GlobalPosition;

        EmitSignalHit(1);
        _healthBar.MakeInvincible(2.0f);

        HitFrame(0.2f);
    }

    private void UpdateAnimationTree() {
        if (CurrentState == PlayerState.Idle) {
            _animationTree.Set("parameters/Run/blend_amount", Mathf.Clamp(Mathf.Abs(_velocity.X) / MovementSpeed / (float)GetPhysicsProcessDeltaTime(), 0.0f, 1.0f));
        } else {
            _animationTree.Set("parameters/Run/blend_amount", 0.0f);
        }
    }

    private void TransitionAnimationTo(PlayerState targetState) {
        _animationBlender.CurrentAnimationState = targetState;
    }

    public string GetSaveID() {
        return GetPath();
    }

    public Dictionary<string, Variant> SaveState() {
        var state = new Dictionary<string, Variant> { };

        state["global_position"] = GlobalPosition;
        state["can_dash"] = _canDash;
        state["can_wall_jump"] = _canWallJump;
        state["can_double_jump"] = _canDoubleJump;

        return state;
    }

    public void LoadState(Dictionary<string, Variant> state) {
        if (state.ContainsKey("health")) {
            _healthBar.Health = (int)state["health"];
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

    private async void HitFrame(float duration) {
        PostProcessRect.Visible = false;
        GetTree().Paused = true;
        await ToSignal(GetTree().CreateTimer(duration, true, false, true), SceneTreeTimer.SignalName.Timeout);
        GetTree().Paused = false;
        PostProcessRect.Visible = true;
    }

    private void FigureOutStateAfterAnimationState() {
        if (IsOnFloor()) {
            TransitionAnimationTo(PlayerState.Idle);
            CurrentState = PlayerState.Idle;
        } else if (IsOnWall() && IsWallClimbable() && _canWallJump) {
            TransitionAnimationTo(PlayerState.OnWall);
            CurrentState = PlayerState.OnWall;
        } else {
            TransitionAnimationTo(PlayerState.Falling);
            CurrentState = PlayerState.Falling;
        }
    }
}
