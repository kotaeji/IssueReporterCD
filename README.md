# Aurora Issue Reporter (IssueReporterCD)

Aurora 기반 장비 소프트웨어가 설치된 PC에서 이슈가 발생했을 때, 현장 엔지니어가 실행해 **이슈 설명과 장비 데이터를 zip 파일 하나로** 만드는 도구입니다. 만든 zip 파일을 한국 담당자에게 보내면 됩니다.

## 동작 방식

1. 프로그램을 실행하는 순간 전체 화면을 캡처합니다. 리포터 창이 뜨기 전의 장비 화면이 그대로 남습니다.
2. 백그라운드에서 시스템 정보와 Aurora 로그, 설정 파일을 수집합니다.
3. 그동안 엔지니어가 템플릿에 맞춰 이슈 설명을 작성합니다.
4. **리포트 생성**을 누르면 먼저 로그 시각을 검사합니다. 가장 최근 로그가 지금과 크게 차이 나면 경로 설정이 잘못되었을 수 있다고 알려줍니다.
5. 이어서 `yyyyMMdd_HHmmss_<장비ID>.zip` 파일이 만들어집니다.

### zip 구성

| 경로 | 내용 |
| --- | --- |
| `issue_report.txt` | 입력한 이슈 설명과 수집 결과 요약 (사람이 읽는 용도) |
| `report.json` | 같은 내용을 기계가 읽는 형식으로 저장 (향후 분석 서비스용, `schemaVersion` 포함) |
| `system_info.txt` | OS, 메모리, 디스크, 실행 중인 프로세스 |
| `screenshots/` | 실행 시점 화면 캡처 |
| `aurora_logs/` | 최근 N일 동안 수정된 Aurora 로그 |
| `aurora_syserror/` | 최근 N일 동안 수정된 sys-error 로그 |
| `aurora_config/` | Aurora 설정 파일 |

## 환경

- Windows 10 이상
- .NET Framework 4.8.1
- C# / WPF

## 빌드

Visual Studio 2022에서 `IssueReporterCD.sln`을 열고 빌드합니다.

`main` 브랜치에 push하면 GitHub Actions가 Windows에서 자동으로 빌드합니다. 빌드된 실행 파일은 Actions 실행 결과의 **Artifacts**에서 내려받을 수 있습니다.

## 설정

앱 오른쪽 위 **설정** 버튼에서 언제든 바꿀 수 있습니다.

| 항목 | 설명 | 기본값 |
| --- | --- | --- |
| 로그 폴더 | Aurora 일반 로그 | Aurora 제공 경로 → 없으면 `C:\Aurora\Logs` (임시값) |
| sys-error 폴더 | Aurora sys-error 로그 | Aurora 제공 경로 → 없으면 `C:\Aurora\SysError` (임시값) |
| 설정(config) 폴더 | Aurora 설정 파일 | Aurora 제공 경로 → 없으면 `C:\Aurora\Config` (임시값) |
| 리포트 저장 폴더 | zip 저장 위치 | `바탕화면\IssueReports` |
| 로그 수집 기간 | 최근 며칠 치 로그와 sys-error를 모을지. `0`이면 전체 | 3일 |
| 로그 시간 차이 경고 기준 | 최근 로그 시각이 지금과 이 시간 이상 차이 나면 경고. `0`이면 끔 | 24시간 |

- **저장 위치**: 설정 화면에서 바꾼 값은 `%ProgramData%\IssueReporterCD\settings.ini`에 저장됩니다. 장비 PC의 모든 계정이 같이 씁니다.
- **Aurora 경로 연동**: Aurora가 경로 정보를 전달하는 방식은 [docs/aurora-integration.md](docs/aurora-integration.md)를 참고하세요.
- **설치 기본값**: `IssueReporterCD.exe.config`(소스에서는 `src/IssueReporterCD/App.config`)에 있습니다.

## 증상 템플릿

증상 유형을 고르면 입력칸에 해당 템플릿이 채워집니다. 템플릿은 exe 옆 `SymptomTemplates\*.txt` 파일입니다.

- 파일을 추가하거나 고치면 다시 빌드하지 않아도 반영됩니다.
- 파일 이름 앞의 `01_` 같은 숫자는 표시 순서이고, 화면에는 나오지 않습니다.
- 파일은 UTF-8로 저장하세요.
