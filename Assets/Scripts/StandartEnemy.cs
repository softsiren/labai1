using UnityEngine;

public class StandardEnemy : BaseEnemy, IHasEnemyStates
{
    [Header("Standard Detection (180°)")]
    public float viewDistance = 10f;
    public float viewAngle = 180f;

    public IState IdleState { get; private set; }
    public IState PatrolState { get; private set; }
    public IState ChaseState { get; private set; }
    public IState AttackState { get; private set; }
    public IState SearchState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        IdleState = new IdleState(this);
        PatrolState = new PatrolState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
        SearchState = new SearchState(this);
    }

    private void Start()
    {
        StateMachine.Initialize(PatrolState);
    }

    public override bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 dir = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= viewDistance && Vector3.Angle(transform.forward, dir) <= viewAngle / 2f)
        {
            return !Physics.Raycast(transform.position + Vector3.up, dir, dist, obstacleMask);
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Vector3 eye = transform.position + Vector3.up;

        // Зона атаки (красный)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Границы 180° обзора (желтый)
        Gizmos.color = Color.yellow;
        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawLine(eye, eye + left * viewDistance);
        Gizmos.DrawLine(eye, eye + right * viewDistance);

        // Raycast луч к игроку (зеленый - видит, красный - не видит)
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(eye, player.position + Vector3.up);
        }
    }
}