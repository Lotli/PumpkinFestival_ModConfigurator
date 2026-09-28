using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppInterop.Runtime;
using MelonLoader;
using Il2CppTMPro;
using Il2Cpp;

namespace Mod_Settings
{
    public class ModSettingsClass : MelonMod
    {
        bool debugMode = false;
        bool freezeMenuControls = false;

        GameObject originalSettingsButton;
        GameObject settingsButton;

        GameObject settingsMenuPrefab;
        GameObject settingsMenu;
        GameObject settingsMenuContent;
        TMP_Text settingsMenuTitle;
        TMP_Text settingsMenuDescription;
        GameObject buttonPlaceholder;

        RectTransform rectSettingsButton;
        TMP_Text textSettingButton;

        private MelonPreferences_Category modSettingsPreferencesCategory;
        private MelonPreferences_Entry<bool> exampleFirstEntry;
        private MelonPreferences_Entry<int> exampleSecondEntry;

        private MelonPreferences_Entry currentEntry;
        private bool isInputOn = false;
        private string currentInput = "";
        private TMP_Text currentButtonText;

        public override void OnInitializeMelon()
        {
            MelonEvents.OnGUI.Subscribe(DrawMenu, 100);
            modSettingsPreferencesCategory = MelonPreferences.CreateCategory("Mod Configurator");
            exampleFirstEntry = modSettingsPreferencesCategory.CreateEntry<bool>("FirstEntry", true);
            exampleSecondEntry = modSettingsPreferencesCategory.CreateEntry<int>("SecondEntry", 5);
        }

        private void DrawMenu()
        {
            //GUI.Box(new Rect(10, 10, 150, 40), "Boat Glider\n By Lotli");
            //flyingForce = float.Parse(GUI.TextArea(new Rect(10, 40, 150, 20), "1000"));
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "PumpkinPickerScene")
            {
                modSettingsPreferencesCategory.LoadFromFile();

                originalSettingsButton = GameObject.Find("/UI/UICanvas/MainUI/ExtraOptions/OptionsButton");
                settingsButton = GameObject.Instantiate(originalSettingsButton);
                settingsButton.transform.SetParent(GameObject.Find("/UI/UICanvas").transform);
                settingsButton.name = "ModSettingsButton";
                settingsButton.transform.localPosition = new Vector3(0, 0, 0);
                settingsButton.transform.localRotation = Quaternion.EulerAngles(0, 0, 0);

                rectSettingsButton = settingsButton.GetComponent<RectTransform>();
                rectSettingsButton.anchorMin = new Vector2(1f, 0f);
                rectSettingsButton.anchorMax = new Vector2(1f, 0f);
                rectSettingsButton.pivot = new Vector2(1f, 0f);
                rectSettingsButton.anchoredPosition = new Vector2(-30f, 30f);

                textSettingButton = settingsButton.GetComponentInChildren<TMP_Text>();
                textSettingButton.text = "Mod Settings";

                foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (obj != null && obj.name == "UI_ScrollingMenu_Base")
                    {
                        settingsMenuPrefab = obj;
                    }
                }

