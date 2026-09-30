# Healthcare EMR Desktop · 건강노트

Healthcare Data를 탐색하고 관리하는 장시간 실행 **WPF Desktop Client** 포트폴리오.
지원 목표는 **EMR Windows Client 개발**이다. 건강노트(HealthNote)는 자체 합성 데이터 시나리오로
WPF architecture, Custom Controls, 대량 데이터 표현, Client-Server 비동기 동작,
성능·메모리/수명·DPI·접근성·진단·실패 복구를 설계·구현·측정 증거로 보여준다.
특정 EMR 제품, 네이버 소비자 앱 또는 의료진 업무 시스템의 Clone을 목표로 하지 않는다.

실행 기반: **HC-101 빌드·테스트·CI + HC-102 MVVM·명령·화면 수명**.
[설계 PR #22](https://github.com/higwon/healthcare-emr-desktop/pull/22)와 HC-101을 병합했다.
HC-102는 readiness API를 호출하는 개발 기반 화면과 열기/닫기·취소·재시도·안전한 진단을 구현한 리뷰 단계다.
제품 UI·업무 기능은 후속 단계다. 기반 검증과 제품 품질 검증을 구분한다.
개발 환경·공통 명령은 [실행 가이드](docs/foundation-build.md), 구현 계약·검증 한계는 [HC-102](docs/mvvm-foundation.md)를 따른다.

## 제품 범위

- 핵심 Feature: **Health Overview → Health Timeline → Health Trend**, 이후 Medication 상태 변경 흐름.
- 첫 기능 단계: 합성 API 조회, 이벤트 필터·상세 탐색, 자체 Timeline Control, 지연/빈/오류/stale 상태와 실제 WPF 측정.
- Trend: 값·단위·결측·참고 구간의 데이터 표현을 자체 Control로 구현한다. Body Measurement·Symptom Record 입력 확장은 후속 범위다.
- 실제 진료 작성·처방·청구, 가족 공유·병원 전달은 현재 범위가 아니다.
- 데이터는 합성 데이터로 시작한다. 개인 의료정보 입력·운영 배포를 허용하는 제품으로 표현하지 않는다.

## 설계 문서

| 기준 | 문서 |
| --- | --- |
| 제품·우선순위 | [제품 범위](docs/project-overview.md), [데이터 탐색 명세](docs/health-data-exploration.md) |
| 도메인 참고 | [실서비스 조사](docs/service-flow-research.md) — 제품 요구사항과 별도 |
| 구조·버전 | [아키텍처](docs/architecture.md), [결정 기록](docs/decisions.md) |
| 도메인·계약 | [도메인 규칙](docs/domain-rules.md), [API 계약](docs/api-contracts.md) |
| UI·상태 | [UI 기준](docs/ui-guide.md), [복약 흐름](docs/medication-flow-spec.md), [시안 검토](docs/ui-prototype-review.md) |
| 코드·WPF | [C#/WPF 규칙](docs/coding-rules.md), [.editorconfig](.editorconfig) |
| 작업 관리 | [Epic/Task](docs/epics-and-tasks.md), [개발·PR 흐름](docs/development-workflow.md) |
| 측정 증거 | [성능·메모리 Case Study 기준](docs/performance-case-studies.md) |
| GitHub 연결 | [실제 Epic/Task 목록](docs/issue-registry.md) |
| 에이전트 작업 | [AGENTS.md](AGENTS.md) |

## 측정 산출물

Performance Investigation 2~3건과 profiler 기반 Memory Case Study 1건을 목표로 한다.
가설·측정·결론·한계를 기록하고 병목이 확인되면 Root Cause → Solution → Before/After를 남긴다.
실제 최적화 사례 1건 이상은 목표이며 결함 발견 수를 완료 조건으로 강제하지 않는다.
현재 결과는 **미측정**이다. 실제 측정 후 README에 해당 PR·환경·결과를 연결한다.

문서의 정책은 자체 포트폴리오 설계다. 공개 서비스의 기능 사실은 출처가 있는 조사 문서로 구분한다.
이전 진료·Workbench 초안은 [archive](docs/archive/README.md)에 보관한다.
앞서 작성한 실행 코드 초안은 Git에서 제외한 로컬 보류 영역에 보존한다.

이전 작업 방식 참고: [retail-pos-desktop](https://github.com/higwon/retail-pos-desktop).
