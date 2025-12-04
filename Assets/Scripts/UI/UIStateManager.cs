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
    private bool isPauseMenuVisible = false;

    public bool IsInventoryVisible => openInventories.Count > 0;
    public bool IsPauseMenuVisible => isPauseMenuVisible;
    public bool IsAnyUIVisible => IsInventoryVisible || IsPauseMenuVisible;

    public event Action<bool> OnInventoryVisibilityChanged;
    public event Action<bool> OnPauseMenuVisibilityChanged;

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
        DebugManager.Log($"UIStateManager.ToggleInventory called. Open inventories count: {openInventories.Count}");

        // Close all open inventories
        if (openInventories.Count > 0)
        {
            // Create a copy to avoid modifying collection during iteration
            var inventoriesToClose = new List<InventoryUI>(openInventories);
            foreach (var inventory in inventoriesToClose)
            {
                if (inventory != null)
                {
                    DebugManager.Log($"UIStateManager: Closing inventory {inventory.name}");
                    inventory.ToggleInventory();
                }
            }
        }
    }

    public void SetPauseMenuVisible(bool visible)
    {
        DebugManager.Log($"UIStateManager.SetPauseMenuVisible called with visible={visible}, current isPauseMenuVisible={isPauseMenuVisible}");

        if (isPauseMenuVisible == visible)
        {
            DebugManager.Log("UIStateManager: State already set, returning early");
            return;
        }

        isPauseMenuVisible = visible;
        DebugManager.Log($"UIStateManager: IsPauseMenuVisible={isPauseMenuVisible}, IsAnyUIVisible={IsAnyUIVisible}");

        // Only track state, don't handle cursor - let PauseMenu handle it like inventory does
        OnPauseMenuVisibilityChanged?.Invoke(visible);

        DebugManager.Log($"UIStateManager: Pause menu visibility changed to: {visible}");
    }

    public void TogglePauseMenu()
    {
        SetPauseMenuVisible(!isPauseMenuVisible);
    }

    /// <summary>
    /// Resets the pause state without triggering cursor lock (used when scene loads).
    /// </summary>
    public void ResetPauseState()
    {
        DebugManager.Log("UIStateManager: ResetPauseState called");
        isPauseMenuVisible = false;
        OnPauseMenuVisibilityChanged?.Invoke(false);
    }
}
