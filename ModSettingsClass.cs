using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppInterop.Runtime;
using MelonLoader;
using Il2CppTMPro;
using Il2Cpp;
using UnityEngine.SceneManagement;

namespace Mod_Settings
{
    public class ModSettingsClass : MelonMod
    {
        bool debugMode = false;
        bool freezeMenuControls = false;

        private GameObject originalSettingsButton;
        private GameObject settingsButton;

        private GameObject settingsMenuPrefab;
        private GameObject settingsMenu;
        private GameObject settingsMenuContent;
        private TMP_Text settingsMenuTitle;
        private TMP_Text settingsMenuDescription;
        private GameObject buttonPlaceholder;

        private RectTransform rectSettingsButton;
        private TMP_Text textSettingButton;

        private MelonPreferences_Category modSettingsPreferencesCategory;
        private MelonPreferences_Entry<bool> exampleFirstEntry;
        private MelonPreferences_Entry<int> exampleSecondEntry;
        private MelonPreferences_Entry<string> customModSettingsDescription;

        private MelonPreferences_Entry currentEntry;
        private bool isInputOn = false;
        private string currentInput = "";
        private TMP_Text currentButtonText;

        private GameObject mainUI;


        public override void OnInitializeMelon()
        {
            modSettingsPreferencesCategory = MelonPreferences.CreateCategory("Mod Configurator");
            exampleFirstEntry = modSettingsPreferencesCategory.CreateEntry<bool>("FirstEntry", true, "First Entry");
            exampleSecondEntry = modSettingsPreferencesCategory.CreateEntry<int>("SecondEntry", 5, "Second Entry");
            customModSettingsDescription = modSettingsPreferencesCategory.CreateEntry<string>("CustomModSettingsDescription", "Test entries that do nothing.\nIf you are a mod developer add\nCustomModSettingsDescription entry\nto your category to get\na custom description in this tab");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Scene_Title")
            {
                modSettingsPreferencesCategory.LoadFromFile();

                originalSettingsButton = GameObject.Find("/__UI/Canvas_UI/MainUI/ExtraOptions/OptionsButton");
                settingsButton = GameObject.Instantiate(originalSettingsButton);
                settingsButton.transform.SetParent(GameObject.Find("/__UI/Canvas_UI").transform);
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

                mainUI = GameObject.Find("/__UI/Canvas_UI/MainUI");

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

            if (mainUI != null && settingsButton != null)
            {
                if (mainUI.active && !settingsButton.active) settingsButton.active = true;
                else if (!mainUI.active && settingsButton.active) settingsButton.active = false;
            }
        }

        private void OnSettingsButtonClicked()
        {
            GameObject.Destroy(GameObject.Find("/__UI/Canvas_UI/UI_ScrollingMenu_Base(Clone)"));
            if (settingsMenuPrefab != null)
            {
                settingsMenu = GameObject.Instantiate(settingsMenuPrefab, GameObject.Find("/__UI/Canvas_UI").transform);
                if (debugMode) PrintAllComponents(settingsMenu);
                if (freezeMenuControls) settingsMenu.GetComponent<FocusedPlayerActions>().enabled = false;

                settingsMenuContent = settingsMenu.transform.GetChild(1).GetChild(0).GetChild(0).gameObject;

                settingsMenuTitle = settingsMenuContent.transform.FindChild("Title").GetComponent<TMP_Text>();
                settingsMenuTitle.text = "mod settings";

                settingsMenuDescription = settingsMenuContent.transform.FindChild("Description").GetComponent<TMP_Text>();
                settingsMenuDescription.text = "configure your mods here";

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
                textButton.text = category.DisplayName.ToLower();

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
                modSettingsMenu = GameObject.Instantiate(settingsMenuPrefab, GameObject.Find("/__UI/Canvas_UI").transform);
                if (freezeMenuControls) modSettingsMenu.GetComponent<FocusedPlayerActions>().enabled = false;

                modSettingsMenuContent = modSettingsMenu.transform.GetChild(1).GetChild(0).GetChild(0).gameObject;

                modSettingsMenuTitle = modSettingsMenuContent.transform.FindChild("Title").GetComponent<TMP_Text>();
                modSettingsMenuTitle.text = category.DisplayName.ToLower();

                modSettingsMenuDescription = modSettingsMenuContent.transform.FindChild("Description").GetComponent<TMP_Text>();
                if (category.HasEntry("CustomModSettingsDescription")) modSettingsMenuDescription.text = category.GetEntry<string>("CustomModSettingsDescription").Value;
                else modSettingsMenuDescription.text = $"settings for {category.DisplayName.ToLower()} mod";

                modButtonPlaceholder = modSettingsMenuContent.transform.FindChild("Button").gameObject;
                foreach (MelonPreferences_Entry entry in category.Entries)
                {
                    if (entry.Identifier == "CustomModSettingsDescription") continue;
                    GameObject button = GameObject.Instantiate(modButtonPlaceholder, modSettingsMenuContent.transform);
                    button.name = entry.DisplayName + "Button";

                    IconButton buttonToOverride = button.GetComponent<IconButton>();
                    buttonToOverride.onClick.RemoveAllListeners();
                    buttonToOverride.onClick.AddListener((UnityAction)delegate { ChangeEntryContent(entry, button); });

                    TMP_Text textButton = button.GetComponentInChildren<TMP_Text>();
                    textButton.text = entry.DisplayName.ToLower() + " : " + entry.BoxedValue.ToString();
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
                textButton.text = entry.DisplayName.ToLower() + " : " + entry.BoxedValue.ToString();
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

            currentButtonText.text = currentEntry.DisplayName.ToLower() + " : " + currentInput + "|";
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
                    currentButtonText.text = currentEntry.DisplayName.ToLower() + " : " + currentEntry.BoxedValue.ToString();
                    currentEntry = null;
                    currentButtonText = null;
                    currentInput = "";
                }
                else currentInput += c;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                isInputOn = false;
                currentButtonText.text = currentEntry.DisplayName.ToLower() + " : " + currentEntry.BoxedValue.ToString();
                currentEntry = null;
                currentButtonText = null;
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

        /* Really badly written debug functions
        
        public void DumpScenes()
        {
            int sceneCount = SceneManager.sceneCount;

            MelonLogger.Msg($"Loaded scenes: {sceneCount}");

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                MelonLogger.Msg($"========== SCENE: {scene.name} ==========");
                MelonLogger.Msg($"Path: {scene.path}");
                MelonLogger.Msg($"Loaded: {scene.isLoaded}");

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    DumpObject(root, 0);
                }
            }
        }

        private void DumpObject(GameObject obj, int depth)
        {
            if (obj == null)
                return;

            string indent = new string(' ', depth * 2);

            MelonLogger.Msg(
                $"{indent}- {obj.name} " +
                $"[Active: {obj.activeSelf}]"
            );

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);

                if (child != null)
                    DumpObject(child.gameObject, depth + 1);
            }
        }
        */
    }
}
