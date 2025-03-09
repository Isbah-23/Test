using UnityEngine;
using UnityEngine.UI;

public class ButtonClicker : MonoBehaviour
{
    public Button myButton; // Assign in Inspector

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Press Spacebar to "click"
        {
            Debug.Log("Song will play now");
            myButton.onClick.Invoke();
        }
    }

}
