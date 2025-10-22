using System;
using Godot;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public partial class PlayerAnimationBlender : Node {

    public AnimationTree PlayerAnimationTree;



    private Dictionary<Player.PlayerState, (string, float)> _blendValuesLookUp = new Dictionary<Player.PlayerState, (string, float)> {
        { Player.PlayerState.Idle, ("parameters/Run/", 0.2f) },

        { Player.PlayerState.Jumping, ("parameters/Jump/", 0.2f) },
        { Player.PlayerState.Falling, ("parameters/Jump/", 0.2f) },

        { Player.PlayerState.OnWall, ("parameters/WallHug/", 0.1f) },
        { Player.PlayerState.Dashing, ("parameters/Dash/", 0.01f) },
    };

    private Tween _animationBlendTween = null;

    private Player.PlayerState _currentAnimationState = Player.PlayerState.Idle;
    public Player.PlayerState CurrentAnimationState {
        get => _currentAnimationState;
        set {

            _currentAnimationState = value;

            if (_animationBlendTween != null && IsInstanceValid(_animationBlendTween)) {
                _animationBlendTween.Kill();
            }
            _animationBlendTween = GetTree().CreateTween().SetParallel(true);

            if (value == Player.PlayerState.Jumping)
                PlayerAnimationTree.Set("parameters/JumpSeek/seek_request", 0.0f);

            if (value != Player.PlayerState.Idle) {
                foreach (var k in _blendValuesLookUp.Keys) {
                    if (k == value) {
                        _animationBlendTween.TweenProperty(PlayerAnimationTree, _blendValuesLookUp[k].Item1 + "blend_amount", 1.0f, _blendValuesLookUp[k].Item2);
                    } else if (_blendValuesLookUp[k].Item1 == _blendValuesLookUp[value].Item1) {
                       // Do Nothing 
                    } else {
                        _animationBlendTween.TweenProperty(PlayerAnimationTree, _blendValuesLookUp[k].Item1 + "blend_amount", 0.0f, _blendValuesLookUp[k].Item2);
                    }
                }
            } else {
                foreach (var k in _blendValuesLookUp.Keys) {
                    if (k != Player.PlayerState.Idle) {
                        _animationBlendTween.TweenProperty(PlayerAnimationTree, _blendValuesLookUp[k].Item1 + "blend_amount", 0.0f, _blendValuesLookUp[k].Item2);
                    }
                }
            }
        }
    }

}