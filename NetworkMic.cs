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

public class NetworkMic : MonoBehaviour
{ //attached to a child of player

    private PhotonView view;
    private PhotonVoiceRecorder mic;
    public float voiceThreshold = 0.1f; //min volume you have to speak for the mic to detect your voice
    public string activeMic; //name of the target mic to listen for your voice

    public TalkMode talkMode;

    #region Test Input - If you have your own input system you prefer to use, this logic can be replaced
    public KeyCode talkKey = KeyCode.T;
    public KeyCode muteKey = KeyCode.M;
    public KeyCode mutePlayerKey = KeyCode.Backspace; //my game only requires 2 players, so this mutes the only other player.
    //in a game that can have more players, a player mute button may not work, and you may have to mute/unmute them through the PhotonNetwork.playerList logic
    #endregion

    public bool selfMuted, otherMuted;

    public enum TalkMode
    {
        /// <summary>
        /// No mic/voice detection is used as if you muted your mic or unplugged it.
        /// </summary>
        Disabled,
        /// <summary>
        /// Your voice is transmitted over the network so long as the volume being recieved is above the threshold. And is stopped as soon as the volume is below the threshold.
        /// </summary>
        VoicePickup,
        /// <summary>
        /// Your voice is transmitted over the network so long as you have the mic talk button held down. And is stopped as soon as it is lifted.
        /// </summary>
        PushToTalk,
        /// <summary>
        /// Press a button to continously transmit your voice over the network, and does not require a threshold or constant hold. It is stopped as soon as the toggle is pressed again.
        /// </summary>
        ToggleTalk
    }

    /// <summary>
    /// Update the game settings relative to the mic settings.
    /// This is called in your SettingsManager for your game, or wherever mic information should be updated.
    /// This is also called in Start to default values.
    /// </summary>
    public void UpdateValues()
    {
        //replace this logic with however youd like to handle updating and storing saved information on mic preferences from the player
        voiceThreshold = SettingsManager.targetMic.voiceThreshold;
        activeMic = SettingsManager.targetMic.mic;
        talkMode = SettingsManager.targetMic.talkMode;
        selfMuted = SettingsManager.targetMic.selfMuted;
    }

    private void Awake()
    {
        //Remember that this script is attached to a child object of the player.
        //This child object should only need a PhotonView with no observed components and default values,
        //and a PhotonVoiceRecorder which will also add a PhotonVoiceSpeaker and AudioSource.
        view = GetComponent<PhotonView>();
        mic = GetComponent<PhotonVoiceRecorder>();
    }

    void Start()
    {
        if (view.isMine)
        {
            //this line just puts the players "channel" into the last number of rooms.
            //This logic can be modified for your game, but is essentially intended to create "private" chats
            //so only players in the same GlobalAudioGroup can hear eachother.
            PhotonVoiceNetwork.Client.GlobalAudioGroup = (byte)(PhotonNetwork.countOfRooms - 1);

            UpdateValues(); //initialization...

            mic.Transmit = !selfMuted; //Transmit is the function that actually begins to listen and network any audio picked up by the mic
            mic.MicrophoneDevice = activeMic; //MicrophoneDevice is the string-name of the microphone Photon Voice will try to find and access on the system
        }

        //my game only requires 2 players, so this is the logic I use to check the mute status of the other player, you may be able to convert this
        //into a for-loop for every player and store it in a serialized or static class
        otherMuted = NetworkedSettings.Players[1].PlayerManager.GetComponent<NetworkMic>().GetComponent<AudioSource>().mute;
    }

    // Update is called once per frame
    void Update()
    {
        //toggle mute other player
        if (InputManager.GetKeyDown(mutePlayerKey)) { NetworkedSettings.Players[1].GetComponent<NetworkMic>().GetComponent<AudioSource>().mute = (otherMuted = !otherMuted); }

        //update active mic
        mic.MicrophoneDevice = activeMic;

        //disable options if your own mic is not set
        if (activeMic == "Disabled")
        {
            mic.Transmit = false;
        }

        //allow options for self-muting, mic modes and talking, if your own mic is set
        else
        {
            if (InputManager.GetKeyDown(muteKey)) { selfMuted = !selfMuted; }

            if (!selfMuted)
            {
                if (talkMode == TalkMode.PushToTalk)
                {
                    mic.Transmit = Input.GetKey(talkKey);
                }
                else if (talkMode == TalkMode.ToggleTalk)
                {
                    if (Input.GetKeyDown(talkKey))
                    {
                        mic.Transmit = !Microphone.IsRecording(activeMic);
                    }
                }
                else if (talkMode == TalkMode.VoicePickup)
                {
                    mic.Detect = true;
                    mic.VoiceDetector.Threshold = voiceThreshold;
                    mic.Transmit = mic.VoiceDetector.Detected;
                }
                else { mic.Transmit = false; mic.Detect = false; } //no mic option selected ("Disabled"/None)
            }
            else { mic.Transmit = false; mic.Detect = false; } //self-muted
        }
    }
}
