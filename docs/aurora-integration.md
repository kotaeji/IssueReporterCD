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
