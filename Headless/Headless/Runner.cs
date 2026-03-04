using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Headless
{
    internal class Runner
    {
        public List<Trajectory> allTrajectories = new List<Trajectory>();
        public List<Trajectory> allValidTrajectories = new List<Trajectory>();
        public List<Trajectory> bestTrajectories = new List<Trajectory>();

        public TwoVariablePolynomial3rdDegree hoodPolynomial;
        public TwoVariablePolynomial3rdDegree flywheelPolynomial;
        public TwoVariablePolynomial3rdDegree tofPolynomial;

        public float launchPointR;

        private string dataInputPath = "shooter.json";
        private string hoodOutputPath = "hoodPolynomial.json";
        private string flywheelOutputPath = "flywheelPolynomial.json";
        private string tofOutputPath = "tofPolynomial.json";

        /*----ALL PARAMETERS FOR SHOOTER HERE----*/
        ShooterConfig config;
        private float dComp;
        private float rComp;

        public void Main(string[] args)
        {
            Awake();

            var permutations = new List<(float, float, List<Trajectory>)>();

            for (int i = 0; i < config.xRes; i++)
            {
                float x = config.minX + i * (config.maxX - config.minX) / config.xRes;
                for (int j = 0; j < config.vxRes; j++)
                {
                    float vx = config.minVX + j * (config.maxVX - config.minVX) / config.vxRes;
                    permutations.Add((x, vx, new List<Trajectory>()));
                }
            }
            Parallel.ForEach(permutations, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, perm =>
            {
                List<Trajectory> localValidTrajectories = new List<Trajectory>();
                for (int i = 0; i < config.angleRes; i++)
                {
                    float angle = config.minAngle + i * (config.maxAngle - config.minAngle) / config.angleRes;
                    //Console.WriteLine("Trying all for x: " + perm.Item1 + " and vx: " + perm.Item2 + " and angle: " + angle);
                    Trajectory traj = BinarySearch(perm.Item1, perm.Item2, angle);

                    localValidTrajectories.Add(traj);
                }
                perm.Item3 = localValidTrajectories;
                EvaluateTrajectories(localValidTrajectories);
            });

            //for (int i = 0; i < permutations.Count; i++)
            //{
            //    List<Trajectory> nonNull = new List<Trajectory>();
            //    foreach (var traj in permutations[i].Item3)
            //    {
            //        if (traj != null)
            //        {
            //            nonNull.Add(traj);
            //        }
            //    }
            //    ;
            //}

            List<Trajectory> nonNullBestTrajectories = new List<Trajectory>();
            foreach (var traj in bestTrajectories)
            {
                if (traj == null)
                {
                    Console.WriteLine("Invalid traj");
                } else
                {
                    Console.WriteLine(JsonConvert.SerializeObject(traj));
                    nonNullBestTrajectories.Add(traj);
                }
            }

            GenerateHoodPolynomial(nonNullBestTrajectories);
            string hoodJson = JsonConvert.SerializeObject(hoodPolynomial);
            File.WriteAllText(hoodOutputPath, hoodJson);

            GenerateFlywheelPolynomial(nonNullBestTrajectories);
            string flywheelJson = JsonConvert.SerializeObject(flywheelPolynomial);
            File.WriteAllText(flywheelOutputPath, flywheelJson);

            GenerateTOFPolynomial(nonNullBestTrajectories);
            string tofJson = JsonConvert.SerializeObject(tofPolynomial);
            File.WriteAllText(tofOutputPath, tofJson);

            double totalError = 0;
            for (int i = 0; i < nonNullBestTrajectories.Count; i++)
            {
                double predicted = hoodPolynomial.Evaluate(
                    nonNullBestTrajectories[i].initX,
                    nonNullBestTrajectories[i].initVX);

                double actual = Helpers.deg2rad * nonNullBestTrajectories[i].initTheta;

                double err = predicted - actual;
                totalError += err * err;
            }

            Console.WriteLine("RMSE: " + Math.Sqrt(totalError / nonNullBestTrajectories.Count));
        }

        void Awake()
        {
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
                    tofOutputPath = outdir + tofOutputPath;
                }
                else
                {
                    hoodOutputPath = outdir + "/" + hoodOutputPath;
                    flywheelOutputPath = outdir + "/" + flywheelOutputPath;
                    tofOutputPath = outdir + "/" + tofOutputPath;
                }
            }

            string json = File.ReadAllText(dataInputPath);
            config = JsonConvert.DeserializeObject<ShooterConfig>(json);
            dComp = config.rHood - config.rRol - config.rFly;
            rComp = dComp / 2;

            launchPointR = (config.rHood - config.rRol - config.rFly) / 2 + config.rFly;
        }

        public static string GetArg(string arg)
        {
            var args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == arg && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        Trajectory Simulate(float robotX, float robotVX, float angleDegs, float flywheelSpeed)
        {
            Fuel obj = new Fuel();

            Vector2 angleUnitVector = new Vector2(Mathf.Sin(angleDegs * Helpers.deg2rad), Mathf.Cos(angleDegs * Helpers.deg2rad));
            Vector2 launchVector = angleUnitVector * getBallExitVelo(flywheelSpeed) + new Vector2(robotVX, 0);
            //Console.WriteLine("Launch vector: " + launchVector);
            obj.init();
            obj.Launch(findLaunchPos(robotX, angleDegs), launchVector);

            while (!obj.dead)
            {
                obj.Update(0.01f);
            }
            Trajectory traj = new Trajectory
            {
                initX = robotX,
                initVX = robotVX,
                initTheta = angleDegs,
                initVFly = flywheelSpeed,

                madeIt = obj.madeIt,
                maxHeight = obj.maxHeight,
                landingX = obj.end.X,
                landingY = obj.end.Y,
                tof = obj.tof
            };

            //allTrajectories.Add(traj);

            //if (obj.madeIt)
            //{
            //    //allValidTrajectories.Add(traj);
            //}

            //obj = null; //Remove handle to be safe

            return traj;
        }

        Trajectory BinarySearch(float robotX, float robotVX, float angleDegs)
        {
            float pivot = config.minVFly + (config.maxVFly - config.minVFly) / 2;
            float currentMaxSpeed = config.maxVFly;
            float currentMinSpeed = config.minVFly;
            int i = 0;
            bool successful = false;

            Trajectory mostRecentTrajectory = new Trajectory
            {
                madeIt = false
            };
            Trajectory mostRecentSuccessfulTrajectory = new Trajectory
            {
                madeIt = true
            };

            while (!mostRecentTrajectory.madeIt && i < config.vFlyMaxTries)
            {
                pivot = currentMinSpeed + (currentMaxSpeed - currentMinSpeed) / 2;
                //Console.WriteLine("Trying speed: " + pivot);
                var traj = Simulate(robotX, robotVX, angleDegs, pivot);
                i++;
                if (traj.landingX != null)
                {
                    if (traj.landingX < 0)
                    {
                        currentMinSpeed = pivot;
                    }
                    else
                    {
                        currentMaxSpeed = pivot;
                    }
                }
                mostRecentTrajectory = traj;
            }
            if (!mostRecentTrajectory.madeIt)
            {
                mostRecentSuccessfulTrajectory = null;
            }
            else
            {
                mostRecentSuccessfulTrajectory = mostRecentTrajectory;
                Console.WriteLine("Successful speed: " + pivot);
            }
            return mostRecentSuccessfulTrajectory;
        }

        Vector2 findLaunchPos(float robotX, float angleDegs)
        {
            Vector2 shooterPos = new Vector2(-robotX, config.shooterHeight);
            Vector2 ballRelativeToShooter = new Vector2(-(float)Math.Cos(angleDegs * Helpers.deg2rad), (float)Math.Sin(angleDegs * Helpers.deg2rad)) * launchPointR;
            return shooterPos + ballRelativeToShooter;
        }

        void EvaluateTrajectories(List<Trajectory> trajectories)
        {
            float lowestScore = float.MaxValue;
            Trajectory best = null;
            for (int i = 1; i < trajectories.Count; i++)
            {
                var trajectory = trajectories[i];
                if (trajectory == null) continue;
                var trajectory2 = Simulate(trajectory.initX, trajectory.initVX, trajectory.initTheta + config.angleDev, trajectory.initVFly + config.vFlyDev);
                float dx = trajectory2.landingX - trajectory.landingX;
                float robustnessScore = (float)(Math.Pow(dx / config.vFlyDev, 2) + Math.Pow(dx / config.angleDev, 2)) * config.robustnessFactor;

                float heightScore = trajectory.maxHeight * config.heightFactor;

                float totalScore = robustnessScore + heightScore;
                if (totalScore < lowestScore)
                {
                    lowestScore = totalScore;
                    best = trajectory;
                }
            }

            
            bestTrajectories.Add(best);
            
        }

        // using MathNet.Numerics.LinearAlgebra;
        // using MathNet.Numerics.LinearAlgebra.Double;
        // using System.Linq;

        TwoVariablePolynomial3rdDegree FitTwoVariable3rdDegree(
            List<Trajectory> trajectories,
            Func<Trajectory, double> ySelector,
            double ridgeLambda = 0.0)
        {
            int N = trajectories.Count;
            const int M = 10;

            if (N < M)
                Console.WriteLine($"Warning: only {N} samples but {M} coefficients.");

            // ----------- Compute means -----------
            double xMean = trajectories.Average(t => t.initX);
            double yMean = trajectories.Average(t => t.initVX);
            double zMean = trajectories.Average(t => ySelector(t));

            // ----------- Compute RMS scales around mean -----------
            double xScale = Math.Sqrt(trajectories.Average(t =>
                Math.Pow(t.initX - xMean, 2)));

            double yScale = Math.Sqrt(trajectories.Average(t =>
                Math.Pow(t.initVX - yMean, 2)));

            double zScale = Math.Sqrt(trajectories.Average(t =>
                Math.Pow(ySelector(t) - zMean, 2)));

            if (xScale == 0) xScale = 1;
            if (yScale == 0) yScale = 1;
            if (zScale == 0) zScale = 1;

            var A = Matrix<double>.Build.Dense(N, M);
            var b = Vector<double>.Build.Dense(N);

            // ----------- Build normalized design matrix -----------
            for (int i = 0; i < N; i++)
            {
                double x1 = (trajectories[i].initX - xMean) / xScale;
                double x2 = (trajectories[i].initVX - yMean) / yScale;
                double z = (ySelector(trajectories[i]) - zMean) / zScale;

                A[i, 0] = 1.0;
                A[i, 1] = x1;
                A[i, 2] = x2;
                A[i, 3] = x1 * x1;
                A[i, 4] = x1 * x2;
                A[i, 5] = x2 * x2;
                A[i, 6] = x1 * x1 * x1;
                A[i, 7] = x1 * x1 * x2;
                A[i, 8] = x1 * x2 * x2;
                A[i, 9] = x2 * x2 * x2;

                b[i] = z;
            }

            // ----------- Solve via SVD -----------
            var svd = A.Svd(true);
            Console.WriteLine($"Condition estimate: {svd.S[0] / svd.S[^1]:E}");

            Vector<double> coeffs;

            if (ridgeLambda <= 0.0)
            {
                coeffs = svd.Solve(b);
            }
            else
            {
                var AtA = A.TransposeThisAndMultiply(A);
                for (int j = 0; j < M; j++)
                    AtA[j, j] += ridgeLambda;

                var Atb = A.TransposeThisAndMultiply(b);
                coeffs = AtA.Solve(Atb);
            }

            return new TwoVariablePolynomial3rdDegree
            {
                coefficients = coeffs.AsArray(),

                xMean = xMean,
                yMean = yMean,
                zMean = zMean,

                xScale = xScale,
                yScale = yScale,
                zScale = zScale
            };
        }

        // Example usage:
        void GenerateHoodPolynomial(List<Trajectory> trajectories)
        {
            // y = radians(theta)
            hoodPolynomial = FitTwoVariable3rdDegree(trajectories, traj => Helpers.deg2rad * traj.initTheta, ridgeLambda: 0.0);
        }

        void GenerateFlywheelPolynomial(List<Trajectory> trajectories)
        {
            // y = initial flywheel velocity (whatever units you use)
            flywheelPolynomial = FitTwoVariable3rdDegree(trajectories, traj => traj.initVFly, ridgeLambda: 0.0);
        }

        void GenerateTOFPolynomial(List<Trajectory> trajectories)
        {
            tofPolynomial = FitTwoVariable3rdDegree(trajectories, traj => traj.tof, ridgeLambda: 0.0);
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

            public float tof { get; set; }
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

            public double Evaluate(double x, double vx)
            {
                double x1 = (x - xMean) / xScale;
                double x2 = (vx - yMean) / yScale;

                double zNorm =
                    coefficients[0]
                  + coefficients[1] * x1
                  + coefficients[2] * x2
                  + coefficients[3] * x1 * x1
                  + coefficients[4] * x1 * x2
                  + coefficients[5] * x2 * x2
                  + coefficients[6] * x1 * x1 * x1
                  + coefficients[7] * x1 * x1 * x2
                  + coefficients[8] * x1 * x2 * x2
                  + coefficients[9] * x2 * x2 * x2;

                return zNorm * zScale + zMean;
            }

            public double xScale;
            public double yScale;
            public double zScale;

            public double xMean;
            public double yMean;
            public double zMean;
        }
    }
}
