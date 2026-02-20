using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public bool hasStarted = false;
    public Trajectory mostRecentTrajectory = new Trajectory { madeIt = false };
    public Trajectory mostRecentSuccessfulTrajectory = new Trajectory { madeIt = true };
    public List<Trajectory> allTrajectories;
    public List<Trajectory> validTrajectories;

    public Transform target;

    public Transform child;
    public GameObject fuel;

    /*----ALL PARAMETERS FOR SHOTOER HERE----*/
    public float minAngle;
    public float maxAngle;

    public float minSpeed;
    public float maxSpeed;

    public float minVX;
    public float maxVX;

    public float minX;
    public float maxX;

    public int angleRes;
    public int vxRes;
    public int xRes;
    public int maxSpeedTries;


    void Start()
    {
        
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space) && !hasStarted)
        //{
        //    transform.eulerAngles = new Vector3(0,0,-45);
        //    fuel.Launch(child.position, new Vector2(10.0f, 10.0f));
        //}
    }

    IEnumerator Simulate(float robotX, float robotVX, float angleDegs, float launchSpeed)
    {
        GameObject obj = Instantiate(fuel, child.position, Quaternion.identity);
        FuelManager manager = obj.GetComponent<FuelManager>();

        Vector2 angleUnitVector = new Vector2(Mathf.Cos(angleDegs * Mathf.Deg2Rad), Mathf.Sin(angleDegs * Mathf.Deg2Rad));
        Vector2 launchVector = angleUnitVector * launchSpeed;
        manager.Launch(child.position, launchVector);

        yield return new WaitUntil(() => manager.dead);

        Trajectory traj = new Trajectory
        {
            initX = robotX,
            initVX = robotVX,
            initTheta = angleDegs,
            initSpeed = launchSpeed,

            madeIt = manager.madeIt,
            maxHeight = manager.maxHeight,
            landingX = manager.end.x,
            landingY = manager.end.y,
        };

        mostRecentTrajectory = traj;
        allTrajectories.Add(traj);

        Destroy(obj);

        yield return null;
    }

    IEnumerator BinarySearch(float robotX, float robotVX, float angleDegs)
    {
        float pivot = (maxSpeed + minSpeed) / 2;
        float currentMaxSpeed = maxSpeed;
        float currentMinSpeed = minSpeed;
        int i = 0;
        bool successful = false;

        transform.eulerAngles = new Vector3(0, 0, -angleDegs);

        while (!mostRecentTrajectory.madeIt && i < maxSpeedTries)
        {
            if (mostRecentTrajectory.landingX != null)
            {
                if (mostRecentTrajectory.landingX > target.position.x)
                {
                    currentMinSpeed = pivot;
                } else
                {
                    currentMaxSpeed = pivot;
                }
            }
            pivot = (maxSpeed + minSpeed) / 2;
            yield return StartCoroutine(Simulate(robotX, robotVX, angleDegs, pivot));
            i++;
        }
        if (!mostRecentTrajectory.madeIt)
        {
            mostRecentSuccessfulTrajectory = null;
        } else
        {
            mostRecentSuccessfulTrajectory = mostRecentTrajectory;
        }
        yield return null;
    }

    public class Trajectory
    {
        public float initX;
        public float initVX;
        public float initTheta;
        public float initSpeed;

        public bool madeIt;
        public float maxHeight;
        public float landingX;
        public float landingY;
    }
}
