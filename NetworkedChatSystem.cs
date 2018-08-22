using AdvancedInputManager;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// This script can be broken apart and seperated as needed. A similar script is used in my actual game project
/// that this script has been modified from. You are welcome to use this script with modification in your project
/// without credits. This is simply provided as an example and concrete use-case if you happen to find it useful
/// for your own game project.
/// 
/// - Dibbie | check my Twitter for updates on my project: https://twitter.com/DibbieGames
/// </summary>

public class NetworkedChatSystem : Photon.MonoBehaviour
{ //This script should be attached to the player with a PhotonView on it. This script does not need to be observed.

    //max number of messages before the first-created message is deleted to make room for the last-sent message
    //this is used as an efficent way to avoid having a history of messages stored in memory, likely never viewed again shortly after it was posted
    public const int MESSAGE_CAP = 25; 

    /// <summary>
    /// Determines if we are currently typing a message. If we are, we can toggle certain scripts such as our input listener, mouse look, movement, etc...
    /// </summary>
    public static bool isTyping { get; internal set; }

    static string identifier = "#name"; //tag that will be replaced with the player name. You should try to use symbols a player cannot have in their name
    static string msgFormat = "[" + identifier + "]: "; //how youd like a message to be displayed. Currently...    [Name]: Message

    public RectTransform container; //Scroll View container all messages will appear in
    public InputField context; //area the player is able to type their message in
    public Text messageTemplate; //Text element of the actual message to create - you can theme your message here in terms of how youd like it to be displayed.

    //colors to identify who is talking. Because my game only requires 2 players I only have 2 colors. You can create an array of colors,
    //or create common colors for "yourself", "others", "server", etc, or store the color in the player who sent the message and acccess
    //the player as an object.
    public Color local = Color.green;
    public Color player2 = Color.blue;

    public float sendRate = 1.2f; //how fast a player can send messages to the sever

    float delay = 0f;
    bool canType;

    //prevent the player from being able to type till the network is initalized and all players have spawned.
    //You should call this in your NetworkManager or however you handle network initalization
    public void Init()
    {
        canType = true;
    }

    void Update()
    {
        if (canType)
        {
            //set the color of typing brighter than actually displayed
            context.textComponent.color = local * 2f;

            //allow typing and lock reading input from the player while typing
            isTyping = context.isFocused;

            //prevent spamming
            if (delay > 0f) { delay -= Time.deltaTime; }

            if (delay <= 0f)
            {
                delay = 0f;

                if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
                {
                    if (context.isFocused) { SendMessage(); } //type field has focus when enter is pressed, send the message
                    else { context.Select(); } //type fiels does NOT have focus when enter is pressed, set its focus so we can type
                }
            }

            //fail-safe check, if we are still considered "typing" in this frame but the type field no longer has focus, ensure IsTyping is false
            //so that any function that uses it, gains access to the player again
            if (isTyping && !context.isFocused) { isTyping = false; }
        }
    }

    void SendMessage()
    {
        string message = context.text.Trim(); //remove white-spaces so something like '  hi    ' becomes 'hi'
        if (string.IsNullOrEmpty(message) || message == " ") { return; } //dont send anything, if the message is essentially nothing or a blank space, even after trimming

        delay = sendRate; //prevent spam
        EventSystem.current.SetSelectedGameObject(null); //unselect the type field
        context.text = string.Empty; //clear the type field after the message has sent

        //photonView is local to the object the script is attached to, when using Photon.MonoBehaviour
        photonView.RPC("SendNetworkChatMessage", PhotonTargets.All, message);

        //NOTE: local info such as unsetting focus and clearing the text field happen BEFORE the message is sent.
        //This is so, in the event there is lag, or for whatever reason sending throws an error, local info is cleared.
        //You can reverse this logic and move the RPC call ABOVE the delay line, so that the message is attempted to send
        //BEFORE clearing local information. However, this presents a possible spam problem, if abused.
    }

    //function that should be called in the OnEndEdit event of the InputField
    //only if enter was pressed, then send the message that way, verse pressing a button or other means
    public void SendMessageOnEnter() { if (delay <= 0f && (Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Return))) { SendMessage(); return; } }

    [PunRPC]
    public void SendNetworkChatMessage(string context, PhotonMessageInfo info)
    {
        //delete the top-most message to make room for the new message, if the number of messages exceeds the cap
        if (container.childCount >= MESSAGE_CAP) { Destroy(container.GetChild(0).gameObject); }

        //create a new message, set it to appear at the bottom of chat, and set the name of the message + message text itself
        Text message = Instantiate(messageTemplate, container);
        message.transform.SetAsLastSibling();
        bool isMe = info.sender.NickName == NetworkedSettings.LocalPlayer.Player.NickName;

        //set the color of the message, depending on who sent it
        if (isMe) { message.text = "<color=#" + ColorUtility.ToHtmlStringRGBA(local) + ">" + msgFormat.Replace(identifier, "<b>" + NetworkedSettings.LocalPlayer.Player.NickName + "</b>") + context + "</color>"; }
        else { message.text = "<color=#" + ColorUtility.ToHtmlStringRGBA(player2) + ">" + msgFormat.Replace(identifier, NetworkedSettings.Player2.Player.NickName) + context + "</color>"; }
    }
}
