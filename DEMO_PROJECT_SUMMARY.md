# Recipe About Life v2 — 데모 버전 프로젝트 요약

> 작성일: 2026-02-23 | 브랜치: develop | 최신 커밋: 1132090

---

## 기본 정보

- **프로젝트명**: Recipe About Life v2 (인생에 대한 레시피)
- **엔진**: Unity 6000.0.62f1
- **언어**: C# (.NET Standard 2.1, C# 7.3+)
- **플랫폼**: 모바일 (세로 모드), 싱글 플레이어
- **네임스페이스**: `RecipeAboutLife` (하위: `.Events`, `.Cooking`, `.Dialogue`, `.UI`, `.Data`, `.Managers`, `.Orders`, `.Utilities`)

---

## 게임 흐름

```
MainMenu → Lobby(스테이지 선택) → GamePlay(5명 서빙) → 결산 → 스토리 대화 → Lobby
                                                                     Day3 클리어 → MainMenu
```

- 각 스테이지에 5명 NPC 등장
- Day 1~2: 스토리 대화 진행, Day 3: 재료 해금 및 다음 맵 해금
- 목표 금액 달성 시 진행 가능 (6000/7000/8000원)

---

## 씬 구성

| 씬 | 파일명 | 역할 |
|----|--------|------|
| 메인 메뉴 | `MainMenuScene.unity` | 게임 시작, 설정 |
| 로비 | `LobbyScene.unity` | 스테이지/맵 선택, 트럭 이동 |
| 게임 플레이 | `GamePlayScene.unity` | 요리, NPC 서빙, 결산 |

---

## 디렉토리 구조 (Assets/Scripts/)

