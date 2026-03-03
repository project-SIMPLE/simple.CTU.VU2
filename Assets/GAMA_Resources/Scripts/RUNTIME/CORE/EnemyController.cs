using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private List<Transform> wayPoints;

    private int currentWayPointIndex = 0;
    private float agentStoppingDistance = .3f;

    private bool wayPointsSet = false;
    private bool hasStartedMoving = false;

    NavMeshAgent agent;

    private Enemy enemy;
    void Start()
    {
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!wayPointsSet)
        {
            return;
        }

        // Dừng di chuyển nếu enemy đã chết
        if (enemy != null && enemy.IsDead()) return;

        // Chờ agent được đặt lên NavMesh
        if (!agent.isOnNavMesh) return;

        // Lần đầu: đặt destination rõ ràng, tránh bị skip do remainingDistance == 0 khi khởi tạo
        if (!hasStartedMoving)
        {
            if (currentWayPointIndex < wayPoints.Count)
            {
                agent.SetDestination(wayPoints[currentWayPointIndex].position);
                currentWayPointIndex++;
                hasStartedMoving = true;
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agentStoppingDistance)
        {
            if (currentWayPointIndex >= wayPoints.Count)
            {
                Destroy(this.gameObject, .1f);
            }
            else
            {
                agent.SetDestination(wayPoints[currentWayPointIndex].position);
                currentWayPointIndex++;
            }
        }
    }

    public void SetDestination(List<Transform> wayPoints)
    {
        this.wayPoints = wayPoints;
        wayPointsSet = true;
        hasStartedMoving = false;
        currentWayPointIndex = 0;
    }
}
