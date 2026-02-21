using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;

public class FuelManager : MonoBehaviour
{
    public float dragCoefficient = 0.47f;
    public float airDensity = 1.225f;

    public float g = 9.81f;

    float area;
    float mass = 0.226f;

    public Vector2 v;
    public Vector2 p;
    public Vector2 initV;
    public Vector2 initPos;

    public Vector2 end;
    public float maxHeight;
    public bool simulating = false;
    public bool madeIt = false;
    public bool dead = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float r = transform.localScale.x * 0.5f;
        area = Mathf.PI * r * r;
    }

    private void FixedUpdate()
    {
        if (simulating)
        {
            this.p = transform.position;
            if (p.y > maxHeight)
            {
                maxHeight = p.y;
            }

            Vector2 totalForces = new Vector2(0, -g * mass);

            float speed = v.magnitude;

            if (speed > 0f)
            {
                Vector2 dragForce =
                    -0.5f * airDensity * speed * speed * dragCoefficient * area * v.normalized;

                totalForces += dragForce;
            }

            Vector2 a = totalForces / mass;

            this.v += a * Time.fixedDeltaTime;
            transform.position += new Vector3(v.x * Time.fixedDeltaTime, v.y * Time.fixedDeltaTime);
        }
    }

    public void Launch(Vector2 pos, Vector2 vel)
    {
        this.p = pos;
        this.initPos = pos;
        transform.position = pos;
        this.v = vel;
        this.initV = vel;

        maxHeight = p.y;

        simulating = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collided");
        if (collision.gameObject.CompareTag("Respawn"))
        {
            madeIt = true;
        }
        else if (collision.gameObject.CompareTag("Finish"))
        {
            dead = true;
            end = transform.position;
        }
        else if (collision.gameObject.CompareTag("Kill"))
        {
            dead = true;
            madeIt = false;
            end = transform.position;
        }
    }
}
