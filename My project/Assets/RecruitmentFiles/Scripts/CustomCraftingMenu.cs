using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Crafting;
using Opsive.UltimateInventorySystem.Crafting.RecipeTypes;
using Opsive.UltimateInventorySystem.UI.Item;
using Opsive.UltimateInventorySystem.UI.Panels;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomCraftingMenu : CustomCraftingMenuBase
{
    [Tooltip("The client inventory Grid.")]
    [SerializeField] protected internal InventoryGrid m_ClientInventoryGrid;
    [Tooltip("The storage inventory Grid.")]
    [SerializeField] protected internal InventoryGrid m_StorageInventoryGrid;
    [Tooltip("The storage inventory Grid.")]
    [SerializeField] protected internal InventoryGrid m_QueueInventoryGrid;

    [SerializeField] protected internal ItemViewSlot m_CurrentlyCraftedItemSlot;
    [SerializeField] protected internal TextMeshProUGUI m_timerText;

    [Tooltip("The quantity picker panel.")]
    [SerializeField] internal QuantityPickerPanel m_QuantityPickerPanel;
    [Tooltip("Close the quantity picker panel when the crafting menu is closed.")]
    [SerializeField] protected bool m_CloseQuantityPickerPanelOnClose = true;
    [Tooltip("Open the quantity picker panel when a crafting recipe has been clicked.")]
    [SerializeField] protected bool m_OpenQuantityPickerPanelOnRecipeClick = true;
    [Tooltip("The Exit button.")]
    [SerializeField] protected Button m_ExitButton;

    protected ItemInfo m_SelectedItemInfo;

    protected bool m_removedCurrentlyCraftedItem;

    public InventoryGrid ClientInventoryGrid => m_ClientInventoryGrid;
    public InventoryGrid StorageInventoryGrid => m_StorageInventoryGrid;
    public InventoryGrid QueueInventoryGrid => m_QueueInventoryGrid;

    /// <summary>
    /// Initialize the Crafting Menu.
    /// </summary>
    /// <param name="display">The display.</param>
    /// <param name="force">Force the initialize.</param>
    public override void Initialize(DisplayPanel display, bool force)
    {
        m_ClientInventoryGrid.Initialize(false);
        m_StorageInventoryGrid.Initialize(false);
        m_QueueInventoryGrid.Initialize(false);

        var wasInitialized = m_IsInitialized;
        if (wasInitialized && !force) { return; }
        base.Initialize(display, force);

        if (wasInitialized == false)
        {
            //Only do it once even if forced.
            m_QuantityPickerPanel.OnAmountChanged += CraftingAmountChanged;
            m_QuantityPickerPanel.ConfirmCancelPanel.OnConfirm += CraftSelectedQuantity;

            if (m_ExitButton != null)
            {
                m_ExitButton.onClick.AddListener(() => m_DisplayPanel.Close(true));
            }
        }

        m_ClientInventoryGrid.OnItemViewSlotClicked += ClientItemClicked;
        m_StorageInventoryGrid.OnItemViewSlotClicked += StorageItemClicked;
        m_QueueInventoryGrid.OnItemViewSlotClicked += QueuedCraftingItemClicked;

        int amountToCraft = currentlyUsedRecipe ? craftingQueue[currentlyUsedRecipe] : 0;
    }

    /// <summary>
    /// update when the crafting amount changes.
    /// </summary>
    /// <param name="amount">The new amount.</param>
    public override void CraftingAmountChanged(int amount)
    {
        var canCraft = m_Crafter.Processor.CanCraft(m_SelectedRecipe, m_StorageInventory, amount);
        if (canCraft == false)
        {
            m_QuantityPickerPanel.QuantityPicker.MaxQuantity = amount;
            m_QuantityPickerPanel.ConfirmCancelPanel.EnableConfirm(false);
        }
        else
        {
            m_QuantityPickerPanel.QuantityPicker.MaxQuantity = amount + 1;
            m_QuantityPickerPanel.ConfirmCancelPanel.EnableConfirm(true);
        }

        base.CraftingAmountChanged(amount);
    }

    /// <summary>
    /// A recipe is selected.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="index">The index.</param>
    public override void CraftingRecipeSelected(CraftingRecipe recipe, int index)
    {
        m_RecipePanel.SetRecipe(recipe);

        if (m_QuantityPickerPanel.IsOpen == false) { return; }
        if (m_SelectedRecipe == recipe) { return; }

        m_SelectedRecipe = recipe;
        m_QuantityPickerPanel.SetPreviousSelectable(m_CraftingRecipeGrid.GetButton(index));
        m_QuantityPickerPanel.QuantityPicker.MinQuantity = 1;
        m_QuantityPickerPanel.QuantityPicker.MaxQuantity = 2;

        m_QuantityPickerPanel.ConfirmCancelPanel.SetConfirmText("Craft");
        m_QuantityPickerPanel.QuantityPicker.SetQuantity(1);
        CraftingAmountChanged(1);
    }

    /// <summary>
    /// Recipe is clicked.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="index">The index.</param>
    public override void CraftingRecipeClicked(CraftingRecipe recipe, int index)
    {
        m_SelectedRecipe = recipe;

        if (m_OpenQuantityPickerPanelOnRecipeClick)
        {
            m_QuantityPickerPanel.Open(m_DisplayPanel, m_CraftingRecipeGrid.GetButton(index));
        }


        m_QuantityPickerPanel.QuantityPicker.MinQuantity = 1;
        m_QuantityPickerPanel.QuantityPicker.MaxQuantity = 2;

        m_QuantityPickerPanel.ConfirmCancelPanel.SetConfirmText("Craft");
        m_QuantityPickerPanel.QuantityPicker.SetQuantity(1);
        m_QuantityPickerPanel.QuantityPicker.SelectMainButton();
        CraftingAmountChanged(1);
    }

    /// <summary>
    /// Set the storage inventory.
    /// </summary>
    /// <param name="inventory">The inventory.</param>
    public virtual void SetStorageInventory(Inventory inventory)
    {
        m_StorageInventory = inventory;
        m_StorageInventoryGrid.SetInventory(m_StorageInventory);
    }

    /// <summary>
    /// Handle the On Open event.
    /// </summary>
    public override void OnOpen()
    {
        base.OnOpen();
        m_ClientInventoryGrid.Panel.Open();
        m_StorageInventoryGrid.Panel.Open();
    }

    /// <summary>
    /// Close the QuantityPickerPanel when the menu is closed. 
    /// </summary>
    public override void OnClose()
    {
        base.OnClose();
        if (m_CloseQuantityPickerPanelOnClose && m_QuantityPickerPanel.IsOpen)
        {
            m_QuantityPickerPanel.Close(false);
        }
    }

    /// <summary>
    /// Set the inventory.
    /// </summary>
    protected override void OnInventoryBound()
    {
        base.OnInventoryBound();
        m_ClientInventoryGrid.SetInventory(m_Inventory);
        m_StorageInventoryGrid.SetInventory(m_StorageInventory);
    }

    /// <summary>
    /// Draw both inventories.
    /// </summary>
    protected virtual void DrawInventories()
    {
        m_ClientInventoryGrid.Draw();
        m_StorageInventoryGrid.Draw();
    }

    /// <summary>
    /// An item in the storage was clicked.
    /// </summary>
    /// <param name="inventoryGrid">The inventory grid UI.</param>
    /// <param name="itemInfo">The item info.</param>
    /// <param name="index">The index.</param>
    protected virtual void StorageItemClicked(ItemViewSlotEventData slotEventData)
    {
        if (slotEventData.ItemViewSlot.ItemInfo.Item == null) { return; }

        m_SelectedItemInfo = slotEventData.ItemViewSlot.ItemInfo;
        m_QuantityPickerPanel.Open(m_StorageInventoryGrid.Panel, slotEventData.ItemViewSlot);

        m_QuantityPickerPanel.QuantityPicker.MinQuantity = 0;
        m_QuantityPickerPanel.QuantityPicker.MaxQuantity = m_SelectedItemInfo.Amount;

        m_QuantityPickerPanel.ConfirmCancelPanel.SetConfirmText("Retrieve");
        m_QuantityPickerPanel.QuantityPicker.SetQuantity(1);

#pragma warning disable 4014
        WaitForQuantityDecision(false);
#pragma warning restore 4014
    }

    /// <summary>
    /// The clients item was clicked.
    /// </summary>
    /// <param name="inventoryGrid">The inventory grid ui.</param>
    /// <param name="itemInfo">The item info.</param>
    /// <param name="index">The index.</param>
    protected virtual void ClientItemClicked(ItemViewSlotEventData slotEventData)
    {
        if (slotEventData.ItemViewSlot.ItemInfo.Item == null) { return; }

        m_SelectedItemInfo = slotEventData.ItemViewSlot.ItemInfo;
        m_QuantityPickerPanel.Open(m_ClientInventoryGrid.Panel, slotEventData.ItemViewSlot);

        m_QuantityPickerPanel.QuantityPicker.MinQuantity = 0;
        m_QuantityPickerPanel.QuantityPicker.MaxQuantity = m_SelectedItemInfo.Amount;

        m_QuantityPickerPanel.ConfirmCancelPanel.SetConfirmText("Store");
        m_QuantityPickerPanel.QuantityPicker.SetQuantity(1);

#pragma warning disable 4014
        WaitForQuantityDecision(true);
#pragma warning restore 4014
    }

    public virtual void CurrentlyCraftedItemClicked() //assigned in inspector
    {
        if (!isCrafting)
        {
            return;
        }

        var itemCrafted = m_CurrentlyCraftedItemSlot.ItemInfo.Item;
        var itemIndex = m_QueueInventoryGrid.GetItemIndex(m_CurrentlyCraftedItemSlot.ItemInfo);

        CraftingRecipe recipeToRemove = craftingQueue.First(e => e.Key.MainItemAmountOutput.Value.Item.Equals(itemCrafted)).Key;
        craftingQueue[recipeToRemove]--;

        m_removedCurrentlyCraftedItem = true;

        if (craftingQueue[recipeToRemove] <= 0)
        {
            craftingQueue.Remove(recipeToRemove);
            helperList.Remove(recipeToRemove);
        }

        if(craftingQueue.Count <= 0)
        {
            m_CurrentlyCraftedItemSlot.DisableImage();
            m_timerText.text = "0:00";

            StopCoroutine(CraftFromQueue());
            isCrafting = false;
        }
    }

    public virtual void QueuedCraftingItemClicked(ItemViewSlotEventData slotEventData)
    {
        var itemCrafted = slotEventData.ItemInfo.Item;
        var itemIndex = m_QueueInventoryGrid.GetItemIndex(slotEventData.ItemInfo);

        if (!currentlyUsedRecipe.MainItemAmountOutput.Value.Item.Equals(itemCrafted))
        {
            CraftingRecipe recipeToRemove = craftingQueue.First(e => e.Key.MainItemAmountOutput.Value.Item.Equals(itemCrafted)).Key;
            helperList.Remove(recipeToRemove);
            craftingQueue.Remove(recipeToRemove);
        }
        else
        {
            craftingQueue[currentlyUsedRecipe] = 1;
        }

        m_QueueInventoryGrid.RemoveItem(slotEventData.ItemInfo, itemIndex);
    }

    /// <summary>
    /// Wait for the player to choose a quantity.
    /// </summary>
    /// <param name="store">Store or Retrieve.</param>
    /// <returns>The task.</returns>
    protected virtual async Task WaitForQuantityDecision(bool store)
    {
        var quantity = await m_QuantityPickerPanel.WaitForQuantity();

        if (quantity < 1) { return; }

        if (store)
        {
            var clientItemCollection = m_SelectedItemInfo.Inventory == (IInventory)m_Inventory
                ? m_SelectedItemInfo.ItemCollection
                : m_Inventory.MainItemCollection;

            clientItemCollection.GiveItem(
                (quantity, m_SelectedItemInfo),
                m_StorageInventory.MainItemCollection,
                (info => clientItemCollection.AddItem(info, info.ItemStack)));
        }
        else
        {
            m_StorageInventory.MainItemCollection.GiveItem(
                (quantity, m_SelectedItemInfo),
                m_Inventory.MainItemCollection,
                (info => m_StorageInventory.MainItemCollection.AddItem(info, info.ItemStack)));
        }

        m_StorageInventoryGrid.Draw();
        m_ClientInventoryGrid.Draw();
    }

    public override void CraftSelectedQuantity()
    {
        var quantity = m_RecipePanel.Quantity;

        if (craftingQueue.ContainsKey(m_SelectedRecipe))
        {
            craftingQueue[m_SelectedRecipe] = craftingQueue[m_SelectedRecipe] + quantity;
            m_QueueInventoryGrid.AddItem(new ItemInfo(quantity, m_SelectedRecipe.MainItemAmountOutput.Value.Item), helperList.IndexOf(m_SelectedRecipe));
        }
        else
        {
            craftingQueue.Add(m_SelectedRecipe, quantity);
            helperList.Add(m_SelectedRecipe);
            m_QueueInventoryGrid.AddItem(new ItemInfo(craftingQueue.Count > 1 ? quantity : quantity - 1, m_SelectedRecipe.MainItemAmountOutput.Value.Item), helperList.IndexOf(m_SelectedRecipe));
        }

        if (!isCrafting)
        {
            isCrafting = true;
            StartCoroutine(CraftFromQueue());
        }
    }

    protected override IEnumerator CraftFromQueue()
    {
        int timeToWait = 10; //set some time by default
        int currentItemIndex = 0;
        bool firstItemMovedToCurrentCraftingSlot = false;
        currentlyUsedRecipe = helperList[0];

        if (currentlyUsedRecipe is CustomCraftingRecipe customRecipe)
        {
            timeToWait = customRecipe.CraftingTimeInSeconds;
        }

        while (craftingQueue.Count > 0)
        {
            ItemInfo currentItemInfo = new ItemInfo(1, currentlyUsedRecipe.DefaultOutput.MainItemAmount.Value.Item);
            m_CurrentlyCraftedItemSlot.SetItemInfo(currentItemInfo);
            if (!currentItemIndex.Equals(0) && !firstItemMovedToCurrentCraftingSlot)
            {
                m_QueueInventoryGrid.RemoveItem(currentItemInfo, m_QueueInventoryGrid.GetItemIndex(currentItemInfo));
                firstItemMovedToCurrentCraftingSlot = true;
            }
            

            for (int i = 0; i < timeToWait; i++)
            {
                if (m_removedCurrentlyCraftedItem) // so if we remove currently crafted item skip the time wait and exit this loop
                {
                    i = timeToWait;
                }
                else
                {
                    var span = TimeSpan.FromSeconds(timeToWait - i);
                    m_timerText.text = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);

                    yield return oneSecondTimer;
                    i++;

                    span = TimeSpan.FromSeconds(timeToWait - i);
                    m_timerText.text = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);
                }
            }

            if (!m_removedCurrentlyCraftedItem)
            {
                DoCraft(1);
                craftingQueue[currentlyUsedRecipe]--;
            }
            else
            {
                m_removedCurrentlyCraftedItem = false;
            }

            m_timerText.text = "0:00";
            m_QueueInventoryGrid.RemoveItem(currentItemInfo, m_QueueInventoryGrid.GetItemIndex(currentItemInfo));

            if (craftingQueue.ContainsKey(currentlyUsedRecipe) && craftingQueue[currentlyUsedRecipe] <= 0)
            {
                craftingQueue.Remove(currentlyUsedRecipe);
                helperList.Remove(currentlyUsedRecipe);

                if (helperList.Count > 0)
                {
                    firstItemMovedToCurrentCraftingSlot = false;
                    currentlyUsedRecipe = helperList[0];
                    currentItemIndex++;
                }

                if (currentlyUsedRecipe is CustomCraftingRecipe customRecip)
                {
                    timeToWait = customRecip.CraftingTimeInSeconds;
                }
            }

            m_CurrentlyCraftedItemSlot.DisableImage();
        }

        Debug.Log("Crafted all enqueued items");
        craftingQueue.Clear();
        isCrafting = false;
    }

    public override void DoCraft(int quantity)
    {
        if (quantity >= 1)
        {
            var result = m_Crafter.Processor.Craft(currentlyUsedRecipe, m_StorageInventory, quantity);
            OnCraftComplete(result, currentlyUsedRecipe, m_StorageInventory, quantity);
        }

        DrawRecipes();
        m_RecipePanel.SetQuantity(1);
        m_RecipePanel.Refresh();
    }
}
