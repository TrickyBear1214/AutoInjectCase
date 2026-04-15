using HarmonyLib;
using UnityEngine;

namespace AutoInjectCase
{
    public partial class ModBehaviour
    {
        protected override void OnAfterSetup()
        {
            base.OnAfterSetup();
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(ModBehaviour).Assembly);
        }

        protected override void OnBeforeDeactivate()
        {
            if (harmony != null)
            {
                harmony.UnpatchAll(HarmonyId);
                harmony = null;
            }

            base.OnBeforeDeactivate();
        }

        private static void Log(string message)
        {
            Debug.Log("[AutoInjectCase] " + message);
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning("[AutoInjectCase] " + message);
        }

        private static void LogError(string message)
        {
            Debug.LogError("[AutoInjectCase] " + message);
        }
    }
}
