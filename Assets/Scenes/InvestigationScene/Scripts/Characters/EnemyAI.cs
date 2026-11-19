using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform eyePoint;
    public LayerMask obstructionMask;

    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition = false;
    private NavMeshAgent agent;
    public EnemyState currentState;
    public float chaseRange = 5;
    public Player playerController;
    Boolean investigating = false;
    public Transform[] patrolPoints;

    private int currentPatrolIndex = -1;
    private float waitTimer = 0f;
    public float patrolWaitTime = 2f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = EnemyState.Patrol;
        agent = GetComponent<NavMeshAgent>();
        //this.gameObject.SetActive(false);
    }

    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Investigate
    }

    public void activateWerewolf()
    {
        this.gameObject.SetActive(true);
    }

    bool CanSeePlayer()
    {
        Vector3 origin = eyePoint.position;
        Vector3 target = playerController.transform.position + Vector3.up;

        Vector3 direction = target - origin;

        Debug.DrawRay(origin, direction, Color.red);

        Debug.Log("Obstruction Mask: " + obstructionMask.value);

        RaycastHit hit;

        if (Physics.Raycast(origin, direction.normalized,
            out hit, direction.magnitude, obstructionMask))
        {
            Debug.Log("Vision blocked by: " + hit.collider.name);
            return false;
        }

        Debug.Log("Player is visible");
        return true;
    }

    void Investigate()
    {
        agent.SetDestination(lastKnownPosition);

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            hasLastKnownPosition = false;
            currentState = EnemyState.Patrol;
        }
    }


    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(
            transform.position,
            playerController.transform.position
        );

        bool canSeePlayer = CanSeePlayer();

        // Player must be within range AND visible
        if (distanceToPlayer <= chaseRange && canSeePlayer)
        {
            lastKnownPosition = playerController.transform.position;
            hasLastKnownPosition = true;

            if (distanceToPlayer <= 2)
            {
                currentState = EnemyState.Attack;
                Attack();
            }
            else
            {
                currentState = EnemyState.Chase;
                Chase();
            }
        }
        else if (hasLastKnownPosition)
        {
            currentState = EnemyState.Investigate;
            Investigate();
        }
        else
        {
            currentState = EnemyState.Patrol;
            Patrol();
        }
    }

    public void alertNoise(Vector3 position)
    {
        agent.SetDestination(position);
    }

    void Patrol()
    {
        if (currentPatrolIndex == -1)
        {
            ChooseNextPatrolPoint();
            return;
        }

        if (agent.pathPending)
            return;

        agent.speed = 3.5f;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;

            waitTimer += Time.deltaTime;

            if (waitTimer >= patrolWaitTime)
            {
                ChooseNextPatrolPoint();
            }
        }
    }

    void Chase()
    {
        agent.speed = 7f;
        agent.SetDestination(playerController.transform.position);
    }

    void Attack()
    {
        Debug.Log("Attacking player");
    }

    void ChooseNextPatrolPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        int nextIndex;

        do
        {
            nextIndex = UnityEngine.Random.Range(0, patrolPoints.Length);
        }
        while (nextIndex == currentPatrolIndex && patrolPoints.Length > 1);

        currentPatrolIndex = nextIndex;

        waitTimer = 0f;

        agent.isStopped = false;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }
}
