# 🟦 행님 전용 아키텍처 & 네이밍 캔버스

**(CompositionRoot · DI/new · SDK 규칙 포함 · 최종 완성판)**
Unity + Clean Architecture 기준으로 가장 실전적인 형태로 재정렬된 템플릿.

---

# 0. 전체 레이어 개념

```text
Domain          = 규칙/상태/엔티티 (순수 C#, new)
Application     = 유즈케이스/절차(Flow) (DI)
Infrastructure  = 기술/저장/외부 SDK (DI)
Presentation    = UI/Scene/View/Controller (Unity + 일부 DI)
CompositionRoot = DI 조립 루트 (DI)
```

---

# 1. DI vs new 규칙

## 1-1. new (직접 생성)

런타임 상태·데이터는 **전부 new**.

* Entity: Character, Enemy, Bullet
* State: StageState, QuestProgress
* ValueObject: DamageInfo, Position
* DTO: 서버/저장 응답 모델
* 읽기 전용 ViewModel 일부
* Event/Message 페이로드

```csharp
var character = new Character(maxHp, attack);
var stageState = new StageState(definition);
var damage = new DamageInfo(amount, isCritical);
```

---

## 1-2. DI (주입)

기능·규칙·재사용·외부 의존성은 **DI**.

* UseCase
* DomainService
* Repository
* Service
* Provider
* Factory
* Gateway
* Adapter
* EventBus / MessageBus
* Manager
* Presenter
* Navigator
* Mediator
* Coordinator / Facade

```csharp
public class StartStageUseCase
{
    private readonly IStageRepository _stageRepository;
    private readonly IAudioService _audioService;
}
```

---

## 1-3. Unity가 생성하는 것

* MonoBehaviour (Controller, View 등)
* ScriptableObject
* Scene에 존재하는 모든 Component
  → DI는 단지 **참조만 등록**.

---

# 2. Domain Layer (도메인 계층)

**Unity/Infra 모르는 순수 C# 규칙/상태 영역**

## 2-1. 패턴 & 네이밍

| 패턴                | 의미     | 템플릿                    | 생성  | 예시                            |
| ----------------- | ------ | ---------------------- | --- | ----------------------------- |
| **Entity**        | 정체성/상태 | (Feature)(Entity)      | new | Character, StageState         |
| **ValueObject**   | 값/불변   | (Feature)(Value)       | new | DamageInfo, Position          |
| **Definition**    | 정적 구조  | (Feature)Definition    | new | StageDefinition               |
| **DomainService** | 계산/규칙  | (Feature)DomainService | DI  | DamageCalculatorDomainService |

## 2-2. 예시

StageState
StageDefinition
Character
Item

---

# 3. Application Layer (UseCase 계층)

**절차/Flow 정의**

## 3-1. 패턴 & 네이밍

| 패턴                 | 의미         | 템플릿                    | 생성  | 예시                   |
| ------------------ | ---------- | ---------------------- | --- | -------------------- |
| **UseCase**        | 하나의 작업 흐름  | (Action)UseCase        | DI  | StartStageUseCase    |
| **Command**        | 단일 명령      | (Action)Command        | new | MoveCommand          |
| **Query**          | 조회 전용      | (Name)Query            | DI  | GetStagesQuery       |
| **CommandHandler** | Command 처리 | (Action)CommandHandler | DI  | AttackCommandHandler |

## 3-2. 예시 네이밍

StartStageUseCase
AttackUseCase
MoveCommand
GetStagesQuery

---

# 4. Infrastructure Layer (인프라 계층)

**저장소/기술/외부 API/SDK**

## 4-1. 패턴 & 네이밍

| 패턴             | 의미        | 템플릿                                              | 생성    | 예시                     |
| -------------- | --------- | ------------------------------------------------ | ----- | ---------------------- |
| **Repository** | 저장/로드     | I(Feature)Repository / (Tech)(Feature)Repository | DI    | IStageRepository       |
| **Service**    | 기능 제공     | (Feature)Service                                 | DI    | AudioService           |
| **Provider**   | 단순 값 제공   | (Feature)Provider                                | DI    | TimeProvider           |
| **Gateway**    | 외부 API    | (Feature)Gateway                                 | DI    | PaymentGateway         |
| **Adapter**    | SDK 변환    | (Name)Adapter                                    | DI    | GpgsAchievementAdapter |
| **Factory**    | 생성 규칙     | (Feature)Factory                                 | 보통 DI | StageStateFactory      |
| **Bus**        | 메시지       | (Name)Bus                                        | DI    | EventBus               |
| **Scheduler**  | 반복/딜레이    | (Feature)Scheduler                               | DI    | WaveScheduler          |
| **Manager**    | 리소스/객체 관리 | (Feature)Manager                                 | DI    | ObjectPoolManager      |

