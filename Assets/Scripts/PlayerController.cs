using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    private readonly NetworkVariable<ulong> _ownerId = new(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> _isSliding = new(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> _speedValue = new(writePerm: NetworkVariableWritePermission.Owner);

    public ulong ownerId;
    private bool isSliding;
    private float speedValue;

    [SerializeField]
    private float speed = 1f;

    public float groundDrag;



    [Header("Grounded")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    KeyCode jumpKey = KeyCode.Space;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public int jumpAvailable = 2;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    private CharacterController _controller;

    [SerializeField]
    private float gravityForce = 9.81f;

    private float vSpeed = 0;

    private Animator _animator;
    private Animator _hitboxAnimator;

    [SerializeField]
    private GameObject playerSpecPrefab;


    private void Awake()
    {
        PlayerManager.Instance.SpawnPlayer(transform);

    }

    /*public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) Destroy(this);
    }*/

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _hitboxAnimator = GetComponent<Animator>();
        _animator = transform.GetChild(0).GetComponent<Animator>();
    }


    private void Update()
    {
        if (IsOwner)
        {
            ownerId = NetworkManager.Singleton.LocalClientId;
            _ownerId.Value = ownerId;
        }
        else
        {
            ownerId = _ownerId.Value;
            isSliding = _isSliding.Value;
            speedValue = _speedValue.Value;
        }

        if (IsOwner)
        {
            Move();
            _isSliding.Value = isSliding;
            _speedValue.Value = speedValue;
        }

        _animator.SetFloat("speed", speedValue);
        _hitboxAnimator.SetBool("isSliding", isSliding);
        _animator.SetBool("isSliding", isSliding);

    }

    private float slideSpeedMax = 1f;
    private float slideSpeedDecreased = 5f;
    private float slideSpeed = 0;
    private Vector3 slideDirection;


    private void Move()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        isSliding = Input.GetKey(KeyCode.LeftControl);


        moveDirection = transform.forward * verticalInput + horizontalInput * transform.right;

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            //Begin to slide
            slideDirection = moveDirection;
            slideSpeed = slideSpeedMax;
        }

        if (isSliding)
        {
            moveDirection = slideDirection;
            slideSpeed -= slideSpeedDecreased * Time.deltaTime;

        }
        else
        {
            slideSpeed = 0;
        }

        float speedUse = (isRunning ? speed * 1.5f : speed) + slideSpeed;

        if (speedUse < 0)
        {
            speedUse = 0;
        }

        moveDirection *= speedUse;

        if (grounded)
        {
            jumpAvailable = 2;
            vSpeed = 0;
        }

        if (Input.GetKeyDown(jumpKey) && jumpAvailable > 0)
        {
            jumpAvailable--;
            vSpeed = jumpForce;
            _animator.SetTrigger("jump");
            //readyToJump = false;
            //StartCoroutine(JumpCooldown());
        }

        vSpeed -= gravityForce * Time.deltaTime;

        moveDirection.y = vSpeed;

        speedValue = moveDirection.magnitude;
        _animator.SetBool("isGrounded", grounded);
        
        _controller.Move(moveDirection * Time.deltaTime);
        

        grounded = _controller.isGrounded;
    }


    public void Kill()
    {
        if (IsOwner)
        {
            //MP_CreatePlayerServerRpc(OwnerClientId);
            GameObject newPlayer = Instantiate(playerSpecPrefab);
            NetworkObject netObj = newPlayer.GetComponent<NetworkObject>();
            newPlayer.SetActive(true);
            //netObj.SpawnAsPlayerObject(OwnerClientId, true);
        }

        gameObject.SetActive(false);
        
    }

    [ServerRpc(RequireOwnership = false)] //server owns this object but client can request a spawn
    public void MP_CreatePlayerServerRpc(ulong clientId)
    {
        GameObject newPlayer = Instantiate(playerSpecPrefab);
        NetworkObject netObj = newPlayer.GetComponent<NetworkObject>();
        newPlayer.SetActive(true);
        netObj.SpawnAsPlayerObject(clientId, true);
    }

}
