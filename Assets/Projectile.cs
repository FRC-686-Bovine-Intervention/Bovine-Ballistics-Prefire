using UnityEngine;
using static Launch;

public class Projectile : MonoBehaviour
{
    public Transform Target;

    public float launchAngle;
    public float launchSpeed;
    private bool landed = false;
    private bool madeIt = false;

    public float dragCoefficient = 0.47f;
    public float mass = 0.226f;

    public float p = 1.225f;
    public float crosssection;
    //public Vector2 init_v;
    public bool launched = false;
    public Vector2 v;
    public Vector2 a;
    public Vector2 f;

    void Start()
    {
        Target = GameObject.FindGameObjectWithTag("Finish").transform;
        float r = transform.localScale.x / 2;
        crosssection = Mathf.PI * r * r;
        a = Vector2.zero;
        f = Vector2.zero;
    }

    //void FixedUpdate()
    //{
    //    f = 9.81f * new Vector2(0, -1) * mass;
    //    if (launched)
    //    {
    //        float fd = 0.5f * p * v.magnitude * v.magnitude * dragCoefficient * crosssection;
    //        f += -fd * v.normalized;
    //        a = f / mass;
    //        v = v + a * Time.fixedDeltaTime;
    //        transform.position = new Vector3(
    //            transform.position.x + v.x * Time.fixedDeltaTime,
    //            transform.position.y + v.y * Time.fixedDeltaTime,
    //            transform.position.z
    //        );
    //    }
    //}

    public Traj Launch(Vector2 init_v)
    {
        this.v = init_v;
        this.launchSpeed = init_v.magnitude;
        this.launchAngle = Mathf.Atan2(init_v.y, init_v.x) * Mathf.Rad2Deg;
        this.launched = true;

        while (!landed)
        {
            f = 9.81f * new Vector2(0, -1) * mass;
            float fd = 0.5f * p * v.magnitude * v.magnitude * dragCoefficient * crosssection;
            f += -fd * v.normalized;
            a = f / mass;
            v = v + a * Time.fixedDeltaTime;
            transform.position = new Vector3(
                transform.position.x + v.x * Time.fixedDeltaTime,
                transform.position.y + v.y * Time.fixedDeltaTime,
                transform.position.z
            );
        }

        return new Traj
        {
            launchAngle = this.launchAngle,
            launchSpeed = this.launchSpeed,
            xError = transform.position.x - Target.position.x,
            madeIt = this.madeIt
        };
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Kill")
        {
            this.landed = true;
            this.madeIt = false;
        }
        else if (collision.gameObject.tag == "Respawn")
        {
            this.madeIt = true;
        }
        else if (collision.gameObject.tag == "Finish" && this.madeIt)
        {
            this.landed = true;
        }
    }

    public void ResetProjectile(Transform transform)
    {
        this.transform.position = transform.position;
        this.launched = false;
        this.v = Vector2.zero;
    }
}
