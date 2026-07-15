using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;



public class GarrisonUnitAI : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Chase,
        Returning
    }
    private NavMeshAgent agent;
    [Header("State")]
    public State currentState = State.Patrol;

    [Header("Patrol")]
    public float patrolRadius = 5f;
    public int patrolPointCount = 4;
    public float patrolSpeed = 2f;
    public float pointReachDistance = 0.2f;

    [Header("Combat")]
    public float chaseSpeed = 3.5f;
    public float detectRadius = 8f;
    public float attackRange = 1.5f;

    private readonly List<Vector3> patrolPoints = new();

    private int currentPoint;
    private Vector3 startPosition;
    private Transform targetEnemy;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        startPosition = transform.position;

        GeneratePatrolPoints();

        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.1f;

        GoNextPatrolPoint();
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                HandlePatrol();
                CheckForEnemies();
                break;

            case State.Chase:
                HandleChase();
                break;

            case State.Returning:
                HandleReturn();
                break;
        }
    }

    #region Patrol

    void GeneratePatrolPoints()
    {
        patrolPoints.Clear();

        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 2 / 4;

            Vector3 point =
                startPosition +
                new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * patrolRadius;

            patrolPoints.Add(point);
        }
    }

    void HandlePatrol()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            currentPoint++;

            if (currentPoint >= patrolPoints.Count)
                currentPoint = 0;

            GoNextPatrolPoint();
        }
    }
    void GoNextPatrolPoint()
    {
        if (patrolPoints.Count == 0)
            return;

        agent.speed = patrolSpeed;
        agent.SetDestination(patrolPoints[currentPoint]);
    }

    #endregion

    #region Enemy

    void CheckForEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            targetEnemy = hit.transform;
            currentState = State.Chase;

            AlertOthers();

            return;
        }
    }

    void HandleChase()
    {
        if (targetEnemy == null)
        {
            BeginReturn();
            return;
        }

        agent.speed = chaseSpeed;
        agent.SetDestination(targetEnemy.position);

        float distance = Vector3.Distance(transform.position, targetEnemy.position);

        if (distance > detectRadius * 2f)
        {
            BeginReturn();
        }

        if (distance <= attackRange)
        {
            // Attack
        }
    }

    void BeginReturn()
    {
        targetEnemy = null;

        currentState = State.Returning;

        agent.speed = patrolSpeed;
        agent.SetDestination(startPosition);
    }

    void HandleReturn()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            currentPoint = 0;

            currentState = State.Patrol;

            GoNextPatrolPoint();
        }
    }

    #endregion



    #region Alert

    void AlertOthers()
    {
        Collider[] friends = Physics.OverlapSphere(transform.position, detectRadius);

        foreach (Collider col in friends)
        {
            if (!col.TryGetComponent(out GarrisonUnitAI ally))
                continue;

            if (ally == this)
                continue;

            if (ally.currentState == State.Patrol)
            {
                ally.targetEnemy = targetEnemy;
                ally.currentState = State.Chase;
            }
        }
    }

    #endregion

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        if (patrolPoints != null)
        {
            Gizmos.color = Color.cyan;

            foreach (Vector3 p in patrolPoints)
            {
                Gizmos.DrawSphere(p, 0.2f);
            }
        }
    }
#endif
}