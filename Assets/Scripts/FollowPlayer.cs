using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class FollowPlayer : NetworkBehaviour
{
    [SerializeField]
    private float sensX = 400f;
    [SerializeField]
    private float sensY = 400f;

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
    }

    void Update()
    {
        Rotate();
        TeleportToTheNearestPlayer();
    }

    private void Rotate()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * Time.deltaTime;

        xRotation -= mouseY;
        yRotation += mouseX;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    private void TeleportToTheNearestPlayer()
    {
        var players = GameObject.FindObjectsOfType<PlayerController>();
        if (players[0])
        {
            transform.position = players[0].transform.position;
        }

    }
}
