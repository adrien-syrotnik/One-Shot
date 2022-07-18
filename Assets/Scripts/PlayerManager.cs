using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public List<Transform> spawnPoints = new List<Transform>();

    private int indexSpawn = 0;

    static public PlayerManager Instance;

    [SerializeField]
    private GameObject UIMenu;
    [SerializeField]
    private Camera spectatorCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnPlayer(Transform player)
    {
        GameManager.Instance._players.Add(player.GetComponent<PlayerController>());
        spectatorCamera.gameObject.SetActive(false);
        UIMenu.SetActive(false);
        if (spawnPoints.Count == 0)
            return;

        if (indexSpawn >= spawnPoints.Count)
            indexSpawn = 0;

        player.position = spawnPoints[indexSpawn].position;
        player.rotation = spawnPoints[indexSpawn].rotation;
        indexSpawn++;
    }
}
