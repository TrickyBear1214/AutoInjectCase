# AutoInjectCase

Duckov용 자동 케이스 적재 모드입니다.

아이템을 주웠을 때 또는 특정 플레이어 인벤토리 전달 경로를 탈 때, 조건에 맞는 케이스가 있으면 일반 인벤토리 대신 케이스에 바로 넣는 것을 목표로 합니다.

## 현재 동작

- 케이스 판정은 현재 `ContainerTagName = "Continer"` 규칙을 사용합니다.
- 실제 타겟 케이스는 슬롯형 케이스를 기준으로 찾습니다.
- 슬롯형 케이스 적재는 `ItemUtilities.TryPlug(...)` 경로를 사용합니다.
- 스택 가능한 아이템은 원본 게임의 슬롯 merge 동작을 최대한 따르도록 합니다.
- 여러 케이스가 가능하면 펫 인벤토리 안의 케이스를 우선합니다.
- 플레이어 창고에서 플레이어 인벤토리로 옮기는 경우 자동 케이스 적재를 건너뜁니다.
- 펫 인벤토리에서 플레이어 인벤토리로 옮기는 경우도 자동 케이스 적재를 건너뜁니다.

## 의도한 흐름

1. 아이템 픽업 시 적재 가능한 케이스를 찾습니다.
2. 넣을 수 있으면 바로 케이스에 넣습니다.
3. 넣지 못하면 원본 게임 로직으로 fallback 합니다.

## 설치

빌드된 DLL을 아래 경로에 배치합니다.

```text
C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov\Duckov_Data\Mods\AutoInjectCase\AutoInjectCase.dll
```

## 빌드

프로젝트는 `netstandard2.1` 대상입니다.

```powershell
dotnet build .\AutoInjectCase.sln
```

현재 `.csproj`는 로컬 Duckov 설치 경로를 직접 참조합니다.

```xml
<DuckovPath>C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov</DuckovPath>
```

기본값은 위 경로지만, 빌드 시 속성으로 덮어쓸 수 있습니다.

```powershell
dotnet build .\AutoInjectCase.sln -p:DuckovPath="D:\Games\Escape from Duckov"
```

## 로그

테스트 시 확인한 Unity 로그 위치:

```text
C:\Users\admin\AppData\LocalLow\TeamSoda\Duckov\Player.log
```

모드 디버깅 시 `AutoInjectCase` 로그를 검색하면 후보 케이스 탐색과 fallback 여부를 확인할 수 있습니다.

## 프로젝트 파일

- `SPEC.md`: 현재 기능 명세
- `AutoInjectCase/AutoInjectionMod.cs`: 공통 상수, 필드, 타입
- `AutoInjectCase/AutoInjectionMod.Lifecycle.cs`: 모드 초기화/해제
- `AutoInjectCase/AutoInjectionMod.PickupPatch.cs`: `PickupItem` 패치
- `AutoInjectCase/AutoInjectionMod.InventoryPatch.cs`: `SendToPlayerCharacterInventory` 패치
- `AutoInjectCase/AutoInjectionMod.Storage.cs`: 케이스 탐색 및 저장 로직

## 주의

- 케이스 구조를 `Inventory` 기반이라고 가정하지 않습니다.
- 원본 동작을 확인하지 않은 상태에서 전체 픽업 로직을 임의로 대체하지 않습니다.
- 동작을 바꿀 때는 `SPEC.md`를 먼저 갱신한 뒤 코드를 수정합니다.
