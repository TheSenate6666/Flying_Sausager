using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float speed = 5f;

    private Transform target;
    private int wavepointIndex = 0;

    void Start()
    {
        target = waypoints.points[0];
    }

    void Update() 
    {
        Vector3 dir = target.position - transform.position;
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);

        if (Vector3.Distance(transform.position, target.position) <= 0.4f)
        {
            GetNextWaypoint();
        }
    }

    void GetNextWaypoint()
    {
        if (wavepointIndex >= waypoints.points.Length - 1)
        {
            speed = 0;
            return;
        }

        wavepointIndex++;
        target = waypoints.points[wavepointIndex];
    }

    public void StopMovement()
    {
        speed = 0;
    }
}
