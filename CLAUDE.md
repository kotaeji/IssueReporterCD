# IssueReporterCD

Aurora 장비 PC에서 이슈 설명과 장비 데이터를 zip으로 묶어 주는 WPF 도구. 개요와 설정은 README.md를 참고한다.

## 제약

- 대상: Windows, .NET Framework 4.8.1, WPF. **C# 7.3까지만** 쓴다(`LangVersion` 7.3). target-typed `new()`, `using var`, switch 식, nullable 참조 형식, record는 쓰지 않는다.
- 개발은 macOS에서 하고, 실행과 검증은 사용자가 회사 Windows PC에서 한다. 여기서는 실행할 수 없다.
- 변경 후 Mac에서 컴파일 검사를 한다(XAML 포함):
  `dotnet build src/IssueReporterCD/IssueReporterCD.csproj -p:EnableWindowsTargeting=true -v:minimal`
- 실제 Windows 빌드는 GitHub Actions(`.github/workflows/build.yml`)에서 확인한다.
- 화면은 `tools/ScreenTour`가 CI(Windows)에서 모든 창과 드롭다운을 PNG로 렌더링해 `ui-screenshots` 브랜치에 올린다. UI를 바꾸면 push 후 `git fetch origin ui-screenshots`로 받아 직접 확인한다. 새 창이나 드롭다운을 추가하면 ScreenTour에도 추가한다.

## 구조 (src/IssueReporterCD)

- `Themes/` — `Colors.xaml`(네이비 코퍼레이트 테마 브러시), `Controls.xaml`(컨트롤 스타일). 색은 항상 `Brush.*` 리소스를 쓴다.
- `Collectors/` — `ICollector` 구현. 새 수집 항목은 여기에 추가하고 `CollectorFactory`에 등록한다. `EnvironmentCollector`가 스키마의 환경 지문 층을 만든다.
- `Reporting/` — 세션 임시 폴더, 리포트 텍스트, `report.json`, zip 생성. `report.json` 필드는 `docs/issue-schema.md`(필드 카탈로그)와 항상 함께 고친다. 필드 이름을 바꾸거나 지우면 `ReportManifest.CurrentSchemaVersion`을 올리고 카탈로그의 버전 기록에 남긴다.
- `Settings/` — 경로와 옵션 결정(`SettingsService`: 사용자 지정 > Aurora 제공 파일 > App.config), 사용자 입력 기억(`UserPrefs`). Aurora 연동 규격은 `docs/aurora-integration.md`.
- `SymptomTemplates/` — 증상 템플릿 텍스트(exe 옆으로 복사됨).
- `Views/` — 설정 창, 로그 시각 경고 창, `DialogService`(`IDialogService` 구현).
- `Assets/app.ico` — 앱 아이콘. 직접 수정하지 말고 `assets/app-icon.svg`(디자인 원본)와 `tools/make_icon.py`를 함께 고친 뒤 스크립트로 다시 만든다.
- `ViewModels/`, `Infrastructure/` — 외부 라이브러리 없는 간단한 MVVM.

## 규칙

- UI 문구는 한국어. 리포트 파일(`issue_report.txt`)의 항목명은 한국어와 영어를 같이 쓴다.
- 텍스트 파일은 BOM 있는 UTF-8로 쓴다(Windows 메모장 호환).
