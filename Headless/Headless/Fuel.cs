using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using System;

namespace Headless
{
    internal class Fuel
    {
        // Physical constants
        public float r = 0.150114f;
        public float dragCoefficient = 0.47f;
        public float airDensity = 1.225f;
        public float g = 9.81f;

        float area;
        float mass = 0.226f;

        // Kinematic state
        public Vector2 p;
        public Vector2 v;
        public Vector2 initV;
        public Vector2 initPos;
        public Vector2 end;
        public float maxHeight;

        Ball fuel;

        public bool simulating = false;
        public bool madeIt = false;
        public bool dead = false;

        public void init()
        {
            fuel = new Ball(r);
            area = Mathf.PI * r * r;
        }

        public void Update(float deltaTime)
        {
            if (!simulating) return;
            fuel.previousPosition = p;

            if (p.Y > maxHeight)
                maxHeight = p.Y;

            Vector2 totalForces = new Vector2(0, -g * mass);

            float speed = Helpers.norm(v);
            if (speed > 0f)
            {
                Vector2 dragForce = -0.5f * airDensity * speed * speed * dragCoefficient * area * Helpers.normalize(v);
                totalForces += dragForce;
            }

            Vector2 a = totalForces / mass;

            v += a * deltaTime;
            p += v * deltaTime;

            fuel.position = p;

            if (fuel.HasHitAnyWall(Wall.allKillWalls, true))
            {
                dead = true;
                madeIt = false;
                end = fuel.position;
            }
            else if (fuel.HasHitWall(Wall.hubTop))
            {
                madeIt = true;
            }
            else if (fuel.HasHitWall(Wall.hubBottom))
            {
                dead = true;
                end = fuel.position;
            }

            //Console.WriteLine("Success: " + madeIt);
            //Console.WriteLine("Dead: " + dead);
            //Console.WriteLine("Position: " + fuel.position);
        }

        public void Launch(Vector2 pos, Vector2 vel)
        {
            p = pos;
            this.initPos = pos;
            this.v = vel;
            this.initV = vel;
            maxHeight = pos.Y;

            simulating = true;
            madeIt = false;
            dead = false;
            end = pos;
        }
    }
}