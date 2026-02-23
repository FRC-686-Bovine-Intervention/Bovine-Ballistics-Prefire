using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using MathNet.Numerics.LinearAlgebra;

public class Launcher : MonoBehaviour
{
    public bool hasStarted = false;
    public bool autoStart = false;
    public Trajectory mostRecentTrajectory = new Trajectory { madeIt = false };
    public Trajectory mostRecentSuccessfulTrajectory = new Trajectory { madeIt = true };
    public List<Trajectory> allTrajectories = new List<Trajectory>();
    public List<Trajectory> allValidTrajectories = new List<Trajectory>();
    public List<Trajectory> bestTrajectories = new List<Trajectory>();

    public TwoVariablePolynomial3rdDegree hoodPolynomial;
    public TwoVariablePolynomial3rdDegree flywheelPolynomial;

    public Transform target;

    public Transform exitWall;
    public Transform flywheel;
    public Transform hood;
    public Transform hoodRoller;

    public GameObject fuel;

    private string dataInputPath = "shooter.json";
    private string hoodOutputPath = "hoodPolynomial.json";
    private string flywheelOutputPath = "flywheelPolynomial.json";

    public float timescale = 1.0f;
    /*----ALL PARAMETERS FOR SHOOTER HERE----*/
    ShooterConfig config;
    private float dComp;
    private float rComp;


    void Awake()
    {
        if (GetArg("--timescale") != null)
        {
            float parsedTimescale = float.Parse(GetArg("--timescale"));
            timescale = parsedTimescale;
        }
        if (GetArg("--inputpath") != null)
        {
            dataInputPath = GetArg("--inputpath");
        }
        if (GetArg("--outputdir") != null)
        {
            string outdir = GetArg("--outputdir");
            
            if (outdir[outdir.Length - 1] == '/')
            {
                hoodOutputPath = outdir + hoodOutputPath;
                flywheelOutputPath = outdir + flywheelOutputPath;
            } else
            {
                hoodOutputPath = outdir + "/" + hoodOutputPath;
                flywheelOutputPath = outdir + "/" + flywheelOutputPath;
            }
        }
        if (Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.LinuxServer || Application.platform == RuntimePlatform.OSXServer || (GetArg("--autostart") != null && (GetArg("--autostart") == "true" || GetArg("--autostart") == "yes" || GetArg("--autostart") == "y")))
        {
            this.autoStart = true;
        }
    }

    private void Start()
    {
        string json = File.ReadAllText(dataInputPath);
        config = JsonConvert.DeserializeObject<ShooterConfig>(json);
        dComp = config.rHood - config.rRol - config.rFly;
        rComp = dComp / 2;

        hood.localScale = new Vector3(config.rHood * 2, config.rHood * 2, 1);
        flywheel.localScale = new Vector3(config.rFly * 2, config.rFly * 2, 1);
        hoodRoller.localPosition = new Vector3(-config.rHood, 0, 0);
        hoodRoller.localScale = new Vector3(config.rRol * 2, config.rRol * 2, 1);
        exitWall.localPosition = new Vector3
        (
            ((-config.rHood + config.rRol) - config.rFly) / 2,
            0,
            0
        );
        exitWall.localScale = new Vector3(rComp, 0.025f, 1);

        if (autoStart)
        {
            StartSim();
        }
    }

