# Recipe About Life v2 — 데모 버전 프로젝트 요약

> 작성일: 2026-02-23 | 브랜치: develop | 커밋: 1eaff32

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
