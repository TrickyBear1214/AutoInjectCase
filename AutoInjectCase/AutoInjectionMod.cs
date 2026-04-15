using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;

namespace AutoInjectCase
{
    public partial class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string HarmonyId = "AutoInjectCase";
        private const string ContainerTagName = "Continer";
        private const int PetInventoryBonus = 100000;

        private static readonly AccessTools.FieldRef<CharacterItemControl, CharacterMainControl> CharacterControlRef =
            AccessTools.FieldRefAccess<CharacterItemControl, CharacterMainControl>("characterMainControl");
        private static readonly System.Reflection.MethodInfo GetPlayerInventoriesMethod =
            AccessTools.Method(typeof(ItemUtilities), "GetPlayerInventories");
        private static readonly System.Reflection.FieldInfo SlotRequireTagsField =
            AccessTools.Field(typeof(Slot), "requireTags");
        private static readonly System.Reflection.FieldInfo SlotExcludeTagsField =
            AccessTools.Field(typeof(Slot), "excludeTags");

        private static Harmony harmony;

        private sealed class StorageTarget
        {
            public Item Container;
            public Inventory Inventory;
            public Slot Slot;
            public int Score;
        }
    }
}
