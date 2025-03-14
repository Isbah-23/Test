using UnityEngine;
using UnityEngine.UI;

public class ButtonDebug : MonoBehaviour
{
    public Button myButton;

    void Start()
    {
        if (myButton != null)
        {
            myButton.onClick.AddListener(() => Debug.Log("Button Click Registered!"));
        }
        else
        {
            Debug.LogError("Button reference is missing!");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse Click Detected!");
        }
    }
}