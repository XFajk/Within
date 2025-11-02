using Godot;
using Godot.Collections;
using System;

public partial class FinalBossComputer : DialogInteractable {
    private bool _hasActivated = false;

    [Export]
    public Array<FinalBossServer> Servers;

    [Export]
    public Array<LaserMachine> LaserMachines;

    [Export]
    public Array<LaserDroneManager> LaserDroneManagers;

    [Export]
    public Array<Node3D> AlarmLights;

    [Export]
    public Array<Light3D> OtherLights;

    [ExportCategory("Drone Spawn")]
    [Export]
    public Node3D DroneSpawnPoint;
    private PackedScene _droneScene = GD.Load<PackedScene>("res://scenes/entities/drone.tscn");

    [Export]
    public float DroneSpawnDelay = 5.0f;

    private Timer _droneSpawnTimer = new();
    private Array<Drone> _spawnedDrones = new();

    private bool _activatedBefore = false;


    public override void _Ready() {
        base._Ready();

        AddChild(_droneSpawnTimer);
        _droneSpawnTimer.Timeout += SpawnDrone;
        _droneSpawnTimer.WaitTime = DroneSpawnDelay;
        _droneSpawnTimer.OneShot = false;

        DialogEnded += OnDialogEnded;
    }

    public override void _Process(double delta) {
        if (!_hasActivated) {
            foreach (var server in Servers) {
                if (!IsInstanceValid(server)) {
                    continue;
                }
                var hitBox = server.GetNode<Area3D>("HitBox");
                hitBox.Monitoring = false;
            }
        } else {
            foreach (var server in Servers) {
                if (!IsInstanceValid(server)) {
                    continue;
                }
                var hitBox = server.GetNode<Area3D>("HitBox");
                hitBox.Monitoring = true;
            }
        }

        var i = 0;
        foreach (var server in Servers) {
            if (!IsInstanceValid(server)) {
                i++;
            }
        }

        if (i >= Servers.Count && _hasActivated) {
            // Stopping the boss fight

            Global.Instance.SwitchMusic("GroundZero");
            var musicBusIndex = AudioServer.GetBusIndex("Music");
            var lowPassFilterEffect = AudioServer.GetBusEffect(musicBusIndex, 0) as AudioEffectLowPassFilter;
            var musicTween = GetTree().CreateTween();
            musicTween.SetPauseMode(Tween.TweenPauseMode.Process);
            musicTween.TweenProperty(lowPassFilterEffect, "cutoff_hz", 100, 1.0);

            _hasActivated = false;
            foreach (var laserMachine in LaserMachines) {
                laserMachine.Enabled = false;
            }

            foreach (var droneManager in LaserDroneManagers) {
                droneManager.Enabled = false;
            }


            foreach (var alarmLight in AlarmLights) {
                alarmLight.GetNode<AnimationPlayer>("AnimationPlayer").Stop();
                alarmLight.GetNode<Node3D>("AlarmAnchor/RightLight").Visible = false;
                alarmLight.GetNode<Node3D>("AlarmAnchor/LeftLight").Visible = false;
                alarmLight.GetNode<Node3D>("AlarmAnchor/MiddleLight").Visible = false;
            }

            foreach (var light in OtherLights) {
                light.Visible = true;
            }

            var player = GetTree().GetNodesInGroup("Player")[0] as Player;
            player.Inventory.Add("ElevatorItem");
            _activatedBefore = true;
            _droneSpawnTimer.Stop();
        }

        if (_hasActivated || _activatedBefore) {
            _player = null;
        }
        base._Process(delta);
    }


    private void OnDialogEnded() {
        if (_hasActivated || _activatedBefore) {
            return;
        }
        _hasActivated = true;
        // Starting the boss fight

        Global.Instance.SwitchMusic("BossFight");
        var musicBusIndex = AudioServer.GetBusIndex("Music");
        var lowPassFilterEffect = AudioServer.GetBusEffect(musicBusIndex, 0) as AudioEffectLowPassFilter;
        var musicTween = GetTree().CreateTween();
        musicTween.SetPauseMode(Tween.TweenPauseMode.Process);
        musicTween.TweenProperty(lowPassFilterEffect, "cutoff_hz", 20500, 0.5);

        var tween = GetTree().CreateTween();
        tween.TweenCallback(Callable.From(() => {
            foreach (var laserMachine in LaserMachines) {
                laserMachine.Enabled = true;
            }

            foreach (var droneManager in LaserDroneManagers) {
                droneManager.Enabled = true;
            }

        })).SetDelay(1.0f);


        foreach (var alarmLight in AlarmLights) {
            alarmLight.GetNode<AnimationPlayer>("AnimationPlayer").Play("run");
            alarmLight.GetNode<Node3D>("AlarmAnchor/RightLight").Visible = true;
            alarmLight.GetNode<Node3D>("AlarmAnchor/LeftLight").Visible = true;
            alarmLight.GetNode<Node3D>("AlarmAnchor/MiddleLight").Visible = true;
        }

        foreach (var light in OtherLights) {
            light.Visible = false;
        }

        _droneSpawnTimer.Start();
    }

    protected override void Interact() {
        if (_hasActivated || _activatedBefore) {
            _player = null;
            return;
        }
        base.Interact();
    }


    private void SpawnDrone() {
        if (_spawnedDrones.Count < 1) {
            var drone = _droneScene.Instantiate<Drone>();
            DroneSpawnPoint.AddChild(drone);
            drone.GlobalPosition = DroneSpawnPoint.GlobalPosition;

            _spawnedDrones.Add(drone);
        }

        Array<int> toRemove = new();

        for (var i = 0; i < _spawnedDrones.Count; i++) {
            var d = _spawnedDrones[i];
            if (!IsInstanceValid(d) || d.Health <= 0 || d.IsQueuedForDeletion()) {
                toRemove.Add(i);
            }
        }

        foreach (var index in toRemove) {
            _spawnedDrones.RemoveAt(index);
        }
    }
}
