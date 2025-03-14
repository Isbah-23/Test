using UnityEngine;
using UnityEngine.UI;

public class ButtonClickDebug : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => Debug.Log("Button Click Registered!"));
    }
}
