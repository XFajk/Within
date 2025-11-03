using Godot;

public partial class Metro : Level {
    public override void _Ready() {
        base._Ready();
        Global.Instance.SwitchMusic("AmbienceOutside");
        var tween = GetTree().CreateTween(); 
        tween.TweenCallback(Callable.From(() => {
            Global.Instance.MusicPlayer.VolumeDb = 0;
        })).SetDelay(1.0);
    }

    public override void _ExitTree() {
        base._ExitTree();
        Global.Instance.MusicPlayer.VolumeDb = -10;
    }
}