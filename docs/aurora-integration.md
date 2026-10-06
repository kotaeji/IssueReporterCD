# Aurora 연동 규격 (경로 정보 전달)

Issue Reporter는 Aurora와 **별도 프로세스**로 실행됩니다. Aurora가 비정상 종료되었거나 응답이 없어도 리포트를 만들 수 있어야 하므로, Aurora 프로세스와 직접 통신하지 않고 **Aurora가 미리 남겨 둔 파일**로 경로 정보를 받습니다.

## Aurora 쪽에서 할 일

Aurora는 **시작할 때**(그리고 경로 설정이 바뀔 때) 아래 파일을 씁니다.

- 위치: `%ProgramData%\Aurora\IssueReporter.ini`
  - 위치는 Issue Reporter의 `App.config` → `AuroraDiscoveryFile`로 바꿀 수 있습니다.
- 인코딩: UTF-8
- 형식: 한 줄에 `키=값`. `#`으로 시작하는 줄은 주석입니다.

```ini
# Aurora가 자동으로 생성합니다. 직접 수정하지 마세요.
LogDir=D:\Aurora\Log
SysErrorDir=D:\Aurora\SysError
ConfigDir=D:\Aurora\Config
```

| 키 | 의미 |
| --- | --- |
| `LogDir` | Aurora 일반 로그 폴더 |
| `SysErrorDir` | sys-error 로그 폴더 |
| `ConfigDir` | 설정 파일 폴더 |

없는 키는 Issue Reporter의 기본값을 사용합니다. 나중에 키를 추가해도 이전 버전 Issue Reporter는 모르는 키를 무시합니다.

## 환경 지문 파일 (environment.ini)

리포트의 환경 지문([issue-schema.md](issue-schema.md) ②층) 중 Aurora만 아는 값은 이 파일로 받습니다. Boot Loader가 이미 화면에 띄우는 정보를 **부팅할 때마다 파일로도 저장**하면 됩니다.

- 위치: `%ProgramData%\Aurora\environment.ini`
  - 위치를 바꾸려면 `IssueReporter.ini`에 `EnvironmentFile=<경로>`를 쓰세요. 그다음 우선순위는 Issue Reporter의 `App.config` → `AuroraEnvironmentFile`입니다.
- 쓰는 시점: Aurora(Boot Loader) 부팅이 끝났을 때. 시퀀스나 레시피를 바꿨을 때도 다시 쓰면 좋습니다.
- 형식: `IssueReporter.ini`와 같습니다(UTF-8, `키=값`, `#` 주석).

```ini
# Boot Loader가 부팅할 때 자동으로 씁니다. 직접 수정하지 마세요.
platform_version=3.3.0.0
machine_sw_version=1.4.2
api_level=12
build_config=Release
architecture=x64
equipment_model=AUR-X200
line=L2
data_path=D:\AuroraData
sequence_id=MainSequence
sequence_revision=57
recipe_id=Default
recipe_revision=12
module.WMX3=3.4.1
module.MIL=10.60
module.VisionSDK=2.1.0
module.Theme=1.0.3
module.Agent=0.9.0
```

| 키 | 의미 |
| --- | --- |
| `platform_version` | Aurora 플랫폼 버전 |
| `machine_sw_version` | 장비 SW 버전 |
| `api_level` | 플랫폼 API Level |
| `build_config` | `Debug` / `Release` |
| `architecture` | Aurora 프로세스 아키텍처 (`x64` 등) |
| `equipment_model` | 장비 모델 |
| `line` | 라인. 현장 엔지니어가 입력하면 그 값이 우선 |
| `data_path` | DataPath |
| `sequence_id`, `sequence_revision` | 실행 중인 시퀀스와 리비전 |
| `recipe_id`, `recipe_revision` | 레시피와 리비전 |
| `module.<이름>` | 모듈 버전. 어셈블리 버전을 런타임에 읽어 쓰면 됨 |

- 모르는 값은 줄을 생략하세요. 빈 값과 생략은 똑같이 "없음"으로 처리합니다.
- 목록에 없는 키를 써도 됩니다. `report.json`의 `environment.extra`에 그대로 보존되고, 쓸모가 확인되면 카탈로그에 정식 필드로 올립니다.
- 파일이 없으면 Issue Reporter는 PC에서 직접 읽을 수 있는 값(OS, 해상도, 설정 해시 등)만 넣고, 수집 목록에 "Aurora 정보 파일 없음"으로 표시합니다.
- 파일의 수정 시각도 기록합니다. 그래서 Aurora가 오래전에 쓴 낡은 정보인지 분석할 때 알 수 있습니다.

## 경로 결정 순서

1. **사용자 지정**: Issue Reporter 설정 화면에서 바꾼 값 (`%ProgramData%\IssueReporterCD\settings.ini`)
2. **Aurora 제공**: 위 `IssueReporter.ini`
3. **기본값**: Issue Reporter `App.config`

설정 화면에서 경로를 자동값과 같게 되돌리면 사용자 지정이 지워집니다. 그러면 이후 Aurora가 경로를 바꿨을 때 자동으로 따라갑니다.

## 이 방식을 고른 이유

| 방식 | Aurora가 죽었을 때 | 비고 |
| --- | --- | --- |
| **파일로 전달 (채택)** | 동작함. 파일이 남아 있음 | 구현이 단순하고, 사람이 열어서 확인할 수 있음 |
| 레지스트리 | 동작함 | 권한 문제가 있고, 현장에서 확인하기 어려움 |
| 실행 중인 Aurora에 질의 (IPC) | **동작 안 함** | 요구사항과 맞지 않음 |
| Aurora 설정 파일 직접 파싱 | 동작함 | Aurora 내부 형식에 의존해서, Aurora가 바뀌면 깨짐 |
