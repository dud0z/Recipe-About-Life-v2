# PROJECT_CONTEXT.md — Recipe About Life 기술 명세서 및 인수인계 문서

> **작성일:** 2025-02-17  
> **작성자:** 수석 개발자 (Claude, claude.ai 프로젝트)  
> **대상:** 새로운 개발자 (Claude Code)  
> **프로젝트:** Recipe About Life — Unity 기반 푸드트럭 핫도그 조리 시뮬레이션 게임  
> **엔진:** Unity 6 (URP)  
> **언어:** C#

---

## 목차

1. [프로젝트 개요](#1-프로젝트-개요)
2. [아키텍처 및 패턴](#2-아키텍처-및-패턴-architecture--patterns)
3. [핵심 게임 로직](#3-핵심-게임-로직-core-mechanics)
4. [데이터 및 에셋 관리](#4-데이터-및-에셋-관리-data--assets)
5. [UI 구조](#5-ui-구조-ui-structure)
6. [사운드 시스템](#6-사운드-시스템)
7. [팀원 작업 영역 (NPC/대화/결산)](#7-팀원-작업-영역-npc대화결산)
8. [알려진 이슈 및 미구현 사항](#8-알려진-이슈-및-미구현-사항-known-issues--to-do)
9. [파일 구조 맵](#9-파일-구조-맵)
10. [개발 규칙 및 컨벤션](#10-개발-규칙-및-컨벤션)

---

## 1. 프로젝트 개요

"Recipe About Life"는 푸드트럭에서 핫도그를 조리하여 손님에게 서빙하는 쿠킹 시뮬레이션 게임이다. 플레이어는 3일(Day 1~3)에 걸쳐 일일 목표 매출을 달성해야 한다. 매일 5명의 손님이 방문하며, 각 손님의 주문에 맞는 핫도그를 6단계 요리 프로세스를 통해 조리한다.

**핵심 게임 루프:**

```
[로비/스테이지 선택] → [게임플레이 (Day N)] → [손님 5명 서빙] → [결산 화면]
                                                                    ├─ 목표 달성 → 다음 Day
                                                                    └─ 목표 미달성 → Day 재시작 + 돈 초기화
```

**씬 구성:**

| 씬 이름 | 역할 |
|---------|------|
| `LobbyScene` | 스테이지(Day) 선택 화면 |
| `GamePlayScene` | 메인 게임플레이 (요리 + NPC 서빙) |

---

## 2. 아키텍처 및 패턴 (Architecture & Patterns)

### 2.1 사용된 디자인 패턴

| 패턴 | 적용 위치 | 설명 |
|------|----------|------|
| **싱글톤 (Singleton)** | `GameManager`, `SimpleCookingManager`, `AudioManager` | `Instance` 프로퍼티를 통한 전역 접근. `GameManager`만 `DontDestroyOnLoad` 적용. |
| **유한 상태 머신 (FSM)** | `SimpleCookingManager.CookingPhase` enum | 요리 시스템의 6단계를 enum으로 관리. `ChangePhase()` → `OnPhaseChanged` 이벤트 발행. |
| **옵저버 패턴 (Observer / Event)** | 모든 매니저 간 통신 | C# `event Action<T>`을 사용. 각 Handler가 `OnPhaseChanged` 이벤트를 구독하여 자기 단계를 감지. |
| **ScriptableObject 데이터 분리** | `RecipeConfigSO`, `SoundSettings`, `OrderData`, `NPCDialogueSet`, `StageDialogueData`, `StoryNPCConfig` | Inspector에서 수정 가능한 데이터 에셋. 코드와 데이터의 명확한 분리. |
| **컴포넌트 기반 핸들러** | 각 요리 단계별 Handler (MonoBehaviour) | 각 단계의 로직이 독립 컴포넌트로 존재. 해당 Phase일 때만 활성화. |

### 2.2 코드 아키텍처 — "Simple" 접두어 시스템

초기에 `CookingManager`, `CookingStation`, `ICookingStep` 등 복잡한 인터페이스 기반 시스템이 존재했으나, 구조 복잡도 문제로 **리팩토링하여 "Simple" 접두어 계열로 전환**하였다. 현재 **실제 런타임에서 사용하는 것은 Simple 계열**이다.

| 현재 사용 (Simple 계열) | 레거시 (미사용/참고용) |
|------------------------|---------------------|
| `SimpleCookingManager` | `CookingManager`, `ICookingStep` |
| `SimpleDraggable` | `DraggableObject` |
| `SimpleDropZone` | `DropZone`, `CookingStation` |
| `StickPickupHandler`, `IngredientPopupHandler`, `BatterHandler`, `FryingHandler`, `ToppingPopupHandler`, `CompletionHandler` | `StickPickupStep`, `IngredientStep`, `BatterStep` 등 |

> ⚠️ **중요:** `Assets/Scripts/Cooking/Core/` 및 `Assets/Scripts/Cooking/Steps/`에 레거시 코드가 남아있다. 혼동하지 않도록 주의. 현재 사용하는 파일은 `Assets/Scripts/Cooking/` 루트에 위치한 Simple 계열이다.

### 2.3 매니저 간 의존성 및 호출 순서

```
GameManager (싱글톤, DontDestroyOnLoad)
    ├── Day/Money/Pause 상태 관리
    ├── OrderData 보관 (NPC 시스템에서 SetCurrentOrder 호출)
    └── DayResultData 보관 (결산 화면에서 GetDayResultData 호출)
         │
         ▼
SimpleCookingManager (싱글톤)
    ├── 6단계 요리 FSM 관리
    ├── HotdogData 보관 (현재 조리 중인 핫도그 정보)
    ├── OnPhaseChanged 이벤트 발행
    ├── ServeHotdog() → ScoreCalculator → GameManager.CompleteServing()
    └── OnHotdogServed 이벤트 발행 (NPC 시스템에서 구독)
         │
         ▼
각 Phase Handler (MonoBehaviour, 이벤트 구독)
    ├── StickPickupHandler → Phase 1
    ├── IngredientPopupHandler → Phase 2
    ├── BatterHandler → Phase 3 (동적 AddComponent)
    ├── FryingHandler → Phase 4 (동적 AddComponent)
    ├── ToppingPopupHandler → Phase 5
    └── CompletionHandler → Phase 6
         │
         ▼
AudioManager (싱글톤)
    └── SoundSettings (ScriptableObject) 참조
         │
         ▼
GameUIManager
    └── GameManager 이벤트 구독 → Day/Money/Pause UI 업데이트
```

**초기화 순서 권장 (Script Execution Order):**  
`GameManager` → `AudioManager` → `SimpleCookingManager` → 기타 Handler

---

## 3. 핵심 게임 로직 (Core Mechanics)

### 3.1 요리 시스템 — 6단계 프로세스

모든 요리 프로세스는 `SimpleCookingManager`가 `CookingPhase` enum으로 관리한다. Phase 전환 시 `OnPhaseChanged` 이벤트가 발행되며, 각 Handler가 자기 Phase를 감지하여 동작한다.

```
Phase 1: StickPickup (꼬치 뽑기)
    → 꼬치통 클릭 → 스틱 프리팹 생성 → 도마로 드래그&드롭
    → Handler: StickPickupHandler
    → 드롭 감지: SimpleDraggable + SimpleDropZone (ZoneType.CuttingBoard)
    → 완료 조건: 도마 드롭존에 성공적으로 드롭

Phase 2: Ingredient (재료 끼우기)
    → IngredientPopup 활성화 (World Space 팝업)
    → 소시지/치즈 소스에서 드래그하여 꼬치에 드롭 (2개)
    → Handler: IngredientPopupHandler
    → HotdogData에 filling1, filling2 저장
    → 완료 시: 메인 화면 스틱에 재료 프리팹 부착 + 스케일 적용 (ingredientCompleteScale)

Phase 3: Batter (반죽 입히기)
    → 도마의 핫도그를 드래그하여 반죽통 영역에 진입
    → Handler: BatterHandler (IngredientPopupHandler에서 동적 AddComponent)
    → 타이밍: firstStageDelay(0.7초) 후 timePerStage(1.5초) 간격으로 1→2→3단계 진행
    → 각 단계마다 스프라이트 변경 (batterStage1/2/3)
    → 스케일 적용: batterScale (공통)
    → 완료 조건: 3단계 도달 시 자동 완료 또는 반죽통 이탈 시 현재 단계로 확정

Phase 4: Frying (튀기기)
    → 핫도그를 튀김기 드롭존에 드롭
    → Handler: FryingHandler (BatterHandler에서 동적 AddComponent)
    → 실시간 타이머: Raw(0~3초) → Yellow(3~7초) → Golden(7~9초, 최적!) → Brown(9~11초) → Burnt(11초+)
    → 각 단계마다 스프라이트 변경 (fryingRaw/Yellow/Golden/Brown/Burnt)
    → 스케일 적용: fryingScale (공통)
    → 보글보글 이펙트: fryingBubblePrefab (위치 오프셋 적용 가능)
    → 완료 조건: 식힘망(CoolingRack)에 드롭

Phase 5: Topping (토핑)
    → ToppingPopup 활성화 (World Space 팝업, 별도 Canvas)
    → 하위 동작:
        a) 설탕: 핫도그를 설탕 트레이(SugarTray)로 드래그&드롭 → sugarOverlay 표시
        b) 소스: 케첩통/머스타드통 클릭 → 스프라이트 트레일 방식으로 핫도그 위에 그리기
    → Handler: ToppingPopupHandler
    → SauceDrawer: 드래그 거리에 따라 소스 점(dot) 스프라이트 배치 + 게이지 감소
    → 게이지 바: UI Canvas (Image.Type.Filled) — gaugeDecrease 속도는 Inspector 조정 가능
    → X 버튼으로 팝업 종료 → OnDisable에서 상태 초기화 (ResetToppingState)
    → 메인 화면 핫도그에 소스 이미지 반영 (sauceScaleRatio, saucePositionOffset)

Phase 6: Completed (서빙)
    → 완성된 핫도그를 창문(servingWindow) 드롭존에 드래그&드롭
    → Handler: CompletionHandler
    → 드롭 성공 → SimpleCookingManager.ServeHotdog() 호출
```

### 3.2 점수 계산 시스템

`ScoreCalculator` (static class)가 주문(`OrderData`)과 완성품(`HotdogData`)을 비교하여 점수를 산출한다.

| 항목 | 점수 | 조건 |
|------|------|------|
| 재료1 일치 | 300원 | `order.filling1 == hotdog.filling1` |
| 재료2 일치 | 300원 | `order.filling2 == hotdog.filling2` |
| 튀김 Golden | 500원 | `fryingState == Golden` |
| 튀김 Yellow/Brown | 300원 | 차선 |
| 튀김 Raw/Burnt | 0원 | 실패 |
| 설탕 일치 | 300원 | `order.wantsSugar == hotdog.hasSugar` |
| 케첩 일치 | 150원 | `order.wantsKetchup == hotdog.hasKetchup` |
| 머스타드 일치 | 150원 | `order.wantsMustard == hotdog.hasMustard` |
| **최대** | **2,000원** | 모든 항목 만점 |

**주문 데이터 없이 테스트:** `ScoreCalculator.CalculateWithoutOrder(hotdog)` — 재료 유무와 튀김 상태로 기본 점수 산출.

### 3.3 게임 루프 — Day 시스템

```
GameManager 필드:
    int currentDay = 1       (1~3)
    int maxDay = 3
    int[] dayGoals = { 6000, 8000, 9000 }
    int customersPerDay = 5
    int todayEarnings = 0
    int customersServed = 0
```

**하루 진행 흐름:**

```
1. Day 시작 → NPC 시스템이 첫 손님 호출 (CallNextCustomer)
2. NPC 등장 → 대화 → 주문 설정 (GameManager.SetCurrentOrder)
3. SimpleCookingManager.StartCooking() 호출
4. 플레이어가 6단계 요리 수행
5. 서빙(ServeHotdog) → ScoreCalculator.Calculate() → GameManager.CompleteServing(earnedMoney)
    → todayEarnings 누적, customersServed++
    → OnCustomerServed 이벤트 발행
    → OnHotdogServed 이벤트 발행 → NPC 시스템이 다음 손님 호출
6. 5명 완료 → GameManager.EndDay()
    → DayResultData 확정 (isGoalAchieved = todayEarnings >= dayGoals[day-1])
    → OnDayEnded 이벤트 발행 → 결산 화면 표시
7. 결산:
    → 성공: GameManager.StartNextDay() → currentDay++, todayEarnings 초기화
    → 실패: GameManager.RestartDay() → todayEarnings + currentMoney 초기화
```

### 3.4 손님(NPC) 시스템 — 팀원 작업 영역

NPC 스폰, 대화, 주문 생성은 **팀원이 담당한 별도 시스템**이다. 요리 시스템과의 인터페이스:

| 시점 | 호출 | 방향 |
|------|------|------|
| 대화 완료 후 주문 설정 | `GameManager.Instance.SetCurrentOrder(order)` | NPC → GameManager |
| 요리 시작 | `SimpleCookingManager.Instance.StartCooking()` | NPC → CookingManager |
| 현재 주문 확인 | `GameManager.Instance.GetCurrentOrder()` | CookingManager → GameManager |
| 서빙 완료 후 다음 손님 | `OnHotdogServed` 이벤트 구독 | CookingManager → NPC |
| 하루 종료 | `OnDayEnded` 이벤트 구독 | GameManager → ResultUI |

**NPC 관련 주요 클래스 (팀원 코드):**

| 클래스 | 역할 |
|--------|------|
| `NPCSpawner` | NPC 프리팹 스폰 관리 (`npcsPerStage = 5`, 프리팹 9개 보유) |
| `NPCMovement` | NPC 이동 애니메이션 |
| `NPCOrderController` | 주문 생성 및 전달 |
| `NPCDialogueController` | ScriptableObject 기반 대화 실행 |
| `DialogueBubbleUI` | 말풍선 UI |
| `ScoreManager` | 팀원측 점수 관리 (GameManager와 **데이터 동기화 미완료** — 이슈 참조) |
| `StageStoryController` | 스테이지별 스토리 NPC 및 대화 관리 |
| `ResultUIController` | 결산 화면 UI (ScoreManager 데이터 사용 중 — 이슈 참조) |

---

## 4. 데이터 및 에셋 관리 (Data & Assets)

### 4.1 ScriptableObject 목록

| SO 타입 | 생성 메뉴 | 용도 | 위치 |
|---------|----------|------|------|
| `RecipeConfigSO` | Recipe About Life > Recipe Config | 요리 밸런스 (감점, 튀김 시간 등) | `Assets/ScriptableObjects/` |
| `SoundSettings` | Recipe About Life > Sound Settings | 모든 효과음/BGM 클립 + 볼륨 | `Assets/ScriptableObjects/` |
| `OrderData` | RecipeAboutLife > Order | 주문 정보 (재료, 토핑) | `Assets/ScriptableObjects/Orders/` |
| `NPCDialogueSet` | RecipeAboutLife > Dialogue > NPC Dialogue Set | NPC별 대화 데이터 | `Assets/ScriptableObjects/Dialogue/` |
| `StageDialogueData` | RecipeAboutLife > Dialogue > Stage Dialogue | 스테이지 종료 대화 | `Assets/ScriptableObjects/Dialogue/` |
| `StoryNPCConfig` | (커스텀 에셋) | 스테이지별 스토리 NPC 매핑 | `Assets/ScriptableObjects/Stage/` |

### 4.2 데이터 저장 방식

| 데이터 | 방식 | 설명 |
|--------|------|------|
| 요리 밸런스 | `RecipeConfigSO` | Inspector에서 수정, 런타임 읽기 전용 |
| 주문 정보 | `OrderData` (SO) + `GameManager.currentOrder` (런타임) | NPC가 SO에서 읽어 GameManager에 전달 |
| 조리 중 핫도그 | `SimpleCookingManager.HotdogData` | 런타임 클래스 인스턴스, 저장 불필요 |
| 하루 결산 | `DayResultData` (일반 클래스) | GameManager가 보관, 결산 화면에서 참조 |
| 게임 진행 (Day, Money) | `GameManager` 필드 | 씬 전환 시 DontDestroyOnLoad로 유지 |
| 영속 저장 | **미구현** | PlayerPrefs 또는 JSON 저장 시스템 필요 |

### 4.3 Day별 배경/오브젝트 변경

`DayBackground` 컴포넌트를 사용한다. 배경 변경이 필요한 **각 SpriteRenderer 오브젝트에 개별 부착**하는 방식:

```csharp
// DayBackground.cs
[RequireComponent(typeof(SpriteRenderer))]
public class DayBackground : MonoBehaviour
{
    public Sprite day1Sprite;
    public Sprite day2Sprite;
    public Sprite day3Sprite;
    // GameManager.OnDayChanged 이벤트 구독 → switch로 스프라이트 교체
}
```

Inspector에서 각 오브젝트의 Day 1/2/3 스프라이트를 할당하면 Day 변경 시 자동 교체된다.

### 4.4 씬 전환

| 전환 | 방법 | 호출 |
|------|------|------|
| 로비 → 게임플레이 | `SceneManager.LoadScene("GamePlayScene")` | 로비 UI 버튼 |
| 게임플레이 → 로비 | `GameManager.Instance.LoadLobbyScene()` | 일시정지 팝업 > 종료 버튼 |
| 페이드 인/아웃 | `LobbyScene`의 `fadePanel` + `fadeImage` (CanvasGroup Alpha) | LobbyScene 내 FadeController |

**`GameManager`는 `DontDestroyOnLoad`이므로 씬 전환 후에도 Day/Money 데이터가 유지된다.**

---

## 5. UI 구조 (UI Structure)

### 5.1 GamePlayScene Canvas 계층 구조

```
Canvas (Screen Space - Overlay, Sort Order: 1)
├── Canvas Scaler: Scale With Screen Size, Reference: 1920x1080, Match: 0.5
│
├── MainScreen (기존 팀원 작업 영역)
├── BackgroundDimmer
├── 주문/대화 관련 UI (팀원 영역)
│
├── DayImage (Image) — 좌상단, Anchor: Top-Left
│   └── Day1/2/3 스프라이트를 GameUIManager가 교체
│
├── MoneyPanel — 우상단, Anchor: Top-Right
│   ├── MoneyBackground (Image)
│   └── MoneyText (TMP_Text) — 숫자만 표시 (format: "{0}")
│   └── ※ 특정 시점에만 표시: 요리 시작 전 + 서빙 완료 후 (moneyDisplayDuration초)
│
├── PauseButton (Button) — 우상단
│
└── PausePopup (초기 비활성화)
    ├── Background (반투명)
    └── ButtonPanel (HorizontalLayout)
        ├── ResumeButton → GameManager.ResumeGame()
        ├── SettingsButton → GameManager.OpenSetting() (미구현)
        └── QuitButton → GameManager.LoadLobbyScene()
```

### 5.2 요리 팝업 — World Space

요리 중 팝업들은 **Screen Space Canvas가 아닌 World Space**에 위치한다. `SimpleDraggable`이 `Camera.main.ScreenToWorldPoint()`를 사용하기 때문이다.

| 팝업 | 위치 | 트리거 |
|------|------|--------|
| `IngredientPopup` | Hierarchy 루트 (World Space, SpriteRenderer) | Phase 2 진입 시 `SetActive(true)` |
| `ToppingPopup` | Hierarchy 루트 (World Space, SpriteRenderer) | Phase 5 진입 시 `SetActive(true)` |
| `ToppingUICanvas` | ToppingPopup 자식 (World Space Canvas) | 게이지 바, X 버튼 등 |

> ⚠️ ToppingUICanvas는 반드시 **Render Mode = World Space**여야 한다. Screen Space - Overlay로 하면 ToppingPopup(World Space)의 자식이므로 렌더링 안 됨.

### 5.3 GameUIManager 제어 방식

`GameUIManager`는 `GameManager`의 이벤트를 구독하여 UI를 업데이트한다:

| 이벤트 | UI 반응 |
|--------|---------|
| `OnDayChanged` | DayImage 스프라이트 교체 |
| `OnMoneyChanged` | MoneyText 갱신 |
| `OnPauseChanged` | PausePopup 활성화/비활성화 + Time.timeScale 반영 |
| `OnCookingStarted` | MoneyPanel 일시 표시 |
| 서빙 완료 | MoneyPanel 일시 표시 |

### 5.4 페이드 인/아웃 (Blackout)

`LobbyScene`에 `fadePanel` (CanvasGroup) + `fadeImage`가 존재한다. `FadeController`(또는 씬 내 MonoBehaviour)가 CanvasGroup의 alpha를 코루틴으로 제어한다. `StageStoryController`에 `fadeDuration = 1f`, `waitTextDuration = 2f` 설정이 있다.

**GamePlayScene에서의 페이드:** 현재 별도 구현 없음 — 필요 시 추가 구현 필요.

---

## 6. 사운드 시스템

### 6.1 구조

```
SoundSettings (ScriptableObject)
    ├── SFX: stickPickup, ingredientAttach, battering(Loop), frying(Loop),
    │        sauceSqueezing(Loop), sugarApply, serving, buttonClick
    ├── BGM: day1BGM, day2BGM, day3BGM
    └── Volume: sfxVolume, bgmVolume, loopSfxVolume

AudioManager (싱글톤 MonoBehaviour)
    ├── bgmSource (AudioSource, Loop)
    ├── sfxSource (AudioSource)
    └── loopSfxSource (AudioSource, Loop)
```

### 6.2 호출 방식

각 Handler에서 `AudioManager.Instance?.Play___()` 형태로 호출한다:

```csharp
AudioManager.Instance?.PlayStickPickup();        // Phase 1
AudioManager.Instance?.PlayIngredientAttach();    // Phase 2
AudioManager.Instance?.PlayBatteringLoop();       // Phase 3 시작
AudioManager.Instance?.StopBatteringLoop();       // Phase 3 종료
AudioManager.Instance?.PlayFryingLoop();          // Phase 4 시작
AudioManager.Instance?.StopFryingLoop();          // Phase 4 종료
AudioManager.Instance?.PlaySauceLoop();           // Phase 5 소스 드래그 중
AudioManager.Instance?.StopSauceLoop();           // Phase 5 드래그 종료
AudioManager.Instance?.PlaySugarApply();          // Phase 5 설탕
AudioManager.Instance?.PlayServing();             // Phase 6 서빙
AudioManager.Instance?.PlayButtonClick();         // UI 버튼
AudioManager.Instance?.PlayBGM(currentDay);       // Day 변경 시
```

**볼륨 조절 UI는 미구현** — 현재 Inspector에서만 조정 가능.

---

## 7. 팀원 작업 영역 (NPC/대화/결산)

### 7.1 대화 시스템 (팀원 구현)

ScriptableObject 기반 대화 시스템으로, NPC 라이프사이클에 따른 5가지 대화 타입을 지원한다.

| DialogueType | 트리거 시점 |
|-------------|------------|
| `Intro` | NPC 도착 |
| `Order` | 주문 시작 |
| `ServedSuccess` | 주문 일치 서빙 |
| `ServedFail` | 주문 불일치 서빙 |
| `Exit` | 퇴장 |

**관련 파일:** `Assets/Scripts/Dialogue/` 디렉토리 전체 (README.md 참조)

### 7.2 주문 데이터 구조 (팀원 구현 OrderData SO)

팀원이 구현한 `OrderData`는 ScriptableObject이며, 다음 필드를 포함한다:

```csharp
// Assets/Scripts/Orders/OrderData.cs
public class OrderData : ScriptableObject
{
    public FillingType filling1;        // 소시지/치즈
    public FillingType filling2;        // 소시지/치즈
    public bool wantsSugar;
    public List<SauceRequirement> sauceRequirements; // 케첩/머스타드 + 양(Low/Medium/High)
}
```

> ⚠️ 내가 구현한 `SimpleCookingManager`의 `HotdogData`는 `string filling1/2` + `bool hasKetchup/hasMustard`로 되어있다. **팀원의 OrderData와 필드 타입이 다를 수 있어 어댑터가 필요할 수 있다.**

### 7.3 결산 화면 (팀원 구현)

팀원이 `ResultUIController`와 `ScoreManager`를 구현해두었다. 현재 `ScoreManager`에서 데이터를 읽고 있으나, 실제 재화는 `GameManager`에서 관리한다. **데이터 동기화가 필요하다** (이슈 섹션 참조).

---

## 8. 알려진 이슈 및 미구현 사항 (Known Issues & To-Do)

### 8.1 알려진 버그 / 수정 필요 사항

| # | 심각도 | 내용 | 상세 |
|---|--------|------|------|
| B1 | 🔴 높음 | **ResultUIController ↔ GameManager 데이터 미연결** | 결산 화면이 `ScoreManager`에서 데이터를 읽지만 실제 재화는 `GameManager.todayEarnings`에 누적됨. `ResultUIController`가 `GameManager.TodayEarnings` / `GetCurrentDayGoal()`을 사용하도록 수정 필요. 또는 `ScoreManager`의 getter를 GameManager로 위임. |
| B2 | 🟡 중간 | **OrderData 타입 불일치** | 팀원 OrderData(SO)의 `SauceRequirement` 구조와 내 `ScoreCalculator`의 `bool wantsKetchup/wantsMustard` 비교 방식이 다름. 어댑터 또는 통합 필요. |
| B3 | 🟡 중간 | **ToppingPopup 소스 초기화** | `OnDisable`에서 `ResetToppingState()` 호출하여 해결 완료. 단, 메인 화면에 반영된 소스 스프라이트 Clone 삭제는 `StartCooking()` 호출 시 처리. 추가 엣지 케이스 테스트 필요. |
| B4 | 🟢 낮음 | **레거시 코드 잔존** | `CookingManager`, `ICookingStep`, `CookingStation`, `DraggableObject`, `DropZone` 등 레거시 파일이 프로젝트에 남아있음. 컴파일 에러는 없으나 혼동 가능. |

### 8.2 미구현 기획 사항 (To-Do)

| # | 우선도 | 내용 | 비고 |
|---|--------|------|------|
| T1 | 🔴 높음 | **Day 전환 시 전체 상태 초기화 검증** | Day 2 시작 시 모든 요리 상태, 팝업, 이전 Clone 오브젝트가 깨끗이 초기화되는지 전체 흐름 테스트 필요 |
| T2 | 🟡 중간 | **볼륨 조절 UI** | 설정 화면에서 SFX/BGM 볼륨 슬라이더 |
| T3 | 🟡 중간 | **GamePlayScene 페이드 인/아웃** | Day 시작/종료 시 Blackout 효과 |
| T4 | 🟡 중간 | **대화 타이핑 효과** | 한 글자씩 표시 (팀원 TODO) |
| T5 | 🟡 중간 | **대화 스킵/넘기기 기능** | 터치로 대화 넘기기 (팀원 TODO) |
| T6 | 🟢 낮음 | **설정 화면(SettingsButton)** | 일시정지 팝업에서 호출되나 `OpenSetting()`이 빈 상태 |
| T7 | 🟢 낮음 | **게임 클리어 화면** | Day 3 목표 달성 시 엔딩/크레딧 |

---

## 9. 파일 구조 맵

```
Assets/
├── Scenes/
│   ├── LobbyScene.unity              # 로비/스테이지 선택
│   ├── GamePlayScene.unity           # 메인 게임플레이
│   └── GamePlayScene 2_Backup.unity  # 백업 씬
│
├── Scripts/
│   ├── Managers/
│   │   └── GameManager.cs            # ★ 게임 전체 상태 (싱글톤, DDOL)
│   │
│   ├── Cooking/                       # ★ 요리 시스템 (내 작업 영역)
│   │   ├── SimpleCookingManager.cs    # ★ 요리 FSM 관리자
│   │   ├── SimpleDraggable.cs         # ★ 드래그 컴포넌트
│   │   ├── SimpleDropZone.cs          # ★ 드롭존 컴포넌트
│   │   ├── StickPickupHandler.cs      # Phase 1
│   │   ├── IngredientPopupHandler.cs  # Phase 2
│   │   ├── BatterHandler.cs           # Phase 3 (동적 AddComponent)
│   │   ├── FryingHandler.cs           # Phase 4 (동적 AddComponent)
│   │   ├── ToppingPopupHandler.cs     # Phase 5
│   │   ├── ToppingSource.cs           # 소스통 클릭 감지
│   │   ├── SauceDrawer.cs             # 소스 스프라이트 트레일 그리기
│   │   ├── CompletionHandler.cs       # Phase 6
│   │   ├── ScoreCalculator.cs         # 점수 계산 (static)
│   │   ├── OrderData.cs               # 주문 데이터 (내 버전 — 팀원 SO와 별도)
│   │   ├── DayResultData.cs           # 결산 데이터
│   │   ├── DayBackground.cs           # Day별 배경 교체
│   │   ├── SoundSettings.cs           # 사운드 ScriptableObject
│   │   ├── AudioManager.cs            # 사운드 매니저
│   │   ├── GameUIManager.cs           # 인게임 UI 제어
│   │   │
│   │   ├── Core/                      # ⚠️ 레거시 (미사용)
│   │   │   ├── CookingManager.cs
│   │   │   ├── CookingStation.cs
│   │   │   ├── CookingDataModels.cs
│   │   │   └── ICookingStep.cs
│   │   │
│   │   ├── Steps/                     # ⚠️ 레거시 (미사용)
│   │   │   ├── StickPickupStep.cs
│   │   │   ├── IngredientStep.cs
│   │   │   └── ...
│   │   │
│   │   ├── Systems/                   # ⚠️ 레거시 (미사용)
│   │   │   ├── DropZone.cs
│   │   │   ├── DraggableObject.cs
│   │   │   └── Popups/IngredientPopup.cs
│   │   │
│   │   └── Data/
│   │       └── RecipeConfigSO.cs      # 요리 밸런스 SO
│   │
│   ├── Orders/
│   │   └── OrderData.cs               # 팀원 버전 OrderData (ScriptableObject)
│   │
│   ├── Dialogue/                      # 팀원 작업 영역
│   │   ├── DialogueEnums.cs
│   │   ├── DialogueLine.cs
│   │   ├── DialogueManager.cs
│   │   ├── NPCDialogueSet.cs
│   │   ├── NPCDialogueController.cs
│   │   ├── StageDialogueData.cs
│   │   ├── StageDialogueController.cs
│   │   └── README.md
│   │
│   ├── NPC/                           # 팀원 작업 영역
│   │   ├── NPCSpawner.cs
│   │   ├── NPCMovement.cs
│   │   └── NPCOrderController.cs
│   │
│   ├── Events/
│   │   └── GameEvents.cs              # 전역 이벤트 (일부 레거시)
│   │
│   └── UI/                            # 팀원 작업 영역
│       ├── ResultUIController.cs
│       ├── ScoreManager.cs
│       ├── DialogueBubbleUI.cs
│       └── ...
│
└── ScriptableObjects/
    ├── RecipeConfig.asset
    ├── SoundSettings.asset
    ├── Stage/
    │   └── StoryNPCConfig.asset
    ├── Dialogue/
    │   └── (NPC별 대화 데이터)
    └── Orders/
        └── (주문 데이터 10개)
```

---

## 10. 개발 규칙 및 컨벤션

### 10.1 코드 작성 원칙

1. **ScriptableObject 우선:** 밸런스 수치, 에셋 참조 등 데이터는 SO로 분리. Inspector에서 수정 가능하게 하여 협업 용이성 확보.
2. **역할 분리:** 하나의 스크립트가 하나의 역할만 담당. Handler는 자기 Phase 로직만 포함.
3. **이벤트 기반 통신:** 매니저 간 직접 참조 최소화. `event Action<T>`으로 느슨한 결합.
4. **코드 작성 전 계획 점검:** 구현 전 계획서를 작성하고 검토를 받은 후 코드 작성 시작.
5. **Simple 계열 사용:** 레거시 Core/Steps/Systems 폴더의 코드는 참고만. 실제 수정은 Simple 계열에서만.

### 10.2 Namespace

| 네임스페이스 | 사용 영역 |
|-------------|----------|
| `RecipeAboutLife.Cooking` | 요리 시스템 전체 |
| `RecipeAboutLife.Events` | GameEvents |
| `RecipeAboutLife.UI` | CookingUIManager 등 |
| (없음) | GameManager, 팀원 코드 일부 |

### 10.3 주요 enum 정리

```csharp
// SimpleCookingManager 내부
enum CookingPhase { None, StickPickup, Ingredient, Batter, Frying, Topping, Completed }
enum FryingState { Raw, Yellow, Golden, Brown, Burnt }

// SimpleDropZone 내부
enum ZoneType { None, CuttingBoard, BatterZone, FryingStation, CoolingRack, StickDropZone, SugarTray }

// ToppingSource / SauceDrawer
enum ToppingType { None, Ketchup, Mustard }

// CookingDataModels (레거시이나 IngredientPopup에서 참조)
enum FillingType { None, Sausage, Cheese, Mixed }

// 대화 시스템 (팀원)
enum DialogueType { Intro, Order, ServedSuccess, ServedFail, Exit }
enum SpeakerType { NPC, Player, System }
```

### 10.4 디버그 도구

| 위치 | ContextMenu | 기능 |
|------|-------------|------|
| `SimpleCookingManager` | Start Cooking | 테스트용 요리 시작 |
| `SimpleCookingManager` | Force Next Phase | 강제 다음 단계 |
| `SimpleCookingManager` | Log Status | 현재 상태 출력 |
| `GameManager` | Add 100 Money | 돈 100 추가 |
| `GameManager` | Next Day | 다음 Day로 |
| `GameManager` | Test Complete Serving | 랜덤 점수로 서빙 테스트 |
| `GameManager` | Force End Day | 강제 하루 종료 |

---

## 부록: 핵심 이벤트 레퍼런스

| 이벤트 | 클래스 | 시그니처 | 발생 시점 |
|--------|--------|----------|----------|
| `OnPhaseChanged` | `SimpleCookingManager` | `Action<CookingPhase>` | 요리 Phase 변경 |
| `OnHotdogServed` | `SimpleCookingManager` | `Action` | 핫도그 서빙 완료 |
| `OnCookingStarted` | `SimpleCookingManager` | `Action` | 요리 시작 |
| `OnDayChanged` | `GameManager` | `Action<int>` | Day 변경 |
| `OnMoneyChanged` | `GameManager` | `Action<int>` | 돈 변경 |
| `OnPauseChanged` | `GameManager` | `Action<bool>` | 일시정지 토글 |
| `OnOrderChanged` | `GameManager` | `Action<OrderData>` | 주문 변경 |
| `OnCustomerServed` | `GameManager` | `Action<int, int>` | 서빙 완료 (served, total) |
| `OnEarningsChanged` | `GameManager` | `Action<int, int>` | 수입 변경 (earnings, goal) |
| `OnDayEnded` | `GameManager` | `Action<DayResultData>` | 하루 종료 |

---

> **문서 끝.** 이 문서는 2025년 2월 17일 기준으로 프로젝트의 모든 확정된 설계와 구현 상태를 반영한다. Claude Code로 작업 시 이 문서를 CLAUDE.md 또는 프로젝트 루트에 배치하여 참조하길 권장한다.
