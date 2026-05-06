# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RA2-style RTS game built with Unity 2021.3.x and URP. Features deterministic lockstep networking for multiplayer support. Target platforms: WebGL (WeChat Mini Game), Android, Standalone.

## Core Architecture

### Three-Layer Architecture (Strict Separation)

```
┌─────────────────────────────────────────────────────┐
│  Layer 3: Unity Bridge (GameWorldBridge)            │
│  - MonoBehaviour lifecycle, Unity resources          │
│  - File: View/GameWorldBridge.cs                    │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│  Layer 2: Application (Game)                        │
│  - Game lifecycle management, system registration   │
│  - Pure C#, cross-platform                          │
│  - File: Sync/Game.cs                              │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│  Layer 1: Core Logic (zWorld)                       │
│  - ECS architecture, deterministic math             │
│  - Command system, event system                     │
│  - File: Simulation/zWorld.cs                      │
└─────────────────────────────────────────────────────┘
```

**Key Principle**: Lower layers don't know about upper layers. zWorld is pure C# with no Unity dependencies.

### Deterministic Math

All game logic uses fixed-point math for cross-platform determinism:
- `zfloat` - Fixed-point number (replaces float)
- `zVector3` - Fixed-point vector (replaces Vector3)
- `zQuaternion` - Fixed-point quaternion

## Key Directories

| Path | Description |
|------|-------------|
| `Assets/Scripts/Ra2Demo/` | Main game controller and UI |
| `Assets/Scripts/Ra2Demo/UIModule/` | UI panels and events (MVC pattern) |
| `Assets/Resources/Data/Json/` | Runtime config data (Unit.json, Camp.json) |
| `Packages/ZLockstep/` | Lockstep engine, ECS, flow field, RVO |
| `Packages/ZFramework/` | MVC framework, UI management, utilities |
| `../config/excel/` | Excel source files for game config |

## Config Export

Config data flows from Excel to JSON and C#:
```bash
cd ../config
exportConfig.bat  # Converts .xlsx → JSON + C# classes
```

Output locations:
- JSON: `Assets/Resources/Data/Json/`
- C# classes: `Packages/ZLockstep/Runtime/Configs/`

## Command System

All game operations go through commands for determinism and replay support:

```csharp
// Creating and submitting a command
var cmd = new CreateUnitCommand(
    playerId: 0,
    unitType: 1,
    position: new zVector3(0, 0, 0),
    prefabId: 1
);
worldBridge.SubmitCommand(cmd);
```

Command sources: `Local`, `AI`, `Network`, `Replay`

## ECS Pattern

### Key Components (in `Packages/ZLockstep/Runtime/Simulation/ECS/Components/`)
- `TransformComponent` - Position, rotation
- `UnitComponent` - Unit type, player ID, move speed
- `HealthComponent` - Health values
- `AttackComponent` - Attack attributes
- `VelocityComponent` - Movement velocity

### Key Systems (in `Packages/ZLockstep/Runtime/Simulation/ECS/Systems/`)
- `MovementSystem` - Unit movement
- `CombatSystem` - Combat logic
- `HealthSystem` - Health management
- `PresentationSystem` - Sync logic to Unity views

## Adding New Features

### New Unit Type
1. Add data to `../config/excel/Unit.xlsx`
2. Run `exportConfig.bat`
3. Create prefab in `Assets/Resources/Prefabs/`
4. Register in `GameWorldBridge` unit prefabs array

### New Command
1. Create class in `Packages/ZLockstep/Runtime/Sync/Command/Commands/`
2. Inherit from `BaseCommand`
3. Implement `Execute(zWorld world)`
4. Register command type ID

### New System
1. Create in `Packages/ZLockstep/Runtime/Simulation/ECS/Systems/`
2. Implement `ISystem` interface
3. Register in `Game.RegisterSystems()`

## Unity Scene Setup

Main scene: `Assets/Scenes/Ra2Demo.unity`

Required components:
- `GameWorldBridge` - Main game controller
- `Ra2Demo` - Input handling and game flow
- `PresentationSystem` - View synchronization

## Documentation References

| Topic | Location |
|-------|----------|
| Architecture | `Packages/ZLockstep/Runtime/ARCHITECTURE.md` |
| Command System | `Packages/ZLockstep/Runtime/Sync/Command/README_COMMAND_SYSTEM.md` |
| Flow Field | `Packages/ZLockstep/Runtime/Flow/FLOW_FIELD_README.md` |
| Lockstep | `Packages/ZLockstep/Runtime/Sync/LOCKSTEP_GUIDE.md` |
| ECS Systems | `Packages/ZLockstep/Runtime/Simulation/ECS/System.md` |

## Namespaces

- `ZLockstep.Simulation` - Core ECS (zWorld, components, systems)
- `ZLockstep.Sync` - Frame sync, commands, game management
- `ZLockstep.View` - Unity view layer
- `ZLockstep.Core` - Deterministic math (zfloat, zVector3)
- `ZFrame` - MVC framework, UI management
