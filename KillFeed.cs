using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script can be broken apart and seperated as needed. A similar script is used in my actual game project
/// that this script has been modified from. You are welcome to use this script with modification in your project
/// without credits. This is simply provided as an example and concrete use-case if you happen to find it useful
/// for your own game project.
/// 
/// - Dibbie | check my Twitter for updates on my project: https://twitter.com/DibbieGames
/// </summary>

public class KillFeed : MonoBehaviour {

    public const int BAD_PING = 312; //ping above this value will be considered "lagging" and display a "poor connection" icon
    
    public RectTransform killFeed;
    public GameObject killReportPrefab; //consists of 4 Text (P1, "killed", P2, "with") and 1 Image (weapon icon)
    
    public Image poorSignal, disconnected;

    GameObject lastKillReport;
    static string killer, action, target;
    static Sprite instrument; //"instrument" as in, the weapon used

    void Start ()
    {
        poorSignal.enabled = false;
        disconnected.enabled = false;

        //this is the killfeed placeholder - we dont actually need it since its prefabbed
        //it is kept on the UI to visually see how it looks and make modifications then update the prefab as needed, from the Editor
        Destroy(killFeed.GetChild(0).gameObject);
        
        StartCoroutine(UpdateKillFeed());
    }
	
	// Update is called once per frame
	void Update () {
        if (PhotonNetwork.player.photonView != null) {
            disconnected.enabled = !PhotonNetwork.connected || PhotonNetwork.connectionState != ConnectionState.Connected;
            poorSignal.enabled = PhotonNetwork.GetPing() >= BAD_PING;
        }
    }

    /// <summary>
    /// This can be called with KillFeed.ReportMurder(GameObject, DeathMode)
    /// This will set all variables for the coroutine to actually update the UI/HUD with the correct kill feed report
    /// </summary>
    /// <param name="Killer">The gameObject who killed the other player</param>
    /// <param name="method">The method of killing the player used against the other player</param>
    public static void ReportMurder(PhotonPlayer Killer, DeathMethod method)
    {
        bool isYou = Killer != PhotonNetwork.player; //"killer" is player who got killed

        killer = isYou ? "You" : Killer.nickname;
        action = method.ToString();
        target = !isYou ? "You" : Killer.nickname;

        //this would be modified to however you store your weapon logic. This is how I store mine
        instrument = Killer.GetComponent<WeaponManager>().weapon.icon; 

        //NOTE: You can take this logic and modify it in a way to only use 1 UI Text and death method, to create
        //funny scenarios such as games like Fortnite "Player spantaniously combusted", "Player got absolutely rekt", etc
    }

    IEnumerator UpdateKillFeed()
    {
        //however you handle determining if the match has ended/killfeed should continue to update. This is how I handle mine
        while (!GameManager.GameOver) 
        {
            if(target != null)
            {
                lastKillReport = Instantiate(killReportPrefab, killFeed);
                lastKillReport.transform.GetChild(0).GetComponent<Text>().text = killer; //"Player 1"
                lastKillReport.transform.GetChild(1).GetComponent<Text>().text = action; //"killed"
                lastKillReport.transform.GetChild(2).GetComponent<Text>().text = target; //"Player 2" with
                lastKillReport.transform.GetChild(4).GetComponent<Image>().sprite = instrument; //(weapon icon)
                lastKillReport.transform.SetAsLastSibling(); //ensure it shows up at the bottom of the killfeed
                Destroy(lastKillReport, 5f); //you can also attach a fade in/out animation to the text so it doesnt just appear then disappear instantly

                target = null;
            }

            yield return null;
        }
    }

    public enum DeathMethod { killed, stabbed, exploded, deleted }; //you can add whatever is relevant for your game
}
