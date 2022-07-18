using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{

    public List<PlayerController> _players = new List<PlayerController>();

    static public GameManager Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private int nbPlayerBefore = 0;

    // Update is called once per frame
    void Update()
    {
        
        if(IsServer)
        {
            int nbPlayer = GameObject.FindObjectsOfType<PlayerController>().Length;

            if (nbPlayer == 1 && nbPlayerBefore == 2 && isReseting == false)
            {
                //It means that 1 player left after 2 fights

                //reset game
                isReseting = true;
                time = timeToWait;
                StartCoroutine(waitCount());
            }


            nbPlayerBefore = nbPlayer;
        }
    }

    private int timeToWait = 5;
    private int time = 5;
    private bool isReseting = false;

    private void ResetGame()
    {
        isReseting = false;
        requestRespawnServerRpc();
    }

    [ServerRpc]
    private void requestRespawnServerRpc()
    {
        respawnAllPlayerClientRpc();
    }

    [ClientRpc]
    private void respawnAllPlayerClientRpc()
    {
        foreach (var player in _players)
        {
            player.gameObject.SetActive(true);
        }
        foreach (var playerSpec in GameObject.FindObjectsOfType<FollowPlayer>())
        {
            if(IsServer)
                Destroy(playerSpec.gameObject);
        }
    }

    private IEnumerator waitCount()
    {
        while (time > 0)
        {
            time--;
            //Draw time
            Debug.Log(time);
            yield return new WaitForSeconds(1);
        }
        ResetGame();
    }

    
}
