using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CameraController : NetworkBehaviour
{
    [SerializeField]
    private float sensX = 5f;
    [SerializeField]
    private float sensY = 5f;

    public Transform orientation;

    float xRotation;
    float yRotation;
    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsOwner)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //Camera.cullingMask
    }

    void Update()
    {
        Rotate();
    }

    private void Rotate()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * Time.deltaTime;

        xRotation -= mouseY;
        yRotation += mouseX;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        if(orientation)
            orientation.rotation = Quaternion.Euler(0, yRotation, 0f);
    }
}
