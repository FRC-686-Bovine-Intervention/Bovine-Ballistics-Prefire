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
    public bool madeIt;
    public bool dead;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = start;
        rb.linearVelocity = initV;
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
}
