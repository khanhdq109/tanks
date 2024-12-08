using UnityEngine;
using UnityEngine.AI;

public class TankAI : MonoBehaviour
{
    public float maxDistance = 20f;
    public float minDistance = 10f;
    public float shootingInterval = 2f;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    public Rigidbody shellPrefab;
    public Transform fireTransform;
    public AudioSource shootingAudio;
    public AudioClip fireClip;
    public float obstacleDetectionDistance = 5f;

    private Rigidbody rb;
    private Transform targetTank;
    private float lastShotTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        lastShotTime = Time.time;
    }

    void Update()
    {
        FindNearestTank();
        if (targetTank != null)
        {
            MoveTowardsTarget();
            if (HasLineOfSight())
            {
                ShootAtTarget();
            }
        }
    }

    void FindNearestTank()
    {
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        float nearestDistance = Mathf.Infinity;
        Transform nearestTank = null;

        foreach (GameObject tank in tanks)
        {
            if (tank.transform != transform)
            {
                float distance = Vector3.Distance(transform.position, tank.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTank = tank.transform;
                }
            }
        }

        targetTank = nearestTank;
    }

    void MoveTowardsTarget()
    {
        if (targetTank == null) return;

        float distance = Vector3.Distance(transform.position, targetTank.position);
        if (distance > minDistance)
        {
            Vector3 direction = (targetTank.position - transform.position).normalized;
            Vector3 avoidanceDirection = Vector3.zero;

            // Check for obstacles using multiple raycasts
            RaycastHit hit;
            Vector3[] rayDirections = { transform.forward, transform.forward + transform.right * 0.5f, transform.forward - transform.right * 0.5f };

            foreach (Vector3 rayDirection in rayDirections)
            {
                if (Physics.Raycast(transform.position, rayDirection, out hit, obstacleDetectionDistance))
                {
                    if (!hit.collider.CompareTag("Tank"))
                    {
                        // Calculate avoidance direction
                        avoidanceDirection += Vector3.Cross(transform.up, hit.normal).normalized;
                    }
                }
            }

            // If an obstacle is detected, adjust the direction to move parallel to the obstacle
            if (avoidanceDirection != Vector3.zero)
            {
                avoidanceDirection.Normalize();
                direction = Vector3.Lerp(direction, avoidanceDirection, 0.5f);
            }

            // Move the tank
            rb.MovePosition(transform.position + direction * moveSpeed * Time.deltaTime);

            // Rotate the tank towards the combined direction
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime));
        }
    }

    bool HasLineOfSight()
    {
        if (targetTank == null) return false;

        RaycastHit hit;
        Vector3 direction = (targetTank.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, direction, out hit, maxDistance))
        {
            if (hit.transform == targetTank)
            {
                return true;
            }
        }
        return false;
    }

    void ShootAtTarget()
    {
        if (Time.time > lastShotTime + shootingInterval)
        {
            lastShotTime = Time.time;
            float distance = Vector3.Distance(transform.position, targetTank.position);
            // Create an instance of the shell and store a reference to its rigidbody.
            Rigidbody shellInstance = Instantiate(shellPrefab, fireTransform.position, fireTransform.rotation) as Rigidbody;

            // Set the shell's linearVelocity to the launch force in the fire position's forward direction.
            shellInstance.linearVelocity = fireTransform.forward * distance;

            // Play the shooting audio.
            shootingAudio.clip = fireClip;
            shootingAudio.Play();
        }
    }
}