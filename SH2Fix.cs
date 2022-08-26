using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SH2Fix
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class SH2Fix : BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> bUltrawideFixes;
        public static ConfigEntry<bool> bIntroSkip;
        public static ConfigEntry<bool> bFOVAdjust;
        public static ConfigEntry<float> fAdditionalFOV;
        public static ConfigEntry<float> fUpdateRate;
        public static ConfigEntry<bool> bCustomResolution;
        public static ConfigEntry<float> fDesiredResolutionX;
        public static ConfigEntry<float> fDesiredResolutionY;
        public static ConfigEntry<int> iWindowMode;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Features
            bUltrawideFixes = Config.Bind("Ultrawide UI Fixes",
                                "UltrawideFixes",
                                true,
                                "Set to true to enable ultrawide UI fixes.");

            fUpdateRate = Config.Bind("General",
                                "PhysicsUpdateRate",
                                (float)0f, // 0 = Auto (Set to refresh rate) || Default = 50
                                new ConfigDescription("Set desired update rate. This will improve camera smoothness in particular. \n0 = Auto (Set to refresh rate). Game default = 50",
                                new AcceptableValueRange<float>(0f, 5000f)));

            bIntroSkip = Config.Bind("General",
                                "IntroSkip",
                                 true,
                                "Skip intro logos.");

            // Game Overrides
            bFOVAdjust = Config.Bind("FOV Adjustment",
                                "FOVAdjustment",
                                true, // True by default to enable Vert+ for narrow aspect ratios.
                                "Set to true to enable adjustment of the FOV. \nIt will also adjust the FOV to be Vert+ if your aspect ratio is narrower than 16:9.");

            fAdditionalFOV = Config.Bind("FOV Adjustment",
                                "AdditionalFOV.Value",
                                (float)0f,
                                new ConfigDescription("Set additional FOV in degrees. This does not adjust FOV in cutscenes.",
                                new AcceptableValueRange<float>(0f, 180f)));

            // Custom Resolution
            bCustomResolution = Config.Bind("Set Custom Resolution",
                                "CustomResolution",
                                 false, // Disable by default as launcher should suffice.
                                "Set to true to enable the custom resolution below.");

            fDesiredResolutionX = Config.Bind("Set Custom Resolution",
                                "ResolutionWidth",
                                (float)Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default.
                                "Set desired resolution width.");

            fDesiredResolutionY = Config.Bind("Set Custom Resolution",
                                "ResolutionHeight",
                                (float)Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default.
                                "Set desired resolution height.");

            iWindowMode = Config.Bind("Set Custom Resolution",
                                "WindowMode",
                                 (int)1,
                                new ConfigDescription("Set window mode. 1 = Fullscreen, 2 = Borderless, 3 = Windowed.",
                                new AcceptableValueRange<int>(1, 3)));

            // Run UltrawidePatches
            if (bUltrawideFixes.Value)
            {
                Harmony.CreateAndPatchAll(typeof(UltrawidePatches));
            }

        }

        [HarmonyPatch]
        public class UltrawidePatches
        {
            public static float DefaultAspectRatio = (float)16 / 9;
            public static float NewAspectRatio = (float)Screen.width / Screen.height; // This is only calculated on startup. Potential issue.
            public static float AspectMultiplier = NewAspectRatio / DefaultAspectRatio;
            public static float AspectDivider = DefaultAspectRatio / NewAspectRatio;

            public static GameObject LetterboxingUp;
            public static GameObject LetterboxingDown;
            public static GameObject LetterboxingLeft;
            public static GameObject LetterboxingRight;

            // Set screen match mode when object has canvas scaler enabled
            [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
            [HarmonyPostfix]
            public static void SetScreenMatchMode(CanvasScaler __instance)
            {
                if (NewAspectRatio > DefaultAspectRatio || NewAspectRatio < DefaultAspectRatio)
                {
                    __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }
            }

            // Letterboxing
            [HarmonyPatch(typeof(Game.UI.System.LetterBox), nameof(Game.UI.System.LetterBox.updateBlack))]
            [HarmonyPostfix]
            public static void DisableLetterboxing(Game.UI.System.LetterBox __instance)
            {
                LetterboxingUp = __instance.Up.gameObject;
                LetterboxingDown = __instance.Down.gameObject; 
                LetterboxingLeft = __instance.Left.gameObject;
                LetterboxingRight = __instance.Right.gameObject;

                LetterboxingUp.gameObject.SetActive(false);
                LetterboxingDown.gameObject.SetActive(false);
                LetterboxingLeft.gameObject.SetActive(false);
                LetterboxingRight.SetActive(false);

                Log.LogInfo($"Disabled letterboxing.");
                
            }
        }
    }
}

