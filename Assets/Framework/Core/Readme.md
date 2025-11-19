아래는 **AES.Framework.Core**용 README 초안이다.
Unity 패키지 내부의 `Runtime/Core/README.md` 또는 루트에 넣어도 된다.
순수 C# Core 라이브러리라는 성격을 명확히 강조했다.

---

# **AES.Framework.Core**

AES.Framework.Core는 Unity 엔진에 의존하지 않는 **순수 C# 기반 유틸리티·패턴·데이터 구조 모음**이다.
Framework 전체의 기반 레이어로, 안정성과 재사용성을 목표로 설계되었다.

Core는 **어떠한 엔진/플랫폼에도 종속되지 않으며**,
Unity 프로젝트뿐 아니라 일반 C# 프로젝트에서도 사용할 수 있다.

---

## **기능 개요**

### 1. **Utilities / Helpers**

* 공통 수학 함수
* 문자열/컬렉션 관련 헬퍼
* 타입 변환/검증 유틸
* 경량 구조체 기반 도구들

### 2. **Collections**

* 안전한 Dictionary/Set 래퍼
* RingBuffer, ObjectPool 같은 자료 구조
* Pair/Triple 등 경량 튜플 구조체

### 3. **Patterns**

* Result / Option 구조
* Singleton(Non-Mono)
* Disposable 패턴 확장
* MVP/Service Locator 등의 엔진 비종속 패턴

### 4. **Serialization**

* 간단한 JSON/바이너리 변환기
  (UnityEngine.JsonUtility 의존 없음)
* ISerializable 패턴 보조 도구

### 5. **Extensions**

* LINQ 대체용 경량 확장 메서드
* string/int/float/date 등 기본 타입 확장
  (UnityEngine.Vector/Color 등의 확장은 Systems에서 처리)

---

## **설계 철학**

### **1. UnityEngine 완전 배제**

Core는 엔진 레벨 API를 절대로 참조하지 않는다.
`noEngineReferences: true` 설정으로 엔진 의존을 방지한다.

### **2. 재사용성과 테스트 용이성**

* 모든 코드는 **순수 C#**
* 단위 테스트 작성이 용이
* 엔진 변경과 무관하게 재사용 가능

### **3. 최소한의 책임**

Core는 Framework의 **가장 안정적이고 변하지 않는 레이어**다.
여기 있는 기능은 가급적 단순·보편적이어야 하며,
엔진 기능이나 외부 패키지 연동과 섞이지 않는다.

---

## **폴더 구조 (예시)**

```
Core/
  Collections/
  Patterns/
  Math/
  Helpers/
  Extensions/
  Serialization/
  AES.Framework.Core.asmdef
```

---

## **Dependencies**

Core는 다음 조건을 가진다:

* 외부 라이브러리 의존 없음
* UnityEngine / UnityEditor 의존 없음
* 플랫폼 종속 API 사용 금지

따라서 다른 패키지 없이 단독으로 컴파일 가능하다.

---

## **사용 예시**

### Result 패턴

```csharp
var result = MathUtil.SafeDivide(10, 0);

if (!result.IsSuccess)
{
    Console.WriteLine(result.Error);
}
```

### 컬렉션 유틸

```csharp
var ring = new RingBuffer<int>(8);
ring.Push(10);
ring.Push(20);
```

### 확장 메서드

```csharp
"Hello".IsNullOrEmptyEx();  // true/false
```

---
