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

        { Player.PlayerState.Dashing, ("parameters/Dash/", 0f) },

        { Player.PlayerState.Damaged, ("parameters/TakingDemage/", 0.02f) },
        { Player.PlayerState.MiniDeath, ("parameters/TakingDemage/", 0.1f) },

        { Player.PlayerState.AttackingFront, ("parameters/AttackingFront/", 0f) },

        { Player.PlayerState.AttackingUp, ("parameters/AttackingAbove/", 0f) },

        { Player.PlayerState.Sleeping, ("parameters/Sleeping/", 0.2f) },

        { Player.PlayerState.WakingUp, ("parameters/WakingUp/", 0.2f) },

        { Player.PlayerState.Crazy, ("parameters/Crazy/", 0.2f) },

        { Player.PlayerState.Death, ("parameters/Death/", 0f) },
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

            // Seeked States
            switch (value) {
                case Player.PlayerState.Jumping:
                    PlayerAnimationTree.Set("parameters/JumpSeek/seek_request", 0.0f);
                    break;
                case Player.PlayerState.AttackingFront:
                    PlayerAnimationTree.Set("parameters/AttackingFrontSeek/seek_request", 0.0f);
                    break;
                case Player.PlayerState.AttackingUp:
                    PlayerAnimationTree.Set("parameters/AttackingAboveSeek/seek_request", 0.0f);
                    break;
            }

            if (value != Player.PlayerState.Idle) {
                foreach (var k in _blendValuesLookUp.Keys) {
                    if (k == value) {
                        if (_blendValuesLookUp[k].Item2 == 0f) {
                            PlayerAnimationTree.Set(_blendValuesLookUp[k].Item1 + "blend_amount", 1.0f);
                        } else {
                            _animationBlendTween.TweenProperty(PlayerAnimationTree, _blendValuesLookUp[k].Item1 + "blend_amount", 1.0f, _blendValuesLookUp[k].Item2);
                        }
                    } else if (_blendValuesLookUp[k].Item1 == _blendValuesLookUp[value].Item1) {
                        // Do Nothing 
                    } else {
                        if (_blendValuesLookUp[k].Item2 == 0f) {
                            PlayerAnimationTree.Set(_blendValuesLookUp[k].Item1 + "blend_amount", 0.0f);
                        } else {
                            _animationBlendTween.TweenProperty(PlayerAnimationTree, _blendValuesLookUp[k].Item1 + "blend_amount", 0.0f, _blendValuesLookUp[k].Item2);
                        }
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