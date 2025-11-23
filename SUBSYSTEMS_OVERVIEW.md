# Exogen - Subsystems Overview

## Project Overview

Exogen is a Unity-based first-person survival game featuring inventory management, crafting mechanics, oxygen-based survival systems, and atmospheric environmental transitions. The project uses a modular architecture with clear separation of concerns across multiple subsystems.

---

## Core Architecture

### Service Locator Pattern
**Location:** `Assets/Scripts/Core/ServiceLocator.cs`

The Service Locator acts as a central dependency injection container, enabling loose coupling between systems.

**Key Features:**
- Register services by interface type using `Register<T>(service)`
- Retrieve services using `Get<T>()`
- Check service availability with `IsRegistered<T>()`
- Enables subsystems to communicate without direct dependencies

**Example Usage:**
```csharp
// Register a service
ServiceLocator.Register<IInventorySystem>(inventorySystem);

// Retrieve and use a service
var inventory = ServiceLocator.Get<IInventorySystem>();
inventory.AddItem(itemData, amount);
```

---

## 1. Inventory Subsystem

**Location:** `Assets/Scripts/Inventory/`

The inventory system is the backbone of item management in Exogen. It handles storage, movement, stacking, and interaction with items.

### Core Components

#### InventorySystem (Singleton)
**File:** `InventorySystem.cs`

The central manager for all inventory operations.

**Configuration:**
- Maximum inventory size: 20 slots
- Maximum stack size: 99 items
- Hand equipment slots: 17 (left hand), 18 (right hand)

**Key Features:**
- Add/remove items with automatic stacking
- Move items between inventories (supports multiple inventory types)
- Item swapping and combining
- Death chest spawning system
- Event-driven notifications for UI updates

**Events:**
- `OnItemAdded` - Fired when an item is added to inventory
- `OnItemRemoved` - Fired when an item is removed
- `OnItemMoved` - Fired when an item moves between slots
- `OnItemSwapped` - Fired when two items are swapped

#### InventoryData
**File:** `InventoryData.cs`

A simple data container that stores items in a dictionary structure (slot index → ItemStack). Can be attached to players, chests, or any other container entity.

#### ItemStack
**File:** `ItemStack.cs`

Represents a quantity of items, combining ItemData (the item type) with an amount (quantity).

### UI Components

#### InventoryUI
**File:** `InventoryUI.cs`

Manages the visual representation of inventories.

**Features:**
- Maintains mapping between slot indices and UI elements
- Handles inventory visibility toggling
- Cursor locking/unlocking when opening inventory
- Listens to inventory events and updates visuals accordingly
- Supports multiple simultaneous inventories (player, chests, crafting)

#### ItemSlot
**File:** `ItemSlot.cs`

Represents a single inventory slot in the UI.

**Features:**
- Displays item icon and quantity
- Hover effects with sprite swapping
- Right-click context menu support
- Drag-and-drop interaction support

#### ItemDragHandler
**File:** `ItemDragHandler.cs`

Handles all drag-and-drop interactions for items.

**Features:**
- Creates visual feedback during dragging
- Raycasting to detect valid drop targets
- Supports moving items between different inventories
- Validates drop locations using layer masks

#### ItemContextMenu
**File:** `ItemContextMenu.cs`

Provides right-click menu options for items.

**Available Actions:**
- Consume (if item is consumable)
- Drop item into the world

### Specialized Features

#### DeathChest
**File:** `DeathChest.cs`

Spawned at the player's death location to preserve inventory.

**Features:**
- Holds all items from player's inventory
- Players can interact to retrieve items
- Auto-destroys when emptied
- Prevents item loss on death

#### PickupItem
**File:** `PickupItem.cs`

Attached to items in the game world.

**Features:**
- Detects player interaction
- Adds item to player's inventory
- Destroys world item after pickup

---

## 2. Player Subsystem

**Location:** `Assets/Scripts/Player/`

Manages all player-related functionality including movement, health, oxygen, equipment, and interactions.

