using UnityEngine;
using UnityEngine.UI;

public class SelectionPanelController : MonoBehaviour
{
    public GameObject selectionPanel; // Assign SelectionPanel in Inspector
    public GameObject buttonPrefab; // Assign a UI Button prefab in Inspector
    public Transform content; // Assign Content (inside Scroll View) in Inspector

    private bool isSpawned = false;
    private const int buttonCount = 3;
    private const float buttonHeight = 100f;
    private const float buttonWidth = 500f;

    public void ToggleSelectionPanel()
    {
        selectionPanel.SetActive(!selectionPanel.activeSelf);

        if (selectionPanel.activeSelf && !isSpawned)
        {
            SpawnButtons();
            isSpawned = true;
        }
    }

    private void SpawnButtons()
    {
        for (int i = 0; i < buttonCount; i++)
        {
            GameObject newButton = Instantiate(buttonPrefab, content);
            RectTransform rectTransform = newButton.GetComponent<RectTransform>();

            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchoredPosition = new Vector2(0, -i * buttonHeight);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Song " + (i + 1);
        }
    }
}
