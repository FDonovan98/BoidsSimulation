using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Camera Options")]
    Camera playerCam;
    Vector3 camLocalResetPos;
    private Quaternion camLocalResetRot;
    bool freeCam = false;
    [SerializeField]
    float camRotationSpeed = 1.0f;
    [SerializeField]
    KeyCode toggleFreeCam = KeyCode.Tab;
    [SerializeField]
    private bool invertYRotation = true;

    [Header("Movement Options")]
    // Movement keys
    [SerializeField]
    private KeyCode forwardKey = KeyCode.W;
    [SerializeField]
    private KeyCode backwardKey = KeyCode.S;
    [SerializeField]
    private KeyCode rightKey = KeyCode.D;
    [SerializeField]
    private KeyCode leftKey = KeyCode.A;
    [SerializeField]
    private KeyCode upKey = KeyCode.Space;
    [SerializeField]
    private KeyCode downKey = KeyCode.LeftControl;

    // Free camera movement
    private Rigidbody cameraRb;
    [SerializeField]
    private float freeCamAcceleration = 1.0f;
    [SerializeField]
    private float freeCamVelCap = 10.0f;

    // Player movement
    private Rigidbody playerRb;
    [SerializeField]
    private float playerAcceleration = 1.0f;
    [SerializeField]
    private float playerVelCap = 10.0f;


    // Start is called before the first frame update
    void Start()
    {
        InitialiseVariables();
    }

    private void InitialiseVariables()
    {
        playerCam = GetComponentInChildren<Camera>();
        camLocalResetPos = playerCam.transform.localPosition;
        camLocalResetRot = playerCam.transform.localRotation;
        cameraRb = playerCam.GetComponent<Rigidbody>();

        playerRb = this.gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggleFreeCam))
        {
            freeCam = !freeCam;
            cameraRb.isKinematic = !cameraRb.isKinematic;

            if (!freeCam)
            {
                playerCam.transform.localPosition = camLocalResetPos;
                playerCam.transform.localRotation = camLocalResetRot;
            }
        }

        RotateCamera();

        if (freeCam)
        {
            MovePlayer(cameraRb, freeCamAcceleration, freeCamVelCap);
        }
        else
        {
            MovePlayer(playerRb, playerAcceleration, playerVelCap);
        }

    }

    private void MovePlayer(Rigidbody rb, float accelaration, float velCap)
    {
        if (Input.GetKey(forwardKey))
        {
            rb.velocity += rb.transform.forward * accelaration * Time.deltaTime;
        }
        if (Input.GetKey(backwardKey))
        {
            rb.velocity += rb.transform.forward * -accelaration * Time.deltaTime;
        }
        if (Input.GetKey(rightKey))
        {
            rb.velocity += rb.transform.right * accelaration * Time.deltaTime;
        }
        if (Input.GetKey(leftKey))
        {
            rb.velocity += rb.transform.right * -accelaration * Time.deltaTime;
        }

        if (rb == cameraRb)
        {
            if (Input.GetKey(upKey))
            {
                rb.velocity += Vector3.up * accelaration * Time.deltaTime;
            }
            if (Input.GetKey(downKey))
            {
                rb.velocity += Vector3.up * -accelaration * Time.deltaTime;
            }
        }

        CapVelocity(rb, velCap);
    }

    private void CapVelocity(Rigidbody rb, float velCap)
    {
        Vector3 velDir = rb.velocity.normalized;
        float newVelMag = Mathf.Clamp(rb.velocity.magnitude, -velCap, velCap);

        rb.velocity = newVelMag * velDir;
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
        else
        {
            playerCam.transform.RotateAround(transform.position, transform.right, Input.GetAxis("Mouse Y") * camRotationSpeed);

            float newXRot = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * camRotationSpeed;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, newXRot, transform.localEulerAngles.z);
        }
    }
}