## 4-2. SDK(GPGS/Firebase 등) 규칙

* 인증/로그인 → Gateway
* SDK API ↔ 내부 인터페이스 변환 → Adapter
* 게임 기능(업적/리더보드) 제공 → Service

---

# 5. Presentation Layer (UI/Scene 계층)

**Scene·UI·View·입력**

## 5-1. 패턴 & 네이밍

| 패턴              | 의미                | 템플릿                 | 생성     | 예시                 |
| --------------- | ----------------- | ------------------- | ------ | ------------------ |
| **Controller**  | 입력/이벤트 처리         | (Feature)Controller | Unity  | StageController    |
| **View**        | 렌더링/위치/UI         | (Feature)View       | Unity  | EnemyView          |
| **ViewModel**   | UI 상태 데이터         | (Feature)ViewModel  | new/DI | InventoryViewModel |
| **Presenter**   | ViewModel→View 반영 | (Feature)Presenter  | DI     | StageHudPresenter  |
| **Mediator**    | UI 중재             | (Feature)Mediator   | DI     | UIShopMediator     |
| **Navigator**   | 화면 전환             | (Feature)Navigator  | DI     | SceneNavigator     |
| **Manager(UI)** | UI 그룹 관리          | (Feature)Manager    | DI     | UIOverlayManager   |

---

# 6. ScriptableObject 규칙

## 6-1. 역할별 레이어

| 역할            | 레이어                   | 예시                  |
| ------------- | --------------------- | ------------------- |
| 정적 데이터        | Infrastructure        | StageDatabase       |
| 연출/설정         | Presentation          | CharacterViewData   |
| Definition 원본 | Infra(SO) + Domain 모델 | StageDefinitionSO   |
| 에디터 설정        | Editor                | StageEditorSettings |

## 6-2. 패턴

SO → Domain 변환 후 반환.

```csharp
[CreateAssetMenu]
public class StageDefinitionSO : ScriptableObject
{
    public int id;
    public string displayName;
    public int timeLimit;
}
```

---

# 7. CompositionRoot (DI 조립)

**DI 바인딩의 시작점 + Scene 연결 지점**

## 7-1. 네이밍

GameLifetimeScope
GameCompositionRoot
RootLifetimeScope

## 7-2. 예시(VContainer)

```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Domain
        builder.Register<DamageCalculatorDomainService>(Lifetime.Singleton);

        // Infrastructure
        builder.Register<IStageRepository, JsonStageRepository>(Lifetime.Singleton);
        builder.Register<IAudioService, UnityAudioService>(Lifetime.Singleton);

        // Application
        builder.Register<StartStageUseCase>(Lifetime.Transient);

        // Presentation
        builder.RegisterComponentInHierarchy<StageController>();
        builder.RegisterComponentInHierarchy<StageHudPresenter>();
    }
}
```

---

# 8. 역할 선택 트리 (빠른 판단용)

```
[1] 데이터/상태? → Entity / State / Definition / ValueObject
[2] 절차/Flow? → UseCase
[3] 단일 명령? → Command
[4] 저장/로드? → Repository
[5] 외부 API/서버? → Gateway
[6] SDK 변환? → Adapter
[7] 기능 제공? → Service
[8] 단순 값 제공? → Provider
[9] 리소스/객체 관리? → Manager
[10] 생성 규칙? → Factory
[11] Scene 입력/트리거? → Controller
[12] 화면/연출? → View
[13] UI 상태 보관? → ViewModel
[14] UI 상태→View 반영? → Presenter
[15] UI 조율? → Mediator
[16] 화면/씬 이동? → Navigator
[17] DI 조립자? → CompositionRoot
```

---

# 9. 최종 네이밍 템플릿 요약 (복붙용)

```
Domain:
  (Feature)(Entity)
  (Feature)(State)
  (Feature)(Definition)
  (Feature)(Value)
  (Feature)DomainService

Application:
  (Action)UseCase
  (Action)Command
  (Name)Query
  (Action)CommandHandler

Infrastructure:
  I(Feature)Repository
  (Tech)(Feature)Repository
  (Feature)Service
  (Feature)(Tech)Service
  (Feature)Provider
  (Feature)Factory
  (Feature)Gateway
  (Name)Adapter
  (Name)Scheduler
  (Name)Bus
  (Feature)Manager

Presentation:
  (Feature)Controller
  (Feature)View
  (Feature)Presenter
  (Feature)ViewModel
  (Feature)Mediator
  (Feature)Navigator
  (Feature)Manager

CompositionRoot:
  GameLifetimeScope
  GameCompositionRoot
```

---

필요하면 **행님 프로젝트에 맞춘 실제 폴더 구조 버전**,
또는 **VContainer 기반 전체 샘플 프로젝트 구조도**도 추가로 생성해준다.
