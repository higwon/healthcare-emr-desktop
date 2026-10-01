# Repository work rules

사용자 지시가 우선이다. 실제 체크아웃의 브랜치·상태부터 확인하고 사용자 변경을 덮어쓰지 않는다.

## 현재 목표 (2026-10-01 사용자 변경)

EMR MVP: 환자 조회 → 진료 이력 → 진료 기록 작성·저장, 실제 RESTful API 연동 테스트. docs/emr-mvp.md가 기존 탐색/복약 범위보다 우선한다. EMR-101 → EMR-102 → EMR-103 순서. DPI/High Contrast 등 미검증 항목은 기록으로 남기되 다음 EMR 구현을 차단하지 않는다. 사용자 승인 없는 merge 금지는 유지한다.

## 이전 기반 단계

설계 PR #22를 사용자 검토 후 병합했다. HC-101부터 Ready 조건을 충족한 Task 하나씩 별도 Draft PR에서 구현한다.
HC-101은 실행/CI 기반만, MVVM은 HC-102, 제품 UI는 HC-103 이후다. Task 종류별 품질 AC만 적용한다.
로컬 `.local/implementation-spike`는 보류한 미커밋 초안이며 구현 기준·검증 증거로 사용하지 않는다.

## 작업 시작

1. README와 현재 Task를 읽는다.
2. 관련 source-of-truth 문서만 읽는다: 범위는 project-overview/epics-and-tasks, 구조는 architecture/decisions, 동작은 health-data-exploration/domain-rules/api-contracts, UI는 ui-guide, 코드는 coding-rules, 측정은 performance-case-studies. 공개 서비스 조사·기존 복약 시안은 참고자료다.
3. `git status --short --branch`, 현재 원격·베이스·이슈 의존성을 확인한다.
4. Ready 기준을 만족한 Task 하나를 선택하고 `codex/hc-xxx-description` 브랜치에서 작업한다.

## 구현 및 리뷰

- Domain에 WPF·HTTP·DB·로그·DTO를 넣지 않는다. ViewModel이 View/Window·HttpClient·DB를 직접 참조하지 않는다.
- 업무 로직은 C# 독립 로직, 포커스·창·Animation 등 시각 처리는 View/Behavior에 둔다.
- 이벤트·메신저·타이머·취소 수명과 해제를 짝지어 정의한다. ViewModel을 `Unloaded`마다 Dispose하지 않는다.
- 사용자 기록을 바꾸는 동작에는 의미 있는 테스트를 추가한다. 문서·단순 스타일 변경에는 불필요한 구현 복제 테스트를 만들지 않는다.
- 작은 Draft PR, 관련 이슈·검증 결과·UI 캡처·제한을 남긴다. 이번 Task 파일만 stage한다.
- 실제 실행·테스트가 없으면 완료라고 하지 않는다. 실패·미검증 환경을 명시한다.
- 민감 데이터·토큰·원문 payload를 로그·이슈·캡처에 넣지 않는다.
- 이슈 생성·PR 생성·상태 변경은 실제 성공 결과를 확인한다. 생성한 PR은 현재 채팅에 연결한다.
- 사용자 승인 없는 merge·공개 운영 배포·보안 설정 변경을 하지 않는다. 별도 승인 절차를 추측해 추가하지 않는다.
- 실행 코드 변경 전에 기능·계약 변경이 있으면 문서와 Task 수용 기준부터 수정한다.

추가 에이전트 작업이나 병렬 위임은 사용자가 요청한 경우에만 수행한다.
