using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;

public class Profile : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI genderText;
    [SerializeField] TextMeshProUGUI dobText;

    [Header("Name Edit")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button nameDoneButton;

    [Header("Gender Edit")]
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private Button otherButton;

    [Header("DOB Edit")]
    [SerializeField] private TMP_InputField dobInputField;
    [SerializeField] private Button dobDoneButton;
    [SerializeField] private TMP_Text dobErrorText;


    private void Awake()
    {
        nameDoneButton.onClick.AddListener(() =>
        {
            // DataManager.Instance.SetInfo("name", nameInputField.text);
            // UpdateDisplayFields();
            UpdateName(nameInputField.text);
        });

        maleButton.onClick.AddListener(() =>
            UpdateGender("Male")
        );
        femaleButton.onClick.AddListener(() =>
            UpdateGender("Female")
        );
        otherButton.onClick.AddListener(() =>
            UpdateGender("Other")
        );

        // dobDoneButton.onClick.AddListener(() =>
        // {
        //     // DataManager.Instance.SetInfo("dob", dobInputField.text);
        //     // UpdateDisplayFields();
        //     UpdateDob(dobInputField.text);
        // });
        dobDoneButton.onClick.AddListener(ValidateAndSaveDob);

        // Add input validation while typing
        dobInputField.onValueChanged.AddListener(_ => ValidateDobFormat());
    }

    private void OnEnable()
    {
        UpdateDisplayFields();
    }

    private void UpdateDisplayFields()
    {
        nameText.text = DataManager.Instance.GetInfo<string>("name", "Not Set");
        genderText.text = DataManager.Instance.GetInfo<string>("gender", "Not Set");
        dobText.text = DataManager.Instance.GetInfo<string>("dob", "Not Set");
    }

    public void OpenNameEdit()
    {
        nameInputField.text = DataManager.Instance.GetInfo<string>("name", "");
        nameInputField.Select();
        nameInputField.ActivateInputField();
    }

    public void OpenDobEdit()
    {
        dobInputField.text = DataManager.Instance.GetInfo<string>("dob", "");
        dobInputField.Select();
        dobInputField.ActivateInputField();
    }
    private void UpdateName(string name)
    {
        DataManager.Instance.SetInfo("name", name);
        UpdateDisplayFields();
    }
    private void UpdateDob(string dob)
    {
        DataManager.Instance.SetInfo("dob", dob);
        UpdateDisplayFields();
    }

    private void UpdateGender(string gender)
    {
        DataManager.Instance.SetInfo("gender", gender);
        UpdateDisplayFields();
    }

    public void UpdateUsername(string newUsername)
    {
        DataManager.Instance.SetUserName(newUsername);
        DataManager.Instance.SetInfo("name", newUsername);
        UpdateDisplayFields();
    }

    private void ValidateDobFormat()
    {
        string input = dobInputField.text;
        bool isValid = Regex.IsMatch(input, @"^\d{2}/\d{2}/\d{4}$|^$");
        
        dobErrorText.gameObject.SetActive(!isValid && !string.IsNullOrEmpty(input));
        dobErrorText.text = "Format: DD/MM/YYYY";
        dobDoneButton.interactable = isValid;
    }

    private bool IsValidDob(string dob)
    {
        if (string.IsNullOrEmpty(dob)) return false;
        
        try
        {
            DateTime date = DateTime.ParseExact(dob, "dd/MM/yyyy", null);
            return date <= DateTime.Today;
        }
        catch
        {
            return false;
        }
    }

    private void ValidateAndSaveDob()
    {
        string dob = dobInputField.text;
        
        if (IsValidDob(dob))
        {
            DataManager.Instance.SetInfo("dob", dob);
            UpdateDisplayFields();
        }
        else
        {
            dobErrorText.gameObject.SetActive(true);
            dobErrorText.text = "Invalid date! Use DD/MM/YYYY";
        }
    }
}
