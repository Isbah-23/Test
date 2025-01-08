using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class TestInteraction : MonoBehaviour, IInteractable,VrInteractable
{
    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelected);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {   
            interactable.selectEntered.RemoveListener(OnSelected);
        }
    }

    public void OnSelected(SelectEnterEventArgs args)
    {
        Interact();
    }
    public void Interact()
    {
        //change scene to the next scene
        Debug.Log("Interacting with the object");
        changeScene();
    }
    public void InteractReleased()
    {
        
    }
    private void changeScene()
    {
        //change scene to the next scene
        Debug.Log("Changing scene");
        int y = SceneManager.GetActiveScene().buildIndex;
        if(y==1)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }
}

