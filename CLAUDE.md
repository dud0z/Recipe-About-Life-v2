# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Guidelines

- 모든 답변과 설명은 반드시 한국어(Korean)로 작성해야 합니다.
- 모델 사용 원칙: 이 프로젝트의 모든 코드 분석, 구조 설계, 문제 해결 및 답변 생성은 반드시 가장 높은 추론 능력을 가진 'Opus (4.6)' 모델을 사용하여 진행할 것.

## Project Overview

**Recipe About Life** (인생에 대한 레시피) — A Unity 2D mobile healing cooking simulation + interactive story game. A widowed chef travels the country with a food truck, selling hotdogs and reconnecting with people.

- **Engine:** Unity 6000.0.62f1
- **Language:** C# (.NET Standard 2.1, C# 7.3+)
- **Platform:** Mobile (portrait), single player
- **Namespace:** `RecipeAboutLife` (sub-namespaces: `.Events`, `.Cooking`, `.Dialogue`, etc.)

## Build & Development

This is a Unity project — there is no CLI build/test pipeline. Open the project in Unity Editor 6000.0.62f1. The solution file is `Recipe-About-Life-v2.slnx`.

**Scenes** (in `Assets/Scenes/`):
- `MainMenuScene` — Entry point, settings
- `LobbyScene` — Stage/map selection
- `GamePlayScene` — Main cooking gameplay

## Architecture

### Core Game Loop

```
Lobby → Cooking (5 Customers per stage) → Final Dialogue → Results → Lobby
```

Each stage has 5 NPCs. Stages 1-2 advance story dialogue; Stage 3 unlocks ingredients and the next map. Daily coin goals must be met to progress.

### Key Systems & Their Locations

All scripts live under `Assets/Scripts/`:

| System | Directory | Entry Point | Role |
|--------|-----------|-------------|------|
| Cooking | `Cooking/` | `SimpleCookingManager.cs` | 6-step FSM (StickPickup → Ingredient → Batter → Frying → Topping → Completed) |
| NPC | `NPC/` | `NPCSpawnManager.cs` | Spawns 5 random NPCs per stage, handles lifecycle |
| Dialogue | `Dialogue/` | `NPCDialogueController.cs`, `StageStoryController.cs` | ScriptableObject-based NPC dialogue and story triggers |
| Orders | `Orders/` | `OrderManager.cs` | Order pool and validation |
| Events | `Events/` | `EventSystem.cs` | Global event bus (two APIs — see below) |
| Managers | `Managers/` | `GameManager.cs`, `ScoreManager.cs`, `GameUIManager.cs` | Day/money/pause, scoring/rewards, UI state |
| Lobby | `Lobby/` | `LobbyManager.cs` | Stage selection, truck animation, unlock tracking |

### Event System (Dual API)

Defined in `Assets/Scripts/Events/EventSystem.cs`:

1. **`EventManager`** — String-keyed, loosely-typed: `EventManager.Subscribe("name", handler)` / `EventManager.Trigger("name", data)`
2. **`GameEvents`** — Type-safe static events: `GameEvents.OnRecipeCompleted`, `GameEvents.OnAllCustomersServed`, `GameEvents.OnNPCServed`, etc.

Prefer `GameEvents` for new code. Both coexist throughout the codebase.

### Patterns

- **Singletons** for global managers: `GameManager`, `ScoreManager`, `SimpleCookingManager`
- **ScriptableObjects** for data: `NPCData`, `OrderData`, `NPCDialogueSet`, `StoryNPCConfig`
- **FSM** in `SimpleCookingManager` via `CookingPhase` enum
- **Component composition** on NPC GameObjects: `NPCMovement` + `NPCOrderController` + `NPCDialogueController`

### NPC Lifecycle Flow

```
NPCSpawnManager.SpawnNPC() → NPCMovement walks in → Intro dialogue →
NPCOrderController.RequestOrder() → Order dialogue → Player cooks 6 steps →
SimpleCookingManager.ServeHotdog() → ScoreManager validates → Success/Fail dialogue →
NPCMovement walks out → Next NPC spawns (repeat ×5) → Stage complete
```

### Cooking Scoring

Frying timing is critical: 0-3s Raw, 3-7s Yellow, **7-9s Golden (optimal)**, 9-11s Brown, 11s+ Burnt. Mental system (0-3 range) desaturates the screen on mistakes (wrong filling, wrong sauce, burnt food).

## Deprecated Code

- `StageDialogueController.cs` and `StageDialogueData.cs` are deprecated — use `StageStoryController.cs` instead
- See `UNUSED_SCRIPTS.md` for full list

## Git Conventions

- **Branches:** `main` ← `develop` ← `feature/*`, `refactor/*`
- **Commit messages:** Korean, short descriptions (e.g., `fix : 두번째 손님부터 소스 이상한거 고침`)

## Key Data Types

- **`HotdogRecipe`** (`Cooking/`): Tracks stick, filling, batter, frying time/color, sugar, sauces, quality score
- **`FillingType`**: `Sausage`, `Cheese`, `Mixed` (cooking) / `HalfSausage`, `HalfCheese` (orders) — note the two different enums in different namespaces
- **`SauceType`**: `Ketchup`, `Mustard` with `SauceAmount` levels (`Low`, `Medium`, `High`)
- **`DialogueType`**: `Intro`, `Order`, `ServedSuccess`, `ServedFail`, `Exit` + `Story*` variants

## Project Documentation

Detailed guides exist in the repo root (in Korean): `PROJECT_STRUCTURE.md`, `DIALOGUE_SYSTEM_OVERVIEW.md`, `STORY_NPC_SYSTEM_GUIDE.md`, `GamePlayScene_Complete_Flow_Guide.md`, `ERROR_FIX_AND_PROCESS_CHECK.md`.
