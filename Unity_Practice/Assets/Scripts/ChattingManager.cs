using System;
using TMPro;
using UnityEngine;

public class ChattingManager : MonoBehaviour
{
    public NetworkClient net;
    public Chatting chatUI;
    public TMP_InputField chatInput;
    public void EnterChat()
    {
        
        chatInput.interactable = true;
        chatInput.ActivateInputField();
        chatInput.Select();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitChat(bool clear)
    {
        if (clear) chatInput.text = "";
        chatInput.DeactivateInputField();
        chatInput.interactable = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SendChat()
    {
        var msg = chatInput.text;
        if (string.IsNullOrWhiteSpace(msg)) return;
        net.SendChat(msg);
        chatUI.AddMessage($"Player[{net.PlayerId}]: {msg}");
        chatInput.text = ""; 
    }
    public void RecvChat(int playerid, string msg)
    {
        chatUI.AddMessage($"Player[{playerid}]: {msg}");
    }
}
