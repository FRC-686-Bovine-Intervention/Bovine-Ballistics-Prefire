using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public bool hasStarted = false;
    public List<Trajectory> allTrajectories;
    public List<Trajectory> validTrajectories;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !hasStarted)
        {
            hasStarted = true;
        }
        

    }

    public class Trajectory
    {
        public float initX;
        public float initVX;
        public float initTheta;
        public float initSpeed;

        public bool madeIt;
        public float maxHeight;
    }
}
