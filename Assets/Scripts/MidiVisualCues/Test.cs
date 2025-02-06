using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private GameObject grandPiano;
    private Dictionary<int, PianoKey> pianoKeysDict = new Dictionary<int, PianoKey>(); // Dictionary to store key references
    private bool allKeysPressed;

    // Start is called before the first frame update
    void Start()
    {
        grandPiano = GameObject.Find("GrandPiano");

        if (grandPiano != null)
        {
            InitializeKeys();  // Initialize the key references
        }
        else
        {
            Debug.LogError("GrandPiano object not found in the scene!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Call CheckKeysPressed in every frame if needed
        //CheckKeysPressed();
    }

    // Method to initialize the key references in the dictionary
    void InitializeKeys()
    {

        Transform pianoKeysTransform = grandPiano.transform.Find("PianoKeys");

        // Loop through the piano keys and add them to the dictionary
        for (int i = 1; i <= 88; i++) // Assuming there are 88 keys
        {
            string keyName = "PianoKey." + i.ToString("D3");  // Construct key name, e.g. "PianoKey.001"
            Transform keyTransform = pianoKeysTransform.transform.Find(keyName);

            if (keyTransform != null)
            {
                PianoKey keyScript = keyTransform.GetComponent<PianoKey>();

                if (keyScript != null)
                {
                    // Add the key to the dictionary
                    pianoKeysDict.Add(i, keyScript);
                }
                else
                {
                    Debug.LogError("PianoKey script not found on " + keyName);
                }
            }
            else
            {
                Debug.LogError("Key " + keyName + " not found in the hierarchy.");
            }
        }
    }

    // Method to check if all specified keys are pressed
    bool CheckKeysPressed(int[] keyNumbersToCheck)
    {
        allKeysPressed = true; // Start assuming all keys are pressed

        // Loop through the array of key numbers to check
        foreach (int keyNumber in keyNumbersToCheck)
        {
            if (pianoKeysDict.TryGetValue(keyNumber, out PianoKey keyScript))
            {
                // Check if the key is pressed
                if (!keyScript.isPressed)
                {
                    allKeysPressed = false;
                    break; // Exit early if any key is not pressed
                }
            }
            else
            {
                Debug.LogError("Key number " + keyNumber + " not found in the dictionary.");
                allKeysPressed = false;
                break;
            }
        }

        // Print the result
        Debug.Log("All specified keys pressed: " + allKeysPressed);
        return allKeysPressed;
    }
}
