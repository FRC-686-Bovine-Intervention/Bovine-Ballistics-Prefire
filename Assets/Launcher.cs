using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public bool hasStarted = false;
    public Trajectory mostRecentTrajectory = new Trajectory { madeIt = false };
    public Trajectory mostRecentSuccessfulTrajectory = new Trajectory { madeIt = true };
    public List<Trajectory> allTrajectories = new List<Trajectory>();
    public List<Trajectory> validTrajectories = new List<Trajectory>();

    public Transform target;

    public Transform child;
    public GameObject fuel;

    private static string Path => Application.persistentDataPath + "/data.txt";

    public float timescale;
    /*----ALL PARAMETERS FOR SHOOTER HERE----*/
    [Header("Angle Params")]
    public float minAngle;
    public float maxAngle;
    public int angleRes;

    [Header("Speed Params")]
    public float minSpeed;
    public float maxSpeed;
    public int maxSpeedTries;

    [Header("Velocity Params")]
    public float minVX;
    public float maxVX;
    public int vxRes;

    [Header("Position Params")]
    public float minX;
    public float maxX;
    public int xRes;


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
        if (Input.GetKeyDown(KeyCode.Space) && !hasStarted)
        {
            Time.timeScale = timescale;
            hasStarted = true;
            StartCoroutine(AllTrajectories());
        }
    }

    IEnumerator Simulate(float robotX, float robotVX, float angleDegs, float launchSpeed)
    {
        GameObject obj = Instantiate(fuel, child.position, Quaternion.identity);
        FuelManager manager = obj.GetComponent<FuelManager>();

        Vector2 angleUnitVector = new Vector2(Mathf.Sin(angleDegs * Mathf.Deg2Rad), Mathf.Cos(angleDegs * Mathf.Deg2Rad));
        Vector2 launchVector = angleUnitVector * launchSpeed;
        manager.Launch(child.position, launchVector);

        yield return new WaitUntil(() => manager.dead);
        Debug.Log("Dead");
        Debug.Log(manager.dead);

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
        allTrajectories.Add(mostRecentTrajectory);

        Destroy(obj);

        yield return null;
    }

    IEnumerator BinarySearch(float robotX, float robotVX, float angleDegs)
    {
        float pivot = minSpeed + (maxSpeed - minSpeed) / 2;
        float currentMaxSpeed = maxSpeed;
        float currentMinSpeed = minSpeed;
        int i = 0;
        bool successful = false;

        transform.position = new Vector3(robotX, transform.position.y, transform.position.z);
        transform.eulerAngles = new Vector3(0, 0, -angleDegs);

        while (!mostRecentTrajectory.madeIt && i < maxSpeedTries)
        {
            pivot = currentMinSpeed + (currentMaxSpeed - currentMinSpeed) / 2;
            Debug.Log("Trying Speed: " + pivot);
            yield return StartCoroutine(Simulate(robotX, robotVX, angleDegs, pivot));
            i++;
            if (mostRecentTrajectory.landingX != null)
            {
                if (mostRecentTrajectory.landingX < target.position.x)
                {
                    currentMinSpeed = pivot;
                }
                else
                {
                    currentMaxSpeed = pivot;
                }
            }
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

    IEnumerator AllTrajectories()
    {
        for (int i = 0; i < xRes; i++)
        {
            float x = minX + i * (maxX - minX) / xRes;
            for (int j = 0; j < vxRes; j++)
            {
                float vx = minVX + j * (maxVX - minVX) / vxRes;
                for (int k = 0; k < angleRes; k++)
                {
                    float angle = minAngle + k * (maxAngle - minAngle) / angleRes;
                    Debug.Log("Trying all for x: " + x + " and vx: " + vx + " and angle: " + angle);
                    mostRecentTrajectory = new Trajectory { madeIt = false };
                    mostRecentSuccessfulTrajectory = new Trajectory { madeIt = true };
                    yield return StartCoroutine(BinarySearch(x, vx, angle));
                }
            }
        }

        string json = JsonUtility.ToJson(allTrajectories, true);
        File.WriteAllText(Path, json);
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
