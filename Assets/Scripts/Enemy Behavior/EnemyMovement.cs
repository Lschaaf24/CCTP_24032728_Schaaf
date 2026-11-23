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

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float seperationRadius = 0.8f;
    [SerializeField] private float seperationWeight = 1.5f;
    [SerializeField] private int seperationEveryNSteps = 3;

    private float nextRepathTime;
    int stepCounter;
    bool hasLineOfSight;
    Vector2 lastPathTargetPos;
    Vector2 cachedSeperation;
    static readonly Collider2D[] neighbourBuffer = new Collider2D[10];
    ContactFilter2D enemyFilter;

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
        //InvokeRepeating("UpdatePath", 0f, 0.6f);

    }

    void Update(){
        if(dead || player == null) return;
        if(Time.time < nextRepathTime) return;
        nextRepathTime = Time.time + repathInterval;

        Vector2 pos = rb.position;
        Vector2 playerPos = player.position;

        if((playerPos - pos).sqrMagnitude > detectionRadius * detectionRadius) return;

        if((playerPos - lastPathTargetPos).sqrMagnitude < repathMinPlayerMove * repathMinPlayerMove && path!=null) return;

        if(seeker.IsDone()){
            lastPathTargetPos = playerPos;
            Debug.Log("start path");
            seeker.StartPath(pos, playerPos, OnPathComplete);
        }
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

        Vector2 pos = rb.position;
        Vector2 toPlayer = (Vector2)player.position - pos;
        float sqrDist = toPlayer.sqrMagnitude;
        float attackRange = enemyCombat.stats.attackRange;

        if(++stepCounter >= seperationEveryNSteps){
            stepCounter = 0;
            cachedSeperation = CalculateSeperation(pos);
        }

        bool inDetection = sqrDist < detectionRadius * detectionRadius;
        bool inAttackRange = sqrDist <= attackRange * attackRange;

        if(!inDetection){
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("Running", false);
            return;
        }
        if(inAttackRange){
            rb.linearVelocity = cachedSeperation * speed * 0.5f;
            animator.SetBool("Running", false);
            return;
        }
        
        Vector2 desired = GetDesiredDirection(pos, toPlayer);

        Vector2 move = desired + cachedSeperation * seperationWeight;
        if(move.sqrMagnitude > 1f) move.Normalize();

        rb.linearVelocity = move * speed;

        animator.SetBool("Running", true);

        if(move.x > 0.01f) spriteRenderer.flipX = false;
        else if(move.x < -0.01f) spriteRenderer.flipX = true;

    }

    Vector2 GetDesiredDirection(Vector2 pos, Vector2 toPlayer)
    {
        if(hasLineOfSight || path == null){
            return toPlayer.normalized;
        }

        while(currentWaypoint < path.vectorPath.Count){
            Vector2 wp = path.vectorPath[currentWaypoint];
            if((wp - pos).sqrMagnitude < nextWaypointDistance * nextWaypointDistance)
            {
                currentWaypoint++;
            }
            else 
            {
                return (wp - pos).normalized;
            }
        }
        
        return toPlayer.normalized;
    }

    Vector2 CalculateSeperation(Vector2 pos)
    {
        int count = Physics2D.OverlapCircle(pos, seperationRadius, enemyFilter, neighbourBuffer);
        Vector2 push = Vector2.zero;
        int neighbours = 0;

        for(int i = 0; i < count; i++){

            var other = neighbourBuffer[i];
            if(other.attachedRigidbody == rb) continue;

            Vector2 away = pos - (Vector2)other.transform.position;
            float dist = away.magnitude;
            if(dist < 0.0001f) {away = Random.insideUnitCircle; dist = 0.1f;}

            push += (away / dist) * (1f - dist / seperationRadius);
            neighbours++;

        }
        return neighbours > 0 ? push / neighbours : Vector2.zero;

    }

    public void MovementDeath(){
        Debug.Log("Enemy died!");
        dead = true;
    }

}
