using EFT;
using EFT.UI.BattleTimer;
using System.Reflection;
using Aki.Reflection.Patching;
using TMPro;

namespace Pause
{
    public class WorldTickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod("DoWorldTick", BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        // various world ticks
        static bool Prefix(GameWorld __instance, float dt)
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
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod("DoOtherWorldTick", BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        // it looks like this just calls the player's update ticks
        static bool Prefix(GameWorld __instance) 
        {
            if (PauseController.IsPaused) 
            {
                return false;
            }

            return true;
        }
    }

    public class GameTimerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameTimerClass).GetMethod("method_0", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        [PatchPrefix]
        static bool Prefix() 
        {
            // this seems to be the real raid timer
            if (PauseController.IsPaused) 
            {
                return false;
            }

            return true;
        } 
    }

    public class TimerPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TimerPanel).GetMethod("UpdateTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        [PatchPrefix]
        static bool Prefix(TextMeshProUGUI ____timerText)
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
}