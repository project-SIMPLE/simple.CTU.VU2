using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Ally : MonoBehaviour, IDamageable, IDamage
{
    /*
    Ally : (vn) --> đồng minh
    Create fresh water to neutralize salt water
     */
    [Header("Basic Info")]
    [SerializeField] private string uniqueName;
    [SerializeField] private int lvl;
    
    [Header("Stats")]
    [SerializeField] private int health = 2;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectRange = 30f;
    [SerializeField] private int attackDamage = 1;

    [Header("Miscellaneous")]
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private Animator animator;

    // runtime privates
    private int currentHealh;
    private Transform target;
    private NavMeshAgent navAgent;
    private float timeLife = 5.0f;
    private bool useNavMesh = true;

    // Getters 
    public int Health { 
        get { return currentHealh; } 
    }
    public float Range { 
        get { return detectRange; }
    }
    public int Damage { 
        get { return attackDamage; } 
    }


    void Start()
    {
        currentHealh = health;
        navAgent = GetComponent<NavMeshAgent>();

        // Warp Ally xuống vị trí NavMesh gần nhất (máy bơm có thể cao hơn NavMesh)
        if (navAgent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            {
                navAgent.enabled = false;
                transform.position = hit.position;
                navAgent.enabled = true;
                navAgent.Warp(hit.position);
                navAgent.speed = moveSpeed;
                useNavMesh = true;
            }
            else
            {
                Debug.LogWarning($"[Ally] No NavMesh within 20m of {transform.position}, using fallback movement");
                navAgent.enabled = false;
                useNavMesh = false;
            }
        }
        else
        {
            useNavMesh = false;
        }

        InvokeRepeating("FindTarget", 0f, .5f);
    }

    void Update()
    {
        if (IsDead()) return;

        if (target)
        {
            timeLife = 5.0f;
            MoveToTarget();
        }
        else
        {
            // Không có target → dừng di chuyển, đếm ngược tự huỷ
            if (useNavMesh && navAgent != null && navAgent.enabled)
                navAgent.ResetPath();

            timeLife -= Time.deltaTime;
            if (timeLife <= 0)
            {
                Die();
            }
        }  
    }

    private void MoveToTarget()
    {
        if (useNavMesh && navAgent != null && navAgent.enabled)
        {
            // Sample vị trí target xuống NavMesh (target có thể không nằm trên NavMesh)
            Vector3 dest = target.position;
            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                dest = hit.position;

            navAgent.SetDestination(dest);
        }
        else
        {
            // Fallback: di chuyển thẳng đến target khi không có NavMesh
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    

    void OnTriggerEnter(Collider other)
    {
        if (HasValidTarget(other.gameObject) && !IsDead())
        {
            Enemy enemy = other.GetComponent<Enemy>();
            enemy.TakeDamage(attackDamage);
            TakeDamage(1);
        }
    }

    void FindTarget()
    {
        // 1. Tìm Enemy gần nhất bằng OverlapSphere (theo layerMask)
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, detectRange, targetLayerMask);

        float closestDistance = Mathf.Infinity;
        GameObject closestTarget = null;

        foreach (Collider col in nearbyTargets)
        {
            if (col == null) continue;
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = col.gameObject;
                }
            }
        }

        // 2. Fallback: tìm bằng Tag "Enemy" nếu OverlapSphere không tìm thấy
        if (closestTarget == null)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null || !enemy.activeSelf) continue;
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null && !enemyScript.IsDead())
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestTarget = enemy;
                    }
                }
            }
        }

        target = closestTarget != null ? closestTarget.transform : null;
    }

    public void TakeDamage(int damage)
    {
        currentHealh -= damage;
        if(currentHealh <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        //navAgent.enabled = false;
        if (animator) animator.Play("Disappear");
        Destroy(gameObject,2f);
    }

    public bool IsDead()
    {
        return currentHealh <= 0;
    }

    public bool HasValidTarget(GameObject target)
    {
        return (targetLayerMask == (targetLayerMask | (1 << target.layer)));
    }

    public void DealDamage(IDamageable target)
    {
        target.TakeDamage(attackDamage);
    }
}
