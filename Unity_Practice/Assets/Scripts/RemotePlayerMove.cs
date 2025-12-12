using UnityEngine;

public class RemotePlayerMove : MonoBehaviour
{
    [Header("Smoothing")]
    [SerializeField] private float posLerpSpeed = 15f;
    [SerializeField] private float rotSlerpSpeed = 15f;
    [SerializeField] private float teleportDistance = 5f; // 너무 멀면 순간이동 처리

    private Vector3 targetPos;
    //private Quaternion targetRot;

    private bool hasTarget = false;

    void Start()
    {
        targetPos = transform.position;
        //targetRot = transform.rotation;
        hasTarget = true;
    }

    void Update()
    {
        SetMove();
    }

    void SetMove()
    {
        if (!hasTarget) return;

        float dist = Vector3.Distance(transform.position, targetPos);
        if (dist >= teleportDistance)
        {
            transform.SetPositionAndRotation(targetPos, transform.rotation);
            return;
        }

        // 부드럽게 보간
        transform.position = Vector3.Lerp(transform.position, targetPos, posLerpSpeed * Time.deltaTime);
        //transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSlerpSpeed * Time.deltaTime);
    }

    public void ApplyServerState(Vector3 serverPos)
    {
        targetPos = serverPos;
        //targetRot = serverRot;
        hasTarget = true;
    }
}
