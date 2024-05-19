using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx.Logging;
using Aki.Reflection.Patching;
using EFT;
using System.Reflection;

namespace Pause
{
    [BepInPlugin("com.dvize.pause", "PAUSE", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<KeyboardShortcut> TogglePause;
        internal static ManualLogSource Log;

        void Awake()
        {
            Log = base.Logger;

            TogglePause = Config.Bind("Keybinds", "Toggle Pause", new KeyboardShortcut(KeyCode.F9));
            Logger.LogInfo($"PAUSE: Loading");

            new NewGamePatch().Enable();    
            new WorldTickPatch().Enable();
            new OtherWorldTickPatch().Enable();
            new GameTimerClassPatch().Enable();
            new TimerPanelPatch().Enable();
        }

        internal class NewGamePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

            [PatchPrefix]
            private static void PatchPrefix()
            {
                PauseController.Enable();
            }
        }
    }
}