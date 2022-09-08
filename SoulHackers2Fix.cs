using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

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
        public static ConfigEntry<float> fMovementFix;

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
                                "Set to true to fix player movement above 60fps.");

            fMovementFix = Config.Bind("General",
                                "MovementFix.Value",
                                (float)0f, // Game Default = 0.1f | Leave on 0 to auto calculate
                                new ConfigDescription("Set player motion blend time. Lower values reduce the amount of time it takes to come to a stop after moving.\n" +
                                "You can set this to 0 to have the fix calculate this automatically.\n" +
                                "Game Default = 0.1",
                                new AcceptableValueRange<float>(0f, 10f)));

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
                                (int)1,
                                new ConfigDescription("Set window mode. 1 = Borderless, 2 = Windowed.\nExclusive Fullscreen is broken at the moment.",
                                new AcceptableValueRange<int>(1, 2)));

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
            public static bool bMovementLogHasRun = false;

            // Adjust graphics settings
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

                    if (fMovementFix.Value == 0f)
                    {
                        // Calculate motion blend time based on FPS cap.
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
                        if (!bMovementLogHasRun)
                        {
                            Log.LogInfo($"MovementFix: Automatic: currFPS = {currFPS}, PlayerMoveMotionBlendTime = {global.m_Common.m_PlayerMoveMotionBlendTime}.");
                            bMovementLogHasRun = true;
                        }
                        
                    }
                    else if (fMovementFix.Value > 0f)
                    {
                        // Set to value defined in config.
                        global.m_Common.m_PlayerMoveMotionBlendTime = fMovementFix.Value;
                        if (!bMovementLogHasRun)
                        {
                            Log.LogInfo($"MovementFix: Manual: PlayerMoveMotionBlendTime = {global.m_Common.m_PlayerMoveMotionBlendTime}.");
                            bMovementLogHasRun = true;
                        }
                        
                    }
                }
            }
        }

        [HarmonyPatch]
        public class CustomResolutionPatches
        {
            // Apply Custom Resolution
            [HarmonyPatch(typeof(Game.Common.ConfigCtrl), nameof(Game.Common.ConfigCtrl.SetDisplayAndResolutionAll))]
            [HarmonyPrefix]
            public static bool ForceCustomResolution(ref bool __0, ref bool __1, ref bool __2, ref int __3, ref Game.Common.eGameGraphicsScreenModeSetting __4, ref int __5, ref int __6)
            {
                var screenMode = iWindowMode.Value switch
                {
                    //1 => Game.Common.eGameGraphicsScreenModeSetting.ExclusiveFullScreen, // Exclusive Full Screen - Broken?
                    1 => Game.Common.eGameGraphicsScreenModeSetting.FullScreen, // Borderless
                    2 => Game.Common.eGameGraphicsScreenModeSetting.Window, // Windowed
                    _ => Game.Common.eGameGraphicsScreenModeSetting.FullScreen, // Borderless
                };

                Log.LogInfo($"Custom Resolution: Old: SetDisplayAndResolutionAll: reso = {__0}, screen = {__1}, monitor = {__2}, monNum = {__3}, screenMode = {__4}, w = {__5}, h = {__6}");
                Game.Common.ConfigCtrl.SetResolutionW((int)fDesiredResolutionX.Value);
                Game.Common.ConfigCtrl.SetResolutionH((int)fDesiredResolutionY.Value);
                Game.Common.ConfigCtrl.SetScreenMode(screenMode);
                __5 = (int)fDesiredResolutionX.Value;
                __6 = (int)fDesiredResolutionY.Value;
                __4 = screenMode;
                Log.LogInfo($"Custom Resolution: New: SetDisplayAndResolutionAll: reso = {__0}, screen = {__1}, monitor = {__2}, monNum = {__3}, screenMode = {__4}, w = {__5}, h = {__6}");
                return true;
            }
        }

        [HarmonyPatch]
        public class EffectPatches
        {
            // Effect Toggles
            [HarmonyPatch(typeof(RpGraphics.RpImageEffectData), nameof(RpGraphics.RpImageEffectData.OnEnable))]
            [HarmonyPostfix]
            public static void EffectToggles()
            {
                var imageEffectManager = RpGraphics.RpGraphicsManager.GetImageEffectManager();

                if (bDisableChromaticAberration.Value)
                {
                    AtGlitch.u_colorGap = 0;
                    Log.LogInfo($"Glitch Effect: Set color gap to {AtGlitch.u_colorGap}");
                    Log.LogInfo($"Vignette Effect: Renderer disabled");
                    var vignetteRenderer = imageEffectManager.GetRenderer<AtVignetteRenderer>();
                    vignetteRenderer.enabled = false;
                }
            }
        }
    }
}

