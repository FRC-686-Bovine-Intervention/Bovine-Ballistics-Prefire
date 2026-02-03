using System.Collections.Generic;
using UnityEngine;
using static Launch;

public class Projectile : MonoBehaviour
{
    [Header("Scene refs")]
    public Transform launcher;
    public Transform target;

    [Header("Launch parameters")]
    public float launchAngleDeg = 25f;
    public float minSpeed = 0f;
    public float maxSpeed = 30f;

    [Header("Physics")]
    public float mass = 0.226f;
    public float dragCoefficient = 0.47f;
    public float airDensity = 1.225f;

    [Header("Results")]
    public List<Traj> trajectories = new List<Traj>();

    Vector2 v;
    Vector2 a;

    bool launched;
    bool landed;
    bool madeIt;

    float crossSection;

    float currentMin;
    float currentMax;
    float pivot;
    bool searching;

    void Start()
    {
        float r = transform.localScale.x * 0.5f;
        crossSection = Mathf.PI * r * r;

        currentMin = minSpeed;
        currentMax = maxSpeed;
        pivot = (currentMin + currentMax) * 0.5f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !searching)
        {
            trajectories.Clear();
            searching = true;
            StartNextShot();
        }
    }

    void FixedUpdate()
    {
        if (!launched || landed)
            return;

        Vector2 force = mass * Physics2D.gravity;

        float speed = v.magnitude;
        if (speed > 0f)
        {
            float fd = 0.5f * airDensity * speed * speed * dragCoefficient * crossSection;
            force += -fd * v.normalized;
        }

        a = force / mass;
        v += a * Time.fixedDeltaTime;

        transform.position += (Vector3)(v * Time.fixedDeltaTime);
    }

    void StartNextShot()
    {
        ResetProjectile();

        float rad = launchAngleDeg * Mathf.Deg2Rad;
        v = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * pivot;

        launched = true;
    }

    void EvaluateShot()
    {
        float xError = transform.position.x - target.position.x;

        Traj t = new Traj
        {
            launchAngle = launchAngleDeg,
            launchSpeed = pivot,
            xError = xError,
            madeIt = madeIt
        };

        trajectories.Add(t);

        if (madeIt)
        {
            Debug.Log($"Solved! speed={pivot}  attempts={trajectories.Count}");
            searching = false;
            return;
        }

        if (xError > 0)
            currentMax = pivot;
        else
            currentMin = pivot;

        pivot = (currentMin + currentMax) * 0.5f;

        StartNextShot();
    }

    void ResetProjectile()
    {
        transform.position = launcher.position;
        v = Vector2.zero;
        a = Vector2.zero;
        launched = false;
        landed = false;
        madeIt = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Kill"))
        {
            landed = true;
            madeIt = false;
            EvaluateShot();
        }
        else if (collision.CompareTag("Respawn"))
        {
            madeIt = true;
        }
        else if (collision.CompareTag("Finish") && madeIt)
        {
            landed = true;
            EvaluateShot();
        }
    }
}