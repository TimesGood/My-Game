using System.Collections;
using UnityEngine;

public abstract class Entity : MonoBehaviour {

    #region Components
    public Animator anim { get; private set; }//动画
    public Rigidbody2D rb { get; private set; }//刚体
    //public EntityFx fx { get; private set; }
    public SpriteRenderer sr { get; private set; }
    //public CharacterStats stats { get; private set; }

    public Collider2D cd { get; private set; }//碰撞盒

    [Header("击退信息")]
    [SerializeField] protected Vector2 knockbackPower;//击退方向
    [SerializeField] protected float knockbackDuration;//击退持续时间
    protected bool isKnocked;//是否击退中

    #endregion

    //碰撞信息
    [Header("碰撞信息")]
    public Transform attackCheck;
    public float attackCheckRadius;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundCheckDistance;//地面检测距离
    [SerializeField] protected LayerMask whatIsGround;//地面图层
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected float wallCheckDistance;//墙壁检测距离
    [SerializeField] protected LayerMask whatIsWall;//墙壁图层


    public int knockbackDir { get; private set; } = 1;//击退方向
    //转向信息
    public int facingDir { get; private set; } = 1;
    protected bool facingRight = true;

    //转身事件
    public System.Action onFlipped;

    protected virtual void Awake() {

    }
    protected virtual void Start() {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        //fx = GetComponent<EntityFx>();
        //stats = GetComponent<CharacterStats>();
        cd = bindCollider();
    }

    protected virtual void Update() {

    }

    //减速
    public virtual void SlowEntityBy(float slowPercentage, float slowDuration) {

    }

    //重置默认速度
    protected virtual void ReturnDefaultSpeed() {
        anim.speed = 1;
    }

    //受伤影响，击退
    public virtual void DamageImpact() {
        StartCoroutine(HitKnockback());
    }

    //根据攻击者位置设置击退方向
    public virtual void SetupKnockbackDir(Transform damageDirection) {
        if (damageDirection.position.x > transform.position.x)
            knockbackDir = -1;
        else if (damageDirection.position.x < transform.position.x)
            knockbackDir = 1;
    }


    protected virtual IEnumerator HitKnockback() {
        isKnocked = true;

        rb.velocity = new Vector2(knockbackPower.x * knockbackDir, knockbackPower.y);
        yield return new WaitForSeconds(knockbackDuration);
        isKnocked = false;
    }

    #region Collision
    //是否已接触地面
    public virtual bool IsGroundDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);

    //是否接触墙面
    public virtual bool IsWallDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);

    protected virtual void OnDrawGizmos() {
        //绘制射线
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);
    }
    #endregion

    #region Flip
    //转向
    public virtual void Flip() {
        facingDir = facingDir * -1;
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);

        if (onFlipped != null)
            onFlipped();

    }
    //转向控制
    public virtual void FlipController(float _x) {

        if (_x > 0 && !facingRight) {
            Flip();
        } else if (_x < 0 && facingRight) {
            Flip();
        }
    }
    #endregion


    #region Velocity
    public void SetZeroVelocity() {
        if (isKnocked)
            return;
        rb.velocity = new Vector2(0, 0);
    }

    //设置向量
    public void SetVelocity(float _xVelocity, float _yVelocity) {
        if (isKnocked)
            return;
        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity);
    }
    #endregion


    //死亡
    public virtual void Die() {

    }

    //绑定碰撞体
    protected abstract Collider2D bindCollider();
}
