using System;
using System.Collections;
using System.Collections.Generic;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;

namespace AutoInjectCase
{
    public partial class ModBehaviour
    {
        private static StorageTarget FindBestTarget(Item item)
        {
            List<Item> candidates = CollectAllPlayerItems();
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            StorageTarget best = null;

            foreach (Item candidate in candidates)
            {
                StorageTarget candidateTarget = BuildStorageTarget(candidate, item);
                if (candidateTarget == null)
                {
                    continue;
                }

                if (best == null || candidateTarget.Score > best.Score)
                {
                    best = candidateTarget;
                }
            }

            return best;
        }

        private static List<Item> CollectAllPlayerItems()
        {
            HashSet<int> visitedInventoryIds = new HashSet<int>();
            HashSet<int> visitedItemIds = new HashSet<int>();
            List<Item> items = new List<Item>();

            if (GetPlayerInventoriesMethod == null)
            {
                LogError("GetPlayerInventories reflection lookup failed");
                return items;
            }

            IEnumerable<Inventory> inventories =
                GetPlayerInventoriesMethod.Invoke(null, null) as IEnumerable<Inventory>;

            if (inventories == null)
            {
                LogError("GetPlayerInventories returned null");
                return items;
            }

            foreach (Inventory inventory in inventories)
            {
                CollectItemsFromInventory(inventory, items, visitedInventoryIds, visitedItemIds);
            }

            try
            {
                if (PetProxy.Instance != null && PetProxy.PetInventory != null)
                {
                    CollectItemsFromInventory(PetProxy.PetInventory, items, visitedInventoryIds, visitedItemIds);
                }
            }
            catch (Exception ex)
            {
                LogError("failed to collect PetInventory: " + ex.Message);
            }

            return items;
        }

        private static bool IsInPetInventory(Item item)
        {
            if (item == null)
            {
                return false;
            }

            Inventory petInventory = null;

            try
            {
                petInventory = PetProxy.PetInventory;
            }
            catch (Exception ex)
            {
                LogError("failed to resolve PetInventory: " + ex.Message);
                return false;
            }

            if (petInventory == null)
            {
                return false;
            }

            for (Item parent = item; parent != null; parent = parent.ParentItem)
            {
                if (parent.InInventory == petInventory)
                {
                    return true;
                }
            }

            return item.InInventory == petInventory;
        }

        private static void CollectItemsFromInventory(
            Inventory inventory,
            List<Item> items,
            HashSet<int> visitedInventoryIds,
            HashSet<int> visitedItemIds)
        {
            if (inventory == null)
            {
                return;
            }

            int inventoryId = inventory.GetInstanceID();
            if (!visitedInventoryIds.Add(inventoryId))
            {
                return;
            }

            foreach (Item item in inventory.Content)
            {
                CollectItemTree(item, items, visitedInventoryIds, visitedItemIds);
            }
        }

        private static void CollectItemTree(
            Item item,
            List<Item> items,
            HashSet<int> visitedInventoryIds,
            HashSet<int> visitedItemIds)
        {
            if (item == null)
            {
                return;
            }

            int itemId = item.GetInstanceID();
            if (!visitedItemIds.Add(itemId))
            {
                return;
            }

            items.Add(item);

            if (item.Inventory != null)
            {
                CollectItemsFromInventory(item.Inventory, items, visitedInventoryIds, visitedItemIds);
            }

            if (item.Slots == null)
            {
                return;
            }

            foreach (Slot slot in item.Slots)
            {
                if (slot?.Content != null)
                {
                    CollectItemTree(slot.Content, items, visitedInventoryIds, visitedItemIds);
                }
            }
        }

        private static StorageTarget BuildStorageTarget(Item container, Item movingItem)
        {
            if (!IsValidContainer(container, movingItem))
            {
                return null;
            }

            return new StorageTarget
            {
                Container = container,
                Score = 100000 + GetContainerPriority(container, movingItem)
            };
        }

        private static bool TryStorePickedItem(Item item, StorageTarget target)
        {
            if (item == null || target == null)
            {
                return false;
            }

            try
            {
                return ItemUtilities.TryPlug(target.Container, item, true);
            }
            catch (Exception ex)
            {
                LogError("TryStorePickedItem error for " + DescribeItem(item) + ": " + ex);
            }

            return false;
        }

