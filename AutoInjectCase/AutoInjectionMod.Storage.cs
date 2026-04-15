using System;
using System.Collections;
using System.Collections.Generic;
using ItemStatsSystem;
using ItemStatsSystem.Items;

namespace AutoInjectCase
{
    public partial class ModBehaviour
    {
        private static bool PrepareItemForPickup(Item item)
        {
            try
            {
                if (item.AgentUtilities != null)
                {
                    item.AgentUtilities.ReleaseActiveAgent();
                }

                item.Detach();
                return true;
            }
            catch (Exception ex)
            {
                LogError("PrepareItemForPickup error for " + DescribeItem(item) + ": " + ex);
                return false;
            }
        }

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
                LogCandidateShape(candidate);

                if (!IsValidContainer(candidate, item))
                {
                    continue;
                }

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

        private static void LogCandidateShape(Item candidate)
        {
            if (candidate == null || !IsContainerLike(candidate))
            {
                return;
            }

            int slotCount = candidate.Slots != null ? candidate.Slots.Count : 0;
            int inventoryCapacity = candidate.Inventory != null ? candidate.Inventory.Capacity : -1;
            int inventoryCount = candidate.Inventory != null ? candidate.Inventory.Content.Count : -1;

            Log(
                "container candidate " +
                DescribeItem(candidate) +
                ", hasInventory=" +
                (candidate.Inventory != null) +
                ", inventoryCount=" +
                inventoryCount +
                ", inventoryCapacity=" +
                inventoryCapacity +
                ", slotCount=" +
                slotCount);
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

            if (container.Inventory != null && container.Inventory.Content.Count < container.Inventory.Capacity)
            {
                return new StorageTarget
                {
                    Container = container,
                    Inventory = container.Inventory,
                    Score = GetAvailableSlots(container.Inventory) + GetContainerPriority(container)
                };
            }

            return null;
        }

        private static bool TryStorePickedItem(Item item, StorageTarget target)
        {
            if (item == null || target == null)
            {
                return false;
            }

            try
            {
                if (target.Inventory != null)
                {
                    return ItemUtilities.AddAndMerge(target.Inventory, item);
                }
            }
            catch (Exception ex)
            {
                LogError("TryStorePickedItem error for " + DescribeItem(item) + ": " + ex);
            }

            return false;
        }

        private static bool TryMoveExistingItem(Item item, StorageTarget target)
        {
            return TryMoveExistingItem(item, target, false);
        }

        private static bool TryMoveExistingItem(Item item, StorageTarget target, bool dontMerge)
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
                if (target.Inventory != null)
                {
                    moved = dontMerge ? target.Inventory.AddItem(item) : ItemUtilities.AddAndMerge(target.Inventory, item);
                }
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

        private static int GetAvailableSlots(Inventory inventory)
        {
            if (inventory == null)
            {
                return -1;
            }

            return inventory.Capacity - inventory.Content.Count;
        }

        private static int GetContainerPriority(Item container)
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

            if (container.Tags != null && container.Tags.Contains(ContainerTagName))
            {
                score += 500;
            }

            return score;
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

            if (candidate.Inventory == null)
            {
                return false;
            }

            if (candidate.IsBeingDestroyed)
            {
                return false;
            }

            if (candidate.Inventory != null && candidate.Inventory.Content.Count >= candidate.Inventory.Capacity)
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
            if (target.Inventory != null)
            {
                return baseText + " via inventory";
            }

            return baseText;
        }
    }
}
