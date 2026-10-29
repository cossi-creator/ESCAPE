using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA enemiga con NavMesh. Estados: Patrullaje (waypoints) → Persecución → Búsqueda → Patrullaje.
/// Ve al jugador con un cono de visión (Vector3.Angle) + Raycast para no ver a través de paredes.
/// Si suena la alarma del GameManager, ignora la visión y persigue al jugador sin parar.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    private enum State { Patrol, Chase, Search }

    [Header("Referencias")]
    [SerializeField] private Player player;
    [SerializeField] private Animator animator;            // Opcional. Necesita un parámetro float "Speed"

    [Header("Patrullaje")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 1.8f;
    [SerializeField] private float waypointWaitTime = 1.5f;

    [Header("Visión")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField, Range(10f, 360f)] private float viewAngle = 100f;   // Ángulo total del cono
    [SerializeField] private float eyeHeight = 1.6f;                     // Debe quedar DENTRO del collider del enemigo
    [SerializeField] private float targetHeight = 0.6f;                  // Punto del jugador al que apunta el rayo
    [SerializeField] private float closeDetectionRadius = 1.5f;          // Te siente aunque estés a su espalda
    [SerializeField] private float crouchVisibility = 0.6f;              // Agachado te ve de más cerca
    [SerializeField] private LayerMask visionMask = ~0;                  // Paredes + jugador
    [SerializeField] private float visionCheckInterval = 0.1f;

    [Header("Persecución")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float alarmChaseSpeed = 4.4f;
    [SerializeField] private float repathInterval = 0.2f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private int attackDamage = 50;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Búsqueda")]
    [SerializeField] private float searchSpeed = 3f;
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float searchTurnSpeed = 90f;                // Grados/seg mirando alrededor

    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private NavMeshAgent agent;
    private State state = State.Patrol;
    private bool alarmed;
    private bool canSeePlayer;
    private float visionTimer;

    // Patrullaje
    private int waypointIndex;
    private bool waiting;
    private float waitTimer;

    // Persecución / búsqueda
    private Vector3 lastKnownPosition;
    private float repathTimer;
    private float nextAttackTime;
    private bool arrivedAtLastKnown;
    private float searchTimer;

    #region Unity
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player == null) player = FindFirstObjectByType<Player>(); // Unity 2022: FindObjectOfType
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AlarmTriggered += OnAlarmTriggered;
            if (GameManager.Instance.AlarmActive) OnAlarmTriggered();
        }

        if (!alarmed) ResumePatrol();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.AlarmTriggered -= OnAlarmTriggered;
    }

    private void Update()
    {
        if (player == null) return;

        if (player.IsDead) // Ya te atrapó: se queda quieto
        {
            agent.isStopped = true;
            UpdateAnimation();
            return;
        }

        UpdateVision();

        switch (state)
        {
            case State.Patrol:
                if (canSeePlayer) { StartChase(); break; }
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.Search:
                UpdateSearch();
                break;
        }

        UpdateAnimation();
    }
    #endregion

    #region Visión
    private void UpdateVision()
    {
        if (alarmed) { canSeePlayer = true; return; } // Con la alarma sabe dónde estás

        visionTimer -= Time.deltaTime;
        if (visionTimer > 0f) return;

        visionTimer = visionCheckInterval;
        canSeePlayer = CanSeePlayer();
    }

    private bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.transform.position + Vector3.up * targetHeight;
        Vector3 toTarget = target - eye;
        float distance = toTarget.magnitude;
        if (distance < 0.01f) return true;

        // 1) ¿Está muy cerca? Lo detecta sin importar el ángulo
        bool isClose = distance <= closeDetectionRadius;

        if (!isClose)
        {
            // 2) Distancia (el jugador agachado es más difícil de ver)
            float range = viewDistance * (player.IsCrouching ? crouchVisibility : 1f);
            if (distance > range) return false;

            // 3) Cono de visión: ángulo entre "adelante" y la dirección al jugador (en el plano horizontal)
            Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            if (Vector3.Angle(transform.forward, flatDirection) > viewAngle * 0.5f) return false;
        }

        // 4) Raycast: lo primero que golpea el rayo tiene que ser el jugador (si es una pared, no lo ve)
        if (Physics.Raycast(eye, toTarget / distance, out RaycastHit hit, distance + 0.5f,
                            visionMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<Player>() != null;
        }
        return false;
    }
    #endregion

    #region Patrullaje
    private void ResumePatrol()
    {
        state = State.Patrol;
        waiting = false;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.3f;

        if (waypoints != null && waypoints.Length > 0)
        {
            waypointIndex = NearestWaypointIndex();
            agent.SetDestination(waypoints[waypointIndex].position);
        }
    }

    private void UpdatePatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (waiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointWaitTime)
            {
                waiting = false;
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[waypointIndex].position);
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waiting = true;
            waitTimer = 0f;
        }
    }

    private int NearestWaypointIndex()
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = (waypoints[i].position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }
    #endregion

    #region Persecución
    private void StartChase()
    {
        state = State.Chase;
        waiting = false;
        agent.isStopped = false;
        agent.speed = alarmed ? alarmChaseSpeed : chaseSpeed;
        agent.stoppingDistance = attackRange * 0.7f;
        repathTimer = 0f;
    }

    private void UpdateChase()
    {
        if (!canSeePlayer)
        {
            StartSearch(); // Te perdió de vista
            return;
        }

        lastKnownPosition = player.transform.position;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            agent.SetDestination(lastKnownPosition);
            repathTimer = repathInterval;
        }

        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        if (Vector3.Distance(transform.position, player.transform.position) > attackRange) return;

        nextAttackTime = Time.time + attackCooldown;
        player.TakeDamage(attackDamage); // IDamageable del PDF
    }

    private void OnAlarmTriggered()
    {
        alarmed = true;
        canSeePlayer = true;
        StartChase();
    }
    #endregion

    #region Búsqueda
    private void StartSearch()
    {
        state = State.Search;
        agent.isStopped = false;
        agent.speed = searchSpeed;
        agent.stoppingDistance = 0.3f;
        arrivedAtLastKnown = false;
        searchTimer = 0f;

        // Última posición donde te vio (ajustada al NavMesh)
        if (NavMesh.SamplePosition(lastKnownPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            lastKnownPosition = navHit.position;

        agent.SetDestination(lastKnownPosition);
    }

    private void UpdateSearch()
    {
        if (canSeePlayer) { StartChase(); return; } // Te volvió a ver

        if (!arrivedAtLastKnown)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                arrivedAtLastKnown = true;
                searchTimer = 0f;
                agent.isStopped = true; // Se frena a revisar la zona
            }
            return;
        }

        // Llegó: mira alrededor durante searchDuration segundos
        searchTimer += Time.deltaTime;
        transform.Rotate(0f, searchTurnSpeed * Time.deltaTime, 0f);

        if (searchTimer >= searchDuration) ResumePatrol();
    }
    #endregion

    #region Animación
    private void UpdateAnimation()
    {
        if (animator == null) return;
        // El Blend Tree usa "Speed": 0 = idle, ~patrolSpeed = caminar, ~chaseSpeed = correr
        animator.SetFloat(SpeedParam, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }
    #endregion

    #region Gizmos (seleccioná al enemigo en la escena para verlos)
    private void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.yellow;
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(eye, left * viewDistance);
        Gizmos.DrawRay(eye, right * viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeDetectionRadius);

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
    #endregion
}