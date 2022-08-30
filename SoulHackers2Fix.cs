using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;
using System;

using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

using AtLib.AtGraphics.AtImageEffect;

namespace SH2Fix
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class SH2 : BasePlugin
    {
        internal static new ManualLogSource Log;

        // Features
        public static ConfigEntry<bool> bUltrawideFixes;
        public static ConfigEntry<bool> bMovementFix;

        // Graphics
        public static ConfigEntry<bool> bDisableChromaticAberration;
        public static ConfigEntry<int> iAnisotropicFiltering;
        public static ConfigEntry<float> fLODBias;
        public static ConfigEntry<float> fRenderScale;

        // Custom Resolution
        public static ConfigEntry<bool> bCustomResolution;
        public static ConfigEntry<float> fDesiredResolutionX;
        public static ConfigEntry<float> fDesiredResolutionY;
        public static ConfigEntry<int> iWindowMode;

        // Aspect Ratio
        public static float DefaultAspectRatio = (float)16 / 9;
        public static float NewAspectRatio = (float)Screen.width / Screen.height; // This is only calculated on startup. Potential issue?
        public static float AspectMultiplier = NewAspectRatio / DefaultAspectRatio;
        public static float AspectDivider = DefaultAspectRatio / NewAspectRatio;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Features
            bUltrawideFixes = Config.Bind("General",
                                "UltrawideFixes",
                                true,
                                "Set to true to enable ultrawide UI fixes.");
            bMovementFix = Config.Bind("General",
                                "MovementFix",
                                true,
                                "Set to true to fix slidey movement on Ringo.");

            // Graphics
            bDisableChromaticAberration = Config.Bind("Graphical Tweaks",
                                "DisableChromaticAberration",
                                false, // Default to false, maybe people like it.
                                "Set to true to disable chromatic aberration effects.");

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

            // Custom Resolution
            bCustomResolution = Config.Bind("Set Custom Resolution",
                                "CustomResolution",
                                true, // Enabled by default to fix the janky startup resolution.
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
                                (int)2,
                                new ConfigDescription("Set window mode. 1 = Exclusive Fullscreen, 2 = Fullscreen Windowed, 3 = Maximized Window, 4 = Windowed.",
                                new AcceptableValueRange<int>(1, 4)));

            // Run UltrawidePatches
            if (bUltrawideFixes.Value)
            {
                Harmony.CreateAndPatchAll(typeof(UltrawidePatches));
            }

            // Run CustomResolutionPatches
            if (bCustomResolution.Value)
            {
                Harmony.CreateAndPatchAll(typeof(CustomResolutionPatches));
            }

            Harmony.CreateAndPatchAll(typeof(EffectPatches));
            Harmony.CreateAndPatchAll(typeof(MiscellaneousPatches));

        }

        [HarmonyPatch]
        public class UltrawidePatches
        {
            
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
                var configData = Game.Common.ConfigCtrl.s_ConfigSystem2Data;

                // Anisotropic Filtering
                if (iAnisotropicFiltering.Value > 0)
                {
                    Log.LogInfo($"Old: Anisotropic filtering = {QualitySettings.anisotropicFiltering}");
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    Texture.SetGlobalAnisotropicFilteringLimits(iAnisotropicFiltering.Value, iAnisotropicFiltering.Value);
                    Log.LogInfo($"New: Anisotropic filtering = {QualitySettings.anisotropicFiltering}. Value = {iAnisotropicFiltering.Value}");
                }

                // Render Scale
                if (fRenderScale.Value > 0f)
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

                Log.LogInfo("Applied custom settings.");
            }

            // Fix movement sliding
            [HarmonyPatch(typeof(MapNew.MapChara), nameof(MapNew.MapChara.SetActive))]
            [HarmonyPostfix]
            public static void FixSliding(MapNew.MapChara __instance)
            {
                if (bMovementFix.Value)
                {
                    var global = MapNew.MapManager.GlobalSettings;

                    var currFPS = Game.Common.ConfigCtrl.GetFps() switch
                    {
                        Game.Common.eGameGraphicsFps.Free => Screen.currentResolution.refreshRate,
                        Game.Common.eGameGraphicsFps.Fps30 => 30f,
                        Game.Common.eGameGraphicsFps.Fps60 => 60f,
                        Game.Common.eGameGraphicsFps.Fps75 => 75f,
                        Game.Common.eGameGraphicsFps.Fps120 => 120f,
                        Game.Common.eGameGraphicsFps.Fps144 => 144f,
                        Game.Common.eGameGraphicsFps.Fps150 => 150f,
                        Game.Common.eGameGraphicsFps.Max => Screen.currentResolution.refreshRate,
                        _ => Screen.currentResolution.refreshRate,
                    };

                    float FPSDivider = (float)currFPS / (float)60; // Assuming default (0.1f) is for 60fps.
                    global.m_Common.m_PlayerMoveMotionBlendTime = (float)0.1f / FPSDivider;

                    //Log.LogInfo($"Set global.m_Common.m_PlayerMoveMotionBlendTime to {global.m_Common.m_PlayerMoveMotionBlendTime}");
                }
            }
        }

        [HarmonyPatch]
        public class CustomResolutionPatches
        {
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
                        _ => UnityEngine.FullScreenMode.FullScreenWindow,
                    };

                    Log.LogInfo($"Prefix: Old: Set resolution = {__0}x{__1}. Window mode = {__2}");
                    __0 = (int)fDesiredResolutionX.Value;
                    __1 = (int)fDesiredResolutionY.Value;
                    __2 = fullscreenMode;

                    Log.LogInfo($"Prefix: New: Set resolution = {(int)fDesiredResolutionX.Value}x{(int)fDesiredResolutionY.Value}. Window mode = {fullscreenMode}");
                    return true;
                }
                // Don't change anything if resolution is set to 0 on any axis
                return true;
            }

            // Override monitor capabilities.
            // I honestly don't know exactly what the fuck kind of weird shit they are doing with this, but overriding it fixes custom resolutions.
            // ??????
            [HarmonyPatch(typeof(RedPencil.Artdink.MonitorUtility), nameof(RedPencil.Artdink.MonitorUtility.GetMonitorResolution))]
            [HarmonyPostfix]
            public static void ChangeReportedMonitorResolution2(ref int __0, ref int __1, ref int __2)
            {
                Log.LogInfo($"Postfix: Old: Artdink.MonitoryUtility.GetMonitorResolution\nMonitor Index: {__0}, Width: {__1}, Height: {__2}");
                __1 = (int)fDesiredResolutionX.Value;
                __2 = (int)fDesiredResolutionY.Value;
                Log.LogInfo($"Postfix: New: Artdink.MonitoryUtility.GetMonitorResolution\nMonitor Index: {__0}, Width: {__1}, Height: {__2}");
            }

        }

        [HarmonyPatch]
        public class EffectPatches
        {
            // Glitch Effect
            [HarmonyPatch(typeof(RpGraphics.RpImageEffectData), nameof(RpGraphics.RpImageEffectData.OnEnable))]
            [HarmonyPostfix]
            public static void GlitchEffect()
            {
                if (bDisableChromaticAberration.Value)
                {
                    AtGlitch.u_colorGap = 0;
                    //Log.LogInfo($"Glitch Effect: Set color gap to {AtGlitch.u_colorGap}");
                    //Log.LogInfo($"Vignette Effect: Render disabled");
                    var imageEffectManager = RpGraphics.RpGraphicsManager.GetImageEffectManager();
                    var vignetteRenderer = imageEffectManager.GetRenderer<AtVignetteRenderer>();
                    vignetteRenderer.enabled = false;
                    
                }
            }
        }
    }
}

