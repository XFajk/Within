using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody3D, ISavable {

    public const float UnitTransformer = 1.0f / 60.0f;

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


    // Sound Variables
    private AudioStreamPlayer3D _swooshSound;
    private AudioStreamPlayer3D _deathSound;
    private AudioStreamPlayer3D _feetSounds;

    [Export]
    public AudioStreamRandomizer ConcreteFootstepSounds;

    [Export]
    public AudioStreamRandomizer MetalFootstepSounds;

    [Export]
    public AudioStreamRandomizer ConcreteJumpSounds;

    [Export]
    public AudioStreamRandomizer ConcreteJumpLandSounds;

    [Export]
    public AudioStreamRandomizer MetalJumpSounds;

    [Export]
    public AudioStreamRandomizer MetalJumpLandSounds;

    // MISC Variables
    public bool EmitBlood = true;

    public AbilityUnlocker.Ability AbilityThatIsCurrentlyBeingUnlocked = AbilityUnlocker.Ability.All;

    public Timer WakeUpTimer = new();

    // Inventory Pickup Variables
    private Array<string> _inventory = new Array<string>();

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
    public int Damage = 5;
    private bool _hitThisFrame = false;

    private HealthBar _healthBar;

    private Area3D _hitBoxArea;

    private Area3D _frontAttackBoxArea;
    private Area3D _aboveAttackBoxArea;

    private Timer _attackDurationTimer = new();
    private Timer _attackCooldownTimer = new();

    [ExportGroup("Camera Settings")]
    public PlayerCamera Camera;

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

    private Vector3 _velocity = Vector3.Zero;

    private HorizontalDirection _horizontalDirection = HorizontalDirection.Right;

    private float _jumpBufferFrames = 0;
    private float _jumpTimeElapsed = 0.0f;
    private float _currentJumpDuration = 0.333f;
    private float _jumpDuration = 0.333f; // 20 frames at 60fps
    private float _doubleJumpDuration = 0.167f; // 10 frames at 60fps
    private float _coyoteTimeElapsed = 0.0f;
    private const float COYOTE_TIME_DURATION = 0.1f;

    private float _jumpModifier = 1.0f;

    [ExportGroup("Wall Jump Settings")]
    public bool CanWallJump = false;

    [Export]
    public float WallJumpHorizontalVelocity = 200.0f;

    [Export]
    public float WallJumpVelocityModifier = 2.0f;

    [Export]
    public float WallSlideVelocity = -70.0f;

    [Export]
    public uint ClimbableWallLayer = 5; // Layer 5 will be for climbable walls

    [ExportGroup("Dash Settings")]
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

    private Node3D _dashTrailInstance;

    private RayCast3D _floorDetector;

    [ExportGroup("Double Jump Settings")]
    public bool CanDoubleJump = false;

    [Export]
    public int SecondJumpFrames = 10;

    private bool _doubleJumpReady = true;

    public override void _Ready() {

        Global.Instance.MiniCheckPointSavedPosition = GlobalPosition;

        CanWallJump = Global.Instance.PlayerHasWallJumpAbility;
        CanDash = Global.Instance.PlayerHasDashAbility;
        CanDoubleJump = Global.Instance.PlayerHasDoubleJumpAbility;

        _inventory = Global.Instance.PlayerInventory;

        if (Global.Instance.TransitionExitPosition is Vector3 transitionPosition) {
            CallDeferred(nameof(SetExitPosition), transitionPosition);
            Global.Instance.TransitionExitPosition = null;
        }

        AddChild(WakeUpTimer);
        WakeUpTimer.OneShot = true;
        WakeUpTimer.WaitTime = 8.7917f;
        WakeUpTimer.Timeout += () => {
            FigureOutStateAfterAnimationState();
        };

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

        _deathSound = GetNode<AudioStreamPlayer3D>("Audio/DeathSound");
        _swooshSound = GetNode<AudioStreamPlayer3D>("Audio/SwooshSound");

        _hitBoxArea = GetNode<Area3D>("HitBox");

        _frontAttackBoxArea = GetNode<Area3D>("FrontAttackBox");
        _frontAttackBoxArea.Position += Vector3.Back * 1000f;

        _aboveAttackBoxArea = GetNode<Area3D>("AboveAttackBox");
        _aboveAttackBoxArea.Position += Vector3.Back * 1000f;

        AddChild(_attackDurationTimer);

        _attackDurationTimer.OneShot = true;
        _attackDurationTimer.Timeout += () => {
            FigureOutStateAfterAnimationState();
            _aboveAttackBoxArea.Position += Vector3.Back * 1000f;
            _frontAttackBoxArea.Position += Vector3.Back * 1000f;
        };

        AddChild(_attackCooldownTimer);

        _attackCooldownTimer.OneShot = true;
        _attackCooldownTimer.WaitTime = 0.4f;

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
            Camera = GetNode<PlayerCamera>("Camera");
            PostProcessRect = Camera.GetNode<ColorRect>("PostProcessRect");
            PostProcessRect.Visible = false;
            Camera.TopLevel = true;


            var tutorialUi = Camera.GetNode<AbilityTutorial>("TutorialUi");
            tutorialUi.TutorialCompleted += () => {
                tutorialUi.Visible = false;

                if (_transitionTween != null) {
                    _transitionTween.Kill();
                }
                _transitionTween = GetTree().CreateTween();

                _transitionTween.TweenProperty(HitMaterial, "shader_parameter/progress", 0.0f, 0.5f);
                _transitionTween.TweenProperty(HitMaterial, "shader_parameter/smooth_amount", 0.3f, 0.5f);
                _transitionTween.TweenCallback(Callable.From(() => {
                    TransitionAnimationTo(PlayerState.Idle);
                    CurrentState = PlayerState.Idle;
                    PostProcessRect.Visible = false;
                }));
            };
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
        // GD.Print("Dash: " + _canDash + ", Wall Jump: " + _canWallJump + ", Double Jump: " + _canDoubleJump);
        Position = new Vector3(Position.X, Position.Y, 0);
        Camera.GlobalPosition = new Vector3(Camera.GlobalPosition.X, Camera.GlobalPosition.Y, CameraDistance);

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

            Camera.Move(Camera.GlobalPosition.Lerp(targetPosition, CameraLerpSpeed * (float)delta));
        }
    }

    private void FigureOutHorizontalDirection() {
        if (CurrentState == PlayerState.Damaged
        || CurrentState == PlayerState.NoControl
        || CurrentState == PlayerState.EnteringArea
        || CurrentState == PlayerState.ExitingArea
        || CurrentState == PlayerState.Death
        || CurrentState == PlayerState.MiniDeath
        || CurrentState == PlayerState.Sleeping
        || CurrentState == PlayerState.WakingUp
        || CurrentState == PlayerState.Crazy) {
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


        float yVelocity = _velocity.Y;

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
                yVelocity = _velocity.Y;
                _velocity = _velocity.MoveToward(Vector3.Zero, MovementSpeed / 4 * UnitTransformer);
                _velocity.Y = yVelocity;
                if (_hitThisFrame) {
                    _hitThisFrame = false;
                    break;
                }
                if (!IsOnFloor()) {
                    _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * UnitTransformer, Gravity * UnitTransformer);
                } else {
                    TransitionAnimationTo(PlayerState.Idle);
                    CurrentState = PlayerState.Idle;
                    _velocity = Vector3.Zero;
                }
                break;
            case PlayerState.Crazy:
                yVelocity = _velocity.Y;
                _velocity = _velocity.MoveToward(Vector3.Zero, MovementSpeed / 4 * UnitTransformer);
                _velocity.Y = yVelocity;
                if (!IsOnFloor()) {
                    _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * UnitTransformer, Gravity * UnitTransformer);
                } else {
                    TransitionAnimationTo(PlayerState.Crazy);
                    if (PostProcessRect.Visible == false)
                        HandleCrazyStateGrounded();
                    _velocity = Vector3.Zero;
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
                yVelocity = _velocity.Y;
                _velocity = _velocity.MoveToward(Vector3.Zero, MovementSpeed / 4 * UnitTransformer);
                _velocity.Y = yVelocity;
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
            _jumpTimeElapsed = 0.0f;
            TransitionAnimationTo(PlayerState.Jumping);
            CurrentState = PlayerState.Jumping;
        } else if (!IsOnFloor()) {
            if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6.0f * UnitTransformer;
            }
            _coyoteTimeElapsed = 0.0f;  // Start coyote time when leaving ground
            TransitionAnimationTo(PlayerState.Jumping);
            CurrentState = PlayerState.Falling;
        }
        CheckAttack();
        CheckDash();
    }

    private void HandleJumpingState(double delta) {
        Move(delta);
        _coyoteTimeElapsed = COYOTE_TIME_DURATION + 1000.0f; // Disable coyote time during jump

        if (IsOnFloor() || _jumpTimeElapsed < _jumpDuration) {
            _velocity.Y = JumpVelocity * _jumpModifier * UnitTransformer;

            if (IsOnCeiling()) {
                _jumpTimeElapsed = _jumpDuration; // Force jump end
                _velocity.Y = -Gravity * UnitTransformer;
                TransitionAnimationTo(PlayerState.Falling);
                CurrentState = PlayerState.Falling;
            }

            if (Input.IsActionPressed("jump")) {
                _jumpTimeElapsed += (float)delta;
                if (_jumpTimeElapsed >= _jumpDuration) {
                    _jumpTimeElapsed = 0.0f;
                    TransitionAnimationTo(PlayerState.Falling);
                    CurrentState = PlayerState.Falling;
                }
            } else {
                _jumpTimeElapsed = _jumpDuration; // Force jump end
                TransitionAnimationTo(PlayerState.Falling);
                CurrentState = PlayerState.Falling;
            }
        } else {
            _jumpTimeElapsed = 0.0f;
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
            _coyoteTimeElapsed = 0.0f;  // Reset coyote time when touching floor
        } else if (IsOnWall() && CanWallJump && IsWallClimbable()) {
            TransitionAnimationTo(PlayerState.OnWall);
            CurrentState = PlayerState.OnWall;
            _velocity.Y = WallSlideVelocity * UnitTransformer;
            _coyoteTimeElapsed = COYOTE_TIME_DURATION;  // Cancel coyote time on wall
        } else {
            _coyoteTimeElapsed += (float)delta;
            _velocity.Y = Mathf.MoveToward(_velocity.Y, TerminalVelocity * UnitTransformer, Gravity * UnitTransformer);

            // Regular jump during coyote time
            if (Input.IsActionJustPressed("jump") && _coyoteTimeElapsed <= COYOTE_TIME_DURATION) {
                _jumpBufferFrames = 0;
                _jumpModifier = 1.0f;
                _jumpTimeElapsed = 0.0f;
                TransitionAnimationTo(PlayerState.Jumping);
                CurrentState = PlayerState.Jumping;
                return;
            }

            // Double jump check
            if (Input.IsActionJustPressed("jump") && CanDoubleJump && _doubleJumpReady) {
                _jumpBufferFrames = 0;
                _jumpModifier = 1.0f;
                _doubleJumpReady = false;
                _jumpTimeElapsed = 0.0f;
                TransitionAnimationTo(PlayerState.Jumping);
                CurrentState = PlayerState.Jumping;
            } else if (Input.IsActionJustPressed("jump")) {
                _jumpBufferFrames = 6.0f * UnitTransformer;
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
                _jumpTimeElapsed = 0.0f;
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

    private void HandleCrazyStateGrounded() {
        if (_transitionTween != null) {
            _transitionTween.Kill();
        }
        _transitionTween = GetTree().CreateTween();

        PostProcessRect.Visible = true;
        PostProcessRect.Material = HitMaterial;

        var noiseTexture = (NoiseTexture2D)HitMaterial.GetShaderParameter("noise_texture");
        noiseTexture.Noise.Set("seed", GD.Randi());

        HitMaterial.SetShaderParameter("progress", 0.0f);
        HitMaterial.SetShaderParameter("smooth_amount", 0.15f);
        GD.Print("Starting crazy state transition animation.");

        _transitionTween.TweenProperty(HitMaterial, "shader_parameter/progress", 0.9f, 2.0f);

        for (int i = 0; i < 3; i++) {
            _transitionTween.TweenProperty(HitMaterial, "shader_parameter/progress", 0.8f, 1.0f);
            _transitionTween.TweenProperty(HitMaterial, "shader_parameter/progress", 0.9f, 1.0f);
        }
        _transitionTween.TweenProperty(HitMaterial, "shader_parameter/progress", 1.0f, 1.0f);
        _transitionTween.TweenProperty(HitMaterial, "shader_parameter/smooth_amount", 0.0f, 1.0f);
        _transitionTween.TweenCallback(Callable.From(() => {
            Camera.ShowAbilityTutorial(AbilityThatIsCurrentlyBeingUnlocked);
            AbilityThatIsCurrentlyBeingUnlocked = AbilityUnlocker.Ability.All;
        })).SetDelay(1.0f);

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
            if (!Global.Instance.IsInMainMenu) {
                TransitionAnimationTo(PlayerState.Idle);
                CurrentState = PlayerState.Idle;
            }
        }));
        _transitionTween.TweenCallback(Callable.From(() => {
            PostProcessRect.Visible = false;
        })).SetDelay(0.5f);

        if (Global.Instance.IsInMainMenu && !Godot.FileAccess.FileExists("user://progress.global.dat")) {
            GD.Print("Player loading from main menu save data.");
            TransitionAnimationTo(PlayerState.Sleeping);
            CurrentState = PlayerState.Sleeping;
        } else {
            GD.Print("Player starting new game from main menu.");
            CurrentState = PlayerState.NoControl;
        }
    }

    private void HandleMiniDeathState(double delta) {
        if (_healthBar.Health == 1) {
            TransitionAnimationTo(PlayerState.Death);
            CurrentState = PlayerState.Death;
            return;
        }

        _velocity = Vector3.Zero;

        _deathSound.Play();
        var tween = GetTree().CreateTween();
        var masterBusIndex = AudioServer.GetBusIndex("Music&SoundFX");
        var lowPassFilterEffect = AudioServer.GetBusEffect(masterBusIndex, 0) as AudioEffectLowPassFilter;

        lowPassFilterEffect.CutoffHz = 300.0f;
        tween.TweenProperty(lowPassFilterEffect, "cutoff_hz", 20500.0f, 2.0f).SetDelay(2.0f);


        if (_transitionTween != null) {
            _transitionTween.Kill();
        }
        _transitionTween = GetTree().CreateTween().SetParallel(true);

        PostProcessRect.Visible = false;

        HitFrame(0.4f);

        if (EmitBlood) {
            Node3D particles = (Node3D)_hitUiParticles.Instantiate();
            GetParent().AddChild(particles);
            particles.GlobalPosition = GlobalPosition;
        }

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
                if (Global.Instance.PlayerCameraLastSavedTransform is Transform3D lastCameraTransfrom) {
                    Camera.GlobalTransform = lastCameraTransfrom;
                }
            }

            if (Global.Instance.MiniCheckPointCameraSpawnPoint is Vector3 cameraSpawnPoint) {
                Camera.GlobalPosition = cameraSpawnPoint;
            }

            // Check for floor below the current position and adjust if needed
            _floorDetector.GlobalPosition = GlobalPosition;
            _floorDetector.ForceRaycastUpdate();

            if (_floorDetector.IsColliding()) {
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
        _deathSound.Play();
        var tween = GetTree().CreateTween();
        var masterBusIndex = AudioServer.GetBusIndex("Music&SoundFX");
        var lowPassFilterEffect = AudioServer.GetBusEffect(masterBusIndex, 0) as AudioEffectLowPassFilter;

        lowPassFilterEffect.CutoffHz = 300.0f;
        tween.TweenProperty(lowPassFilterEffect, "cutoff_hz", 20500.0f, 2.0f).SetDelay(2.6f);

        if (_transitionTween != null) {
            _transitionTween.Kill();
        }
        _transitionTween = GetTree().CreateTween().SetParallel(true);

        HitFrame(0.4f);

        if (EmitBlood) {
            Node3D particles = (Node3D)_hitUiParticles.Instantiate();
            GetParent().AddChild(particles);
            particles.GlobalPosition = GlobalPosition;
        }

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
        if (Input.IsActionJustPressed("dash") && CanDash && _dashReadyToUse) {
            TransitionAnimationTo(PlayerState.Dashing);
            _jumpTimeElapsed = _jumpDuration; // Force jump end
            CurrentState = PlayerState.Dashing;
            _dashReadyToUse = false;
            _dashDurationTimer.Start();
            _dashRecoverTimer.Start();
        }
    }

    private void CheckAttack() {
        bool isAttackAlreadyInProgress = !(_attackDurationTimer.IsStopped() && _attackCooldownTimer.IsStopped());
        if (isAttackAlreadyInProgress) {
            return;
        }

        if (Input.IsActionJustPressed("hit") && Input.IsActionPressed("look_up")) {
            TransitionAnimationTo(PlayerState.AttackingUp);
            CurrentState = PlayerState.AttackingUp;

            _jumpTimeElapsed = _jumpDuration; // Force jump end

            _aboveAttackBoxArea.Position = Vector3.Zero;

            if (!IsInstanceValid(_wrenchTrailInstance)) {
                _wrenchTrailInstance = (Node3D)_wrenchTrail.Instantiate();
                _playerSkeleton.AddChild(_wrenchTrailInstance);
            }

            _attackDurationTimer.Start(0.2f);
            _attackCooldownTimer.Start();
        } else if (Input.IsActionJustPressed("hit")) {
            TransitionAnimationTo(PlayerState.AttackingFront);
            CurrentState = PlayerState.AttackingFront;

            _jumpTimeElapsed = _jumpDuration; // Force jump end

            _frontAttackBoxArea.Position = Vector3.Zero;

            if (!IsInstanceValid(_wrenchTrailInstance)) {
                _wrenchTrailInstance = (Node3D)_wrenchTrail.Instantiate();
                _playerSkeleton.AddChild(_wrenchTrailInstance);
            }

            _attackDurationTimer.Start(0.2f);
            _attackCooldownTimer.Start();
        }
    }

    private void ProcessJumpBuffer() {
        if (_jumpBufferFrames > 0) {
            _jumpBufferFrames -= (float)GetPhysicsProcessDeltaTime();
        }
    }

    private void ResetDoubleJump() {
        if (IsOnFloor() || (IsOnWall() && CanWallJump && IsWallClimbable())) _doubleJumpReady = true;
    }

    private bool WantsToJump() {
        return (Input.IsActionJustPressed("jump") || _jumpBufferFrames > 0);
    }

    public void SetExitPosition(Vector3 position) {
        GlobalPosition = position;
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
    }

    private void OnDamaged() {
        if (_healthBar.IsInvincible || CurrentState == PlayerState.Death) return;
        if (_healthBar.Health == 1) {
            TransitionAnimationTo(PlayerState.Death);
            CurrentState = PlayerState.Death;
            return;
        }

        _deathSound.Play();
        var tween = GetTree().CreateTween();
        var masterBusIndex = AudioServer.GetBusIndex("Music&SoundFX");
        var lowPassFilterEffect = AudioServer.GetBusEffect(masterBusIndex, 0) as AudioEffectLowPassFilter;

        lowPassFilterEffect.CutoffHz = 300.0f;
        tween.TweenProperty(lowPassFilterEffect, "cutoff_hz", 20500.0f, 2.0f).SetDelay(1.0f);

        _hitThisFrame = true;

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

    public void UnlockAbility(AbilityUnlocker.Ability ability) {
        switch (ability) {
            case AbilityUnlocker.Ability.Dash:
                CanDash = true;
                Global.Instance.PlayerHasDashAbility = true;
                break;
            case AbilityUnlocker.Ability.WallJump:
                CanWallJump = true;
                Global.Instance.PlayerHasWallJumpAbility = true;
                break;
            case AbilityUnlocker.Ability.DoubleJump:
                CanDoubleJump = true;
                Global.Instance.PlayerHasDoubleJumpAbility = true;
                break;
            case AbilityUnlocker.Ability.All:
                CanDash = true;
                CanWallJump = true;
                CanDoubleJump = true;
                Global.Instance.PlayerHasDashAbility = true;
                Global.Instance.PlayerHasWallJumpAbility = true;
                Global.Instance.PlayerHasDoubleJumpAbility = true;
                break;
        }
        AbilityThatIsCurrentlyBeingUnlocked = ability;
        TransitionAnimationTo(PlayerState.Crazy);
        CurrentState = PlayerState.Crazy;
    }

    private void UpdateAnimationTree() {
        if (CurrentState == PlayerState.Idle) {
            _animationTree.Set("parameters/Run/blend_amount", Mathf.Clamp(Mathf.Abs(_velocity.X) / MovementSpeed / (float)GetPhysicsProcessDeltaTime(), 0.0f, 1.0f));
        } else {
            _animationTree.Set("parameters/Run/blend_amount", 0.0f);
        }
    }

    public void TransitionAnimationTo(PlayerState targetState) {
        _animationBlender.CurrentAnimationState = targetState;
    }

    public void PickupItem(string itemName, Texture2D iconTexture) {
        _inventory.Add(itemName);
        var pickedUpItem = Camera.GetNode<TextureRect>("UserInterface/PickedUpItem");
        pickedUpItem.Visible = true;
        pickedUpItem.Texture = iconTexture;
        var tween = GetTree().CreateTween();
        tween.TweenProperty(pickedUpItem, "modulate:a", 1.0f, 0.5f);
        tween.TweenProperty(pickedUpItem, "modulate:a", 0.0f, 2.0f);
        tween.TweenCallback(Callable.From(() => {
            pickedUpItem.Visible = false;
        }));
    }

    public void RemoveItem(string itemName) {
        _inventory.Remove(itemName);
    }

    public bool HasItem(string itemName) {
        return _inventory.Contains(itemName);
    }

    public string GetSaveID() {
        return GetPath();
    }

    public Dictionary<string, Variant> SaveState() {
        var state = new Dictionary<string, Variant> { };

        state["global_position"] = GlobalPosition;

        return state;
    }

    public void LoadState(Dictionary<string, Variant> state) {
        if (state.ContainsKey("global_position")) {
            GlobalPosition = (Vector3)state["global_position"];
        }
    }

    private void OverrideTransformOnFirstLoadIn() {
        if (!Global.Instance.PlayerHasTakenTransform && Global.Instance.PlayerLastSavedTransform is Transform3D newTransform) {
            GlobalTransform = newTransform;
            Global.Instance.PlayerHasTakenTransform = true;
            // Force an immediate update of the raycast
            _floorDetector.ForceRaycastUpdate();

            // If we hit something, position the player above it with proper offset
            if (_floorDetector.IsColliding()) {
                Vector3 collisionPoint = _floorDetector.GetCollisionPoint();
                // Add 0.3 units (half of the player's height) to position player properly
                GlobalPosition = new Vector3(GlobalPosition.X, collisionPoint.Y + 0.3f, GlobalPosition.Z);
            }
        }
        if (Global.Instance.PlayerCameraLastSavedTransform is Transform3D lastCameraTransfrom) {
            Camera.GlobalTransform = lastCameraTransfrom;
        }
    }

    private async void HitFrame(float duration) {
        if (Global.Instance.ImpactFrameEnabled == false) return;
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
        } else if (IsOnWall() && IsWallClimbable() && CanWallJump) {
            TransitionAnimationTo(PlayerState.OnWall);
            CurrentState = PlayerState.OnWall;
        } else {
            TransitionAnimationTo(PlayerState.Falling);
            CurrentState = PlayerState.Falling;
        }
    }
}
