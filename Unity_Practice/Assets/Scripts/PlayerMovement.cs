using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 관련")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f * 2;
    public float jumpPower = 5f;

    public Transform cameraTransform;      // 메인 카메라

    CharacterController controller;
    Vector3 velocity;   // y속도(중력/점프)만 여기서 관리

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;
        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camRight * h + camForward * v;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        Vector3 horizontalVelocity = moveDir * moveSpeed;

        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
                velocity.y = -2f; // 땅에 살짝 눌러주기

            if (Input.GetButtonDown("Jump")) // 기본: Space
            {
                velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
            }
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = horizontalVelocity;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }
}