### FirstPersonController
**File:** `FirstPersonController.cs`

Handles player movement and camera control.

**Movement Features:**
- Walk speed: 5 m/s
- Sprint speed: 8 m/s
- Jump height: 2 units
- Gravity: -18 m/s²
- Ducking/crouching mechanics
- Ground checking with sphere cast
- Head clearance checking for standing up

**Camera Control:**
- Mouse sensitivity: 2.0 (configurable)
- Pitch clamping: ±80 degrees
- Smooth mouse look

**Input Integration:**
- Uses Unity's new Input System
- Blocks input when inventory is open
- Callbacks: `OnMove()`, `OnLook()`, `OnJump()`, `OnDuck()`, `OnSprint()`

### PlayerHealth
**File:** `PlayerHealth.cs`

Manages player health and death/respawn mechanics.

**Features:**
- Current/max health tracking
- Damage and healing methods
- Death triggered by oxygen depletion
- Configurable respawn delay
- Drops inventory into death chest
- Resets player to spawn position

**Death Flow:**
1. Player dies (health/oxygen depleted)
2. Inventory dumped into death chest at death location
3. Player controls disabled
4. Wait for respawn delay
5. Teleport to spawn point
6. Restore health and oxygen
7. Re-enable controls

### Oxygen System
**File:** `Oxygen.cs`

Core survival mechanic that depletes outside safe zones.

**Configuration:**
- Normal depletion rate: 1 unit/second
- Sprint depletion rate: 2.5 units/second
- Vignette warning threshold: 8 units
- Max vignette intensity: 1.0

**Features:**
- Depletes when outside "Oxygen Area" zones
- Restores when inside safe zones
- Faster depletion when sprinting
- Visual vignette effect as oxygen gets low
- Triggers death when oxygen reaches zero

**UI Integration:**
- Updates oxygen slider in real-time
- Post-processing vignette effect for low oxygen warning

### EquipmentManager
**File:** `EquipmentManager.cs`

Manages items equipped in the player's hands.

**Features:**
- Monitors hand slots (17 = left, 18 = right)
- Spawns item prefabs in hand positions
- Makes equipped items follow camera rotation
- Disables physics on equipped items
- Automatically updates when inventory changes

### LanternController
**File:** `LanternController.cs`

Specialized equipment for the lantern item.

**Configuration:**
- Max lantern time: 30 seconds
- Lumini recharge amount: 30 seconds

**Features:**
- Timer-based charge depletion
- Rechargeable via Lumini pickups
- Light follows camera position
- Different hand positions for left/right hands
- Automatically enables/disables based on equipment

### PlayerInteractionController
**File:** `PlayerInteractionController.cs`

Handles player interaction with world objects.

**Features:**
- Sphere raycast from camera for detection
- Configurable interaction reach distance
- Highlights interactable objects with outline effect
- Shows interaction prompt UI
- Triggers interaction on key press (E)

---

## 3. Audio Subsystem

**Location:** `Assets/Scripts/Audio/`

Manages all game audio using FMOD integration.

### AudioManager
**File:** `AudioManager.cs`

Central audio management system using FMOD.

**FMOD Integration:**
- Loads Master banks at startup
- Creates persistent event instances
- Parameter-based audio control

**Features:**

**Footstep System:**
- Surface-based sounds (stone vs metal)
- Jump and landing sounds
- Sprint footstep variations
- Integrated with FirstPersonController

**Environmental Audio:**
- Dynamic wind sounds
- Inside/outside audio transitions
- Parameter adjustments based on location
- Integrated with AtmosphereTransition

**Volume Control:**
- Master VCA control
- Methods: `SetMasterVolume()`, `GetMasterVolume()`
- Configurable volume settings

---

## 4. Crafting Subsystem

**Location:** `Assets/Scripts/Crafting/`

Recipe-based item crafting system.

### CraftingSystem
**File:** `CraftingSystem.cs`

Manages crafting recipes and validation.