    public static string GetArg(string arg)
    {
        var args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == arg && i + 1 < args.Length)
            {
                return args[i+1];
            }
        }
        return null;
    }

    public void StartSim()
    {
        if (!hasStarted)
        {
            Time.timeScale = timescale;
            hasStarted = true;
            StartCoroutine(AllTrajectories());
        }
    }

    IEnumerator Simulate(float robotX, float robotVX, float angleDegs, float flywheelSpeed)
    {
        GameObject obj = Instantiate(fuel, exitWall.position, Quaternion.identity);
        FuelManager manager = obj.GetComponent<FuelManager>();

        Vector2 angleUnitVector = new Vector2(Mathf.Sin(angleDegs * Mathf.Deg2Rad), Mathf.Cos(angleDegs * Mathf.Deg2Rad));
        Vector2 launchVector = angleUnitVector * getBallExitVelo(flywheelSpeed);
        manager.Launch(exitWall.position, launchVector);

        yield return new WaitUntil(() => manager.dead);
        Debug.Log("Dead");
        Debug.Log(manager.dead);

        Trajectory traj = new Trajectory
        {
            initX = robotX,
            initVX = robotVX,
            initTheta = angleDegs,
            initVFly = flywheelSpeed,

            madeIt = manager.madeIt,
            maxHeight = manager.maxHeight,
            landingX = manager.end.x,
            landingY = manager.end.y,
        };

        mostRecentTrajectory = traj;
        allTrajectories.Add(mostRecentTrajectory);

        if (manager.madeIt)
        {
            allValidTrajectories.Add(mostRecentTrajectory);
        }

        Destroy(obj);

        yield return null;
    }

    IEnumerator BinarySearch(float robotX, float robotVX, float angleDegs)
    {
        float pivot = config.minVFly + (config.maxVFly - config.minVFly) / 2;
        float currentMaxSpeed = config.maxVFly;
        float currentMinSpeed = config.minVFly;
        int i = 0;
        bool successful = false;

        transform.position = new Vector3(-robotX, config.shooterHeight, transform.position.z);
        transform.eulerAngles = new Vector3(0, 0, -angleDegs);

        while (!mostRecentTrajectory.madeIt && i < config.vFlyMaxTries)
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
        for (int i = 0; i < config.xRes; i++)
        {
            float x = config.minX + i * (config.maxX - config.minX) / config.xRes;
            for (int j = 0; j < config.vxRes; j++)
            {
                List<Trajectory> localValidTrajectories = new List<Trajectory>();
                float vx = config.minVX + j * (config.maxVX - config.minVX) / config.vxRes;
                for (int k = 0; k < config.angleRes; k++)
                {
                    float angle = config.minAngle + k * (config.maxAngle - config.minAngle) / config.angleRes;
                    Debug.Log("Trying all for x: " + x + " and vx: " + vx + " and angle: " + angle);
                    mostRecentTrajectory = new Trajectory { madeIt = false };
                    mostRecentSuccessfulTrajectory = new Trajectory { madeIt = true };
                    yield return StartCoroutine(BinarySearch(x, vx, angle));

                    localValidTrajectories.Add(mostRecentSuccessfulTrajectory);
                }

                yield return StartCoroutine(EvaluateTrajectories(localValidTrajectories));
            }
        }

        yield return StartCoroutine(GenerateHoodPolynomial(bestTrajectories));
        string hoodJson = JsonConvert.SerializeObject(hoodPolynomial);
        File.WriteAllText(hoodOutputPath, hoodJson);

        yield return StartCoroutine(GenerateFlywheelPolynomial(bestTrajectories));
        string flywheelJson = JsonConvert.SerializeObject(flywheelPolynomial);
        File.WriteAllText(flywheelOutputPath, flywheelJson);

        if (!autoStart)
        {
            hasStarted = false;
        } else
        {
            Application.Quit();
        }
        yield return null;
    }

    IEnumerator EvaluateTrajectories(List<Trajectory> trajectories)
    {
        float lowestScore = float.MaxValue;
        Trajectory best = trajectories[0];
        for (int i = 1; i < trajectories.Count; i++)
        {
            var trajectory = trajectories[i];
            if (trajectory == null) continue;
            Debug.Log("Testing error");
            yield return StartCoroutine(Simulate(trajectory.initX, trajectory.initVX, trajectory.initTheta + config.angleDev, trajectory.initVFly + config.vFlyDev));
            var trajectory2 = mostRecentTrajectory;
            float dx = trajectory2.landingX - trajectory.landingX;
            Debug.Log("Got error of: " + dx);
            float robustnessScore = (Mathf.Pow(dx/config.vFlyDev, 2) + Mathf.Pow(dx/config.angleDev, 2)) * config.robustnessFactor;

            float heightScore = trajectory.maxHeight * config.heightFactor;

            float totalScore = robustnessScore + heightScore;
            if (totalScore < lowestScore)
            {
                lowestScore = totalScore;
                best = trajectory;
            }
        }

        bestTrajectories.Add(best);
        yield return null;
    }

    IEnumerator GenerateHoodPolynomial(List<Trajectory> trajectories)
    {
        int N = trajectories.Count;
        var A = Matrix<double>.Build.Dense(N, 10);
        var b = Vector<double>.Build.Dense(N);

        for (int i = 0; i < N; i++)
        {
            double x1 = trajectories[i].initX;
            double x2 = trajectories[i].initVX;
            double y = Mathf.Deg2Rad * trajectories[i].initTheta;

            A[i, 0] = 1;
            A[i, 1] = x1;
            A[i, 2] = x2;
            A[i, 3] = x1 * x1;
            A[i, 4] = x1 * x2;
            A[i, 5] = x2 * x2;
            A[i, 6] = x1 * x1 * x1;
            A[i, 7] = x1 * x1 * x2;
            A[i, 8] = x1 * x2 * x2;
            A[i, 9] = x2 * x2 * x2;

            b[i] = y;
        }

        var qr = A.QR();
        Vector<double> coeffs = qr.Solve(b);

        hoodPolynomial = new TwoVariablePolynomial3rdDegree
        {
            coefficients = coeffs.AsArray()
        };
        yield return null;
    }

    IEnumerator GenerateFlywheelPolynomial(List<Trajectory> trajectories)
    {
        int N = trajectories.Count;
        var A = Matrix<double>.Build.Dense(N, 10);
        var b = Vector<double>.Build.Dense(N);

        for (int i = 0; i < N; i++)
        {
            double x1 = trajectories[i].initX;
            double x2 = trajectories[i].initVX;
            double y = trajectories[i].initVFly;

            A[i, 0] = 1;
            A[i, 1] = x1;
            A[i, 2] = x2;
            A[i, 3] = x1 * x1;
            A[i, 4] = x1 * x2;
            A[i, 5] = x2 * x2;
            A[i, 6] = x1 * x1 * x1;
            A[i, 7] = x1 * x1 * x2;
            A[i, 8] = x1 * x2 * x2;
            A[i, 9] = x2 * x2 * x2;

            b[i] = y;
        }

        var qr = A.QR();
        Vector<double> coeffs = qr.Solve(b);

        flywheelPolynomial = new TwoVariablePolynomial3rdDegree
        {
            coefficients = coeffs.AsArray()
        };
        yield return null;
    }


    public float getBallExitVelo(float vFly)
    {
        return (vFly + vFly * config.fVelo) / 2;
    }

    [Serializable]
    public class Trajectory
    {
        public float initX { get; set; }
        public float initVX { get; set; }
        public float initTheta { get; set; }
        public float initVFly { get; set; }

        public bool madeIt { get; set; }
        public float maxHeight { get; set; }
        public float landingX { get; set; }
        public float landingY { get; set; }
    }

    [Serializable]
    public class ShooterConfig
    {
        public float shooterHeight;

        public float rFly;
        public float rRol;
        public float rHood;
        public float fVelo;

        public float maxVFly;
        public float minVFly;
        public float vFlyMaxTries;

        public float minAngle;
        public float maxAngle;
        public int angleRes;

        public float minVX;
        public float maxVX;
        public int vxRes;

        public float minX;
        public float maxX;
        public int xRes;

        public float angleDev;
        public float vFlyDev;

        public float robustnessFactor;
        public float heightFactor;
    }

    [Serializable]
    public class TwoVariablePolynomial3rdDegree
    {
        public double[] coefficients;
    }
}
