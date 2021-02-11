using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Camera related vars
    Camera playerCam;
    Vector3 camLocalResetPos;
    bool freeCam = false;
    [SerializeField]
    float camRotationSpeed = 1.0f;
    [SerializeField]
    KeyCode toggleFreeCam = KeyCode.Space;
    [SerializeField]
    private bool invertYRotation = true;

    // Start is called before the first frame update
    void Start()
    {
        InitialiseVariables();
    }

    private void InitialiseVariables()
    {
        playerCam = GetComponentInChildren<Camera>();
        camLocalResetPos = playerCam.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggleFreeCam))
        {
            freeCam = !freeCam;
            print(freeCam);
        }

        RotateCamera();
        // MovePlayer();

    }

    private void MovePlayer()
    {
        throw new NotImplementedException();
    }

    private void RotateCamera()
    {
        if (freeCam)
        {
            float newXRot = playerCam.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * camRotationSpeed;

            float newYRot;
            if (invertYRotation)
            {
                newYRot = playerCam.transform.localEulerAngles.x + Input.GetAxis("Mouse Y") * camRotationSpeed;
            }
            else
            {
                newYRot = playerCam.transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * camRotationSpeed;
            }

            playerCam.transform.localEulerAngles = new Vector3(newYRot, newXRot, 0f);
        }
    }
}
