using UnityEngine;

public class ButtonClickDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button click
        {
            Debug.Log("Mouse Click Detected");
        }
    }

    public void OnButtonClick()
    {
        Debug.Log($"UI Button Click Detected: {gameObject.name}");
    }
}
