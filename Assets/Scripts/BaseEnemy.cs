using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class BaseEnemy : MonoBehaviour
{
    [Header("General Settings")]
    public Transform player;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public Transform[] patrolPoints;

    [Header("Stats")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 10;
    public float searchTime = 5f;

    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Vector3 lastKnownPlayerPosition;

    public StateMachine StateMachine { get; protected set; }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        StateMachine = new StateMachine();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    protected virtual void Update()
    {
        StateMachine.CurrentState?.LogicUpdate();
    }

    // Абстрактная проверка обнаружения (разная у двух типов врагов)
    public abstract bool CanSeePlayer();

    // Нанесение урона игроку
    public virtual void PerformAttack()
    {
        Debug.Log($"{gameObject.name} атакует игрока и наносит {attackDamage} урона!");

        if (player != null)
        {
            // Находим скрипт PlayerHealth на игроке и передаем ему урон
            var hp = player.GetComponentInParent<PlayerHealth>() ?? player.GetComponentInChildren<PlayerHealth>();
            if (hp != null)
            {
                hp.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning($"[BaseEnemy] На объекте '{player.name}' не найден компонент PlayerHealth!");
            }
        }
    }
}