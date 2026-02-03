using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launch : MonoBehaviour
{
    //public List<Projectile> projectiles;
    //public List<bool> madeIt;
    public float minSpeed = 0f;
    public float maxSpeed = 30f;
    private float currentMinSpeed;
    private float currentMaxSpeed;
    private float startingPivot;

    public Transform launchPoint;
    public GameObject projectilePrefab;
    public List<Traj> trajs;

    private void Start()
    {
        currentMinSpeed = minSpeed;
        currentMaxSpeed = maxSpeed;
        startingPivot = (minSpeed + maxSpeed) / 2;
    }

    public void ChangeAngle(float angle)
    {
        transform.eulerAngles = new Vector3(0, 0, -angle);
    }
    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        StartCoroutine(testFunc());
    //    }
    //}

    //private void Start()
    //{
    //    float pivot = speed;
    //    while (!madeIt.Contains(true))
    //    {
    //        projectiles.Add(LaunchProjectile(pivot, 45f));

    //    }
    //}
    //IEnumerator testFunc()
    //{
    //    float pivot = startingPivot;
    //    Projectile proj = Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity).GetComponent<Projectile>();
    //    bool withinTolerance = false;
    //    while (!withinTolerance)
    //    {
    //        Traj result = RelaunchProjectile(proj, pivot, 25f);
    //        withinTolerance = true;
    //        //if (result.madeIt)
    //        //{
    //        //    withinTolerance = true;
    //        //} else
    //        //{
    //        //    if (result.xError > 0)
    //        //    {
    //        //        currentMaxSpeed = pivot;
    //        //    }
    //        //    else
    //        //    {
    //        //        currentMinSpeed = pivot;
    //        //    }
    //        //    pivot = (currentMinSpeed + currentMaxSpeed) / 2;
    //        //}
    //    }
    //    yield return null;
    //}
    //public Projectile LaunchProjectile(float mps, float degs)
    //    {
    //        transform.eulerAngles = new Vector3(0, 0, -degs);

    //        float rad = degs * Mathf.Deg2Rad;

    //        Vector2 velocity = new Vector2(
    //            Mathf.Sin(rad),
    //            Mathf.Cos(rad)
    //        ) * mps / 2;

    //        GameObject projectile =
    //            Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity);

    //        Projectile projScript = projectile.GetComponent<Projectile>();
    //        projScript.Launch(velocity);
    //        return projScript;
    //    }

    //    public Traj RelaunchProjectile(Projectile proj, float mps, float degs)
    //    {
    //        proj.ResetProjectile(launchPoint);
    //        transform.eulerAngles = new Vector3(0, 0, -degs);
    //        float rad = degs * Mathf.Deg2Rad;
    //        Vector2 velocity = new Vector2(
    //            Mathf.Sin(rad),
    //            Mathf.Cos(rad)
    //        ) * mps / 2;
    //        return proj.Launch(velocity);
    //    }

    //    //public void setMadeIt(int index, bool val)
    //    //{
    //    //    madeIt[index] = val;
    //    //}

    public class Traj
    {
        public float launchAngle;
        public float launchSpeed;
        public float xError;
        public bool madeIt;
    }

}