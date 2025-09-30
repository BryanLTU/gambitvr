using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class QuickJoin : MonoBehaviour
{
    [SerializeField] UnityTransport transport;


    async void Awake()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void Host()
    {
        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(2);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        await LobbyService.Instance.CreateLobbyAsync(
            "VRChess2P", 2,
            new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new() { { "code", new Unity.Services.Lobbies.Models.DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
            }
        );

        var relayData = AllocationUtils.ToRelayServerData(alloc, "dtls");
        transport.SetRelayServerData(relayData);

        NetworkManager.Singleton.StartHost();
        Debug.Log($"Join code: {joinCode}");
    }

    public async void Join(string code)
    {
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(code);
        var relayData = AllocationUtils.ToRelayServerData(joinAlloc, "dtls");
        transport.SetRelayServerData(relayData);

        NetworkManager.Singleton.StartClient();
    }
}
