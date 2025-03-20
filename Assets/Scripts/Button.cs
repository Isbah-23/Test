using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private Image buttonImage;
    private Color normalColor;
    public GameObject CubeObj;
    TestInteraction script;

    private void Start()
    {
        originalScale = transform.localScale;

        script = FindObjectOfType<TestInteraction>();
        if (script != null)
        {
            CubeObj = script.gameObject;
            Debug.Log("Script found on: " + CubeObj.name);
        }
        else
        {
            Debug.Log("Couldn't find script");
        }
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            normalColor = buttonImage.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * 1.05f, 0.2f).SetEase(Ease.OutBack); // 5% scale, in 0.2 seconds

        // call funtion in testinteraction script
        if (script != null)
        {
            script.PlayHoverAudio();
        }
        else
        {
            Debug.Log("Script is null");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutBack);
        buttonImage.DOColor(normalColor, 0.2f); 
    }

    // return to normal size before the button is disabled
    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
    
}