**Features:**
- Recipe dictionary for fast lookup
- Order-independent recipes (A+B = B+A)
- Method: `TryGetRecipe(ItemA, ItemB, out RecipeData)`

**Recipe Design:**
- Two ingredients → One output
- Uses ScriptableObject data
- Normalized recipe keys for order independence

### CraftingUI
**File:** `CraftingUI.cs`

Visual interface for crafting.

**Features:**
- 2 input slots + 1 output slot
- Real-time recipe validation
- Removes ingredients and creates output
- Integrates with InventorySystem

---

## 5. Item Interaction Subsystem

**Location:** `Assets/Scripts/ItemInteraction/`

Handles world object interactions and pickups.

### Interactable
**File:** `Interactable.cs`

Makes objects interactable in the world.

**Features:**
- Outline effect for visual feedback
- UnityEvent callbacks for custom behavior
- Custom interaction messages
- Methods: `Interact()`, `EnableOutline()`, `DisableOutline()`

### LuminiPickup
**File:** `LuminiPickup.cs`

Special pickup that recharges the lantern.

**Features:**
- Requires lantern to be equipped
- Recharges lantern on pickup
- Optional pickup effects/sounds
- Self-destructs after use
- Integrates with LanternController

### InteractionTextUI
**File:** `InteractionTextUI.cs`

Displays interaction prompts to the player.

**Features:**
- Shows interaction key prompt
- Enable/disable methods
- Singleton pattern for global access

---

## 6. Environment Subsystem

**Location:** `Assets/Scripts/Environment/`

Manages dynamic environmental atmosphere.

### AtmosphereTransition
**File:** `AtmosphereTransition.cs`

Controls inside/outside environmental effects.

**Features:**

**Location Detection:**
- Trigger colliders detect player position
- Tracks inside/outside state
- Updates audio and visual systems

**Post-Processing Transitions:**
- Fog density adjustments
- Bloom intensity changes
- Film grain effects
- Chromatic aberration
- Motion blur adjustments

**Smooth Interpolation:**
- Configurable transition speed
- Lerp-based smooth changes
- No jarring visual transitions

**Integration:**
- Drives AudioManager audio parameters
- Controls post-processing volume
- Affects player immersion

---

## 7. Data Definition Subsystem

**Location:** `Assets/Scripts/ScriptableObjects/`

Data-driven asset definitions using Unity's ScriptableObject system.

### ItemData
**File:** `ItemData.cs`

Defines individual item properties.

**Properties:**
- Name, icon, description
- Icon sizing information
- World prefab for drops
- Consumable flag
- Created via: `Scriptable Objects/ItemData` menu

### RecipeData
**File:** `RecipeData.cs`

Defines crafting recipes.

**Properties:**
- First ingredient (ItemData)
- Second ingredient (ItemData)
- Crafted result (ItemData)
- Created via: `Scriptable Objects/RecipeData` menu

---

## 8. Scene Management Subsystem

**Location:** `Assets/Scripts/UI/`

Handles scene loading and transitions.

### SceneChanger
**File:** `SceneChanger.cs`

Manages all scene transitions.

**Features:**
- Async scene loading with progress bar
- Minimum load time prevents flashing
- Static methods for global access

**Available Methods:**
- `LoadSceneByName(string)` / `LoadSceneByIndex(int)`
- `LoadNextScene()` / `LoadPreviousScene()`
- `ReloadCurrentScene()`
- `QuitGame()`

---

## 9. Utility Subsystem

**Location:** `Assets/Scripts/Generals/` and root scripts

Reusable utilities and helper classes.

### Singleton<T>
**File:** `Singleton.cs`

Generic singleton pattern for MonoBehaviours.

**Features:**
- Automatic instance management
- Prevents duplicates
- Cleanup on application quit

### DictionaryExtension
**File:** `DictionaryExtension.cs`

Extension methods for dictionaries.

**Methods:**
- `SwapEntries<K,V>()` - Swap two entries in same dictionary
- `SwapEntries<K,V>()` - Swap entries across two dictionaries
- Used by inventory for item swapping

