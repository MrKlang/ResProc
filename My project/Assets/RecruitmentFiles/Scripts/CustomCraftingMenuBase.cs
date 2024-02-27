using System;
using System.Collections;
using System.Collections.Generic;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Crafting;
using Opsive.UltimateInventorySystem.Crafting.RecipeTypes;
using Opsive.UltimateInventorySystem.UI.Menus.Crafting;
using Opsive.UltimateInventorySystem.UI.Panels;
using Opsive.UltimateInventorySystem.UI.Panels.Crafting;
using Opsive.UltimateInventorySystem.UI.Panels.ItemViewSlotContainers;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class BoolUnityEvent : UnityEvent<bool> { }

public abstract class CustomCraftingMenuBase : InventoryPanelBinding
{
    [Tooltip("The crafter has the list of available recipes to craft.")]
    [SerializeField] protected internal Crafter m_Crafter;
    [Tooltip("The storage Inventory.")]
    [SerializeField] protected Inventory m_StorageInventory;
    [Tooltip("The recipes display.")]
    [SerializeField] protected internal CraftingRecipeGrid m_CraftingRecipeGrid;
    [Tooltip("The selected recipe panel.")]
    [SerializeField] protected internal RecipePanel m_RecipePanel;
    [Tooltip("Draw recipes on open.")]
    [SerializeField] protected bool m_DrawRecipesOnOpen = true;
    [Tooltip("An Event called when the crafting is done.")]
    [SerializeField] protected BoolUnityEvent m_OnCraftComplete;

    protected Dictionary<CraftingRecipe, int> craftingQueue = new Dictionary<CraftingRecipe, int>();
    protected List<CraftingRecipe> helperList = new List<CraftingRecipe>();
    protected CraftingRecipe currentlyUsedRecipe = null;
    protected bool isCrafting = false;

    protected WaitForSecondsRealtime oneSecondTimer = new WaitForSecondsRealtime(1);

    protected CraftingRecipe m_SelectedRecipe;

    public Crafter Crafter => m_Crafter;
    public CraftingRecipe SelectedRecipe => m_SelectedRecipe;

