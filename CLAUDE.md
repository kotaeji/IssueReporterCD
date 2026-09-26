# IssueReporterCD

Aurora 장비 PC에서 이슈 설명과 장비 데이터를 zip으로 묶어 주는 WPF 도구. 개요와 설정은 README.md를 참고한다.

## 제약

- 대상: Windows, .NET Framework 4.8.1, WPF. **C# 7.3까지만** 쓴다(`LangVersion` 7.3). target-typed `new()`, `using var`, switch 식, nullable 참조 형식, record는 쓰지 않는다.
- 개발은 macOS에서 하고, 실행과 검증은 사용자가 회사 Windows PC에서 한다. 여기서는 실행할 수 없다.
- 변경 후 Mac에서 컴파일 검사를 한다(XAML 포함):
  `dotnet build src/IssueReporterCD/IssueReporterCD.csproj -p:EnableWindowsTargeting=true -v:minimal`
- 실제 Windows 빌드는 GitHub Actions(`.github/workflows/build.yml`)에서 확인한다.

## 구조 (src/IssueReporterCD)

- `Themes/` — `Colors.xaml`(네이비 코퍼레이트 테마 브러시), `Controls.xaml`(컨트롤 스타일). 색은 항상 `Brush.*` 리소스를 쓴다.
- `Collectors/` — `ICollector` 구현. 새 수집 항목은 여기에 추가하고 `App.OnStartup`에 등록한다.
- `Reporting/` — 세션 임시 폴더, 리포트 텍스트, zip 생성.
- `Settings/` — `App.config`의 appSettings(`AppSettings`), 사용자 입력 기억(`UserPrefs`).
- `ViewModels/`, `Infrastructure/` — 외부 라이브러리 없는 간단한 MVVM.

## 규칙

- UI 문구는 한국어. 리포트 파일(`issue_report.txt`)의 항목명은 한국어와 영어를 같이 쓴다.
- 텍스트 파일은 BOM 있는 UTF-8로 쓴다(Windows 메모장 호환).
