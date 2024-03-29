using Mirror;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : EntityController
{
    [Header("Enemy Physic Settings")]
    [SerializeField] private bool dummy = false;
    [SerializeField] private float moveSpeed = 300;
    [SerializeField] private float movementSmoothing = 0.1f;

    [Header("Enemy Attack Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackDelayTime = 0.5f;
    //[SerializeField] private float attackCdr = 0.5f;
    [SerializeField] private float attackRecorveryTime = 1f;
    //[SerializeField] private float knockBackForce = 500f;
    [SerializeField] private GameObject attackVfx;
    [SerializeField] private GameObject attackSuccessVfx;
    //[SerializeField] private float attackRadius = 1.25f;
    //[SerializeField] private LayerMask attackableLayer;

    [Header("Enemy Pathfinding Settings")]
    [SerializeField] private float nextWaypointDistance = 3f;
    [SerializeField] private float pathUpdateCdr = 0.5f;
    //[SerializeField] private float obstacleScanDistance = 0.7f;
    //[SerializeField] private float playerScanCdr = 1f;

    [Header("Enemy Debugs")]
    [SerializeField] private Transform target;

    private Path path;
    private int currentWaypoint = 0;
    private bool reachedEndOfPath = false;
    private float nextPathUpdate;

    [SyncVar(hook = nameof(HandleAttackingChange))]
    [SerializeField] private bool attacking;
    [SerializeField] private EntityStatus targetStatus;
    private float nextAttackRecovery;
    private float nextDelayAttack;

    private bool controllable = true;

    private Seeker seeker;
    private Rigidbody2D rb2D;

    private Vector3 velocity = Vector3.zero;

    // Start is called before the first frame update
    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    [ServerCallback]
    void Update()
    {
        if (!controllable || dummy && target != null) return;

        UpdatePath();
    }

    Vector3 cachedPosition;

    private void FixedUpdate()
    {
        if (transform.position != cachedPosition)
            PlayMovementSFXs();
        else
            StopMovementSFXs();

        cachedPosition = transform.position;

        if (!isServer) return;

        if (!controllable) return;

        Attack();

        if (dummy) return;

        if (path != null)
        {
            if (currentWaypoint >= path.vectorPath.Count)
                reachedEndOfPath = true;
            else
                reachedEndOfPath = false;
        }

        if (path != null && !reachedEndOfPath)
            Move();
    }

    #region Pathfinding

    [Server]
    private void UpdatePath()
    {
        if (Time.time > nextPathUpdate)
        {
            if (target == null)
            {
                target = GameManager.instance.GetTarget();
                targetStatus = target.GetComponent<EntityStatus>();
            }

            if (seeker.IsDone() && target != null)
                seeker.StartPath(rb2D.position, target.position, OnPathComplete);
            nextPathUpdate = Time.time + pathUpdateCdr;
        }
    }

    [Server]
    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    [Server]
    private void Move()
    {
        if (Time.time < attackRecorveryTime) return;

        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb2D.position).normalized;
        Vector3 targetVelocity = direction * moveSpeed * Time.fixedDeltaTime;
        rb2D.velocity = Vector3.SmoothDamp(rb2D.velocity, targetVelocity, ref velocity, movementSmoothing);

        if (Vector2.Distance(rb2D.position, path.vectorPath[currentWaypoint]) < nextWaypointDistance)
        {
            currentWaypoint++;
        }
    }

    #endregion

    #region Attack

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (attacking) return;

        if (collision.transform == target)
        {
            attacking = true;

            nextDelayAttack = Time.time + attackDelayTime;
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform == target)
        {
            attacking = false;
        }
    }

    [Server]
    private void Attack()
    {
        if (!attacking || Time.time < nextDelayAttack) return;

        nextDelayAttack = Time.time + attackDelayTime + attackRecorveryTime;
        // Strike
        targetStatus.CmdRecieveDamage(damage, netIdentity);
        RpcShowAttackSuccessVFX();
        nextAttackRecovery = Time.time + attackRecorveryTime;
    }

    private void HandleAttackingChange(bool oldAttacking, bool newAttacking)
    {
        ShowAttackVFX(newAttacking);
    }

    [Client]
    private void ShowAttackVFX(bool show)
    {
        attackVfx.SetActive(show);
    }

    [ClientRpc]
    private void RpcShowAttackSuccessVFX()
    {
        StartCoroutine(AttackSuccessVFXCoroutine());
    }

    [Client]
    IEnumerator AttackSuccessVFXCoroutine()
    {
        attackSuccessVfx.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        attackSuccessVfx.SetActive(false);
    }

    [Server]
    public void SetControllable(bool newControllable)
    {
        controllable = newControllable;

        if (controllable) return;

        attacking = false;
        nextAttackRecovery = 0f;
        nextDelayAttack = 0f;
        nextPathUpdate = 0f;
    }

    #endregion

    #region Obsolete but might be useful code

    /*[SerializeField] float distanceCache;

    [Server]
    private void Attack()
    {
        if (target == null || targetStatus == null) return;

        distanceCache = Vector2.Distance(transform.position, target.position);

        if (distanceCache < attackRadius)
        {
            if (Time.time > nextAttack)
            {
                if (!attacking)
                {
                    attackExecutionTime = Time.time + attackDelayTime;
                    attacking = true;
                }
                else
                {
                    if (Time.time > attackExecutionTime)
                    {
                        attacking = false;
                        nextAttack = Time.time + attackCdr;

                        targetStatus.CmdDealDamage(damage, netIdentity);
                    }
                }
            }
        }
        else
        {
            attacking = false;
        }
    }*/

    #endregion

}
