using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;
using Il2CppInterop.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using AtLib.AtGraphics.AtImageEffect;

namespace SH2Fix
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class SH2: BasePlugin
    {
        internal static new ManualLogSource Log;

        // Features
        public static ConfigEntry<bool> bUltrawideFixes;

        // Graphics
        public static ConfigEntry<int> iAnisotropicFiltering;
        public static ConfigEntry<float> fLODBias;
        public static ConfigEntry<float> fRenderScale;

        // Custom Resolution
        public static ConfigEntry<bool> bCustomResolution;
        public static ConfigEntry<float> fDesiredResolutionX;
        public static ConfigEntry<float> fDesiredResolutionY;
        public static ConfigEntry<int> iWindowMode;
        //public static ConfigEntry<int> iMonitorIndex;

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

            // Graphics
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

            //iMonitorIndex = Config.Bind("Set Custom Resolution",
                                //"DisplayNumber",
                                //(int)0,
                                //new ConfigDescription("Set display number. Let's you change which monitor the game is displayed on.",
                                //new AcceptableValueRange<int>(0, 8)));

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
            public static GameObject imageEffMgr;

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

            // Cam stuff
            [HarmonyPatch(typeof(VirtualCamera.RpVirtualCameraControl), nameof(VirtualCamera.RpVirtualCameraControl.SetActiveCamera))]
            [HarmonyPostfix]
            public static void Camstuff(ref VirtualCamera.RpVirtualCamera __0)
            {
                Log.LogInfo("camera changed!");

                var renderers = Resources.FindObjectsOfTypeAll<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    //Log.LogInfo($"Renderer = {renderer.name}");
                   
                }
                var imageEffectManager = RpGraphics.RpGraphicsManager.GetImageEffectManager();
                var silhouetterRenderer = imageEffectManager.GetRenderer<AtLib.AtGraphics.AtImageEffect.AtSilhouetteRenderer>();
                silhouetterRenderer.enabled = false;

            }

            // Cam stuff
            [HarmonyPatch(typeof(AtSilhouette), nameof(AtSilhouette._RenderCmd))]
            [HarmonyPatch(typeof(AtSilhouette), nameof(AtSilhouette._OnRenderImage))]
            [HarmonyPostfix]
            public static void Camstuff4(AtSilhouette __instance, ref UnityEngine.Rendering.CommandBuffer __0)
            {
                Log.LogInfo($"CA rendercmd");

                var imageEffectManager = RpGraphics.RpGraphicsManager.GetImageEffectManager();
                var silhouetterRenderer = imageEffectManager.GetRenderer<AtLib.AtGraphics.AtImageEffect.AtSilhouetteRenderer>();
                silhouetterRenderer.enabled = false;

                //var imageEffMgr = RpGraphics.RpGraphicsManager.GetImageEffectManager();
                //var vigRend = imageEffMgr.GetRenderer<AtLib.AtGraphics.AtImageEffect.AtVignetteRenderer>();
                //__instance.m_chromAberrationMaterial.SetFloat("_AxialAberration", 0);
                //__instance.m_chromAberrationMaterial.SetFloat("_ChromaticAberration", 0);
                //__instance.m_chromAberrationMaterial.SetFloat("_Luminance", 0);
                //__instance.m_chromAberrationMaterial.SetFloat("u_ChroAbre_Luminance", 0);
                //__instance.m_chromAberrationMaterial.SetFloat("u_ChroAbre_ChromaticAberration", 0);
                //__instance.m_chromAberrationMaterial.SetFloat("u_ChroAbre_AxialAberration", 0);
                //__instance.m_separableBlurMaterial.SetVector("offsets", new Vector4(0f, 0f, 0f, 0f));
                //__0.SetGlobalVector("u_SeparableBlur_offsets", new Vector4(0f, 0f, 0f, 0f));
                //__instance.m_chromAberrationMaterial.DisableKeyword("_ChromaticAberration"); 
                //__instance.m_chromAberrationMaterial.DisableKeyword("_AxialAberration");
                //vigRend.SetEnable(false);
                //vigRend.gameObject.SetActive(false);
                // Log.LogInfo("Disalbed shit");

                //__instance.m_vignetteMaterial.mainTexture = null;
                //__instance.m_chromAberrationMaterial.mainTexture = null;
                //__instance.m_separableBlurMaterial.mainTexture = null;
                //__instance.m_vignetteMaterial.SetFloat("u_Vignette_Intensity", 0);
                //__instance.m_chromAberrationMaterial.SetFloat("u_ChroAbre_ChromaticAberration", 0);
                //__instance.m_chromAberrationMaterial.SetFloat("_ChromaticAberration", 0);
                //__instance.m_vignetteMaterial.SetFloat("_Intensity", 0);
                //__instance.m_separableBlurMaterial.SetVector("offsets", new Vector4(0,0,0,0));
                //__instance._DestroyMaterial(__instance.m_separableBlurMaterial);
                //__instance._DestroyMaterial(__instance.m_chromAberrationMaterial);
                //__instance._DestroyMaterial(__instance.m_vignetteMaterial);

            }

            // Fix movement sliding
            [HarmonyPatch(typeof(MapNew.MapChara), nameof(MapNew.MapChara.SetActive))]
            [HarmonyPostfix]
            public static void FixSliding(MapNew.MapChara __instance)
            {
                var global = MapNew.MapManager.GlobalSettings;
                Log.LogInfo($"global.m_Companion.m_InterpolateMotionSec = {global.m_Companion.m_InterpolateMotionSec}");
                Log.LogInfo($"global.m_Chara.m_RingoAnimSpeedFactor = {global.m_Chara.m_RingoAnimSpeedFactor}");
                Log.LogInfo($"global.m_Common.m_PlayerMinMoveDistance = {global.m_Common.m_PlayerMinMoveDistance}");
                Log.LogInfo($"global.m_Common.m_PlayerMoveMotionBlendTime = {global.m_Common.m_PlayerMoveMotionBlendTime}");
                //global.m_Common.m_PlayerMinMoveDistance = 0.01f;
                var currFPS = Screen.currentResolution.refreshRate;
                float FPSDivider = (float)currFPS / (float)60; // Assuming default (0.1f) is for 60fps.
                //global.m_Common.m_PlayerMoveMotionBlendTime = (float)0.1f / FPSDivider;
                //global.m_Companion.m_InterpolateMotionSec = 10f;
                //global.m_Chara.m_RingoAnimSpeedFactor = 20f;
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
                //__0 = iMonitorIndex.Value;
                __1 = (int)fDesiredResolutionX.Value;
                __2 = (int)fDesiredResolutionY.Value;
                Log.LogInfo($"Postfix: New: Artdink.MonitoryUtility.GetMonitorResolution\nMonitor Index: {__0}, Width: {__1}, Height: {__2}");
            }


        }
    }
}

