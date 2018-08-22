using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script can be broken apart and seperated as needed. A similar script is used in my actual game project
/// that this script has been modified from. You are welcome to use this script with modification in your project
/// without credits. This is simply provided as an example and concrete use-case if you happen to find it useful
/// for your own game project.
/// 
/// - Dibbie | check my Twitter for updates on my project: https://twitter.com/DibbieGames
/// </summary>

public class LocalNetworkPlayer : MonoBehaviour {

    private static ExitGames.Client.Photon.Hashtable playerData = new ExitGames.Client.Photon.Hashtable();
    
    private PhotonView view;
    private bool isAlive = false;
    
    void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        view = GetComponent<PhotonView>();
        name = name.Replace("(Clone)", ""); //remove the (Clone) at the end of the instanced player object

        if (view.isMine)
        {
            //initalization logic takes place here...

            Invoke("BeginNetworking", 1f); //delay sending network information to give other scripts time to initalize
            //before accessing player info
        }
        else { RuntimeNetworkManager.AssignPlayer2(gameObject); }
    }
    
    void BeginNetworking()
    {
        SetProperty("Kills", 0);
        SetProperty("Deaths", 0);
        SetProperty("Ping", 0);
        isAlive = true;
    }

	void Update () {
        if (isAlive)
        {
            
            //you can set player states here, such as "dead", "respawning", "alive", etc...

            //Network Ping
            SetProperty("Ping", PhotonNetwork.GetPing());
        }
    }

    /// <summary>
    /// Local player property to send to the target player (often Player2)
    /// </summary>
    public void SendPlayerProperty(string propName, object value)
    {
        if (playerData.ContainsKey(propName)) { playerData[propName] = value; }
        else { playerData.Add(propName, value); }
    }

    /// <summary>
    /// Local player property to retrieve from the target player (often Player2)
    /// </summary>
    public object GetPlayerProperty(string propName)
    {
        if (playerData.ContainsKey(propName)) { return playerData[propName]; }
        else { return null; }
    }

    /// <summary>
    /// Set a propety of your own player (often LocalPlayer)
    /// </summary>
    public static void SetProperty(string propName, object value)
    {
        if (playerData.ContainsKey(propName)) { playerData[propName] = value; }
        else { playerData.Add(propName, value); }
    }

    /// <summary>
    /// Get a property of your own player (often LocalPlayer)
    /// </summary>
    public static object GetProperty(string propName)
    {
        if (playerData.ContainsKey(propName)) { return playerData[propName]; }
        else { return null; }
    }
}

public enum PlayerState
{
    Alive, Respawning, Dead
}
