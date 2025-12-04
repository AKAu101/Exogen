using System;

public interface IUIStateManagement
{
    // Properties
    bool IsInventoryVisible { get; }
    bool IsPauseMenuVisible { get; }
    bool IsAnyUIVisible { get; }

    // Methods
    void SetInventoryVisible(bool visible);
    void ToggleInventory();
    void SetPauseMenuVisible(bool visible);
    void TogglePauseMenu();

    // Events
    event Action<bool> OnInventoryVisibilityChanged;
    event Action<bool> OnPauseMenuVisibilityChanged;
}