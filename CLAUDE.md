# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Vespasian is a Unity 2019.4.24f1 space exploration game (2D, top-down) with orbital mechanics simulation, procedural map generation, factions, and a mission system.

## Building and Running

This is a Unity project — there is no CLI build command for day-to-day development. Open the project in **Unity 2019.4.24f1** and use the Editor Play button to run. All scripts are in `Assets/Scripts/`.

An in-game debug console is available (IngameDebugConsole plugin). Methods decorated with `[ConsoleMethod("command.name", "description")]` are callable at runtime.

## Code Architecture

All game scripts live under `Assets/Scripts/Runtime/` (plus `Assets/Scripts/Editor/` for editor tooling). The game is 2D; all positions must have `z = 0` — this is asserted in several places.

### Simulation (`Runtime/Simulation/`)

`Simulation` is a MonoBehaviour that drives a deterministic fixed-update loop. Objects participate by implementing `ISimUpdate` (two methods: `SimUpdate` and `SimRefresh`). Call `Simulation.Register/Unregister` for dynamically spawned objects.

`SimModel` owns the orbital mechanics: it builds a list of all `Orbit` and `GravitySource` objects, then numerically integrates gravity each tick. Path prediction (`CalculateSimPathAsync`) runs on a background thread (except WebGL, where it yields per frame). `SectionedSimPath` wraps a rolling window of computed future path sections.

`SimMovement` is the primary physics component for any moving object (player, AI ships, bullets, rockets). It drives a `Rigidbody` position based on simulated paths and applied forces.

### Map (`Runtime/Map/`)

**Data model** (`Map.cs`): `Map` holds `List<SolarSystem>` and `List<Link>`. Each `SolarSystem` contains a tree of `Body` objects. `BodyRef(systemId, bodyId)` is the stable cross-save identity for any body.

Body hierarchy: `Body` → `OrbitingBody` → `StarOrPlanet` / `Station` / `Comet`; `Belt` is a non-orbiting `Body`. Bodies store properties (mass, radius, temperature, resources, energy, habitability) used for simulation and mission scoring.

`MapGenerator` (ScriptableObject) generates the galaxy procedurally: Delaunay triangulation → pruned link graph → per-system planet generation.

**Runtime management** (`MapComponent`): Handles map generation, system load/unload, warp jumps between systems. When a system loads, `SolarSystem.LoadAsync` instantiates prefabs for all bodies via `BodySpecs` (the prefab registry), then calls `Faction.SpawnSystem` for each faction. Only one system is loaded at a time.

`BodySpecs` maps spec IDs to prefabs/parameters. `BodyGenerator` is the MonoBehaviour on instantiated body prefabs that connects the live GameObject back to its `Body` data model (via `BodyRef`).

### Save System (`Runtime/SaveSystem.cs`)

Attribute-driven serialization to XML using `DataContractSerializer`.

- Implement `ISavable` on any class that can be saved.
- Tag fields/properties with `[Saved]` to serialize them automatically.
- Tag any non-primitive type used in saved fields with `[RegisterSavableType]` — this is required for the DataContractSerializer's known types.
- Call `SaveSystem.RegisterForSaving(this)` from `Awake` for static scene objects.
- Implement `ISavableCustom` for manual save/load logic beyond field reflection.
- Implement `IPostLoadAsync` for async setup that must happen after all objects are loaded (e.g., `MapComponent` loads the current system here).
- Dynamic objects (missions, stations) must be handled inside a registered savable's custom serialization — `SaveSystem` cannot instantiate new Unity objects.

### Missions (`Runtime/Missions/`)

`Missions` (MonoBehaviour, `ISavable`) manages active missions and player credits. Mission generation is delegated to `IMissionFactory` components on the same GameObject (`MissionFindFactory`, `MissionSurveyFactory`, `MissionExpandFactory`, `MissionMapSystemFactory`).

Key interfaces: `IMissionBase` (all missions), `IBodyMission` (missions that "claim" discovered bodies to prevent double-counting), `ITargetBodiesMission` (missions targeting specific known bodies in the world).

Data discovery flows through `DataCatalog.OnDataAdded` → `Missions.PlayerDataCatalogOnDataAdded`, which assigns newly discovered bodies to active missions.

### Factions (`Runtime/AI/`)

`Faction` (MonoBehaviour) is composed with `FactionExpansion` and `FactionSpawns`. `FactionExpansion` accumulates resources from stations (yields based on body properties × station type × faction multipliers), tracks expansion targets, and builds new stations during map generation via `PopulateMap`. Three station types: `HomeStation`, `MiningStation`, `CollectorStation`, `HabitatStation`.

`FactionType` is a flags enum (Player, Pirate, Alien). Each faction has a `DataCatalog` representing what the faction knows about universe bodies.

### Ship Components (`Runtime/Ship/`)

Ships are assembled from MonoBehaviour components. Player input goes through `PlayerController` → `ControllerBase` → `EngineController` → `ThrustComponent` (applies forces to `SimMovement`). AI ships use `AIController` / `AIEnemyBehaviour` in place of `PlayerController`.

Weapons follow a similar component pattern: `WeaponController` → `WeaponComponentBase` subclasses (Laser, Machinegun, RocketLauncher).

Effects are pair-based: `DamageSource`/`DamageReceiver`, `DragSource`/`DragReceiver`, `FuelSource`/`FuelScoop`, `Scanner`/`Scannable`, `DockActive`/`DockPassive`.

### Effects and Data (`Runtime/Effects/`, `Runtime/System/`)

`DataCatalog` stores what data a faction or player has about each body, keyed by `BodyRef`. `DataMask` is a flags enum (Orbit, Basic, Composition, Resources, Habitability). Scanner/Scannable components drive data discovery during gameplay.

`ComponentCache` wraps Unity's `FindObjectOfType` / `FindObjectsOfType` — use it instead of Unity's built-ins for consistency.

### GameLogic (`Runtime/GameLogic.cs`)

A ScriptableObject (singleton-style, referenced from the scene) that coordinates top-level game flow: new game initialization, save/load, jump between systems, and lose condition. Uses a semaphore to prevent concurrent state transitions.

### Key Patterns

- `ComponentCache.FindObjectOfType<T>()` is used everywhere instead of `Object.FindObjectOfType<T>()`.
- Async operations use `Task`/`async`/`await`. Unity-thread continuations are handled via `Awaiters.NextFrame` and `ThreadingX.RunOnUnityThread`.
- `RandomX` is the project's seeded RNG wrapper (used for deterministic procedural generation).
- `GameConstants` (ScriptableObject) holds all global tuning values (gravity constant, thrust multipliers, fuel rates, etc.).
