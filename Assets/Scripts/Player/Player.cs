using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity {
    [Header("攻击详细")]
    public Vector2[] attackMovement;//给攻击每一招加点偏移
    public float counterAttackDuration = 0.2f;//反击持续时间
    public bool isBusy { get; private set; }
    [Header("移动信息")]
    public float moveSpeed = 7f;
    public float jumpForce;
    public float swordReturnImpact;
    private float defaultMoveSpeed;
    private float defaultJumpForce;

    [Header("冲刺信息")]
    public float dashSpeed;//冲刺速度
    public float dashDuration;//冲刺持续时间
    private float defaultDashSpeed;
    public float dashDir { get; private set; }

    //public SkillManager skill { get; private set; }
    public GameObject sword { get; private set; }

    #region States
    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }

    #endregion

    protected override void Awake() {
        base.Awake();
        stateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
    }

    protected override void Start() {
        base.Start();
        //skill = SkillManager.instance;
        stateMachine.Initialize(idleState);

        defaultMoveSpeed = moveSpeed;
        defaultJumpForce = jumpForce;
        defaultDashSpeed = dashSpeed;
    }

    protected override void Update() {
        if (Time.timeScale == 0) return;

        base.Update();
        stateMachine.currentState.Update();
        CheckForDashInput();
        //if (Input.GetKeyDown(KeyCode.F))
        //    skill.crystal.CanUseSkill();

        //if (Input.GetKeyDown(KeyCode.Alpha1)) {
        //    Inventory.instance.UseFlask();
        //}

    }

    //减速
    public override void SlowEntityBy(float slowPercentage, float slowDuration) {
        moveSpeed = moveSpeed * (1 - slowPercentage);
        jumpForce = jumpForce * (1 - slowPercentage);
        dashSpeed = dashSpeed * (1 - slowPercentage);

        Invoke("ReturnDefaultSpeed", slowDuration);
    }

    //恢复默认速度
    protected override void ReturnDefaultSpeed() {
        base.ReturnDefaultSpeed();

        moveSpeed = defaultMoveSpeed;
        jumpForce = defaultJumpForce;
        dashSpeed = defaultDashSpeed;
    }

    public void AssignNewSword(GameObject _newSword) {
        sword = _newSword;
    }

    //接剑
    public void CatchTheSword() {
        //stateMachine.ChangeState(catchSwordState);
        Destroy(sword);
    }

    public void AnimationTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    //切换冲刺状态
    private void CheckForDashInput() {
        if (IsWallDetected())
            return;
        //技能没解锁
        //if (!skill.dash.dashUnlocked) return;
        //左shit冲刺
        //if (Input.GetKeyDown(KeyCode.LeftShift) && SkillManager.instance.dash.CanUseSkill()) {
        //    dashDir = Input.GetAxisRaw("Horizontal");
        //    if (dashDir == 0)
        //        dashDir = facingDir;
        //    stateMachine.ChangeState(dashState);
        //}

    }

    public override void Die() {
        base.Die();
        //stateMachine.ChangeState(deadState);
    }

    protected override Collider2D bindCollider() {
        return GetComponent<CapsuleCollider2D>();
    }
}
