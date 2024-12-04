using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestInteraction : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        //change scene to the next scene
        Debug.Log("Interacting with the object");
        changeScene();
    }
    private void changeScene()
    {
        //change scene to the next scene
        Debug.Log("Changing scene");
        SceneManager.LoadScene(1);
    }
}

