using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class TestInteraction : MonoBehaviour, IInteractable,VrInteractable
{
    private XRBaseInteractable interactable;
    private AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip hoverSound;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        audioSource = GetComponent<AudioSource>();
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
        ChangeScene();
    }
    public void InteractReleased()
    {
        
    }
    public void ChangeScene()
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

    public void PlayClickAudio()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        else
        {
            Debug.Log("Couldnt play click sound");
        }
    }
    public void PlayHoverAudio()
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
        else
        {
            Debug.Log("Couldnt play hover sound");
        }
    }
    public void QuitGame()
    {
        Debug.Log("Quitting Application...");
        Application.Quit();
        
        // To make sure it also works in the Unity Editor
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}