### MouseInputUtility
**File:** `MouseInputUtility.cs`

Mouse input helper methods.

**Methods:**
- `GetRawMouse()` - Raw screen position
- `GetMousePositionInWorldSpace()` - World position via raycast
- Uses Unity's new Input System

### LayerHelpers
**File:** `LayerHelpers.cs`

Physics layer utility methods.

**Methods:**
- `IsInLayerMask()` - Check if GameObject is in layer mask
- Used for slot detection in drag handlers

### DebugManager
**File:** `DebugManager.cs`

Centralized debug logging system.

**Features:**
- Global debug toggle (`enableDebugLogs`)
- Methods: `Log()`, `LogWarning()`, `LogError()`
- Easy debug control for builds

---

## System Interactions

### Initialization Flow

```
Game Start
  ↓
Singleton Managers (Awake)
  ├→ InventorySystem → Register with ServiceLocator
  ├→ CraftingSystem → Register with ServiceLocator
  └→ InventoryUI → Register with ServiceLocator
  ↓
Other Systems (Start) - Lazy Initialization
  ├→ FirstPersonController → Get IUIStateManagement
  ├→ PlayerHealth → Get IInventorySystem
  └→ EquipmentManager → Get IInventorySystem
```

### Item Pickup Flow

```
Player interacts with world item
  ↓
PlayerInteractionController detects interaction
  ↓
Interactable.Interact() called
  ↓
PickupItem.PickUp() executes
  ↓
InventorySystem.AddItem() called
  ↓
OnItemAdded event fired
  ├→ InventoryUI updates visual display
  ├→ EquipmentManager updates hand visual (if hand slots)
  └→ LanternController enables light (if lantern)
```

### Item Movement Flow

```
User drags item
  ↓
ItemDragHandler.OnBeginDrag()
  └→ Creates visual drag icon
  ↓
ItemDragHandler.OnDrag()
  └→ Updates icon position
  ↓
ItemDragHandler.OnEndDrag()
  └→ Raycasts for target slot
  └→ InventorySystem.TryMoveItem()
      ├→ Validates move
      ├→ Combines stacks if same item
      └→ Fires OnItemMoved/OnItemSwapped
  ↓
All listeners update their state
  ├→ InventoryUI updates visuals
  ├→ EquipmentManager updates hands
  └→ LanternController updates light
```

### Player Death and Respawn Flow

```
Oxygen depletes to 0
  ↓
PlayerHealth.Die()
  ├→ Spawn DeathChest at death position
  ├→ Dump all inventory items into chest
  ├→ Clear player inventory
  ├→ Disable player controls
  └→ Wait for respawn delay
  ↓
Respawn()
  ├→ Teleport to spawn position
  ├→ Reset health to maximum
  ├→ Reset oxygen to maximum
  └→ Re-enable controls
```

### Atmosphere Transition Flow

```
Player enters/exits trigger zones
  ↓
Oxygen.OnTriggerEnter/Exit()
  └→ Change oxygen depletion behavior

AtmosphereTransition.OnTriggerEnter/Exit()
  └→ Set inside/outside state
  ↓
AudioManager adjusts audio
  ├→ Wind volume changes
  └→ Footstep surface parameters

AtmosphereTransition.Update()
  └→ Smooth lerp transitions
      ├→ Fog density
      ├→ Bloom intensity
      ├→ Film grain
      ├→ Chromatic aberration
      └→ Motion blur
```

---

## Design Patterns Used

| Pattern | Implementation | Purpose |
|---------|----------------|---------|
| **Singleton** | Multiple managers | Ensure single instance |
| **Service Locator** | ServiceLocator.cs | Dependency injection |
| **Observer/Events** | InventorySystem events | Decoupled notifications |
| **Factory** | EquipmentManager | Dynamic item instantiation |
| **Interface Segregation** | IInventorySystem, etc. | Loose coupling |
| **Registry** | InventoryUI slot tracking | Multi-UI management |
| **Multi-Instance** | InventoryData | Support multiple containers |

