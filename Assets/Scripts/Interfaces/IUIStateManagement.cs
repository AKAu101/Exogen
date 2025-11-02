using System;

public interface IUIStateManagement
{
    // Properties
    bool IsInventoryVisible { get; }

    // Methods
    void SetInventoryVisible(bool visible);
    void ToggleInventory();

    // Events
    event Action<bool> OnInventoryVisibilityChanged;
}