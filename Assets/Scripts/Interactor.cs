using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IInteractable
{
    public void Interact();
    public void InteractReleased();
}
public class Interactor : MonoBehaviour
{
    public Transform InteractorSource;
    public float InteractorRange;
    private PlayerControllers controls;
    
    void Start()
    {
        controls = new PlayerControllers();
        controls.Enable();
    }

    void Update()
    {
        // check for button press
        if(controls.Interact.Interact.WasPerformedThisFrame())
        {
            Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
            if(Physics.Raycast(r, out RaycastHit hit, InteractorRange))
            {
                if(hit.collider.TryGetComponent<IInteractable>(out IInteractable interactableObject))
                {
                    interactableObject.Interact();
                }
            }
        }

        // check for button release
        if (controls.Interact.Interact.WasReleasedThisFrame())
        {
            Ray ray = new Ray(InteractorSource.position, InteractorSource.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, InteractorRange))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out IInteractable interactableObject))
                {
                    interactableObject.InteractReleased();
                }
            }
        }
    }
}