    public override void Initialize(DisplayPanel display, bool force)
    {
        var wasInitialized = m_IsInitialized;
        if (wasInitialized && !force) { return; }
        base.Initialize(display, force);

        if (wasInitialized == false)
        {
            //Only do it once even if forced.
            if (m_Inventory == null)
            {
                m_Inventory = GameObject.FindWithTag("Player")?.GetComponent<Inventory>();
            }

            //Only do it once even if forced.
            if (m_StorageInventory == null)
            {
                m_StorageInventory = GameObject.FindWithTag("TestCraftingTable")?.GetComponent<Inventory>();
            }

            if (m_Crafter != null) { m_Crafter.Initialize(false); }

            m_CraftingRecipeGrid.SetParentPanel(m_DisplayPanel);
            m_CraftingRecipeGrid.Initialize(false);

            m_CraftingRecipeGrid.OnElementSelected += CraftingRecipeSelected;
            m_CraftingRecipeGrid.OnEmptySelected += (x) => CraftingRecipeSelected(null, x);
            m_CraftingRecipeGrid.OnElementClicked += CraftingRecipeClicked;

            var tabControl = m_CraftingRecipeGrid.TabControl;

            if (tabControl != null)
            {
                tabControl.Initialize(false);
                tabControl.OnTabChange += HandleTabChange;

                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    var tab = tabControl.TabToggles[i];
                    var craftingTabData = tab.GetComponent<CraftingTabData>();
                    if (craftingTabData != null)
                    {
                        craftingTabData.Initialize(false);
                    }
                }

                HandleTabChange(-1, tabControl.TabIndex, false);
            }
        }
    }

    /// <summary>
    /// Set the crafter.
    /// </summary>
    /// <param name="crafter">The crafter to set.</param>
    public virtual void SetCrafter(Crafter crafter)
    {
        m_Crafter = crafter;
        m_Crafter.Initialize(false);
        DrawRecipes();
    }

    /// <summary>
    /// Refresh the display.
    /// </summary>
    public virtual void DrawRecipes()
    {
        m_CraftingRecipeGrid.SetElements(m_Crafter.GetRecipes());
        m_CraftingRecipeGrid.Draw();
    }

    /// <summary>
    /// Set the inventory.
    /// </summary>
    protected override void OnInventoryBound()
    {
        m_RecipePanel.SetInventory(m_StorageInventory);
    }

    /// <summary>
    /// Handle the On Open event.
    /// </summary>
    public override void OnOpen()
    {
        base.OnOpen();

        m_RecipePanel.SetInventory(m_StorageInventory);

        if (m_DrawRecipesOnOpen)
        {
            var tabControl = m_CraftingRecipeGrid.TabControl;
            if (tabControl != null)
            {
                HandleTabChange(-1, tabControl.TabIndex, true);
            }
            else
            {
                DrawRecipes();
            }
        }

        m_CraftingRecipeGrid.SelectButton(0);
    }

    /// <summary>
    /// Handle a tab change.
    /// </summary>
    /// <param name="previousIndex">The previous tab index.</param>
    /// <param name="newIndex">The new tab index.</param>
    private void HandleTabChange(int previousIndex, int newIndex)
    {
        HandleTabChange(previousIndex, newIndex, true);
    }

    /// <summary>
    /// Handle the tab change.
    /// </summary>
    /// <param name="previousIndex">The previous tab index.</param>
    /// <param name="newIndex">The new tab index.</param>
    /// <param name="draw">Should the recipes be drawn?</param>
    protected virtual void HandleTabChange(int previousIndex, int newIndex, bool draw)
    {
        if (previousIndex == newIndex) { return; }

        var craftingTabData = m_CraftingRecipeGrid.TabControl.CurrentTab.GetComponent<CraftingTabData>();

        if (craftingTabData == null)
        {
            Debug.LogWarning("The selected tab is either null or does not have an CraftingTabData", gameObject);
            return;
        }

        if (craftingTabData.CraftingFilter != null)
        {
            m_CraftingRecipeGrid.BindGridFilterSorter(craftingTabData.CraftingFilter);
        }

        if (draw)
        {
            DrawRecipes();
        }
    }

    /// <summary>
    /// update when the crafting amount changes.
    /// </summary>
    /// <param name="amount">The new amount.</param>
    public virtual void CraftingAmountChanged(int amount)
    {
        m_RecipePanel.SetQuantity(amount);
        m_RecipePanel.Refresh();
    }

    /// <summary>
    /// A recipe is selected.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="index">The index.</param>
    public virtual void CraftingRecipeSelected(CraftingRecipe recipe, int index)
    {
        m_RecipePanel.SetRecipe(recipe);

        if (m_SelectedRecipe == recipe) { return; }

        m_SelectedRecipe = recipe;
        CraftingAmountChanged(1);
    }

    /// <summary>
    /// Recipe is clicked.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="index">The index.</param>
    public virtual void CraftingRecipeClicked(CraftingRecipe recipe, int index)
    {
        m_RecipePanel.SetRecipe(recipe);

        if (m_SelectedRecipe == recipe) { return; }

        m_SelectedRecipe = recipe;
        CraftingAmountChanged(1);
    }

    /// <summary>
    /// Wait for the player to select a quantity.
    /// </summary>
    /// <returns>The task.</returns>
    public virtual void CraftSelectedQuantity()
    {
        var quantity = m_RecipePanel.Quantity;

        if (craftingQueue.ContainsKey(m_SelectedRecipe))
        {
            craftingQueue[m_SelectedRecipe] = craftingQueue[m_SelectedRecipe] + quantity;
        }
        else
        {
            craftingQueue.Add(m_SelectedRecipe, quantity);
            helperList.Add(m_SelectedRecipe);
        }

        if (!isCrafting)
        {
            isCrafting = true;
            StartCoroutine(CraftFromQueue());
        }
    }

    protected virtual IEnumerator CraftFromQueue()
    {
        int currentKeyIndex = 0;
        int timeToWait = 10; //set some time JUST IN CASE
        currentlyUsedRecipe = helperList[currentKeyIndex];

        if (currentlyUsedRecipe is CustomCraftingRecipe customRecipe)
        {
            timeToWait = customRecipe.CraftingTimeInSeconds;
        }

        while (craftingQueue.Count > 0)
        {
            for (int i = 0; i < timeToWait; i++)
            {
                yield return oneSecondTimer;
                i++;
            }

            DoCraft(1);
            craftingQueue[currentlyUsedRecipe]--;

            if(craftingQueue[currentlyUsedRecipe] <= 0)
            {
                craftingQueue.Remove(currentlyUsedRecipe);
                helperList.Remove(currentlyUsedRecipe);

                currentKeyIndex++;

                if (helperList.Count > 0)
                {
                    currentlyUsedRecipe = helperList[currentKeyIndex] ?? null;
                }

                if (currentlyUsedRecipe is CustomCraftingRecipe customRecip)
                {
                    timeToWait = customRecip.CraftingTimeInSeconds;
                }
            }
        }

        Debug.Log("Crafted all enqueued items");

        isCrafting = false;
    }

    /// <summary>
    /// Do craft the item.
    /// </summary>
    /// <param name="quantity">The quantity to craft.</param>
    public virtual void DoCraft(int quantity)
    {
        if (quantity >= 1)
        {
            var result = m_Crafter.Processor.Craft(m_SelectedRecipe, m_StorageInventory, quantity);
            OnCraftComplete(result, m_SelectedRecipe, m_StorageInventory, quantity);
        }

        DrawRecipes();
        m_RecipePanel.SetQuantity(1);
        m_RecipePanel.Refresh();
    }

    /// <summary>
    /// When the Craft has been complete send an event.
    /// </summary>
    /// <param name="result">The crafting Result.</param>
    /// <param name="selectedRecipe">The selected Recipe.</param>
    /// <param name="inventory">The Inventory where the item was added.</param>
    /// <param name="quantity">The quantity crafted.</param>
    public virtual void OnCraftComplete(CraftingResult result, CraftingRecipe selectedRecipe, Inventory inventory, int quantity)
    {
        m_OnCraftComplete.Invoke(result.Success);
    }
}
