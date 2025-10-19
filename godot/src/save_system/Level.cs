using Godot;
using System;
using Godot.Collections;
using System.ComponentModel.DataAnnotations;

[GlobalClass]
public partial class Level : Node {
    public Dictionary<string, Variant> SavableObjects = new();


    public override void _Ready() {
        AddToGroup("Level");
        LoadState();
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("reset_everything")) {
            SaveSystem.Instance.ResetGame();
        }
    }

    public void LoadState() {
        GD.Print($"Loading state for level: {GetName()}");
        var loadFile = FileAccess.Open($"user://{GetName()}.dat", FileAccess.ModeFlags.Read);
        if (loadFile == null) {
            return; // No save file found
        }

        string savedData = loadFile.GetAsText();
        loadFile.Close();

        var loadedStates = (Dictionary<string, Variant>)GD.StrToVar(savedData);

        var savableNodes = GetTree().GetNodesInGroup("Savable");

        foreach (var node in savableNodes) {
            if (IsAncestorOf(node) && node is ISavable savable) {
                string id = savable.GetSaveID();
                if (loadedStates.ContainsKey(id)) {
                    savable.LoadState((Dictionary<string, Variant>)loadedStates[id]);
                }
            }
        }
    }

    public void SaveState() {
        var savableNodes = GetTree().GetNodesInGroup("Savable");
        foreach (var node in savableNodes) {
            if (IsAncestorOf(node) && node is ISavable savable) {
                SavableObjects[savable.GetSaveID()] = savable.SaveState();
            }
        }

        var saveFile = FileAccess.Open($"user://{GetName()}.dat", FileAccess.ModeFlags.Write);
        saveFile.StoreString(GD.VarToStr(SavableObjects));
        saveFile.Close();
    }

    public void ResetState() {
        var savableNodes = GetTree().GetNodesInGroup("Savable");
        foreach (var node in savableNodes) {
            if (IsAncestorOf(node) && node is ISavable savable) {
                // Assuming each savable has a method to reset to default state
                savable.LoadState(new Dictionary<string, Variant>());
            }
        }
    }
}