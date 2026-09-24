using UnityEngine;
using UnityEngine.AI;

// --- ИНТЕРФЕЙСЫ И МЕНЕДЖЕР ---
public interface IState
{
    void Enter();
    void LogicUpdate();
    void Exit();
}

public interface IHasEnemyStates
{
    IState IdleState { get; }
    IState PatrolState { get; }
    IState ChaseState { get; }
    IState AttackState { get; }
    IState SearchState { get; }
}

public class StateMachine
{
    public IState CurrentState { get; private set; }
    public void Initialize(IState start) { CurrentState = start; CurrentState.Enter(); }
    public void ChangeState(IState next)
    {
        if (next == null || next == CurrentState) return;
        CurrentState?.Exit();
        CurrentState = next;
        CurrentState.Enter();
    }
}

// --- СОСТОЯНИЯ ---

// 1. Oжидание (Idle)
public class IdleState : IState
{
    private BaseEnemy e; private float timer;
    public IdleState(BaseEnemy enemy) => e = enemy;
    public void Enter() { e.agent.isStopped = true; timer = 0f; }
    public void LogicUpdate()
    {
        if (e.CanSeePlayer()) { e.StateMachine.ChangeState(((IHasEnemyStates)e).ChaseState); return; }
        if ((timer += Time.deltaTime) >= 2f) e.StateMachine.ChangeState(((IHasEnemyStates)e).PatrolState);
    }
    public void Exit() => e.agent.isStopped = false;
}

// 2. Патруль (Patrol)
public class PatrolState : IState
{
    private BaseEnemy e; private int idx;
    public PatrolState(BaseEnemy enemy) => e = enemy;
    public void Enter()
    {
        if (e.patrolPoints.Length == 0) return;
        e.agent.isStopped = false;
        e.agent.SetDestination(e.patrolPoints[idx].position);
    }
    public void LogicUpdate()
    {
        if (e.CanSeePlayer()) { e.StateMachine.ChangeState(((IHasEnemyStates)e).ChaseState); return; }
        if (e.patrolPoints.Length > 0 && !e.agent.pathPending && e.agent.remainingDistance <= e.agent.stoppingDistance)
        {
            idx = (idx + 1) % e.patrolPoints.Length;
            e.StateMachine.ChangeState(((IHasEnemyStates)e).IdleState);
        }
    }
    public void Exit() { }
}

// 3. Погоня (Chase)
public class ChaseState : IState
{
    private BaseEnemy e;
    public ChaseState(BaseEnemy enemy) => e = enemy;
    public void Enter() => e.agent.isStopped = false;
    public void LogicUpdate()
    {
        if (e.CanSeePlayer())
        {
            e.lastKnownPlayerPosition = e.player.position;
            e.agent.SetDestination(e.player.position);
            if (Vector3.Distance(e.transform.position, e.player.position) <= e.attackRange)
            {
                e.StateMachine.ChangeState(((IHasEnemyStates)e).AttackState); return;
            }
            if (e is TeleportEnemy t && t.CanTeleport()) { e.StateMachine.ChangeState(t.TeleportState); return; }
        }
        else
        {
            e.StateMachine.ChangeState(((IHasEnemyStates)e).SearchState);
        }
    }
    public void Exit() { }
}

// 4. Атака (Attack)
public class AttackState : IState
{
    private BaseEnemy e; private float lastAttackTime;
    public AttackState(BaseEnemy enemy) => e = enemy;
    public void Enter() => e.agent.isStopped = true;
    public void LogicUpdate()
    {
        if (!e.player) return;
        Vector3 dir = (e.player.position - e.transform.position).normalized; dir.y = 0;
        if (dir != Vector3.zero) e.transform.rotation = Quaternion.Slerp(e.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);

        if (Vector3.Distance(e.transform.position, e.player.position) > e.attackRange)
        {
            e.StateMachine.ChangeState(((IHasEnemyStates)e).ChaseState); return;
        }
        if (Time.time >= lastAttackTime + e.attackCooldown) { e.PerformAttack(); lastAttackTime = Time.time; }
    }
    public void Exit() => e.agent.isStopped = false;
}

// 5. Поиск (Search)
public class SearchState : IState
{
    private BaseEnemy e; private float timer;
    public SearchState(BaseEnemy enemy) => e = enemy;
    public void Enter() { e.agent.isStopped = false; e.agent.SetDestination(e.lastKnownPlayerPosition); timer = 0f; }
    public void LogicUpdate()
    {
        if (e.CanSeePlayer()) { e.StateMachine.ChangeState(((IHasEnemyStates)e).ChaseState); return; }
        if (!e.agent.pathPending && e.agent.remainingDistance <= e.agent.stoppingDistance)
        {
            e.transform.Rotate(Vector3.up, 90f * Time.deltaTime);
            if ((timer += Time.deltaTime) >= e.searchTime) e.StateMachine.ChangeState(((IHasEnemyStates)e).PatrolState);
        }
    }
    public void Exit() { }
}

// 6. Телепорт (Teleport)
// 6. Телепорт (Teleport)
// 6. Телепорт (Teleport)
public class TeleportState : IState
{
    private TeleportEnemy e;
    public TeleportState(TeleportEnemy enemy) => e = enemy;

    public void Enter()
    {
        e.agent.isStopped = true;

        Vector3 targetPos = Vector3.zero;
        bool foundValidSpot = false;

        // Приоритет направлений: 1. За спиной, 2. Справа, 3. Слева, 4. Спереди
        Vector3[] directions = new Vector3[]
        {
            -e.player.forward,
            e.player.right,
            -e.player.right,
            e.player.forward
        };

        // Дистанция телепортации — прямо вплотную (чуть дальше радиуса атаки)
        float spawnDistance = Mathf.Max(1.5f, e.attackRange + 0.3f);

        foreach (Vector3 dir in directions)
        {
            Vector3 candidatePos = e.player.position + dir.normalized * spawnDistance;

            // 1. Проверяем, что между игроком и точкой нет стены
            if (!Physics.Raycast(e.player.position + Vector3.up * 0.5f, dir, spawnDistance, e.obstacleMask))
            {
                // 2. Ищем ближайшую точку NavMesh (с маленьким радиусом 1.5м, чтобы не уходить в соседние комнаты)
                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    // 3. Проверяем, не застрянет ли хитбокс врага в стене в этой точке
                    if (!Physics.CheckSphere(hit.position + Vector3.up * 0.5f, 0.45f, e.obstacleMask))
                    {
                        targetPos = hit.position;
                        foundValidSpot = true;
                        break; // Точка найдена
                    }
                }
            }
        }

        // Если безопасная точка прямо у игрока найдена — телепортируем
        if (foundValidSpot)
        {
            e.agent.Warp(targetPos);

            // Сразу разворачиваем врага лицом к игроку
            Vector3 lookDir = (e.player.position - e.transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                e.transform.rotation = Quaternion.LookRotation(lookDir);
        }

        e.ResetTeleportCooldown();
        e.StateMachine.ChangeState(e.ChaseState);
    }

    public void LogicUpdate() { }
    public void Exit() => e.agent.isStopped = false;
}