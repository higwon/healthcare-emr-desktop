# Healthcare Data 탐색 명세

2026-09-30. 자체 합성 데이터 engineering scenario다. 첫 기능 단계(E03)는 Overview → Timeline → 상세 → Trend 흐름이다.

## 정보 구조

합성 subject 한 개를 기본으로 테스트 fixture는 여러 subject를 지원한다. 실제 환자 등록·권한·진료 workflow는 제외한다.
Overview는 선택 subject·기간의 countByType·recentEvents·latestMetrics만 보여준다. 임의 건강 점수는 없다.
Timeline은 BloodTest/Medication/HealthCheckup/BodyMeasurement/Visit/Symptom 이벤트를 탐색한다.
OccurredAtUtc 내림차순, 동률 Id 내림차순. 연/월 grouping은 표시 시간대 Asia/Seoul로 계산한다.
타입별 상세 DTO와 측정 series를 분리한다. 공통 HealthEvent는 읽기 projection이며 거대 domain entity가 아니다.

## 계약

- Event: Id, SubjectId, OccurredAtUtc, Type, Title, Summary, Source, DetailId.
- Overview: query context·datasetVersion·countByType·recentEvents·latestMetrics.
- 기간은 UTC [from, to), from < to, 최대 3년. pageSize 1~100. types 생략은 전체, unknown type은 400.
- cursor는 query fingerprint(필터/subject/기간)·datasetVersion·마지막 시각/Id와 연계한다. 다른 context는 400 cursorMismatch, fixture 교체는 409 datasetChanged로 처음부터 조회한다.
- 단일 version 페이지 경계의 누락/중복 방지. append 실패는 기존 자료 보존·동일 cursor retry·중복 ID 삽입 차단.
- Trend: 단일 metric/unit, 최대 10,000 raw points. 한도 초과는 400 rangeTooLarge로 기간 축소 안내; 자동 data loss 없음.
- Point: id·observedAtUtc·decimal? value·unit·missingReason·source·optional referenceLow/referenceHigh/referenceSource.
- null은 gap, 0이나 임의 연결로 바꾸지 않는다. 단위 mismatch는 오류. 참고 구간은 합성 자료 표시·출처와 함께 보여주며 정상/비정상 판정 없음.
- request identity는 subject/기간/필터/metric/선택을 포함한다. token+generation으로 이전 응답/상세/series 적용을 막는다.

## 상태·선택

Idle/Loading/LoadedEmpty/Loaded/Error/Stale를 구분한다. 초기 실패와 다음 페이지 실패는 별도 메시지·retry다.
filter 변경 시 이전 자료를 이전 context로 표시하거나 비운다. 새 필터의 결과로 보이면 안 된다.
선택은 안정적 event Id로 유지하며 삭제/필터 제외 시 해제한다. detail response도 현재 선택 context일 때만 적용한다.
pointer/keyboard는 같은 selection model을 쓴다. retry는 focus를 빼앗지 않고 닫힌 detail은 late callback으로 다시 열리지 않는다.

## 초기 Custom Controls

HC-301 HealthTimelineControl: templated ItemsControl 계열 후보. DP(ItemsSource/SelectedEventId/DisplayTimeZone)·템플릿·grouping·가상화 panel·selection/focus·hit testing·AutomationPeer.
기본 container recycling을 실제 확인하고 grouping·Measure/Arrange·DP invalidation 비용을 측정한다.
HC-302 HealthTrendControl: 자체 drawing 후보. DP(Series/SelectedPointId/Viewport/DisplayUnit)·selection event·Measure/Arrange·render·hover·keyboard·AutomationPeer.
hover/selection은 전체 series 전처리를 반복하지 않는다. resize/DPI/series 변경의 invalidation 범위를 정의한다. 표 대안은 가상화 목록.
외부 Chart/UI library 없음. 오픈소스 분석은 관련 PR에서 primary source·license·차이·재구현 근거를 기록한다.
drawing 경로·downsampling은 baseline 후 ADR-004에서 확정한다. 결측·극값·선택 원본·접근 가능한 표를 보존한다.

## 검증·미결정

고정 seed/version의 100/1,000/10,000 events·최대 10,000 points, 긴 title·동일 시각·month boundary·결측 fixture.
API/VM race·Control STA·실제 WPF DPI 100/150/200%·keyboard/Automation·최소 창·Binding 오류·query/layout/render 측정은 각 UI Task Done 조건.
반복 detail 열기/닫기·subject 변경·Loaded/Unloaded·late callback을 초기부터 재현한다. [측정 기준](performance-case-studies.md)을 따른다.
미결정: 탐색 UI 승인 시안·최소 창 크기, 지원 Windows/모니터 matrix, 패키지/TFM 호환, profiler·성능 예산·downsampling.
기존 복약 브라우저 시안은 탐색 UI 승인 증거가 아니다.
