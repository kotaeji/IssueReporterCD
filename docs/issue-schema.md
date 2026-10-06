# 이슈 스키마 필드 카탈로그 (report.json v2)

Issue Reporter가 만드는 `report.json`의 필드 목록입니다. 앞으로 만들 분석 서비스와 GitLab 이슈 템플릿이 같은 이름을 쓰도록 하는 기준 문서입니다.

- 출처: Ruka 「장비SW 이슈 스키마 설계」 답변 (2026-10-05 12:20 문의)
- 원칙: 층은 **누가 채우는가**로 나눕니다. 사람이 채울 수 없는 칸은 자동으로 채우고, 사람에게는 꼭 필요한 칸만 요구합니다.
- 필수 입력은 증상 4칸(제목, 기대 동작, 실제 동작, 재현 절차)과 기본 정보뿐입니다. 나머지는 선택이거나 자동입니다.

## 채우는 주체

| 표시 | 의미 |
| --- | --- |
| **자동(PC)** | Issue Reporter가 이 PC에서 직접 읽음 |
| **자동(Aurora)** | Aurora / Boot Loader가 남긴 `environment.ini`에서 읽음 ([aurora-integration.md](aurora-integration.md)) |
| **사람** | 현장 엔지니어가 입력 |
| **예약** | 지금은 항상 `null`. Aurora 런타임이 값을 넘겨주면 채움 |

선택 항목에서 "모름"을 고르거나 값이 없으면 `null`로 기록합니다.

## ① 식별 `identification`

| 필드 | 주체 | 설명 |
| --- | --- | --- |
| `issue_id` | 자동(PC) | 리포트마다 새 GUID |
| `reported_at` | 자동(PC) | 리포트 생성 시각 (ISO 8601) |
| `reporter` | 사람 | 작성자 |
| `source` | 자동(PC) | 유입 경로. 항상 `issue-reporter` |
| `reporter_app_version` | 자동(PC) | Issue Reporter 버전 |

## ② 환경 지문 `environment`

스키마의 핵심입니다. 사람은 채울 수 없지만, 회귀 탐지와 호환성 분석이 모두 이 칸에 달려 있습니다.

| 필드 | 주체 | 설명 |
| --- | --- | --- |
| `platform_version` | 자동(Aurora) | Aurora 플랫폼 버전. 예: `3.3.0.0` |
| `machine_sw_version` | 자동(Aurora) | 장비 SW 버전 |
| `api_level` | 자동(Aurora) | 플랫폼 API Level |
| `build_config` | 자동(Aurora) | `Debug` / `Release` |
| `architecture` | 자동(Aurora) → 없으면 자동(PC) | Aurora 프로세스 아키텍처. 예: `x64` |
| `module_versions` | 자동(Aurora) | `{ "WMX3": "3.4.1", "MIL": "10.60", ... }` |
| `site` | 사람 | 사이트 |
| `line` | 사람 → 없으면 자동(Aurora) | 라인 |
| `machine_id` | 사람 (기본값 PC 이름) | 장비 ID |
| `equipment_model` | 자동(Aurora) | 장비 모델 |
| `hostname` | 자동(PC) | PC 이름 |
| `os_version` | 자동(PC) | 예: `Windows 10 Pro 22H2 (build 19045)` |
| `os_architecture` | 자동(PC) | 예: `x64` |
| `screen_resolution` | 자동(PC) | 모니터별 해상도. 예: `1920x1080 (primary); 1280x1024` |
| `data_path` | 자동(Aurora) | Aurora DataPath |
| `config_hash` | 자동(PC) | 설정 폴더 전체의 `sha256:...`. 파일 내용과 상대 경로만 반영하므로, 같은 설정이면 언제 어디서 계산해도 같은 값 |
| `sequence_id`, `sequence_revision` | 자동(Aurora) | 실행 중인 시퀀스와 리비전 |
| `recipe_id`, `recipe_revision` | 자동(Aurora) | 레시피와 리비전 |
| `aurora_environment_file` | 자동(PC) | `{ path, found, written_at }`. `written_at`이 오래됐으면 Aurora 정보가 낡았을 수 있음 |
| `extra` | 자동(Aurora) | `environment.ini`에 있지만 위 목록에 없는 키. 카탈로그에 올리기 전까지 여기 보존 |

