using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using TMPro; // for TMP_Dropdown
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    // References to your TextMeshPros
    public TextMeshProUGUI mageText;
    public TextMeshProUGUI priestText;
    public TextMeshProUGUI rangerText;
    public TextMeshProUGUI fighterText;
    public TMP_Dropdown difficultyDropdown;
    public TMP_Dropdown enemyType;


    public int map = 0;

    public AudioMixer audioMixer;

    public TMP_Dropdown[] dropdowns; // Array of your dropdowns.
    public int totalPoints = 18; // Total available points.

    private int[] pointsAllocated; // Points allocated to each dropdown.

    private void Start()
    {
        // Initially disable all TextMeshPros
        mageText.enabled = false;
        priestText.enabled = false;
        rangerText.enabled = false;
        fighterText.enabled = false;

        pointsAllocated = new int[dropdowns.Length]; // Initialize the array.

        // Initialize the dropdowns.
        for (int i = 0; i < dropdowns.Length; i++)
        {
            int index = i; // Important to create a new variable in the loop for the delegate.

            // Add listener for when the dropdown value is changed.
            dropdowns[i].onValueChanged.AddListener(delegate { OnDropdownValueChanged(index); });

            // Initialize dropdown options.
            UpdateDropdownOptions(dropdowns[i], totalPoints);
        }

        difficultyDropdown.onValueChanged.AddListener(OnDifficultyDropdownValueChanged);
        enemyType.onValueChanged.AddListener(OnEnemyTypeDropdownValueChanged);

    }

    private void OnDropdownValueChanged(int index)
    {
        // Calculate total points allocated.
        int totalAllocated = 0;
        for (int i = 0; i < pointsAllocated.Length; i++)
        {
            if (i == index)
                pointsAllocated[i] = dropdowns[i].value; // Update the points allocated to the current dropdown.

            totalAllocated += pointsAllocated[i];
        }

        // Calculate the remaining points.
        int remainingPoints = totalPoints - totalAllocated;

        // Update the options of the other dropdowns.
        for (int i = 0; i < dropdowns.Length; i++)
        {
            if (i != index) // Skip the current dropdown.
                UpdateDropdownOptions(dropdowns[i], remainingPoints + pointsAllocated[i]);
        }
    }

    private void UpdateDropdownOptions(TMP_Dropdown dropdown, int maxPoints)
    {
        // Clear the existing options.
        dropdown.options.Clear();

        // Add new options.
        for (int i = 0; i <= maxPoints; i++)
            dropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", volume);
    }

    public void OnMageButtonClicked()
    {
        mageText.enabled = true;
        priestText.enabled = false;
        rangerText.enabled = false;
        fighterText.enabled = false;
    }

    public void OnPriestButtonClicked()
    {
        mageText.enabled = false;
        priestText.enabled = true;
        rangerText.enabled = false;
        fighterText.enabled = false;
    }

    public void OnRangerButtonClicked()
    {
        mageText.enabled = false;
        priestText.enabled = false;
        rangerText.enabled = true;
        fighterText.enabled = false;
    }

    public void OnFighterButtonClicked()
    {
        mageText.enabled = false;
        priestText.enabled = false;
        rangerText.enabled = false;
        fighterText.enabled = true;
    }

    public void OnSaveButtonClicked()
    {
        if (mageText.enabled)
        {
            SaveDropdownValues("Mage");
        }
        else if (priestText.enabled)
        {
            SaveDropdownValues("Priest");
        }
        else if (rangerText.enabled)
        {
            SaveDropdownValues("Ranger");
        }
        else if (fighterText.enabled)
        {
            SaveDropdownValues("Fighter");
        }
    }

    public void OnForestButtonClicked()
    {
        GlobalVariables.map = 1;
    }
    public void OnSnowForestButtonClicked()
    {
        GlobalVariables.map = 2;
    }
    public void OnSavanaButtonClicked()
    {
        GlobalVariables.map = 3;
    }
    public void OnTropicalButtonClicked()
    {
        GlobalVariables.map = 4;
    }




    public void SaveDropdownValues(string cClass)
    {
        GlobalVariables.characterAttributes attributes;

        attributes.cClass = cClass;
        int[] myArray = new int[5];
        for (int i = 0; i < dropdowns.Length; i++)
        {
            myArray[i] = dropdowns[i].value;
        }
        attributes.dexteriry = myArray[0];
        attributes.constitution = myArray[1];
        attributes.charisma = myArray[2];
        attributes.stength = myArray[3];
        attributes.vitality = myArray[4];

        GlobalVariables.characters.Add(attributes);

    }

    public void deleteSavedCharacters()
    {
        GlobalVariables.characters.Clear();
    }

    public void ResetDropdowns()
    {
        for (int i = 0; i < dropdowns.Length; i++)
        {
            dropdowns[i].value = 0;
        }

        // Reset points allocated array and reinitialize dropdowns
        pointsAllocated = new int[dropdowns.Length];
        for (int i = 0; i < dropdowns.Length; i++)
        {
            UpdateDropdownOptions(dropdowns[i], totalPoints);
        }
    }

    private void OnDifficultyDropdownValueChanged(int value)
    {
        // value will be 0 for "Easy", 1 for "Moderate", and 2 for "Hard".
        switch (value)
        {
            case 0:
                GlobalVariables.difficulty = "Easy";
                break;
            case 1:
                GlobalVariables.difficulty = "Moderate";
                break;
            case 2:
                GlobalVariables.difficulty = "Hard";
                break;
        }
    }
    private void OnEnemyTypeDropdownValueChanged(int value)
    {
        // value will be 0 for "Autonomous", 1 for "Manual".
        switch (value)
        {
            case 0:
                GlobalVariables.enemyType = "Autonomous";
                break;
            case 1:
                GlobalVariables.enemyType = "Manual";
                break;
        }
    }

}
