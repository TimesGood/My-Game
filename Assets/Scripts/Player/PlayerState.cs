using System.Collections;
using System.Collections.Generic;
using GameInput;
using UnityEngine;
using UnityEngine.InputSystem.Users;

//玩家状态
public class PlayerState
{
    protected PlayerStateMachine stateMachine;
    protected Player player;
    protected Rigidbody2D rb;
    protected MouseUser input;

    protected float xInput;
    protected float yInput;
    private string animBoolName;

    protected float stateTimer;//状态计时器
    protected bool triggerCalled;//动画帧是否触发判定，动画结束帧触发时为true
    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;

        Vector2 input = this.input.MovementInput();

        xInput = input.x;
        yInput = input.y;
        player.anim.SetFloat("yVelocity", rb.velocity.y);
    }

    //进入状态时触发
    public virtual void Enter()
    {
        player.anim.SetBool(animBoolName, true);
        rb = player.rb;
        input = player.GetComponent<MouseUser>();
        triggerCalled = false;
    }
    //退出状态时触发

    public virtual void Exit()
    {
        player.anim.SetBool(animBoolName, false);
    }

    public virtual void AnimationFinishTrigger()
    {
        triggerCalled = true;
    }
}
