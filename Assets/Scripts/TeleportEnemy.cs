using UnityEngine;

public class TeleportEnemy : BaseEnemy, IHasEnemyStates
{
    [Header("360 Scan Settings")]
    public float scanRadius = 10f;

    [Header("Teleport Settings")]
    public float teleportCooldown = 5f;
    public float maxTeleportDistance = 6f;
    private float nextTeleportTime;

    public IState IdleState { get; private set; }
    public IState PatrolState { get; private set; }
    public IState ChaseState { get; private set; }
    public IState AttackState { get; private set; }
    public IState SearchState { get; private set; }
    public TeleportState TeleportState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        IdleState = new IdleState(this);
        PatrolState = new PatrolState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
        SearchState = new SearchState(this);
        TeleportState = new TeleportState(this);
    }

    private void Start()
    {
        StateMachine.Initialize(PatrolState);
    }

    public override bool CanSeePlayer()
    {
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= scanRadius)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            return !Physics.Raycast(transform.position + Vector3.up, dir, dist, obstacleMask);
        }
        return false;
    }

    public bool CanTeleport()
    {
        return Time.time >= nextTeleportTime && Vector3.Distance(transform.position, player.position) > attackRange * 2f;
    }

    public void ResetTeleportCooldown()
    {
        nextTeleportTime = Time.time + teleportCooldown;
    }

    private void OnDrawGizmos()
    {
        Vector3 eye = transform.position + Vector3.up;

        // Радиус 360° сканера (голубой)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        // Зона атаки (красный)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Raycast луч (зеленый - видит, красный - не видит)
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(eye, player.position + Vector3.up);
        }
    }
}