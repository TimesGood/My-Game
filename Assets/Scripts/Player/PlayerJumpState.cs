using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//跳跃状态
public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        rb.velocity = new Vector2(rb.velocity.x, player.jumpForce);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        //如果y轴小于零，下落中
        if (rb.velocity.y < 0)
            stateMachine.ChangeState(player.airState);
        //跳跃中移动
        if (xInput != 0) {
            float jumpVelocity = rb.velocity.y > player.jumpForce ? 0 : rb.velocity.y;//角色跳跃时触碰斜面上可能会超过所设置的跳跃高度
            player.SetVelocity(player.moveSpeed * xInput, jumpVelocity);
        } else if(xInput == 0 && rb.velocity.x != 0) {
            player.SetVelocity(0, rb.velocity.y);
        }
            
    }
}
