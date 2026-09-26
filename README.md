# Aurora Issue Reporter (IssueReporterCD)

Aurora 기반 장비 소프트웨어가 설치된 PC에서 이슈가 발생했을 때, 현장 엔지니어가 실행해 **이슈 설명과 장비 데이터를 zip 파일 하나로** 만드는 도구입니다. 만든 zip 파일을 한국 담당자에게 보내면 됩니다.

## 동작 방식

1. 프로그램을 실행하는 순간 전체 화면을 캡처합니다. 리포터 창이 뜨기 전의 장비 화면이 그대로 남습니다.
2. 백그라운드에서 시스템 정보와 Aurora 로그, 설정 파일을 수집합니다.
3. 그동안 엔지니어가 템플릿에 맞춰 이슈 설명을 작성합니다.
4. **리포트 생성**을 누르면 `yyyyMMdd_HHmmss_<장비ID>.zip` 파일이 만들어집니다.

### zip 구성

| 경로 | 내용 |
| --- | --- |
| `issue_report.txt` | 입력한 이슈 설명과 수집 결과 요약 |
| `system_info.txt` | OS, 메모리, 디스크, 실행 중인 프로세스 |
| `screenshots/` | 실행 시점 화면 캡처 |
| `aurora_logs/` | 최근 N일 동안 수정된 Aurora 로그 |
| `aurora_config/` | Aurora 설정 파일 |

## 환경

- Windows 10 이상
- .NET Framework 4.8.1
- C# / WPF

## 빌드

Visual Studio 2022에서 `IssueReporterCD.sln`을 열고 빌드합니다.

`main` 브랜치에 push하면 GitHub Actions가 Windows에서 자동으로 빌드합니다. 빌드된 실행 파일은 Actions 실행 결과의 **Artifacts**에서 내려받을 수 있습니다.

## 설정

`IssueReporterCD.exe.config`(소스에서는 `src/IssueReporterCD/App.config`)의 `appSettings`에서 설정합니다.

| 키 | 설명 | 기본값 |
| --- | --- | --- |
| `AuroraLogDir` | Aurora 로그 폴더 | `C:\Aurora\Logs` (임시값) |
| `AuroraConfigDir` | Aurora 설정 폴더 | `C:\Aurora\Config` (임시값) |
| `LogMaxAgeDays` | 최근 며칠 치 로그를 수집할지. `0`이면 전체 | `3` |
| `OutputDir` | zip 저장 폴더. 비우면 `바탕화면\IssueReports` | (비어 있음) |

경로에는 `%ProgramData%` 같은 환경 변수를 쓸 수 있습니다.
