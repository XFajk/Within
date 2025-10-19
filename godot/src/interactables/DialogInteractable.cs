using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DialogInteractable : Interactable {
    [Export]
    public Json DialogData;

    protected override void Interact() {
        // Expecting DialogData to be a JSON Resource (imported .json file)
        if (DialogData == null) {
            GD.PushWarning($"{nameof(DialogInteractable)}: No DialogData assigned.");
            return;
        }

        Variant data = DialogData.Data;
        if (data.VariantType != Variant.Type.Array) {
            GD.PushWarning($"{nameof(DialogInteractable)}: DialogData has no parsed data.");
            return;
        }

        Array dialogArray = data.AsGodotArray();

        foreach (Variant item in dialogArray) {
            if (item.VariantType != Variant.Type.Dictionary) {
                GD.PushWarning($"{nameof(DialogInteractable)}: Dialog item is not a Dictionary.");
                continue;
            }

            Dictionary dialogEntry = item.AsGodotDictionary();
            GD.Print(dialogEntry["speaker"] + ": " + dialogEntry["speech"]);
        }
    }

}