## ③ 증상 `symptom`

| 필드 | 주체 | 필수 | 값 |
| --- | --- | --- | --- |
| `title` | 사람 | ✔ | 한 줄 요약 |
| `symptom_type` | 사람 | | 증상 템플릿 이름. 예: `알람 발생` |
| `expected` | 사람 | ✔ | 기대 동작 |
| `actual` | 사람 | ✔ | 실제 동작. 증상 템플릿으로 채워짐 |
| `repro_steps` | 사람 | ✔ | 재현 절차. 재현이 안 되면 발생 직전 작업 |
| `frequency` | 사람 | | `always` / `intermittent` / `once` |
| `reproducible` | 사람 | | `site` (현장에서 재현됨) / `none` (재현 안 됨). `lab`(사내 재현)은 분류 단계에서 기록 |
| `occurred_at` | 사람 | ✔ | 발생 시각 (입력한 문자열 그대로) |
| `severity` | 사람 | | `high` / `medium` / `low` |
| `actions_taken` | 사람 | | 현장에서 한 조치 |

현장 영향과 긴급도는 자동화하지 않습니다. 긴급도는 받는 쪽의 상황이 정하기 때문입니다.

## ④ 발생 좌표 `location`

Aurora 개념으로 위치를 찍는 칸입니다.

| 필드 | 주체 | 값 |
| --- | --- | --- |
| `operating_mode` | 사람 | `Auto` / `Manual` / `Teaching` / `Simulation` |
| `lifecycle_phase` | 사람 | `Boot` / `Load` / `Run` / `Stop` / `Shutdown` |
| `station`, `substation`, `motion_device`, `axis`, `step_id`, `transition_id` | 예약 | Aurora 런타임이 현재 노드를 넘겨주면 자동 |

## ⑤ 증거 `evidence`

| 필드 | 주체 | 설명 |
| --- | --- | --- |
| `alarm_codes` | 사람 | 알람 코드 목록. 쉼표·공백·줄바꿈으로 구분해 입력하고, 중복은 제거 |
| `exception_type`, `message`, `stack_hash` | 예약 | Aurora가 예외 정보를 남기면 자동 |
| `log_bundle` | 자동(PC) | zip 안의 로그 폴더 |
| `screenshot` | 자동(PC) | zip 안의 화면 캡처 폴더 |
| `config_snapshot` | 자동(PC) | zip 안의 설정 사본 폴더 |
| `newest_log_time` | 자동(PC) | 로그 폴더에서 가장 최근 수정 시각 |
| `log_gap_warning_acknowledged` | 자동(PC) | 로그 시각 차이 경고를 보고도 진행했는지 |
| `collections` | 자동(PC) | 수집 항목별 결과 `{ name, status, detail }` |

## ⑥ 판정·해결 (Issue Reporter 범위 밖)

`defect_type`, `trigger`, `root_cause_layer`, `component`, `owner`, `fixed_in_version`, `mr_url`, `resolved_at`, `field_verified_at`, `duplicate_of`는 분류·해결 단계(GitLab 등)에서 채웁니다. 현장 리포트에는 넣지 않습니다.

## 버전 기록

| 버전 | Issue Reporter | 변경 |
| --- | --- | --- |
| 2 | 0.3.0 | 층 구조로 재편, 필드 이름을 snake_case로 통일, 환경 지문 추가. 최상위 버전 키가 `schemaVersion` → `schema_version`으로 바뀜 |
| 1 | 0.2.0 | 평면 구조 (`schemaVersion`, camelCase) |

읽는 쪽은 `schema_version`(v2 이상) 또는 `schemaVersion`(v1)으로 버전을 먼저 확인하세요. 필드 이름을 바꾸거나 지울 때만 버전을 올립니다. 필드 추가는 버전을 올리지 않습니다.
