using Godot;
using System;
using Godot.Collections;

public interface ISavable {
    public string GetSaveID();
    public Dictionary<string, Variant> SaveState();
    public void LoadState(Dictionary<string, Variant> state);
}

public partial class SaveSystem : Node {

    public static SaveSystem Instance { get; private set; }

    public override void _EnterTree() {
        Instance = this;
    }

    public void SaveGame() {
        var runningScenes = GetTree().GetNodesInGroup("Level");
        foreach (var scene in runningScenes) {
            if (scene is Level level) {
                level.SaveState();
            }
        }
    }

    public void LoadGame() {
        var runningScenes = GetTree().GetNodesInGroup("Level");
        foreach (var scene in runningScenes) {
            if (scene is Level level) {
                level.LoadState();
            }
        }
    }

    public void ResetGame() {
        GD.Print("Resetting game...");
        var dir = DirAccess.Open("user://");
        if (dir != null) {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "") {
                if (!dir.CurrentIsDir()) {
                    dir.Remove(fileName);
                }
                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
        }
    }
}