| 폴더 | 파일 수 | 핵심 역할 |
|------|--------|----------|
| **Cooking/** | 21개 | 요리 6단계 FSM, 오디오, 점수 계산 |
| **Dialogue/** | 8개 | NPC/스토리 대화 시스템 (deprecated 2개 포함) |
| **UI/** | 10개 | 결산, 설정, 대화 버블, 페이드, 프레임 |
| **NPC/** | 6개 | NPC 데이터, 이동, 주문, 스폰 |
| **Managers/** | 3개 | GameManager, ScoreManager, GameUIManager |
| **Lobby/** | 4개 | 로비 관리, 스테이지 핀, 트럭 |
| **Orders/** | 3개 | 주문 데이터/DB/관리 |
| **Events/** | 1개 | EventManager(레거시) + GameEvents(타입 안전) |
| **Data/** | 2개 | StageData 관리 |
| **Debug/** | 2개 | 디버그 스킵/테스트 버튼 |
| **Camera/** | 1개 | 카메라 줌 컨트롤 |
| **Utilities/** | 1개 | OrderValidator |
| **Test/** | 1개 | NPC 주문 테스트 버튼 |

**총 61개 C# 스크립트 파일**

---

## 싱글톤 매니저 현황

| 매니저 | DontDestroyOnLoad | Instance 패턴 | 비고 |
|--------|:-:|------|------|
| GameManager | O | auto-property | 전역 상태 (Day/Money/Pause) |
| AudioManager | O | auto-property | 사운드 관리 |
| GameUIManager | X | auto-property | GamePlayScene 전용 |
| ScoreManager | X | lazy FindFirstObjectByType | GamePlayScene 전용 |
| SimpleCookingManager | X | auto-property | GamePlayScene 전용 |
| SettingsPopupController | X | lazy FindFirstObjectByType(Inactive포함) | 씬별 독립 |
| NPCSpawnManager | X | auto-property | GamePlayScene 전용 |

---

## 요리 FSM (SimpleCookingManager)

```
1. StickPickup → 2. Ingredient → 3. Batter → 4. Frying → 5. Topping → 6. Completed
```

- 튀김 타이밍: 0-3초 Raw, 3-7초 Yellow, **7-9초 Golden(최적)**, 9-11초 Brown, 11초+ Burnt
- 멘탈 시스템 (0-3): 실수 시 화면 채도 감소

---

## 이벤트 시스템

- **EventManager** (문자열 기반, 레거시): 정의만 있고 실제 사용처 0개
- **GameEvents** (타입 안전, 정적 이벤트): 모든 코드에서 사용 중
  - `OnRecipeCompleted`, `OnAllCustomersServed`, `OnNPCServed` 등

---

## ScriptableObject 데이터

| 종류 | 위치 | 개수 |
|------|------|------|
| NPC 대사 세트 | `ScriptableObjects/Dialogue/NPCs/` | 13개 |
| 주문 프리셋 | `ScriptableObjects/Orders/` | 10개 |
| 스테이지 대화 | `ScriptableObjects/Stage/` | 3개 |
| 스토리 NPC 설정 | `ScriptableObjects/Stage/` | 1개 |
| 스테이지 해금 | `ScriptableObjects/Stage/` | 1개 |
| 사운드 설정 | `Scripts/` | 1개 |

---

## 데모 버전에서 완료된 작업

1. Day2/Day3 스토리 화면 배경 통합 (FramePanelUI 2-layer 시스템)
2. Day3 클리어 텍스트 버그 수정 ("To be continued..")
3. 결산 화면 Day 이미지/배경 수정
4. 재화 누적 버그 수정 (Day 시작 시 초기화)
5. Day 목표 금액 조정 (6000/7000/8000원)
6. 튀김 Raw 스프라이트 수정
7. 로비 스테이지 핀 ↔ Day 연동
8. 설정 팝업 구현 (GamePlayScene + MainMenuScene)
9. 버튼 클릭음 볼륨 조정 (40%)
10. Day3 클리어 후 MainMenuScene 전환
11. DebugLogger 유틸리티 도입 (릴리스 빌드 Debug.Log 자동 제거)
12. ScoreManager 씬 전환 안정성 개선 (OnDestroy _instance 정리)
13. 페이드 검정 화면 테두리 수정 (FadeImage 확장)
14. GamePlayScene 설정 팝업 초기 비활성화 수정

---

## 잠재적 이슈 및 향후 정리 대상

### 주의 필요 (개선 완료)
- ~~**Debug.Log 과다**~~: 상위 3개 파일 DebugLogger로 전환 완료 (릴리스 빌드에서 자동 제거)
- ~~**ScoreManager static _instance**~~: OnDestroy에서 명시적 정리 추가 완료

### 향후 정리 대상
- **Deprecated 스크립트**: `StageDialogueController.cs`, `StageDialogueData.cs` (삭제 가능)
- **미사용 이벤트 시스템**: `EventSystem.cs`의 EventManager 클래스 (사용처 0개)
- **백업 씬**: `GamePlayScene 2_Backup.unity` (Git 관리로 불필요)
- **일회성 문서**: `COPY_UI_PANELS_NOW.md`, `UI_Panel_Import_Guide.md` 등

---

## 개발 변경 이력 (2월 15일 이후)

> 이전 작업(2025-12-31 `d676022`) 이후, 데모 버전 개발 기간(2026-02-17 ~ 02-23)의 6개 커밋 변경 내역

### 2026-02-17: 개발 환경 설정 및 핵심 시스템 구축

**커밋 `e1b4b82` — claude 설치 및 설정** (2개 파일)
- `CLAUDE.md` 프로젝트 가이드라인 작성
- `.slnx` 솔루션 파일 추가

**커밋 `7852b7d` — fix: 게임 흐름/보상 중복/이벤트 충돌 문제 해결 및 안정성 개선** (6개 파일, +134 -59)
- `GameManager.cs`: 재화 누적 버그 수정 (Day 시작 시 초기화)
- `ScoreManager.cs`: 보상 중복 지급 방지
- `SimpleCookingManager.cs`: 이벤트 충돌 해결
- `NPCSpawnManager.cs`: NPC 스폰 흐름 안정화
- `GameUIManager.cs`: UI 상태 동기화 개선
- `OrderManager.cs`: 주문 시스템 안정성 보완

**커밋 `e422025` — feat: 게임 루프 완성 및 점수-재화 시스템 연동 + StageData 기반 환경 시스템 구현** (15개 파일, +874 -103)
- `ScoreManager.cs`: 점수 계산 및 재화 연동 로직 구현
- `GameManager.cs`: Day/Money 상태 관리 확장
- `StageDataManager.cs`, `StageEnvironmentData.cs`: 스테이지별 환경 데이터 시스템 신규 구현
- `ResultUIController.cs`: 결산 화면 구현
- `NPCOrderController.cs`: 주문-서빙 흐름 연동
- 총 15개 파일, +874줄 대규모 기능 추가

### 2026-02-22: 로비 연동 및 디버그 도구

**커밋 `6e62fa0` — feat: 스테이지 핀 활성화 수정 + 디버그 스킵 버튼 + 점수/보상 시스템 안정화** (19개 파일, +513 -66)
- `LobbyManager.cs`, `StagePinController.cs`: 스테이지 핀 ↔ Day 연동
- `DebugSkipButton.cs`, `DebugTestButton.cs`: 디버그용 스킵/테스트 버튼 추가
- `ScoreManager.cs`: 점수 계산 안정화
- `SimpleCookingManager.cs`: 요리 단계 흐름 보완
- `GamePlayScene.unity`: 디버그 UI 배치

### 2026-02-23: 데모 버전 완성 및 코드 정리

**커밋 `1eaff32` — feat: 데모 버전 완성** (27개 파일, +4,974 -192)
- `SettingsPopupController.cs`: 설정 팝업 시스템 신규 구현 (BGM/SFX 볼륨 조절)
- `FramePanelUI.cs`: 스토리 화면 2-layer 배경 시스템 신규 구현
- `AudioManager.cs`: 버튼 클릭음 볼륨 40% 조정, SoundSettings 확장
- `StageStoryController.cs`: Day2/Day3 스토리 배경 통합, Day3 클리어 텍스트 수정
- `ResultUIController.cs`: 결산 화면 Day 이미지/배경 수정
- `GameManager.cs`: 설정 팝업 연동
- `LobbyManager.cs`: 스테이지 핀 Day 연동
- 스토리 배경 이미지 4장 + UI 리소스 추가
- `GamePlayScene.unity`, `MainMenuScene.unity`: 설정 팝업 UI 배치
- 데모 최대 규모 커밋

**커밋 `1132090` — refactor: 코드 정리 및 안정성 개선** (7개 파일, +337 -154)
- `DebugLogger.cs`: 릴리스 빌드 Debug.Log 자동 제거 유틸리티 신규 구현
- `StageStoryController.cs`: Debug.Log → DebugLogger 교체 (~85건), Day3 클리어 시 MainMenuScene 전환
- `ScoreManager.cs`: Debug.Log → DebugLogger 교체 (~27건), OnDestroy _instance 정리
- `SimpleCookingManager.cs`: Debug.Log → DebugLogger 교체 (~38건)
- `GamePlayScene.unity`: FadeImage 확장 (검정화면 테두리 수정), SettingsPopup 초기 비활성화
- `DEMO_PROJECT_SUMMARY.md`: 프로젝트 요약 문서 작성

---

## 문서 목록 (프로젝트 루트)

| 파일 | 설명 |
|------|------|
| `CLAUDE.md` | Claude Code 프로젝트 가이드라인 |
| `PROJECT_STRUCTURE.md` | 프로젝트 구조 정리 |
| `PROJECT_CONTEXT.md` | 전체 컨텍스트 |
| `DIALOGUE_SYSTEM_OVERVIEW.md` | 대사 시스템 개요 |
| `STORY_NPC_SYSTEM_GUIDE.md` | 스토리 NPC 시스템 |
| `GamePlayScene_Complete_Flow_Guide.md` | 게임플레이 흐름 |
| `GamePlayScene_Setup_Guide.md` | 게임플레이 씬 설정 |
| `ERROR_FIX_AND_PROCESS_CHECK.md` | 에러 수정 기록 |
| `UNUSED_SCRIPTS.md` | 미사용 스크립트 목록 |
| `DEMO_PROJECT_SUMMARY.md` | 데모 버전 요약 (본 문서) |
