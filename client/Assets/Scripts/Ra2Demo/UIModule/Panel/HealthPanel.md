# HealthPanel UI Structure

## Canvas (Environment)
- HealthPanel
  - GreenHp
    - Background
    - Fill
  - RedHp
    - Background
    - Fill

## Components Required

### HealthPanel GameObject
- UIModel attribute with:
  - panelID = "HealthPanel"
  - panelPath = "HealthPanel"
  - panelName = "血量面板"
  - panelUIDepthType = ClientUIDepthTypeID.GameTop

### Health Text Elements
- Health/Value: TMP_Text for current health value
- Health/MaxValue: TMP_Text for max health value (displayed as "/{maxHealth}")
- Health/Bar: Image component for health bar visualization

## Implementation Notes

1. The HealthPanel should be placed in the UI hierarchy under the Canvas
2. The health bar should use a horizontal fill type with left-to-right filling
3. The health text elements should be positioned appropriately relative to the health bar
4. The panel should be initially hidden and shown when needed via the HealthEvent
5. The health bar color should change based on health percentage:
   - Green (>70% health)
   - Yellow (30%-70% health)
   - Red (<30% health)

## Usage Example

```csharp
// In your game logic, dispatch HealthEvent when unit health changes
Frame.DispatchEvent(new HealthEvent(currentHealth, maxHealth));

// The Ra2Processor will handle the event and update the HealthPanel
```