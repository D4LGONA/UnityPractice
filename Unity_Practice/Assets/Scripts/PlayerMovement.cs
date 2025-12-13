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

    float h, v;      // x: 좌우(h), y: 전후(v)
    bool jumpPressed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void SetMoveInput(float h, float v)
    {
        this.h = h;
        this.v = v;
    }

    public void PressJump()
    {
        jumpPressed = true;
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camRight * h + camForward * v;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        Vector3 horizontalVelocity = moveDir * moveSpeed;

        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
                velocity.y = -2f;

            if (jumpPressed)
            {
                velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
                jumpPressed = false;
            }
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = horizontalVelocity;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }
}
