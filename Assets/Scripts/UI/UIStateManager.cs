using System;
using System.Collections.Generic;
using Generals;
using UnityEngine;

/// <summary>
/// Centralized manager to track UI visibility states across all InventoryUI instances.
/// Implements IUIStateManagement to provide a unified interface for checking if any UI is open.
/// </summary>
public class UIStateManager : Singleton<UIStateManager>, IUIStateManagement
{
    private HashSet<InventoryUI> openInventories = new HashSet<InventoryUI>();

    public bool IsInventoryVisible => openInventories.Count > 0;

    public event Action<bool> OnInventoryVisibilityChanged;

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.Instance.Register<IUIStateManagement>(this);
    }

    // Ensure instance exists - creates GameObject if needed
    public static UIStateManager EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("UIStateManager");
            go.AddComponent<UIStateManager>();
            DontDestroyOnLoad(go);
        }
        return Instance;
    }

    public void RegisterInventoryOpened(InventoryUI inventory)
    {
        bool wasAnyOpen = IsInventoryVisible;
        openInventories.Add(inventory);

        if (!wasAnyOpen && IsInventoryVisible)
        {
            OnInventoryVisibilityChanged?.Invoke(true);
        }
    }

    public void RegisterInventoryClosed(InventoryUI inventory)
    {
        bool wasAnyOpen = IsInventoryVisible;
        openInventories.Remove(inventory);

        if (wasAnyOpen && !IsInventoryVisible)
        {
            OnInventoryVisibilityChanged?.Invoke(false);
        }
    }

    public void SetInventoryVisible(bool visible)
    {
        // This method is part of the interface but not used by the centralized manager
        // Individual InventoryUI instances handle their own visibility
        Debug.LogWarning("SetInventoryVisible called on UIStateManager - use individual InventoryUI.SetInventoryVisible instead");
    }

    public void ToggleInventory()
    {
        // This method is part of the interface but not used by the centralized manager
        // Individual InventoryUI instances handle their own toggling
        Debug.LogWarning("ToggleInventory called on UIStateManager - use individual InventoryUI.ToggleInventory instead");
    }
}
