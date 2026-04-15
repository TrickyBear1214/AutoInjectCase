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

                    StorageTarget target = FindBestTarget(item);
                    if (target == null)
                    {
                        return true;
                    }

                    CharacterMainControl character = CharacterControlRef(__instance);
                    if (character == null)
                    {
                        LogWarning("failed to resolve CharacterMainControl, fallback to default pickup");
                        return true;
                    }

                    if (!PrepareItemForPickup(item))
                    {
                        LogWarning("failed to prepare item for pickup, fallback to default inventory pickup for " + DescribeItem(item));
                        return true;
                    }

                    if (!TryStorePickedItem(item, target))
                    {
                        LogWarning("container store failed, fallback to default inventory pickup for " + DescribeItem(item));
                        return true;
                    }

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
