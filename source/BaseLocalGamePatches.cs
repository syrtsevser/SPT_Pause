using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using System;
using HarmonyLib;
using SPT.Reflection.Utils;

namespace Pause
{
    public class BaseLocalGameUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() 
        {
            //typeof(BaseLocalGame<>).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
        } 

        [PatchPrefix]
        internal static bool Prefix()
        {
            return !PauseController.IsPaused;
        }
    }


}

