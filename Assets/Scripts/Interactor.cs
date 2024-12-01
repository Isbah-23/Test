using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IInteractable
{
    public void Interact();
}
public class Interactor : MonoBehaviour
{
    public Transform InteractorSource;
    public float InteractorRange;

    
    void Start()
    {
        
    }

    void Update()
    {
        //check for mouse button press
        if(Input.GetMouseButtonDown(0))
        {
            Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
            if(Physics.Raycast(r,out RaycastHit hit, InteractorRange))
            {
                if(hit.collider.TryGetComponent<IInteractable>(out IInteractable interactableObject))
                {
                    interactableObject.Interact();
                }
            }
        }
    }
}
