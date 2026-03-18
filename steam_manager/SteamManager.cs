using Godot;
using System;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public partial class SteamManager : Node
{
	public static SteamManager Manager {get; set;}
	private static uint GameAppId {get; set;} = 2833140;
	public string PlayerName {get; set;}
	public SteamId PlayerSteamId{get; set;}

	public bool Connected {get; set;}

	public bool SteamRunningInOnlineMode {get; set;}
	private Lobby CurrentLobby {get; set;}
	private List<Lobby> AvailableLobbies {get; set;} = new List<Lobby>();

	public static event Action<List<Lobby>> OnAvailableLobbiesRefreshCompleted;
	public static event Action<Friend> OnPlayerJoinedLobby;
	public static event Action<Friend> OnPlayerLeftLobby;

	public SteamConnectionManager SteamConnectionManager;

	public SteamSocketManager SteamSocketManager;

	public bool IsHost;

	public SteamManager(){
		if (Manager == null){
			Manager = this;
			try{
				SteamClient.Init(GameAppId, true);
				if (!SteamClient.IsValid){
					GD.Print("SteamClient is not valid ):");
					throw new Exception();
				}

				PlayerName = SteamClient.Name;
				PlayerSteamId = SteamClient.SteamId;
				SteamUserStats.RequestCurrentStats();
				// Running in offline mode will still be "connected"
				if (SteamClient.State != FriendState.Offline){
					this.SteamRunningInOnlineMode = true;
				}

				this.Connected = true;

				
				GD.Print("Steam is connected, playerName: " + PlayerName + " userid: " + PlayerSteamId.AccountId.ToString());
			}
			catch (System.Exception e){
				Connected = false;
				GD.Print("Error connecting to steam: " + e.Message);
			}
		}
	}

	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
		SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
		SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
		SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
		SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
		SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
		SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequestedCallback;
	}
	
	private void OnLobbyMemberDisconnectedCallback(Lobby lobby, Friend friend){
		GD.Print("ML: User has disconnected to the lobby: " + friend.Name);
		OnPlayerLeftLobby(friend);
	}

	private void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend){
		GD.Print("ML: User has left the lobby: " + friend.Name);
		OnPlayerLeftLobby(friend);
	}

	private void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend){
		GD.Print("ML: User has joined the lobby: " + friend.Name);
		OnPlayerJoinedLobby(friend);
	}

	private void OnLobbyCreatedCallback(Result result, Lobby lobby){
		if (result != Result.OK){
			GD.Print("ML: lobby create wasn't ok..");
			return;
		}

		GD.Print($"ML: created lobby! = {lobby.Id}");
		CreateSteamSocketServer();
	}

	private void OnLobbyEnteredCallback(Lobby lobby){
		// This fires when you specifically join a lobby
		if (lobby.Owner.Id == SteamManager.Manager.PlayerSteamId)
		{
			GD.Print("ML: You have joined your own lobby");
		}
		else
		{
			GD.Print($"ML: you joined {lobby.Owner.Name}'s lobby");
		}

		CurrentLobby = lobby;

		foreach(var friend in lobby.Members){
			OnPlayerJoinedLobby(friend);
		}

		JoinSteamSocketServer(lobby.Owner.Id);
	}

	private void OnLobbyGameCreatedCallback(Lobby lobby, uint id, ushort port, SteamId steamId){
		// TODO load game scene here?
		GD.Print($"ML: lobby game created callback! = {lobby.Id} - {id}");
	}

	private async void OnGameLobbyJoinRequestedCallback(Lobby lobby, SteamId id){
		GetTree().ChangeSceneToFile("res://scenes/main_menu/LobbySceneManager.tscn");
		RoomEnter joinSuccessful = await lobby.Join();
		if (joinSuccessful != RoomEnter.Success){
			GD.Print("Failed to join lobby");
			return;
		}

		CurrentLobby = lobby;
	}

	public void OpenFriendOverlayForInvite(){
		GD.Print("SM: opening overlay: " + CurrentLobby.Id.ToString());
		SteamFriends.OpenGameInviteOverlay(CurrentLobby.Id);
	}

	
	public async Task<bool> CreateLobby(bool Multiplayer=true){
		if (!this.Connected || !this.SteamRunningInOnlineMode){
			GD.Print("Not connected to steam, no lobby created for offline game");
			this.IsHost = true;
			return true;
		}

		try{
			GD.Print("SM: Creating lobby");
			int MaxLobbyMembers = Multiplayer ? 10 : 1;
			Lobby? createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(MaxLobbyMembers);
			if (!createLobbyOutput.HasValue){
				GD.Print("SM: Lobby created, but no instance");
				throw new Exception();
			}

			CurrentLobby = createLobbyOutput.Value;
			CurrentLobby.SetData("ownerNameString", PlayerName);
			// could have multiple calls to SetData for arbitrary values. see LobbyList.WithKeyValue

			if (!Multiplayer){
				CurrentLobby.SetPrivate();
				CurrentLobby.SetJoinable(false);
			}else{
				CurrentLobby.SetPublic();
				CurrentLobby.SetJoinable(true);
			}

			GD.Print("SM: Lobby created!");
			this.IsHost = true;
			return true;

		}catch (System.Exception e){
			GD.Print("SM: Failed to create lobby" + e.Message);
			return false;
		}
	}

	public async Task<bool> SearchLobbies(){
		AvailableLobbies.Clear();
		try{
			Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithMaxResults(10).RequestAsync();
			if (lobbies != null){
				foreach (var i in lobbies){
					GD.Print("SM: lobby: " + i.Id);
					AvailableLobbies.Add(i);
				}
			}

			OnAvailableLobbiesRefreshCompleted.Invoke(AvailableLobbies);

			return true;

		}catch(System.Exception e){
			GD.Print("SM: Search failed: " + e.Message);
			return false;
		}
	}

	public void LeaveCurrentLobby(){
		if (!this.Connected){
			return;
		}

		CurrentLobby.Leave();
	}

	public void BroadcastOrSend(string message){
		if (!this.SteamRunningInOnlineMode){
			return;
		}

		if(IsHost){
			this.Broadcast(message);
		}else{
			this.SteamConnectionManager.Connection.SendMessage(message);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		SteamClient.RunCallbacks();

		try{
			// If you create the socket, there will be stuff to receive from the socket manager ("server")
			if(SteamSocketManager != null){
				this.SteamSocketManager.Receive();
			}
			// if you joined the socket, there will be stuff to receive from the connection manager ("client")
			if((SteamConnectionManager != null) && SteamConnectionManager.Connected){
				this.SteamConnectionManager.Receive();
			}
		}catch(System.Exception e){
			GD.Print("Error receiving data: "+ e.Message);
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		//GD.Print("notification was : " + what);

	}

	public void CreateSteamSocketServer(){
		this.SteamSocketManager = SteamNetworkingSockets.CreateRelaySocket<SteamSocketManager>();
		this.SteamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(PlayerSteamId);
		GD.Print("creating socket server!" + PlayerSteamId.ToString());
	}

	public void JoinSteamSocketServer(SteamId host){
		if(!IsHost){
			GD.Print("joining socket server!" + host.ToString());
			this.SteamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(host);
		}
	}

	private void Broadcast(string data){
		// Skipping 0th index which will be "us"
		foreach (var item in SteamSocketManager.Connected.Skip(1).ToArray()){
			item.SendMessage(data);
		}
	}

	public void UnlockSteamAchievement(SteamAchievementsApi.AchievementId id){
		if (!this.Connected){
			return;
		}

		var achievement = new Achievement(id.ToString());
		achievement.Trigger();
	}

	public void IncrementSteamAchievementStat(SteamAchievementsApi.StatId id){
		if (!this.Connected){
			return;
		}

		SteamUserStats.AddStat(id.ToString(), 1);
	}

	public void __DebugClearAllStats(){
		if (!this.Connected){
			return;
		}

		SteamUserStats.ResetAll(true);
	}
}
