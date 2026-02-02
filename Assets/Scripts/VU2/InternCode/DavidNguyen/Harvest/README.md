# 🌾 Harvest Zone System

## Scripts

| Script | Mô tả |
|--------|-------|
| `HarvestZone.cs` | Base class - zone detection + input |
| `HarvestPromptUI.cs` | World Space UI prompt |

## Quick Setup

1. Add **Box Collider** (IsTrigger ✓) vào zone
2. Add **`HarvestZone`** component
3. Set **`zoneType`** phù hợp
4. Set **`interactAction`** → XRI RightHand/Select

## Keyboard Testing

- **Enable Keyboard Testing** = ✓
- **Test Interact Key** = E
- Play → Đi vào zone → Nhấn **E**

## Events

| Event | Khi nào |
|-------|---------|
| `OnInteract` | Player nhấn E hoặc bóp Trigger |
| `OnPlayerEnter` | Player vào zone |
| `OnPlayerExit` | Player ra khỏi zone |
