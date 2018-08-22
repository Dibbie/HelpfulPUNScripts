using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This script can be broken apart and seperated as needed. A similar script is used in my actual game project
/// that this script has been modified from. You are welcome to use this script with modification in your project
/// without credits. This is simply provided as an example and concrete use-case if you happen to find it useful
/// for your own game project.
/// 
/// - Dibbie | check my Twitter for updates on my project: https://twitter.com/DibbieGames
/// </summary>

public class MenuNetworkManager : Photon.MonoBehaviour
{
    public const int MAX_CCU = 20; //my game uses the free plan, which is 20 CCU, you can modify this for your plan

    [Header("DC")]
    public Text playerCount;
    public Button retryBtn;
    public Canvas dc;

    [Space, Header("Screens")]
    public GameObject capacityScreen, roomScreen;
    private GameObject activeScreen;

    [Space, Header("Room")]
    public Button readyBtn;

    //Photon version of your game build - anyone who is NOT on the same version, cannot play with eachother
    string version = "v1.0a";

    //this will store the ready status of every client (minus the host) in the room - since my game only requires 2 players, this logic is easy
    ExitGames.Client.Photon.Hashtable ReadyStatus = new ExitGames.Client.Photon.Hashtable();
    bool isReady = false;

    void Awake()
    {
        capacityScreen.SetActive(false);
        roomScreen.SetActive(false);
        activeScreen = roomScreen;

        ReadyStatus.Add("Ready", false);
    }

