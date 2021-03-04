using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSTracker : MonoBehaviour
{
    Text text;
    int frameCount = 0;
    float deltaTime = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += Time.deltaTime;
        frameCount++;
        if (frameCount > 10)
        {
            text.text = (20.0f / deltaTime).ToString();
            frameCount = 0;
            deltaTime = 0.0f;
        }
    }
}
