using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using System.Linq;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using TMPro;

public class NetworkMenu : MonoBehaviour
{
    const int m_MaxConnections = 8;

    public TMP_InputField RelayJoinCode;
    public TMP_Text RelayJoinCodeHost;

    public string playerID = "";


    // Start is called before the first frame update
    void Start()
    {
        AuthenticatingAPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Join()
    {
        if(playerID != "" && RelayJoinCode.text != "")
        {
            StartCoroutine(ConfigreTransportAndStartNgoAsConnectingPlayer());
        }
    }
    public void Host()
    {
        if (playerID != "")
        {
            StartCoroutine(ConfigureTransportAndStartNgoAsHost());
        }
    }

    async void AuthenticatingAPlayer()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            playerID = AuthenticationService.Instance.PlayerId;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public static async Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] key, string joinCode)> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
    {
        Allocation allocation;
        string createJoinCode;
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed {e.Message}");
            throw;
        }

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            createJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.Key, createJoinCode);
    }

    IEnumerator ConfigureTransportAndStartNgoAsHost()
    {
        var serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(m_MaxConnections);
        while (!serverRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }
        if (serverRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
            yield break;
        }

        var (ipv4address, port, allocationIdBytes, connectionData, key, joinCode) = serverRelayUtilityTask.Result;

        Debug.Log($"JoinCode: {joinCode}");
        RelayJoinCodeHost.text = $"Code : {joinCode}";
        // Display the join code to the user.

        // The .GetComponent method returns a UTP NetworkDriver (or a proxy to it)
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ipv4address, port, allocationIdBytes, key, connectionData, true);
        NetworkManager.Singleton.StartHost();
        yield return null;
    }

    public static async Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] hostConnectionData, byte[] key)> JoinRelayServerFromJoinCode(string joinCode)
    {
        JoinAllocation allocation;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.HostConnectionData, allocation.Key);
    }



    IEnumerator ConfigreTransportAndStartNgoAsConnectingPlayer()
    {
        // Populate RelayJoinCode beforehand through the UI
        var clientRelayUtilityTask = JoinRelayServerFromJoinCode(RelayJoinCode.text);

        while (!clientRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }

        if (clientRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception: " + clientRelayUtilityTask.Exception.Message);
            yield break;
        }

        var (ipv4address, port, allocationIdBytes, connectionData, hostConnectionData, key) = clientRelayUtilityTask.Result;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ipv4address, port, allocationIdBytes, key, connectionData, hostConnectionData, true);

        NetworkManager.Singleton.StartClient();
        yield return null;
    }
}
