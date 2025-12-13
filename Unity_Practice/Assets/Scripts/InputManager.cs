using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum InputMode { Game, Chat }

public class InputManager : MonoBehaviour
{
    public InputMode mode = InputMode.Game;

    public PlayerMovement player;
    public ThirdPersonCamera cameraLookScript;
    public NetworkClient net;
    public ChattingManager ChatManager;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        if (mode == InputMode.Game)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            cameraLookScript.SetLookInput(mx, my);


            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.A)) h = -1f;
            if (Input.GetKey(KeyCode.D)) h = 1f;
            if (Input.GetKey(KeyCode.W)) v = 1f;
            if (Input.GetKey(KeyCode.S)) v = -1f;

            player.SetMoveInput(h, v);

            if (Input.GetButtonDown("Jump"))
                player.PressJump();

            if (Input.GetKeyDown(KeyCode.Return))
            {
                mode = InputMode.Chat;
                ChatManager.EnterChat();
            }
        }
        else 
        {
            player.SetMoveInput(0, 0);

            if (Input.GetKeyDown(KeyCode.Return)) 
            {
                ChatManager.SendChat();
                ChatManager.ExitChat(clear: false);
                mode = InputMode.Game;
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }
}
