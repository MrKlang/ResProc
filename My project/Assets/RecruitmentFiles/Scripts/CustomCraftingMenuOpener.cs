using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Crafting;
using Opsive.UltimateInventorySystem.UI.Panels;
using UnityEngine;

public class CustomCraftingMenuOpener : InventoryPanelOpener<CustomCraftingMenu>
{
    [Tooltip("The storage inventory.")]
    [SerializeField] protected Inventory m_StorageInventory;
    [Tooltip("The Crafter to bind to the menu.")]
    [SerializeField] protected Crafter m_Crafter;
    /// <summary>
    /// Open the menu.
    /// </summary>
    /// <param name="interactor">The inventory.</param>
    public override void Open(Inventory inventory)
    {
        m_Menu.BindInventory(inventory);
        m_Menu.SetCrafter(m_Crafter);
        m_Menu.SetStorageInventory(m_StorageInventory);
        m_Menu.DisplayPanel.SmartOpen();

        Cursor.visible = true;
    }
}
