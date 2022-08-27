using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SH2Fix
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class SH2: BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> bUltrawideFixes;
        public static ConfigEntry<bool> bFOVAdjust;
        public static ConfigEntry<float> fAdditionalFOV;

        // Graphics
        public static ConfigEntry<int> iAnisotropicFiltering;
        public static ConfigEntry<float> fLODBias;
        public static ConfigEntry<float> fRenderScale;

        // Custom Resolution
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

            // Game Overrides
            //bFOVAdjust = Config.Bind("FOV Adjustment",
                                //"FOVAdjustment",
                                //true, // True by default to enable Vert+ for narrow aspect ratios.
                                //"Set to true to enable adjustment of the FOV. \nIt will also adjust the FOV to be Vert+ if your aspect ratio is narrower than 16:9.");

            //fAdditionalFOV = Config.Bind("FOV Adjustment",
                                //"AdditionalFOV.Value",
                                //(float)0f,
                                //new ConfigDescription("Set additional FOV in degrees. This does not adjust FOV in cutscenes.",
                                //new AcceptableValueRange<float>(0f, 180f)));

            // Custom Resolution
            //bCustomResolution = Config.Bind("Set Custom Resolution",
                                //"CustomResolution",
                                //false, // Disable by default as launcher should suffice.
                                //"Set to true to enable the custom resolution below.");

            //fDesiredResolutionX = Config.Bind("Set Custom Resolution",
                                //"ResolutionWidth",
                                //(float)Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default.
                                //"Set desired resolution width.");

            //fDesiredResolutionY = Config.Bind("Set Custom Resolution",
                                //"ResolutionHeight",
                                //(float)Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default.
                                //"Set desired resolution height.");

            //iWindowMode = Config.Bind("Set Custom Resolution",
                                //"WindowMode",
                                //(int)1,
                                //new ConfigDescription("Set window mode. 1 = Exclusive Fullscreen, 2 = Fullscreen Windowed, 3 = Maximized Window, 4 = Windowed.",
                                //new AcceptableValueRange<int>(1, 4)));

            // Graphical Settings
            iAnisotropicFiltering = Config.Bind("Graphical Tweaks",
                                "AnisotropicFiltering.Value",
                                (int)16,
                                new ConfigDescription("Set Anisotropic Filtering level. Higher values improve clarity of textures at oblique angles.",
                                new AcceptableValueRange<int>(1, 16)));

            fLODBias = Config.Bind("Graphical Tweaks",
                                "LODBias.Value",
                                (float)1.5f, // Default = 1.5f
                                new ConfigDescription("Set LOD Bias. Controls distance for level of detail switching.",
                                new AcceptableValueRange<float>(0.1f, 10f)));

            fRenderScale = Config.Bind("Graphical Tweaks",
                                "RenderScale.Value",
                                (float)100f, // Default = 100
                                new ConfigDescription("Set Render Scale. Setting higher than 100 provides downsampling for much improved anti-aliasing.",
                                new AcceptableValueRange<float>(10f, 400f)));

            // Run UltrawidePatches
            if (bUltrawideFixes.Value)
            {
                Harmony.CreateAndPatchAll(typeof(UltrawidePatches));
            }
            // Run CustomResolutionPatches
            //if (bCustomResolution.Value)
            //{
                //Harmony.CreateAndPatchAll(typeof(CustomResolutionPatches));
            //}
            // Run FOVPatches
            //if (bFOVAdjust.Value)
            //{
                //Harmony.CreateAndPatchAll(typeof(FOVPatches));
            //}

            Harmony.CreateAndPatchAll(typeof(MiscellaneousPatches));
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

            public static bool bLetterboxPatchHasRun = false;

            // Set screen match mode when object has canvas scaler enabled
            [HarmonyPatch(typeof(CanvasScaler), nameof(CanvasScaler.OnEnable))]
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
                if (!bLetterboxPatchHasRun)
                {
                    LetterboxingUp = __instance.Up.gameObject;
                    LetterboxingDown = __instance.Down.gameObject;
                    LetterboxingLeft = __instance.Left.gameObject;
                    LetterboxingRight = __instance.Right.gameObject;

                    LetterboxingUp.gameObject.SetActive(false);
                    LetterboxingDown.gameObject.SetActive(false);
                    LetterboxingLeft.gameObject.SetActive(false);
                    LetterboxingRight.SetActive(false);

                    bLetterboxPatchHasRun = true;
                    Log.LogInfo($"Disabled letterboxing.");
                }
            }
        }

        [HarmonyPatch]
        public class MiscellaneousPatches
        {
            // Apply custom resolution
            [HarmonyPatch(typeof(Game.Common.ConfigCtrl), nameof(Game.Common.ConfigCtrl.ApplyGraphicsSettings))]
            [HarmonyPostfix]
            public static void PostApplyGraphicsSettings()
            {
                var saveData = Game.Common.ConfigCtrl.s_ConfigData;

                // Anisotropic Filtering
                if (iAnisotropicFiltering.Value > 0)
                {
                    Log.LogInfo($"Old: Anisotropic filtering = {QualitySettings.anisotropicFiltering}");
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    Texture.SetGlobalAnisotropicFilteringLimits(iAnisotropicFiltering.Value, iAnisotropicFiltering.Value);
                    Log.LogInfo($"New: Anisotropic filtering = {QualitySettings.anisotropicFiltering}. Value = {iAnisotropicFiltering.Value}");
                }

                // Render Scale
                if (fRenderScale.Value > 9f)
                {
                    Log.LogInfo($"Old: Game RenderScale = {Game.Common.ConfigCtrl.GetRenderScale()}. URP m_RenderScale = {UniversalRenderPipeline.asset.m_RenderScale}");
                    Game.Common.ConfigCtrl.SetRenderScale((int)fRenderScale.Value);
                    UniversalRenderPipeline.asset.m_RenderScale = fRenderScale.Value / 100;
                    Log.LogInfo($"New: Game RenderScale = {Game.Common.ConfigCtrl.GetRenderScale()}. URP m_RenderScale = {UniversalRenderPipeline.asset.m_RenderScale}");
                }

                // LOD Bias
                if (fLODBias.Value >= 0.1f)
                {
                    Log.LogInfo($"Old: LODBias set to {fLODBias.Value}");
                    QualitySettings.lodBias = fLODBias.Value; // Default = 1.5f    
                    Log.LogInfo($"New: LODBias set to {fLODBias.Value}");
                }

                // MSAA is broken, results in messed up graphics.
                //Log.LogInfo($"antiAliasing = {QualitySettings.antiAliasing}");
                //QualitySettings.antiAliasing = 8;
                //Log.LogInfo($"URP m_MSAA = {UniversalRenderPipeline.asset.m_MSAA}");
                //UniversalRenderPipeline.asset.m_MSAA = MsaaQuality._8x;

                Log.LogInfo("Applied custom settings.");
            }
        }

        [HarmonyPatch]
        public class FOVPatches
        {
           
            // FOV Control
            // Needs work before enabling
            [HarmonyPatch(typeof(VirtualCamera.RpVirtualCameraControl), nameof(VirtualCamera.RpVirtualCameraControl.SetActiveCamera))]
            [HarmonyPostfix]
            public static void FOVAdjust(VirtualCamera.RpVirtualCameraControl __instance, VirtualCamera.RpVirtualCamera __0)
            {
                float DefaultAspectRatio = (float)16 / 9;
                float NewAspectRatio = (float)Screen.width / Screen.height; // This is only calculated on startup. Potential issue.

                if (__0 != null)
                {
                    Log.LogInfo($"Camera name = {__0.name}. Camera FOV = {__0.FieldOfView}");
                    float currFOV = __0.FieldOfView;

                    // Vert+ FOV
                    if (NewAspectRatio < DefaultAspectRatio)
                    {
                        float newFOV = Mathf.Floor(Mathf.Atan(Mathf.Tan(currFOV * Mathf.PI / 360) / NewAspectRatio * DefaultAspectRatio) * 360 / Mathf.PI);
                        __instance.m_GameCamera.SetFov(newFOV);
                        Log.LogInfo($"Camera name = {__0.name}. New Camera FOV = {newFOV}");
                    }
                        
                }
                
            }
        }

        [HarmonyPatch]
        public class CustomResolutionPatches
        {
            // THIS SHOULD BE WORKING BUT IT AIN'T!

            // Apply custom resolution
            [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), new Type[] { typeof(int), typeof(int), typeof(UnityEngine.FullScreenMode) })]
            [HarmonyPrefix]
            public static bool ApplyCustomResolution(ref int __0, ref int __1, ref UnityEngine.FullScreenMode __2)
            {
                if (fDesiredResolutionX.Value > 0 && fDesiredResolutionY.Value > 0)
                {
                    var fullscreenMode = iWindowMode.Value switch
                    {
                        1 => UnityEngine.FullScreenMode.ExclusiveFullScreen,
                        2 => UnityEngine.FullScreenMode.FullScreenWindow,
                        3 => UnityEngine.FullScreenMode.MaximizedWindow,
                        4 => UnityEngine.FullScreenMode.Windowed,
                        _ => UnityEngine.FullScreenMode.ExclusiveFullScreen,
                    };

                    Log.LogInfo($"Previous resolution = {__0}x{__1}. Window mode = {__2}");
                    __0 = (int)fDesiredResolutionX.Value;
                    __1 = (int)fDesiredResolutionY.Value;
                    __2 = fullscreenMode;
                    
                    Log.LogInfo($"Custom resolution enabled. {(int)fDesiredResolutionX.Value}x{(int)fDesiredResolutionY.Value}. Window mode = {fullscreenMode}");
                    return true;
                }
                return true;
            }
        }
    }
}

