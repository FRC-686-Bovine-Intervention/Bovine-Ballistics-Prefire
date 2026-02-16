using UnityEngine;

public class FuelManager : MonoBehaviour
{
    public Rigidbody2D rb;

    public float dragCoefficient = 0.47f;
    public float airDensity = 1.225f;

    float area;

    public Vector2 initV;
    public Vector2 start;

    public Vector2 end;
    public float maxHeight;
    public bool madeIt = false;
    public bool dead = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var launcher = GameObject.FindGameObjectsWithTag("Launcher")[0].GetComponent<Launcher>();
        transform.position = launcher.getInitPos();
        rb.linearVelocity = launcher.getInitVel();
        initV = launcher.getInitV();
        start = launcher.getInitStart();
        float r = transform.localScale.x * 0.5f;
        area = Mathf.PI * r * r;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;

        float speed = v.magnitude;

        if (speed > 0f)
        {
            Vector2 dragForce =
                -0.5f * airDensity * speed * speed * dragCoefficient * area * v.normalized;

            rb.AddForce(dragForce);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Respawn"))
        {
            madeIt = true;
        }
        else if (collision.gameObject.CompareTag("Finish")
        {
            dead = true;
            end = transform.position;
        }
        else if (collision.gameObject.CompareTag("Kill")
        {
            dead = true;
            madeIt = false;
            end = transform.position;
        }
    }
}
