using UnityEngine;
using UnityEngine.UI;

public class ButtonClicker : MonoBehaviour
{
    public Button myButton; // Assign in Inspector

    void Start()
    {
        Invoke("StartCues", 10f); // Calls MyFunction after 2 seconds
    }

    void StartCues()
    {
        Debug.Log("Button clicked i hope");
        myButton.onClick.Invoke();
    }

}
