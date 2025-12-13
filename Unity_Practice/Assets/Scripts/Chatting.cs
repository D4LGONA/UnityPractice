using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Chatting : MonoBehaviour
{
    public ScrollRect scrollRect;     // Scroll View
    public Transform content;         // Viewport/Content
    public GameObject messagePrefab;  // ChatMessageItem 프리팹

    public void AddMessage(string msg)
    {
        var go = Instantiate(messagePrefab, content);
        var tmp = go.GetComponent<TMP_Text>();
        tmp.text = msg;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f; // 맨 아래로
    }
}