---

## Key Interfaces

**Location:** `Assets/Scripts/Interfaces/`

### IInventorySystem
Contract for inventory operations (add, remove, move items).

### IInventoryData
Contract for inventory data storage (dictionary of slots).

### IUIStateManagement
Contract for UI visibility and state control.

### IInteractionText
Contract for interaction text display.

---

## Configuration Reference

### Inventory Settings
- Max slots: 20
- Max stack size: 99
- Left hand slot: 17
- Right hand slot: 18

### Player Movement
- Walk speed: 5 m/s
- Sprint speed: 8 m/s
- Jump height: 2 units
- Gravity: -18 m/s²
- Mouse sensitivity: 2.0
- Look angle limit: 80°

### Oxygen System
- Normal depletion: 1 unit/sec
- Sprint depletion: 2.5 units/sec
- Vignette threshold: 8 units
- Max vignette: 1.0

### Lantern System
- Max charge time: 30 seconds
- Lumini recharge: 30 seconds

### Audio (FMOD)
- Master bank loaded at startup
- Dynamic parameters: Volume, Surface
- VCA-based volume control

---

## Project Structure Summary

```
Assets/
├── Scripts/
│   ├── Core/               # Service Locator, events, helpers
│   ├── Inventory/          # Complete inventory system (10 files)
│   ├── Player/             # Player mechanics (6 files)
│   ├── Audio/              # FMOD audio management
│   ├── Crafting/           # Recipe system (2 files)
│   ├── ItemInteraction/    # World interactions (3 files)
│   ├── Environment/        # Atmosphere transitions
│   ├── ScriptableObjects/  # Data definitions
│   ├── UI/                 # Scene management
│   ├── Generals/           # Utilities (4 files)
│   └── Interfaces/         # System contracts (4 files)
├── Prefabs/
│   ├── Oxygen System/
│   ├── Mobs/
│   ├── Items/
│   └── ScriptPrefabs/
├── ScriptableObjects/
│   ├── Items/              # Item definitions
│   └── Recipes/            # Craft recipes
├── Audio/                  # FMOD banks
├── Art/                    # Models, textures, materials
└── 3rd Party/              # QuickOutline, skyboxes, etc.
```

---

## Notable Implementation Details

1. **Lazy Initialization:** Services registered in Awake(), retrieved in Start()
2. **Multi-Inventory Support:** System designed for player, chests, crafting tables
3. **Order-Independent Recipes:** Normalized keys ensure A+B = B+A
4. **Event-Driven UI:** All UI updates react to inventory events
5. **Non-Physics Equipped Items:** Hand items use kinematic rigidbodies
6. **Post-Processing Transitions:** Smooth atmosphere via URP volume
7. **Death Preservation:** Items stored in death chests, not lost
8. **Visual Oxygen Warning:** Vignette effect before death
9. **Outline Interactions:** QuickOutline for highlighting interactables
10. **Parameter-Based Audio:** FMOD parameters for dynamic sound

---

## Extension Points

The system is designed to be extended easily:

- **New Items:** Create ItemData ScriptableObjects
- **New Recipes:** Create RecipeData ScriptableObjects
- **New Interactables:** Add Interactable component with UnityEvents
- **New Inventory Types:** Instantiate InventoryData on new entities
- **New Services:** Register via ServiceLocator
- **New UI States:** Implement IUIStateManagement
- **New Audio Events:** Add to FMOD and reference in AudioManager

---

## Conclusion

Exogen features a well-architected survival game framework with:
- Clear separation of concerns across subsystems
- Event-driven communication for loose coupling
- Service Locator pattern for dependency management
- Extensible data-driven design via ScriptableObjects
- Robust inventory and crafting systems
- Immersive atmosphere and audio systems
- Player-focused survival mechanics

This architecture provides a solid foundation for continued development and feature expansion.
