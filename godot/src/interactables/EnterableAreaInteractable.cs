using Godot;
using System;

[GlobalClass]
public partial class EnterableAreaInteractable : Interactable {

    [Export]
    public StringName EnterableAreaScene;

    [Export]
    public Vector3 ExitPosition;

    [Export]
    public AudioStream EnterAreaSound;

    [Export]
    public float SoundDb = 0f;

    private AudioStreamPlayer _audioPlayer;

    public override void _Ready() {
        base._Ready();
        _audioPlayer = new AudioStreamPlayer();
        _audioPlayer.Stream = EnterAreaSound;
        _audioPlayer.VolumeDb = SoundDb;
        _audioPlayer.Finished += () => _audioPlayer.QueueFree();
    }

    protected override void Interact() {        
        _player.CurrentState = Player.PlayerState.EnteringArea;

        _player.PlayerAreaToEnter = GD.Load<PackedScene>(EnterableAreaScene);

        Global.Instance.TransitionExitPosition = ExitPosition;
        Global.Instance.AddChild(_audioPlayer);
        _audioPlayer.Play();
        _player = null;
    }
}