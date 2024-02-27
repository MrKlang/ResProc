using Opsive.Shared.Utility;
using Opsive.UltimateInventorySystem.Crafting;
using UnityEngine;

namespace Opsive.UltimateInventorySystem.Crafting.RecipeTypes
{
    public class CustomCraftingRecipe : CraftingRecipe
    {
        [LabelOverride("Craft Time")]
        [SerializeField] private int _craftingTimeInSeconds;

        public int CraftingTimeInSeconds => _craftingTimeInSeconds;
    }
}