# Healthcare EMR Desktop · 건강노트

네이버 소비자 헬스케어의 공개 기능을 참고한 개인 건강관리 Windows 포트폴리오.
지원 목표는 **EMR Windows Client 개발**이며, 소비자 서비스 기능과 병원 EMR 제품을 구분한다.
제품 수준의 UI와 WPF·MVVM·비동기 처리·Windows 품질을 설계·구현·테스트 증거로 보여준다.

현재 단계: **설계 및 개발 운영 기준 수립**. 홈·복약관리 조작 시안은 있으나 WPF 기능 구현은 보류했다.
설계 기준 PR을 검토한 뒤 이슈 단위로 구현한다. 실행 코드·실제 API 연동·배포 완료를 주장하지 않는다.

## 제품 범위

- 첫 흐름: 약 직접 등록 → 요일·복수 시각 일정 → 날짜별 조회 → 복용 체크·취소 → 관리 종료.
- 다음 범위: 체성분 기록·추이, 증상 기록 UI. 실제 인바디·네이버 API와 의료 판단 알고리즘은 별도 검증이 필요하다.
- 실제 진료 작성·처방·청구, 가족 공유·병원 전달은 현재 범위가 아니다.
- 데이터는 합성 데이터로 시작한다. 개인 의료정보 입력·운영 배포를 허용하는 제품으로 표현하지 않는다.

## 설계 문서

| 기준 | 문서 |
| --- | --- |
| 제품·우선순위 | [제품 범위](docs/project-overview.md), [실서비스 조사](docs/service-flow-research.md) |
| 구조·버전 | [아키텍처](docs/architecture.md), [결정 기록](docs/decisions.md) |
| 도메인·계약 | [도메인 규칙](docs/domain-rules.md), [API 계약](docs/api-contracts.md) |
| UI·상태 | [UI 기준](docs/ui-guide.md), [복약 흐름](docs/medication-flow-spec.md), [시안 검토](docs/ui-prototype-review.md) |
| 코드·WPF | [C#/WPF 규칙](docs/coding-rules.md), [.editorconfig](.editorconfig) |
| 작업 관리 | [Epic/Task](docs/epics-and-tasks.md), [개발·PR 흐름](docs/development-workflow.md) |
| GitHub 연결 | [실제 Epic/Task 목록](docs/issue-registry.md) |
| 에이전트 작업 | [AGENTS.md](AGENTS.md) |

문서의 정책은 자체 포트폴리오 설계다. 공개 서비스의 기능 사실은 출처가 있는 조사 문서로 구분한다.
이전 진료·Workbench 초안은 [archive](docs/archive/README.md)에 보관한다.
앞서 작성한 실행 코드 초안은 Git에서 제외한 로컬 보류 영역에 보존한다.

이전 작업 방식 참고: [retail-pos-desktop](https://github.com/higwon/retail-pos-desktop).
