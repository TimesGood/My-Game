using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

//地面状态
public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
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
        //if (Input.GetKeyDown(KeyCode.R))
        //    stateMachine.ChangeState(player.blackholeState);
        //if (Input.GetKeyDown(KeyCode.Mouse1) && HasNoSword())
        //    stateMachine.ChangeState(player.aimSwordSate);
        //if (Input.GetKeyDown(KeyCode.Q) && player.skill.parry.parryUnlocked)
        //    stateMachine.ChangeState(player.counterAttackState);
        //if (Input.GetKeyDown(KeyCode.Mouse0))
        //    stateMachine.ChangeState(player.primaryAttackState);
        if (!player.IsGroundDetected())
            stateMachine.ChangeState(player.airState);
        //地面状态下，空格跳跃
        if (Input.GetKeyDown(KeyCode.Space) && player.IsGroundDetected())
            stateMachine.ChangeState(player.jumpState);
    }

    //是否有剑
    private bool HasNoSword()
    {
        if (!player.sword)
        {
            return true;
        }
        //player.sword.GetComponent<Sword_Skill_Controller>().ReturnSword();
        return false;
    }
}
