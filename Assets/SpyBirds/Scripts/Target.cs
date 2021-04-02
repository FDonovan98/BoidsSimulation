using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Vector3 lastPos;
    // Start is called before the first frame update
    private void Awake()
    {
        lastPos = transform.position;
    }

    private void Update()
    {
        lastPos = transform.position;
    }
}
