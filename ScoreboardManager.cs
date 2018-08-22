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

public class ScoreboardManager : MonoBehaviour
{
    public Text timeRemaining; //if your game does not require a "match time", you can remove this and all references.

    public ScoreHUD[] players; //this assumes that the first index [0] is always you/the local player
    //however, my game only requires 2 players, this is modified to be more dynamic

    [System.Serializable]
    public class ScoreHUD
    {
        public Text name;
        public Text healthValue; //to sync health, you will need to observe your stats script
        public Slider healthBar; //"hp bar", for visual effect + health numeric display
        public Text state; //such as "alive", "dead", "respawning", etc...
        public Text kills, deaths;
        public Text networkInfo;
    }

    WaitForSecondsRealtime delayedTime; //caching a yield-return call instead of always calling "new ..." for efficency

    /// <summary>
    /// This should be called from a script that handles initalizing your network info, so match time and scores are in sync with the network
    /// </summary>
    public void Init()
    {
        delayedTime = new WaitForSecondsRealtime(1f);

        for (int i = 0; i < players.Length; i++)
        {
            //script I created to manage network logic and syncing network data for each client.
            //However, my game only requires 2 players so this script was excluded from the examples as it would add to the confusion
            if (NetworkedSettings.Players[i].PlayerManager != null)
            {
                players[i].name.text = PhotonNetwork.playerList[i].nickname;

                //this is how I handle syncing stats, on each player when OnPhotonInitialized() is called, they add themselves to NetworkedSettings.Players (script I created).
                //replace this logic with however you handle syncing your players stats.
                players[i].healthBar.maxValue = NetworkedSettings.Players[i].PlayerManager.stats.MaxHealth;
            }
            else { players[i].name.text = "--"; } //player is not in the room, disconnected, or is still loading
        }

        //update the scoreboard and time at a delayed rate through a coroutine, to prevent too many network calls
        StartCoroutine(DelayedUpdate()); 
    }

    void Awake()
    {
        for (int i = 0; i < players.Length; i++)
        {
            players[i].healthValue.text = "--";
            players[i].healthBar.value = 0;
            players[i].state.text = "Dead";
            players[i].kills.text = "--";
            players[i].deaths.text = "--";
            players[i].networkInfo.text = "Not Connected";
        }
    }

    IEnumerator DelayedUpdate()
    {
        //you can set this to "while(true)" or replace with whatever logic you use to determine if a game should continue and scores should remain updated
        while (!GameManager.GameOver) 
        {
            //NetworkedSettings.matchtime contains the minutes and seconds, this is a script I created to handle most of my networking logic and syncing.
            //this script will call the "Tick()" logic which looks like the following:
            //public void Tick()
            //{
            //  seconds--;
            //  if (seconds < 0) { minutes--; seconds = 59; }
            //  if (minutes < 0) { minutes = 0;
            //  GameManager.GameOver = minutes == 0 && seconds == 0;
            //}
            NetworkedSettings.matchTime.Tick();
            timeRemaining.text = NetworkedSettings.matchTime.minutes.ToString("00") + ":" + NetworkedSettings.matchTime.seconds.ToString("00");

            #region Player [i] Score Info
            for (int i = 0; i < players.Length; i++)
            {
                //the following all use NetworkedSettings.Players[i].PlayerManager which is a script I created, that contains
                //information for GetProperty and SetPropety - also functions I created to access each players customPlayerProperties
                //this information is provided in these examples, in "LocalNetworkPlayer"
                players[i].healthValue.text = NetworkedSettings.Players[i].PlayerManager.stats.health.ToString("#,#");
                players[i].healthBar.value = NetworkedSettings.Players[i].PlayerManager.stats.health;
                players[i].state.text = NetworkedSettings.Players[i].PlayerManager.currentState.ToString();

                try
                {
                    players[i].kills.text = ((int)NetworkedSettings.Players[i].GetProperty("Kills")).ToString("#,#");
                    players[i].deaths.text = ((int)NetworkedSettings.Players[i].GetProperty("Deaths")).ToString("#,#");
                }
                catch
                {
                    players[i].kills.text = "--";
                    players[i].deaths.text = "--";
                }

                players[i].networkInfo.text = (PhotonNetwork.connectionState == ConnectionState.Connected) ? ((int)NetworkedSettings.Players[i].GetProperty("Ping")).ToString("#,#") + " ms" : "(Disconnected)";
            }
                #endregion
            
            yield return delayedTime;
        }
    }
}
