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

    private void Start()
    {
        originalScale = transform.localScale;
        
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            normalColor = buttonImage.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.OutBack);
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
