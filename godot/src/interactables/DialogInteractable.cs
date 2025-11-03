using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DialogInteractable : Interactable {
    [Signal]
    public delegate void DialogEndedEventHandler();


    [Export]
    public Array<Json> DialogData;

    public int CurrentDialogIndex = 0;

    private Label3D _textBox;

    private bool _isDialogActive = false;
    private bool _isDialogPaused = false;

    private string _currentDialogText = null;

    private double _timeAccumulator = 0.0;

    private int _currentSectionIndex = 0;
    private int _currentCharIndex = 0;

    private Dictionary _currentDialogSections = null;

    private bool _interactionBuffer = false; // To prevent immediate skiping of dialog

    protected AudioStreamPlayer3D _speachSounds;

    public override void _Ready() {
        base._Ready();
        _speachSounds = GetNodeOrNull<AudioStreamPlayer3D>("SpeechSounds");
        _textBox = GetNode<Label3D>("TextBox");
        if (_textBox != null) {
            _textBox.Text = "";
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);

        if (_player == null) {
            _textBox.Text = "";
            _isDialogActive = false;
            _isDialogPaused = false;
            _currentDialogText = "";
            _currentCharIndex = 0;
            _currentSectionIndex = 0;
            return;
        }

        if (_isDialogActive) {
            _interactionTitleLabel.Visible = false;
        }

        if (Input.IsActionJustPressed("interact") && !_interactionBuffer) {
            if (_isDialogActive) {
                if (_isDialogPaused) {
                    // End of current section
                    _currentSectionIndex++;
                    _currentCharIndex = 0;
                    _currentDialogText = "";

                    _textBox.Text = "";
                    _player.TextBox.Text = "";

                    if (_currentSectionIndex >= ((Array)DialogData[CurrentDialogIndex].Data).Count) {
                        // End of dialog
                        EmitSignalDialogEnded();
                        _isDialogActive = false;
                        _interactionTitleLabel.Visible = true;
                        _player.CurrentState = Player.PlayerState.Idle;
                        _currentSectionIndex = 0;
                        _player = null;
                        return;
                    } else {
                        Array sections = (Array)DialogData[CurrentDialogIndex].Data;
                        _currentDialogSections = (Dictionary)sections[_currentSectionIndex];
                        if ((int)_currentDialogSections["speaker"] == 1)
                            _speachSounds?.Play();
                    }

                    _isDialogPaused = false;
                } else {
                    // Fast-forward current section
                    _currentDialogText = (string)_currentDialogSections["text"];
                    _speachSounds?.Stop();
                    if ((int)_currentDialogSections["speaker"] == 0) {
                        _player.TextBox.Text = _currentDialogText;
                    } else {
                        _textBox.Text = _currentDialogText;
                    }
                    _isDialogPaused = true;
                }
            }
        }
        if (_isDialogActive && !_isDialogPaused) {
            _timeAccumulator += delta;

            if (_speachSounds != null) {
                if (!_speachSounds.Playing) {
                    if ((int)_currentDialogSections["speaker"] == 1)
                        _speachSounds.Play();
                } else {
                    if ((int)_currentDialogSections["speaker"] == 0)
                        _speachSounds.Stop();
                }
            }


            if (_timeAccumulator >= (double)_currentDialogSections["typing_speed"]) {
                _timeAccumulator = 0.0;

                if (_currentCharIndex >= ((string)_currentDialogSections["text"]).Length) {
                    _isDialogPaused = true;
                    _speachSounds?.Stop();
                    return;
                }

                _currentDialogText += ((string)_currentDialogSections["text"])[_currentCharIndex];
                if ((int)_currentDialogSections["speaker"] == 0) {
                    _player.TextBox.Text = _currentDialogText;
                } else {
                    _textBox.Text = _currentDialogText;
                }
                _currentCharIndex++;
            }
        }
        _interactionBuffer = false;
    }

    protected override void Interact() {
        if (DialogData == null) {
            GD.PushWarning($"{nameof(DialogInteractable)}: No DialogData assigned.");
            return;
        }

        Variant data = DialogData[CurrentDialogIndex].Data;
        if (data.VariantType != Variant.Type.Array) {
            GD.PushWarning($"{nameof(DialogInteractable)}: DialogData has no parsed data.");
            return;
        }

        _currentCharIndex = 0;
        _currentSectionIndex = 0;
        _isDialogPaused = false;

        Array sections = (Array)data;

        _currentDialogSections = (Dictionary)sections[_currentSectionIndex];

        _interactionTitleLabel.Visible = false;
        _player.CurrentState = Player.PlayerState.NoControl;
        _isDialogActive = true;
        _interactionBuffer = true;
        if ((int)_currentDialogSections["speaker"] == 1)
            _speachSounds?.Play();
    }
}
