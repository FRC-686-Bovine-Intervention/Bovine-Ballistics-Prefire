using System.Collections.Generic;
using UnityEngine;
using static Launch;

public class Projectile : MonoBehaviour
{
    public Transform launchPoint;
    public Transform target;

    public float launchAngleDegs = 25f;

    public bool inAir = false;
    public bool isTrajStart = true;
    public bool madeIt = false;
    
    Vector2 v;
    Vector2 a;

    public float initVx;
    public float initX;

    public int i;
    public int j;
    public int k;

    [Header("Launch parameters")]
    public float minSpeed = 0f;
    public float maxSpeed = 30f;
    public float minAngle = 0f;
    public float maxAngle = 90f;

    [Header("Physics")]
    public float mass = 0.226f;
    public float dragCoefficient = 0.47f;
    public float airDensity = 1.225f;

    public List<Traj> trajectories = new List<Traj>();

    float currentMinSpeed;
    float currentMaxSpeed;
    float currentPivotSpeed;

    int angleRes;
    int initVxRes;
    int initXRes;

    float crossSection;

    private void Start()
    {
        float r = transform.localScale.x * 0.5f;
        crossSection = Mathf.PI * r * r;

        currentMinSpeed = minSpeed;
        currentMaxSpeed = maxSpeed;
        currentPivotSpeed = (currentMaxSpeed + currentMinSpeed) / 2;
    }

    private void FixedUpdate()
    {
        if (!inAir && isTrajStart)
        {
            Debug.Log("INIT TRAJ");
            v = new Vector2(initVx, 0);
            v += currentPivotSpeed * new Vector2(Mathf.Cos(Mathf.Deg2Rad * launchAngleDegs), Mathf.Sin(Mathf.Deg2Rad * launchAngleDegs));
            transform.position = launchPoint.position;
            isTrajStart = false;
            madeIt = false;
            inAir = true;
        }
        if (inAir)
        {
            Vector2 force = mass * new Vector2(0, -9.81f);

            float speed = v.magnitude;
            if (Mathf.Abs(speed) > 0f)
            {
                float fd = 0.5f * airDensity * speed * speed * dragCoefficient * crossSection;
                force += -fd * v.normalized;
            }

            a = force / mass;
            v += a * Time.fixedDeltaTime;

            transform.position += (Vector3)(v * Time.fixedDeltaTime);
        }
        if (!inAir && !isTrajStart)
        {
            if (transform.position.x < target.position.x)
            {
                currentMinSpeed = currentPivotSpeed;
                currentPivotSpeed = (currentMaxSpeed + currentMinSpeed) / 2;
            }
            else
            {
                currentMaxSpeed = currentPivotSpeed;
                currentPivotSpeed = (currentMaxSpeed + currentMinSpeed) / 2;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Kill"))
        {
            isTrajStart = true;
            inAir = false;
            madeIt = false;
        }
        else if (collision.CompareTag("Respawn"))
        {
            madeIt = true;
        }
        else if (collision.CompareTag("Finish") && madeIt)
        {
            isTrajStart = true;
            inAir = false;
        }
    }
}