    void Update()
    {
        //check for internet connection
        dc.enabled = Application.internetReachability == NetworkReachability.NotReachable;

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            //always update the active screen
            if (activeScreen != null) { activeScreen.SetActive(true); }

            //Capacity/DC Text Display...
            if (capacityScreen.activeSelf)
            {
                //according to the Photon docs, you should be able to access the current CCU without being connected to the server
                //therefore, this should be a generally accurate representation of player count on the server
                playerCount.text = PhotonNetwork.countOfPlayersOnMaster + "/" + MAX_CCU + " Players";
            }

            //Room Text Display...
            if (roomScreen.activeSelf && PhotonNetwork.inRoom)
            {
                //PhotonNetwork.room.PlayerCount == 2 will only allow the "start game" button to be clickable, if exactly 2 clients are in the room.
                //My game only requires 2 players, however you can modify this to be however many your game requires at minimal and/or use >=

                //PhotonNetwork.room.CustomProperties["Ready"] == true will check that the ROOM properties (not the player properties) are set to ready.
                //This works for my game because there is only ever 2 clients, however in a game with more players, you may want to
                //store them in the player properties for each player and run a for-loop to do a similar check.
                if (PhotonNetwork.player.IsMasterClient) { readyBtn.interactable = PhotonNetwork.room.PlayerCount == 2 && (bool)PhotonNetwork.room.CustomProperties["Ready"] == true; }
            }
        }
    }

    #region Button Events
    public void BtnQuitGame()
    {
        //this is only okay, because we have a OnApplicationQuit event that will disconnect us from the Photon server.
        //you should always make sure you disconnect the player from all Photon services BEFORE closing the client.
        //this prevents the player from remaining on your CCU and potentially sending any irrelevant data in its last
        //few frames.
        //Photon Servers do not update in realtime and is delays by a few seconds, this is why its good to ensure they are DC'd
        Application.Quit();
    }

    //---------Capacity/DC--------------
    public void BtnRetry()
    {
        retryBtn.interactable = false;
        retryBtn.GetComponentInChildren<Text>().text = "<size=20>Retrying...</size>";

        if (ConnectToServer()) { SetActiveScreen(roomScreen); }
        else { Debug.Log("Server is still at capacity =( Try again later."); }

        retryBtn.interactable = true; retryBtn.GetComponentInChildren<Text>().text = "Retry";
    }

    //---------Room--------------
    //set in the Inspector on a "Join Room" and "Creatte Room" button, relevant to my game.
    public void BtnRegisterAction(string task)
    {
        action = task;
    }

    //Whatever logic you use to join or create rooms... Here is a (massively) modified and streamlined/simplified version of mine
    public void BtnJoinOrCreateRoom()
    {
        //"action" is simply just a string, my logic is different than this, but the basic idea is there
        if (action == "Join Room")
        {
            bool roomExists = false;

            //if this returns true, a room already exists with the same name the player wants to join
            foreach (var room in PhotonNetwork.GetRoomList()) { if (room.Name == roomName) { roomExists = true; break; } }

            if (roomExists)
            {
                action = "Joining Room '" + roomName + "'...";
                PhotonNetwork.JoinRoom(roomName);
            }
            else { Debug.Log("No room named: " + roomName + " exists."); }
        }
        else if (action == "Create Room")
        {
            bool roomExists = false;

            //if this returns true, a room already exists with the same name the player wants to create
            foreach (var room in PhotonNetwork.GetRoomList()) { if (room.Name == roomName) { roomExists = true; break; } }

            if (!roomExists) //if the room does NOT already exist...
            {
                action = "Creating Room...";

                RoomOptions options = new RoomOptions();
                options.CleanupCacheOnLeave = true; //After a player DC's or leaves a room, their saved info will be cleared, meaning on rejoin, their previous state is reset
                options.DeleteNullProperties = true; //Any customRoomProperties left null or unset are cleared from the room. I believe this includes playerProperties as well. This is a efficency thing
                options.EmptyRoomTtl = 1000; //If no one is in the room, for more than 1 second, Photon will delete the room
                options.PlayerTtl = 1000; //ttl = "time till leave", after 1 second (1000ms = 1 second), Photon will delete the player from the room, if they DC'd or left
                options.IsOpen = true; //leaves the room open so if a player disconnects they can rejoin. If the room is closed, rejoining and late-joining is impossible
                options.MaxPlayers = 2; //my game only requires 2 players. Youd set this to however many max players are allowed for your rooms
                PhotonNetwork.CreateRoom(roomCode, options, TypedLobby.Default); //auto-joins room after created
            }
        }
    }

    public void BtnReadyOrStart()
    {
        if (PhotonNetwork.player.IsMasterClient) { readyBtn.GetComponentInChildren<Text>().text = "Loading"; StartGame(); }
        else //player is not master and the button should be a "ready" instead of "start game"
        {
            //this code is specific to a 2-player game. You may need to update a list of all networked/connected players ready status
            //then check them before the master client can start a game
            var ReadyStatus = new ExitGames.Client.Photon.Hashtable();
            ReadyStatus["Ready"] = (isReady = !isReady); //ready status can be toggled, every time the button is clicked
            PhotonNetwork.room.SetCustomProperties(ReadyStatus); //in heinsight, this can be spammed, which sends multiple requests...
            readyBtn.GetComponentInChildren<Text>().text = (bool)ReadyStatus["Ready"] ? "Unready" : "Ready"; //() ? : is a ternary operator, basically a in-line if-else conditon... "variable = (if this condition is true) ? do this : else, do that;
        }
    }

    void StartGame()
    {
        if (PhotonNetwork.isMasterClient) { LoadGame(); }
    }

    public void BtnLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }
    //------------------------------------
    #endregion

    #region Logic
    bool ConnectToServer()
    {
        //this checks if the player even has a internet connection at all, using Unity's API inside the Application class.
        //you can do your own network check by sending a ping to a website (such as google) and if you do not recieve a ping
        //then there is no connection. This function essentially does that behind-the-scenes, I imagine it pings the Unity site, internally.
        if (Application.internetReachability == NetworkReachability.NotReachable) { return false; }

        //ConnectUsingSettings actually returns a bool if it was able to sucessfully connect or not.
        //Conflicts can include: versions dont match, version not set, server is full, bad/non-existant internet connection,
        //or any other network conflicts
        if (PhotonNetwork.countOfPlayersOnMaster < MAX_CCU) { return PhotonNetwork.ConnectUsingSettings(version); }
        else { return false; }
    }

    //called in OnApplicationExit and can also manually be called from any function like a Quit btn or Logout btn, etc
    void DisconnectFromServer()
    {
        if (PhotonNetwork.connected) { PhotonNetwork.Disconnect(); }
    }

    //just a simple function to toggle specific Canvas UI gameobjects on/off that act as screens
    //such as login, server full/dc, lobby, room, etc...
    public void SetActiveScreen(GameObject screen)
    {
        if (activeScreen != null) { activeScreen.SetActive(false); }
        activeScreen = screen;
    }

    public void LoadGame(string sceneName)
    {
        //this will load the scene for all clients (ONLY IF PhotonNetwork.automaticallySyncScene = true in OnJoinedRoom() event).
        //HOWEVER, network connectivity can still be a problem, and people with slow connection may still see the previous scene
        //and not exist in the next scene yet. There is logic in these examples to basically "wait" until the player is spawned
        //and their OnPhotonInstantiated is called. Players with fast internet connection may instantly load the scene
        //as if it wasnt even on the network. Most games handle this conflict with a loading screen so players dont see
        //what would otherwise be a blank scene, as game assets are loaded from memory into the game world, and players are spawned.
        //These examples do NOT include a loading screen, but you can create a Canvas for your loading screen on your main menu and
        //then call DontDestroyOnLoad so it carries over to the next scene, then destroy (or fade out, however you want to handle it)
        //the loading screen, once you know all assets are loaded, all players are spawned and everything is initialized and ready for gameplay.
        //This logic validation is often handled through a NetworkManager.

        //The way that my game works is the following: The scene is changed to a scene with ONLY my UI logic > that then spawns the map and other assets
        //> that then spawns the players > that then waits for all players to spawn before updating the UI to the players > that then initalizes all network code
        //So basically: new scene > wait for map spawn > wait for assets spawn > wait for network initalization > wait for player spawn > begin game
        PhotonNetwork.LoadLevel(sceneName);
    }
    #endregion

    #region Network Events
    /// <summary>
    /// Called as soon as any form of PhotonNetwork.Connect (in our case, ConnectUsingDefaultSettings) is successful.
    /// </summary>
    private void OnConnectedToMaster()
    {
        print("connected to server.");
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    /// <summary>
    /// Called as soon as any form of PhotonNetwork.Join is successful. This includes JoinOrCreate, Create, Join, etc... 
    /// </summary>
    void OnJoinedLobby()
    {
        print("joined the lobby.");
        SetActiveScreen(roomScreen);
    }

    /// <summary>
    /// Called as soon as a established connection to the room has been sucessfully made.
    /// The player now exists in a ROOM instead of a lobby.
    /// Every time the player is disconnected from a room or leaves a room, they are re-instantiated in the LOBBY.
    /// This means, OnJoinedLobby will automatically be called again if PhotonNetwork.room.Disconnect or PhotonNetwork.LeaveRoom is called.
    /// This will disconnect them from the ROOM, and Photon servers, then automatically re-connect them to the LOBBY.
    /// Looking online, there is no way to keep the connection consistant, at least with a free plan. This is by the Photon Team design.
    /// 
    /// FUN FACT: 'Toby'/'Tobias' is one of the lead developers of the Photon Unity Networking framework, and coded most of this assets logic.
    /// He is also active on the official PUN forums. You can find a lot of common questions you might have, already asked and answered on the PUN forums.
    /// </summary>
    void OnJoinedRoom()
    {
        print("Joined room.");
        SetActiveScreen(roomScreen);

        //this will make sure that any PhotonNetwork.Instantiate calls can only happen within a room. If you are not in a room or leave a room, the Instantiate method will throw an error.
        //unless you have a MMO where objects/players can exist in the lobby as actual instantiated objects, you will often never need this to be false.
        PhotonNetwork.InstantiateInRoomOnly = true;

        //this line can be removed if you DO NOT want voice chat to be allowed while in a room. For my game, I find it helpful.
        //this line initalizes PhotonVoice chat. For voice chat to work in general, you NEED the Photon Voice asset imported in your project.
        var voiceClientInit = PhotonVoiceNetwork.Client;

        //automatically make the master client "ready" and make the button a "start" instead of "ready" button
        if (PhotonNetwork.player.IsMasterClient) { readyBtn.GetComponentInChildren<Text>().text = "Start Game"; ReadyStatus["Ready"] = (isReady = true); }
        //if the player is NOT the master, set the ready status to false, and make the button a "ready" button to toggle the ready status
        else { readyBtn.GetComponentInChildren<Text>().text = "Ready Up"; ReadyStatus["Ready"] = (isReady = false); }
        PhotonNetwork.room.SetCustomProperties(ReadyStatus); //actually send the "ready" status to the Photon servers ROOM properties
        
        //this will make sure that when PhotonNetwork.LoadLevel is called, ALL clients get updated and load the level (generally) at the same time.
        //for games where it is important that all clients are initalized before a game starts (such as round-based games), this helps ensure that.
        //for games that it doesnt matter if everyone is initalized before gameplay can begin (such as MMO's) then this really doesnt matter.
        PhotonNetwork.automaticallySyncScene = true;
    }

    //automatically called whenever Application.Quit is called or the client (exe) is exited in a respectful way (not stopped through task manager or crashed, etc)
    private void OnApplicationQuit()
    {
        DisconnectFromServer();
    }
    #endregion
}
