using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FingertipUIInteractor : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Button button))
        {
            Debug.Log($"Fingertip touched button: {button.name}");
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }
    }
}
