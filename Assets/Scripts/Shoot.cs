using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Shoot : NetworkBehaviour
{

    public float fireRate = 2f;
    public float weaponRange = 50f;
    public float hitForce = 100f;
    public Transform stickEnd;
    
    [SerializeField]
    private Camera fpsCam;
    private float shotDuration = 1f;
    private AudioSource gunAudio;
    [SerializeField]
    private GameObject laserPrefab;
    private LineRenderer laserLine;
    private bool canShoot = true;

    private int groundLayerMask;

    [SerializeField]
    private Animator stickAnimator;

    // Start is called before the first frame update
    void Start()
    {
        //laserLine = GetComponent<LineRenderer>();
        gunAudio = GetComponent<AudioSource>();
        //fpsCam = GetComponentInParent<Camera>();
        groundLayerMask = LayerMask.GetMask("Ground");
        //stickAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1") && canShoot && IsOwner)
        {
            ShootServerRpc(stickEnd.position, fpsCam.transform.forward, OwnerClientId);
        }
    }

    [ClientRpc]
    private void ShootClientRpc(Vector3 laserStartPosition, Vector3 laserDirection, ulong[] playersHitId)
    {
        //Show laser on clients
        canShoot = false;

        laserLine = Instantiate(laserPrefab, transform.position, laserPrefab.transform.rotation).GetComponent<LineRenderer>();

        if (stickAnimator)
            stickAnimator.SetTrigger("reload");

        StartCoroutine(Reload(fireRate));
        ShotEffect(shotDuration);

        Vector3 rayOrigin = laserStartPosition;//fpsCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));

        laserLine.SetPosition(0, laserStartPosition);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, laserDirection, out hit, weaponRange, groundLayerMask))
        {
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            laserLine.SetPosition(1, rayOrigin + (laserDirection * weaponRange));
        }
        //Kill hit players
        foreach (ulong playerId in playersHitId)
        {
            //Find playerController with clientId
            PlayerController playerController = FindPlayerController(playerId);
            if (playerController)
            {
                playerController.GetComponent<Shoot>().canShoot = true;
                playerController.Kill();
            }
                
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 laserStartPosition, Vector3 laserDirection, ulong clientId)
    {
        //Find players to kill and laser to draw
        Vector3 maxPoint;

        RaycastHit hit;
        if (Physics.Raycast(laserStartPosition, laserDirection, out hit, weaponRange, groundLayerMask))
        {
            maxPoint = hit.point;
        }
        else
        {
            maxPoint = laserStartPosition + (laserDirection * weaponRange);
        }


        float rangeToKill = (maxPoint - laserStartPosition).magnitude;

        RaycastHit[] playerHits = Physics.RaycastAll(laserStartPosition, laserDirection, rangeToKill);
        List<ulong> finalPlayersHitId = new List<ulong>();
        foreach (RaycastHit playerHit in playerHits)
        {
            PlayerController player = playerHit.collider.GetComponent<PlayerController>();
            if (player != null && clientId != player.ownerId)
            {
                finalPlayersHitId.Add(player.ownerId);
            }
        }
        //Execute on each client the laser spawn and the player kills
        ShootClientRpc(laserStartPosition, laserDirection, finalPlayersHitId.ToArray());
    }

    private void ShotEffect(float duration)
    {
        //gunAudio.Play();
        //randomColor
        laserLine.material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        laserLine.enabled = true;
        //laserLine.enabled = false;
        Destroy(laserLine.gameObject, duration);
    }

    private IEnumerator Reload(float duration)
    {
        yield return new WaitForSeconds(duration);
        canShoot = true;
    }

    private PlayerController FindPlayerController(ulong clientId)
    {
        foreach (PlayerController playerController in FindObjectsOfType<PlayerController>())
        {
            if (playerController.ownerId == clientId)
            {
                return playerController;
            }
        }
        return null;
    }

}
