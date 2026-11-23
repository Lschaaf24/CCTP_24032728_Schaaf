using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Rigidbody2D), typeof(Seeker))]
public class EnemyMovement : MonoBehaviour
{
    public bool dead = false;

    [SerializeField] private float speed = 2.0f;
    [SerializeField] private float detectionRadius = 5.0f;
    [SerializeField] private float nextWaypointDistance = 1f;

    [SerializeField] private float repathInterval = 0.6f;
    [SerializeField] private float repathMinPlayerMove = 0.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float seperationRadius = 0.8f;
    [SerializeField] private float seperationWeight = 1.5f;
    [SerializeField] private int seperationEveryNSteps = 3;

    private float nextRepathTime;
    int stepCounter;

    Path path;
    int currentWaypoint = 0;
    bool reachedEndOfPath = false;

    public Animator animator;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Seeker seeker;
    private Rigidbody2D rb;
    private EnemyCombat enemyCombat;
    private Health health;

    // Start is called before the first frame update
    void Start()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        enemyCombat = GetComponent<EnemyCombat>();
        health = GetComponent<Health>();

        player = GameObject.FindGameObjectWithTag("Player").transform;

        nextRepathTime = Time.time + Random.value * repathInterval;
        stepCounter = Random.Range(0, seperationEveryNSteps);
        InvokeRepeating("UpdatePath", 0f, 0.6f);

    }

    void UpdatePath(){
        if(seeker.IsDone())
        seeker.StartPath(rb.position, player.position, OnPathComplete);
    }

    void OnPathComplete(Path p){
        if(!p.error){
            path = p;
            currentWaypoint = 0;
        }
    }

    void FixedUpdate()
    {
        if (path == null || dead || health.isKnockedBack || enemyCombat.isStunned) return;

        if (currentWaypoint >= path.vectorPath.Count)
        {
            reachedEndOfPath = true;
            return;
            
        }
        else
        {
            reachedEndOfPath = false;
        }


        if (player != null)
        {
            Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized ;
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer > enemyCombat.stats.attackRange && distanceToPlayer < detectionRadius)
            {
                rb.linearVelocity = direction * speed;
                animator.SetBool("Running", true);

                if (direction.x > 0f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (direction.x < 0f)
                {
                    spriteRenderer.flipX = true;
                }
                
                float distanceAlongPath = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);
            
                if (distanceAlongPath < nextWaypointDistance)
                {
                    currentWaypoint++;
                }
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("Running", false);
            }
        }
    }
    public void MovementDeath(){
        Debug.Log("Enemy died!");
        dead = true;
    }

}
