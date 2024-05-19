using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using EFT.UI.BattleTimer;
using HarmonyLib;
using TMPro;

namespace Pause
{
    public class WorldTickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.DoWorldTick));

        [PatchPrefix]
        // various world ticks
        internal static bool Prefix(GameWorld __instance, float dt)
        {
            if (PauseController.IsPaused)
            {
                // invoking the PlayerTick to prevent hand jank
                typeof(GameWorld)
                        .GetMethod("PlayerTick", BindingFlags.Instance | BindingFlags.Public)
                        .Invoke(__instance, new object[] { dt });

                return false;
            }

            return true;
        }
    }

    public class OtherWorldTickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.DoOtherWorldTick));

        [PatchPrefix]
        // it looks like this just calls the player's update ticks
        internal static bool Prefix(GameWorld __instance)
        {
            if (PauseController.IsPaused)
            {
                return false;
            }

            return true;
        }
    }

    public class GameTimerClassUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameTimerClass), nameof(GameTimerClass.method_0));

        [PatchPrefix]
        internal static bool Prefix()
        {
            return !PauseController.IsPaused;
        }
    }

    public class TimerPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TimerPanel), nameof(TimerPanel.UpdateTimer));

        [PatchPrefix]
        internal static bool Prefix(TextMeshProUGUI ____timerText)
        {
            // patch for 'fake' gaame ui timer when you press o
            // set the text to PAUSED for fun
            if (PauseController.IsPaused)
            {
                ____timerText.SetMonospaceText("PAUSED", false);
                return false;
            }

            return true;
        }
    }
    public class PlayerUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.UpdateTick));

        [PatchPrefix]
        internal static bool Prefix()
        {
            return !PauseController.IsPaused;
        }
    }

    public class EndByTimerScenarioUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EndByTimerScenario), nameof(EndByTimerScenario.Update));

        [PatchPrefix]
        internal static bool Prefix()
        {
            return !PauseController.IsPaused;
        }
    }

}