        private static void ReleasePickupAgent(Item item)
        {
            try
            {
                item?.AgentUtilities?.ReleaseActiveAgent();
            }
            catch (Exception ex)
            {
                LogError("ReleasePickupAgent error for " + DescribeItem(item) + ": " + ex);
            }
        }

        private static bool TryMoveExistingItem(Item item, StorageTarget target)
        {
            if (item == null || target == null)
            {
                return false;
            }

            Inventory sourceInventory = item.InInventory;
            if (sourceInventory == null)
            {
                return false;
            }

            int sourceIndex = sourceInventory.GetIndex(item);
            if (sourceIndex < 0)
            {
                return false;
            }

            if (!sourceInventory.RemoveItem(item))
            {
                LogWarning("failed to remove item from source inventory: " + DescribeItem(item));
                return false;
            }

            bool moved = false;
            try
            {
                moved = ItemUtilities.TryPlug(target.Container, item, true, sourceInventory, sourceIndex);
            }
            catch (Exception ex)
            {
                LogError("TryMoveExistingItem error for " + DescribeItem(item) + ": " + ex);
            }

            if (moved)
            {
                return true;
            }

            if (!sourceInventory.AddAt(item, sourceIndex))
            {
                sourceInventory.AddItem(item);
            }

            return false;
        }

        private static int GetContainerPriority(Item container, Item movingItem)
        {
            int score = 0;

            if (container != null &&
                !ItemUtilities.IsInPlayerCharacter(container) &&
                !ItemUtilities.IsInPlayerStorage(container))
            {
                score += PetInventoryBonus;
            }

            if (container.ParentItem != null)
            {
                score += 1000;
            }

            score += CountMatchingPriorityTags(container, movingItem) * MatchingTagBonus;

            return score;
        }

        private static int CountMatchingPriorityTags(Item container, Item movingItem)
        {
            if (container?.Tags == null || movingItem?.Tags == null)
            {
                return 0;
            }

            int matchCount = 0;

            foreach (Tag containerTag in container.Tags)
            {
                if (!IsPriorityTag(containerTag))
                {
                    continue;
                }

                if (movingItem.Tags.Contains(containerTag))
                {
                    matchCount++;
                }
            }

            return matchCount;
        }

        private static bool IsPriorityTag(Tag tag)
        {
            if (tag == null)
            {
                return false;
            }

            return tag.name != ContainerTagName &&
                   tag.name != DontDropOnDeadInSlotTagName;
        }

        private static bool IsValidContainer(Item candidate, Item movingItem)
        {
            if (candidate == null || movingItem == null || candidate == movingItem)
            {
                return false;
            }

            if (!IsContainerLike(candidate))
            {
                return false;
            }

            if (candidate.Slots == null)
            {
                return false;
            }

            if (candidate.IsBeingDestroyed)
            {
                return false;
            }

            if (!HasAvailableSlot(candidate, movingItem))
            {
                return false;
            }

            for (Item parent = candidate; parent != null; parent = parent.ParentItem)
            {
                if (ReferenceEquals(parent, movingItem))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsContainerLike(Item item)
        {
            return item != null &&
                   item.Tags != null &&
                   item.Tags.Contains(ContainerTagName);
        }

        private static bool HasAvailableSlot(Item container, Item movingItem)
        {
            if (container == null || container.Slots == null)
            {
                return false;
            }

            foreach (Slot slot in container.Slots)
            {
                if (slot == null || !slot.CanPlug(movingItem))
                {
                    continue;
                }

                if (slot.Content == null)
                {
                    return true;
                }

                if (CanMergeIntoSlot(slot, movingItem))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanMergeIntoSlot(Slot slot, Item movingItem)
        {
            if (slot?.Content == null || movingItem == null || !movingItem.Stackable)
            {
                return false;
            }

            Item content = slot.Content;
            return content.Stackable &&
                   content.TypeID == movingItem.TypeID &&
                   content.StackCount < content.MaxStackCount;
        }

        private static string DescribeItem(Item item)
        {
            if (item == null)
            {
                return "<null item>";
            }

            return "'" + item.DisplayName + "'(TypeID=" + item.TypeID + ", InstanceID=" + item.GetInstanceID() + ")";
        }

        private static string DescribeContainer(StorageTarget target)
        {
            if (target == null || target.Container == null)
            {
                return "<null container>";
            }

            string baseText = "'" + target.Container.DisplayName + "'(TypeID=" + target.Container.TypeID + ")";
            return baseText + " via slot";
        }
    }
}
