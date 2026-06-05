using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//¿ÕÖÐ×´Ì¬
public class PlayerAirState : PlayerState
{
    public PlayerAirState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (xInput != 0) {
            player.SetVelocity(player.moveSpeed * .8f * xInput, rb.velocity.y);
        } else if (xInput == 0 && rb.velocity.x != 0) {
            player.SetVelocity(0, rb.velocity.y);
        }
            
        //´¥ÅöÇ½±Ú
        //if (player.IsWallDetected())
        //    stateMachine.ChangeState(player.wallSlideState);
        if (player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.idleState);
        }

    }
}
