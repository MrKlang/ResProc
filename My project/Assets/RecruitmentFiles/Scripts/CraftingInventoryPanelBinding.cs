using Opsive.Shared.Game;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.UI.Panels;
using UnityEngine;

public abstract class CraftingInventoryPanelBinding : DisplayPanelBinding
{
    [Tooltip("Bind the user inventory with a specific Identifier ID.")]
    [SerializeField] protected uint u_BindToUserInventoryByIdentifier = 0;
    [Tooltip("Bind the station inventory with a specific Identifier ID.")]
    [SerializeField] protected uint s_BindToStationInventoryByIdentifier = 0;
    [Tooltip("Set the user Inventory to bind to this panel.")]
    [SerializeField] protected Inventory u_Inventory;
    [Tooltip("Set the station Inventory to bind to this panel.")]
    [SerializeField] protected Inventory s_Inventory;


    public Inventory UserInventory
    {
        get => u_Inventory;
        internal set => u_Inventory = value;
    }

    public Inventory StationInventory
    {
        get => s_Inventory;
        internal set => s_Inventory = value;
    }

    /// <summary>
    /// Initialize.
    /// </summary>
    /// <param name="display">The bound display panel.</param>
    /// <param name="force"></param>
    public override void Initialize(DisplayPanel display, bool force)
    {
        var wasInitialized = m_IsInitialized;
        if (wasInitialized && !force) { return; }
        base.Initialize(display, force);

        OnInitializeBeforeInventoryBind();

        BindStationInventory();
    }

    /// <summary>
    /// On Inititalize before the inventory is bound.
    /// </summary>
    protected virtual void OnInitializeBeforeInventoryBind()
    { }

    /// <summary>
    /// Bind the inventory.
    /// </summary>
    public void BindStationInventory()
    {
        if (s_Inventory != null)
        {
            BindInventory(s_Inventory, true);
            return;
        }

        if (s_BindToStationInventoryByIdentifier != 0)
        {
            var identifier = InventorySystemManager.GetInventoryIdentifier(s_BindToStationInventoryByIdentifier);
            if (identifier == null)
            {
                Debug.LogWarning($"The Inventory Identifier with ID '{s_BindToStationInventoryByIdentifier}' could not be found", gameObject);
                return;
            }
            BindInventory(identifier.Inventory, true);
        }
    }

    /// <summary>
    /// Bind the inventory.
    /// </summary>
    public void BindUserInventory()
    {
        if (u_Inventory != null)
        {
            BindInventory(u_Inventory, false);
            return;
        }

        if (u_BindToUserInventoryByIdentifier != 0)
        {
            var identifier = InventorySystemManager.GetInventoryIdentifier(u_BindToUserInventoryByIdentifier);
            if (identifier == null)
            {
                Debug.LogWarning($"The Inventory Identifier with ID '{u_BindToUserInventoryByIdentifier}' could not be found", gameObject);
                return;
            }
            BindInventory(identifier.Inventory, false);
        }
    }

    /// <summary>
    /// Bind the inventory.
    /// </summary>
    /// <param name="inventory">The inventory.</param>
    public void BindInventory(Inventory inventory, bool isStationInventory)
    {
        if (isStationInventory)
        {
            s_Inventory = inventory;
            if (s_Inventory == null)
            {
                Debug.LogWarning("You are binding a Null Inventory, Please make sure the Display Panel Manager, Panel Owner field is set to your main Inventory.");
            }
        }
        else
        {
            u_Inventory = inventory;
            if (u_Inventory == null)
            {
                Debug.LogWarning("You are binding a Null Inventory, Please make sure the Display Panel Manager, Panel Owner field is set to your main Inventory.");
            }
        }

        OnInventoryBound();
    }

    /// <summary>
    /// The inventory was bound.
    /// </summary>
    protected abstract void OnInventoryBound();
}


