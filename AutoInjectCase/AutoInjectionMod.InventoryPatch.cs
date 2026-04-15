using System;
using HarmonyLib;
using ItemStatsSystem;

namespace AutoInjectCase
{
    public partial class ModBehaviour
    {
        [HarmonyPatch(typeof(ItemUtilities), "SendToPlayerCharacterInventory")]
        private static class PatchSendToPlayerCharacterInventory
        {
            private static bool Prefix(Item item, bool dontMerge, ref bool __result)
            {
                try
                {
                    if (item == null)
                    {
                        return true;
                    }

                    Inventory sourceInventory = item.InInventory;
                    if (sourceInventory == null)
                    {
                        return true;
                    }

                    if (ItemUtilities.IsInPlayerCharacter(item))
                    {
                        return true;
                    }

                    if (ItemUtilities.IsInPlayerStorage(item))
                    {
                        return true;
                    }

                    if (IsInPetInventory(item))
                    {
                        return true;
                    }

                    StorageTarget target = FindBestTarget(item);
                    if (target == null)
                    {
                        return true;
                    }

                    if (TryMoveExistingItem(item, target))
                    {
                        __result = true;
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogError("SendToPlayerCharacterInventory prefix error, fallback to default send: " + ex);
                    return true;
                }
            }
        }
    }
}
