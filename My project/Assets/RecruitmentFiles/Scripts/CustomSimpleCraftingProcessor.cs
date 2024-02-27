using Opsive.Shared.Utility;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Crafting;
using Opsive.UltimateInventorySystem.Crafting.Processors;
using System.Collections;
using System.Collections.Generic;

public class CustomSimpleCraftingProcessor : SimpleCraftingProcessor
{
    Dictionary<CraftingRecipe, int> craftingQueue = new Dictionary<CraftingRecipe, int>();

    /// <summary>
    /// Craft the items.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="inventory">The inventory containing the items.</param>
    /// <param name="selectedIngredients">The item infos selected.</param>
    /// <param name="quantity">The quantity to craft.</param>
    /// <returns>True if you can craft.</returns>
    protected override CraftingResult CraftInternal(CraftingRecipe recipe, IInventory inventory,
        ListSlice<ItemInfo> selectedIngredients, int quantity)
    {
        if (CanCraftInternal(recipe, inventory, selectedIngredients, quantity) == false)
        {
            return new CraftingResult(null, false);
        }

        if (RemoveIngredients(inventory, selectedIngredients) == false)
        {
            return new CraftingResult(null, false);
        }

        if (!craftingQueue.ContainsKey(recipe))
        {
            craftingQueue.Add(recipe, quantity);
        }

        var output = CreateCraftingOutput(recipe, inventory, quantity);

        return new CraftingResult(output, true);
    }
}