                IconButton buttonToOverride = settingsButton.GetComponent<IconButton>();
                buttonToOverride.onClick.RemoveAllListeners();
                buttonToOverride.onClick.AddListener((UnityAction)OnSettingsButtonClicked);
            }
        }

        public override void OnUpdate()
        {
            ProcessInput();
        }

        private void OnSettingsButtonClicked()
        {
            GameObject.Destroy(GameObject.Find("/UI/UICanvas/UI_ScrollingMenu_Base(Clone)"));
            if (settingsMenuPrefab != null)
            {
                settingsMenu = GameObject.Instantiate(settingsMenuPrefab, GameObject.Find("/UI/UICanvas").transform);
                if (debugMode) PrintAllComponents(settingsMenu);
                if (freezeMenuControls) settingsMenu.GetComponent<FocusedPlayerActions>().enabled = false;

                settingsMenuContent = settingsMenu.transform.GetChild(1).GetChild(0).GetChild(0).gameObject;

                settingsMenuTitle = settingsMenuContent.transform.FindChild("Title").GetComponent<TMP_Text>();
                settingsMenuTitle.text = "Mod Settings";

                settingsMenuDescription = settingsMenuContent.transform.FindChild("Description").GetComponent<TMP_Text>();
                settingsMenuDescription.text = "Configure your mods here";

                buttonPlaceholder = settingsMenuContent.transform.FindChild("Button").gameObject;
                SpawnModCategoryButtons();
                buttonPlaceholder.SetActive(false);
            }
        }

        void PrintAllComponents(GameObject obj)
        {
            foreach (Component component in obj.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                LoggerInstance.Msg(
                    $"{component.gameObject.name} | {component.GetIl2CppType().FullName}"
                );
            }
            foreach (Component component in obj.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                    continue;

                LoggerInstance.Msg(
                    $"{component.gameObject.name} | {component.GetIl2CppType().FullName}"
                );
            }
        }

        void SpawnModCategoryButtons()
        {
            foreach (MelonPreferences_Category category in MelonPreferences.Categories)
            {
                GameObject button = GameObject.Instantiate(buttonPlaceholder, settingsMenuContent.transform);
                button.name = category.DisplayName + "Button";

                TMP_Text textButton = button.GetComponentInChildren<TMP_Text>();
                textButton.text = category.DisplayName;

                IconButton buttonToOverride = button.GetComponent<IconButton>();
                buttonToOverride.onClick.RemoveAllListeners();
                buttonToOverride.onClick.AddListener((UnityAction)delegate { SpawnSettingsForCategory(category); });
            }
        }

        void SpawnSettingsForCategory(MelonPreferences_Category category)
        {
            GameObject modSettingsMenu;
            GameObject modSettingsMenuContent;

            TMP_Text modSettingsMenuTitle;
            TMP_Text modSettingsMenuDescription;
            GameObject modButtonPlaceholder;

            if (settingsMenuPrefab != null)
            {
                modSettingsMenu = GameObject.Instantiate(settingsMenuPrefab, GameObject.Find("/UI/UICanvas").transform);
                if (freezeMenuControls) modSettingsMenu.GetComponent<FocusedPlayerActions>().enabled = false;

                modSettingsMenuContent = modSettingsMenu.transform.GetChild(1).GetChild(0).GetChild(0).gameObject;

                modSettingsMenuTitle = modSettingsMenuContent.transform.FindChild("Title").GetComponent<TMP_Text>();
                modSettingsMenuTitle.text = category.DisplayName;

                modSettingsMenuDescription = modSettingsMenuContent.transform.FindChild("Description").GetComponent<TMP_Text>();
                if (category.HasEntry("CustomModSettingsDescription")) modSettingsMenuDescription.text = category.GetEntry<string>("CustomModSettingsDescription").Value;
                else modSettingsMenuDescription.text = $"Settings for {category.DisplayName} mod";

                modButtonPlaceholder = modSettingsMenuContent.transform.FindChild("Button").gameObject;
                foreach (MelonPreferences_Entry entry in category.Entries)
                {
                    GameObject button = GameObject.Instantiate(modButtonPlaceholder, modSettingsMenuContent.transform);
                    button.name = entry.DisplayName + "Button";

                    IconButton buttonToOverride = button.GetComponent<IconButton>();
                    buttonToOverride.onClick.RemoveAllListeners();
                    buttonToOverride.onClick.AddListener((UnityAction)delegate { ChangeEntryContent(entry, button); });

                    TMP_Text textButton = button.GetComponentInChildren<TMP_Text>();
                    textButton.text = entry.DisplayName + " : " + entry.BoxedValue.ToString();
                }
                modButtonPlaceholder.SetActive(false);
            }
        }

        void ChangeEntryContent(MelonPreferences_Entry entry, GameObject button)
        {
            if (entry.BoxedValue.GetType() == typeof(bool))
            {
                entry.Category.GetEntry<bool>(entry.Identifier).Value = !entry.Category.GetEntry<bool>(entry.Identifier).Value;
                TMP_Text textButton = button.GetComponentInChildren<TMP_Text>();
                textButton.text = entry.DisplayName + " : " + entry.BoxedValue.ToString();
            }
            else
            {
                isInputOn = true;
                currentEntry = entry;
                currentButtonText = button.GetComponentInChildren<TMP_Text>();
            }
        }

        void ProcessInput()
        {
            if (!isInputOn) return;

            currentButtonText.text = currentEntry.DisplayName + " : " + currentInput + "|";
            foreach (char c in Input.inputString)
            {
                if (c == '\b')
                {
                    if (currentInput.Length > 0) currentInput.Substring(0, currentInput.Length - 1);
                }
                else if (c == '\r' || c == '\n')
                {
                    isInputOn = false;
                    currentEntry.BoxedValue = ConvertString(currentInput, currentEntry.BoxedValue.GetType());
                    currentButtonText.text = currentEntry.DisplayName + " : " + currentEntry.BoxedValue.ToString();
                    currentInput = "";
                }
                else currentInput += c;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                isInputOn = false;
                currentButtonText.text = currentEntry.DisplayName + " : " + currentEntry.BoxedValue.ToString();
                currentInput = "";
            }
        }

        object ConvertString(string input, Type type)
        {
            if (type == typeof(string))
                return input;

            if (type.IsEnum)
                return Enum.Parse(type, input, true);

            if (type == typeof(float))
                return float.Parse(input, System.Globalization.CultureInfo.InvariantCulture);

            if (type == typeof(double))
                return double.Parse(input, System.Globalization.CultureInfo.InvariantCulture);

            return Convert.ChangeType(input, type);
        }
    }
}
