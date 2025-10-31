using Godot;

[GlobalClass]
public partial class BasicPhysicsObject : CharacterBody3D {
    [Export]
    public Vector3 InitialVelocity = Vector3.Zero;

    [Export]
    public float Gravity = 15.8f;


    private Vector3 _velocity = Vector3.Zero;

    public override void _Ready() {
        _velocity = InitialVelocity;
    }

    public override void _PhysicsProcess(double delta) {

        _velocity.X = Mathf.MoveToward(_velocity.X, 0f, .5f);
        _velocity.Z = Mathf.MoveToward(_velocity.Z, 0f, .5f);
        if (!IsOnFloor()) {
            _velocity.Y = Mathf.MoveToward(_velocity.Y, -400.0f * Player.UnitTransformer, Gravity * Player.UnitTransformer);
        } else {
            _velocity.Y = 0f;
        }

        Velocity = _velocity;
        MoveAndSlide();
    }
}