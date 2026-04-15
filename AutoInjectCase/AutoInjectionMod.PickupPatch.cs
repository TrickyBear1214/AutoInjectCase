using System;
using HarmonyLib;
using ItemStatsSystem;

namespace AutoInjectCase
{
    public partial class ModBehaviour
    {
        [HarmonyPatch(typeof(CharacterItemControl), "PickupItem")]
        private static class PatchCharacterItemControlPickupItem
        {
            private static bool Prefix(CharacterItemControl __instance, Item item, ref bool __result)
            {
                try
                {
                    if (item == null)
                    {
                        return true;
                    }

                    if (CharacterControlRef(__instance) == null)
                    {
                        return true;
                    }

                    StorageTarget target = FindBestTarget(item);
                    if (target == null)
                    {
                        return true;
                    }

                    if (!TryStorePickedItem(item, target))
                    {
                        return true;
                    }

                    ReleasePickupAgent(item);
                    __result = true;
                    return false;
                }
                catch (Exception ex)
                {
                    LogError("pickup prefix error, fallback to default pickup: " + ex);
                    return true;
                }
            }
        }
    }
}
