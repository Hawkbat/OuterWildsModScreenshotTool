using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScreenshotTool
{
    public class ScreenshotTool : ModBehaviour
    {
        const string CONF_SHOW_QUICK_SETTINGS = "Show Quick Settings";
        const string CONF_SCREENSHOT_KEY = "Screenshot Key";
        const string CONF_SCREENSHOT_MODE = "Screenshot Mode";
        const string CONF_CUSTOM_WIDTH = "Custom Width";
        const string CONF_CUSTOM_HEIGHT = "Custom Height";
        const string CONF_CHANGE_FIELD_OF_VIEW = "Change Field of View";
        const string CONF_FIELD_OF_VIEW = "Field of View";
        const string CONF_GREYSCALE_MODE = "Greyscale Mode";

        bool showQuickSettings = true;
        Key screenshotKey = Key.K;
        ScreenshotMode screenshotMode = ScreenshotMode.ShipLog;
        int customWidth = 512;
        int customHeight = 512;
        bool changeFieldOfView = true;
        float fieldOfView = 120f;
        GreyscaleMode greyscaleMode = GreyscaleMode.Maximum;

        bool takingScreenshot = false;
        float quickSettingsVisibilityMessageTimer = 0f;

        private bool IsInGame =>
            LoadManager.GetCurrentScene() == OWScene.SolarSystem || LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse;

        public override void Configure(IModConfig config)
        {
            showQuickSettings = config.GetSettingsValue<bool>(CONF_SHOW_QUICK_SETTINGS);
            screenshotKey = config.GetSettingsValue<Key>(CONF_SCREENSHOT_KEY);
            screenshotMode = config.GetSettingsValue<ScreenshotMode>(CONF_SCREENSHOT_MODE);
            customWidth = config.GetSettingsValue<int>(CONF_CUSTOM_WIDTH);
            customHeight = config.GetSettingsValue<int>(CONF_CUSTOM_HEIGHT);
            changeFieldOfView = config.GetSettingsValue<bool>(CONF_CHANGE_FIELD_OF_VIEW);
            fieldOfView = config.GetSettingsValue<float>(CONF_FIELD_OF_VIEW);
            greyscaleMode = config.GetSettingsValue<GreyscaleMode>(CONF_GREYSCALE_MODE);
        }

        private void Update()
        {
            if (takingScreenshot || !IsInGame) return;
            if (Keyboard.current[screenshotKey].wasPressedThisFrame)
            {
                TakeShipLogScreenshot();
            }
        }

        private void OnGUI()
        {
            if (takingScreenshot || !IsInGame) return;
            GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            if (showQuickSettings)
            {
                if (BoolField(ref showQuickSettings, CONF_SHOW_QUICK_SETTINGS))
                {
                    quickSettingsVisibilityMessageTimer = Time.realtimeSinceStartup + 5f;
                }
                EnumTextField(ref screenshotKey, CONF_SCREENSHOT_KEY);
                EnumSelectionField(ref screenshotMode, CONF_SCREENSHOT_MODE);
                if (screenshotMode == ScreenshotMode.Custom)
                {
                    IntField(ref customWidth, CONF_CUSTOM_WIDTH);
                    IntField(ref customHeight, CONF_CUSTOM_HEIGHT);
                }
                BoolField(ref changeFieldOfView, CONF_CHANGE_FIELD_OF_VIEW);
                if (changeFieldOfView)
                {
                    FloatField(ref fieldOfView, CONF_FIELD_OF_VIEW);
                }
                EnumSelectionField(ref greyscaleMode, CONF_GREYSCALE_MODE);
            }
            else if (Time.realtimeSinceStartup < quickSettingsVisibilityMessageTimer)
            {
                GUILayout.Label("Settings can be re-enabled or changed from the mod menu.");
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        readonly Dictionary<string, string> fieldStringValues = new();
        readonly Dictionary<string, int> intFieldValues = new();
        readonly Dictionary<string, float> floatFieldValues = new();
        readonly Dictionary<string, object> enumFieldValues = new();
        readonly Dictionary<string, string[]> enumNamesCache = new();
        readonly Dictionary<string, Array> enumValuesCache = new();

        private bool BoolField(ref bool value, string settingName)
        {
            var newValue = GUILayout.Toggle(value, settingName);
            if (newValue != value)
            {
                UpdateConfigValue(settingName, newValue);
                value = newValue;
                return true;
            }
            return false;
        }

        private bool IntField(ref int value, string settingName)
        {
            if (!intFieldValues.ContainsKey(settingName) || intFieldValues[settingName] != value)
            {
                intFieldValues[settingName] = value;
                fieldStringValues[settingName] = value.ToString();
            }
            GUILayout.Label(settingName);
            var newStringValue = GUILayout.TextField(fieldStringValues[settingName]);
            if (int.TryParse(newStringValue, out var newValue) && newValue != value)
            {
                intFieldValues[settingName] = newValue;
                fieldStringValues[settingName] = newValue.ToString();
                UpdateConfigValue(settingName, newValue);
                value = newValue;
                return true;
            }
            else
            {
                fieldStringValues[settingName] = newStringValue;
            }
            return false;
        }

        private bool FloatField(ref float value, string settingName)
        {
            if (!floatFieldValues.ContainsKey(settingName) || floatFieldValues[settingName] != value)
            {
                floatFieldValues[settingName] = value;
                fieldStringValues[settingName] = value.ToString();
            }
            GUILayout.Label(settingName);
            var newStringValue = GUILayout.TextField(fieldStringValues[settingName]);
            if (float.TryParse(newStringValue, out var newValue) && newValue != value)
            {
                floatFieldValues[settingName] = newValue;
                fieldStringValues[settingName] = newValue.ToString();
                UpdateConfigValue(settingName, newValue);
                value = newValue;
                return true;
            }
            else
            {
                fieldStringValues[settingName] = newStringValue;
            }
            return false;
        }

        private bool EnumTextField<T>(ref T value, string settingName) where T : struct, Enum
        {
            if (!enumFieldValues.ContainsKey(settingName) || !enumFieldValues[settingName].Equals(value))
            {
                enumFieldValues[settingName] = value;
                fieldStringValues[settingName] = value.ToString();
            }
            GUILayout.Label(settingName);
            var newStringValue = GUILayout.TextField(fieldStringValues[settingName]);
            if (Enum.TryParse<T>(newStringValue, out var newValue) && !newValue.Equals(value))
            {
                enumFieldValues[settingName] = newValue;
                fieldStringValues[settingName] = newValue.ToString();
                UpdateConfigValue(settingName, newValue.ToString());
                value = newValue;
                return true;
            }
            else
            {
                fieldStringValues[settingName] = newStringValue;
            }
            return false;
        }

        private bool EnumSelectionField<T>(ref T value, string settingName) where T : struct, Enum
        {
            GUILayout.Label(settingName);
            if (!enumNamesCache.TryGetValue(typeof(T).FullName, out var names))
            {
                names = Enum.GetNames(typeof(T));
                enumNamesCache[typeof(T).FullName] = names;
            }
            if (!enumValuesCache.TryGetValue(typeof(T).FullName, out var values))
            {
                values = Enum.GetValues(typeof(T));
                enumValuesCache[typeof(T).FullName] = values;
            }
            var valueIndex = Array.IndexOf(values, value);
            var newIndex = GUILayout.SelectionGrid(valueIndex, names, names.Length);
            var newValue = (T)values.GetValue(newIndex);
            if (!newValue.Equals(value))
            {
                UpdateConfigValue(settingName, newValue.ToString());
                value = newValue;
                return true;
            }
            return false;
        }

        private void UpdateConfigValue(string key, object value)
        {
            ModHelper.Console.WriteLine($"Updated config value \"{key}\" to \"{value.ToString().Replace("\"", "\\\"")}\"");
            ModHelper.Config.SetSettingsValue(key, value);
            ModHelper.Storage.Save(ModHelper.Config, "config.json");
            var modMenu = ModHelper.Menus.ModsMenu.GetModMenu(this);
            modMenu.UpdateUIValues();
        }

        private void TakeShipLogScreenshot()
        {
            StartCoroutine(DoTakeShipLogScreenshot());
        }

        private IEnumerator DoTakeShipLogScreenshot()
        {
            takingScreenshot = true;
            var previousRenderMode = GUIMode._renderMode;
            GUIMode.SetRenderMode(GUIMode.RenderMode.Hidden);
            if (changeFieldOfView)
            {
                Locator.GetPlayerCameraController().SnapToFieldOfView(fieldOfView, 0.5f);
                yield return new WaitForSeconds(0.5f);
            }
            yield return null;
            yield return new WaitForEndOfFrame();
            var w = Screen.width;
            var h = Screen.height;
            switch (screenshotMode)
            {
                case ScreenshotMode.ShipLog:
                    w = h = 512;
                    break;
                case ScreenshotMode.SlideReel:
                    w = h = 1024;
                    break;
                case ScreenshotMode.Custom:
                    w = customWidth;
                    h = customHeight;
                    break;
            }
            var x = (Screen.width - w) / 2f;
            var y = (Screen.height - h) / 2f;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
            tex.Apply();
            var colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                switch (greyscaleMode)
                {
                    case GreyscaleMode.None:
                        break;
                    case GreyscaleMode.Luminance:
                        var intensity = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                        c.r = c.g = c.b = intensity;
                        break;
                    case GreyscaleMode.Average:
                        var average = (c.r + c.g + c.b) / 3f;
                        c.r = c.g = c.b = average;
                        break;
                    case GreyscaleMode.Maximum:
                        var maximum = Mathf.Max(c.r, c.g, c.b);
                        c.r = c.g = c.b = maximum;
                        break;
                }
                colors[i] = c;
            }
            tex.SetPixels(colors);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            var path = $"{ModHelper.Manifest.ModFolderPath}Screenshots/{DateTime.Now:yyyy-MM-dd HH-mm-ss}.png";
            path = System.IO.Path.GetFullPath(path);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, bytes);
            ModHelper.Console.WriteLine($"Saved screenshot to {path}");
            Locator.GetPlayerAudioController().PlayProbeSnapshot();
            if (changeFieldOfView)
            {
                Locator.GetPlayerCameraController().SnapToInitFieldOfView(0.5f);
            }
            GUIMode.SetRenderMode(previousRenderMode);
            takingScreenshot = false;
        }

        private enum ScreenshotMode
        {
            Full = 0,
            ShipLog = 1,
            SlideReel = 2,
            Custom = 3,
        }

        private enum GreyscaleMode
        {
            None = 0,
            Luminance = 1,
            Average = 2,
            Maximum = 3,
        }
    }

}
