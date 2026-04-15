# AutoInjectCase Spec

## Purpose

When the player picks up an item or an item is sent toward the player inventory, store it directly into a matching case when possible instead of the normal player inventory.

## Current Assumptions

- Actual cases used by this mod are slot-based, not inventory-based.
- Slot-based stack merging in the base game happens through `ItemUtilities.TryPlug(...)`, not direct `Slot.Plug(...)`.
- Therefore slot-based auto-insert must use `TryPlug(...)`.

## Functional Requirements

1. Pickup flow
   - If a matching case exists, the item should go directly into that case.
   - Do not first place the item into the normal player inventory and then move it into a case.
   - If case insert fails, fall back to the original game pickup logic.

2. `SendToPlayerCharacterInventory(item, dontMerge)` flow
   - Auto case insert is allowed only for cases where the item is being sent from outside player storage and outside pet inventory.
   - If the source item is already in player storage, skip auto case insert and let the original game logic run.
   - If the source item is already in pet inventory, skip auto case insert and let the original game logic run.
   - If auto case insert is attempted and fails, fall back to the original game logic.

3. Case detection
   - Keep using the current implementation rule based on `ContainerTagName = "Continer"`.
   - Current targeting is slot-based case targeting.

4. Load priority
   - If multiple cases can accept the same item, prefer a case inside pet inventory.
   - Example:
   - Player-held case `A` can accept the item.
   - Pet-inventory case `B` can also accept the item.
   - Case `B` should be selected first.

5. Slot-based case behavior
   - Slot-based case insert uses `ItemUtilities.TryPlug(...)`.
   - Stackable items should merge with existing matching slot contents the same way the base game does.

6. Preserve base behavior
   - Failed auto insert must not lose or duplicate items.
   - Do not replace original game behavior blindly when the original required work is not understood.
   - Preserve the base-game meaning of `dontMerge`.

## Non-Goals

- Do not assume cases are inventory-based and simplify the logic on that assumption.
- Do not replace the full `PickupItem` behavior with a custom flow unless the original behavior has been verified.

## Working Rules

- Future code changes should follow this file.
- If the intended behavior changes, update this file first.
