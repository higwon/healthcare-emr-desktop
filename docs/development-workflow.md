# 저장소·이슈·PR 운영

범위 source of truth는 epics-and-tasks.md, 운영 상태는 GitHub issue다. 문서에 상세 진행 로그를 중복해서 쌓지 않는다.
사용자가 명시한 설계 우선 요구에 따라 설계 기준 PR 검토 이전 기능 구현은 보류한다.

## 상태와 진입 기준

Backlog → Ready → In Progress → Review → Done. 제목은 `[Epic] HC-E01 ...`, `[Task] HC-001 ...`.
Ready: 사용자 흐름·영향 계층·계약/상태·수용 기준·의존성·검증법이 있고 미확정 내용이 구현을 막지 않음.
Done: 수용 기준 충족, 실제 검증 증거, 문서 정합성, PR merge. 이슈를 만들거나 초안을 썼다는 이유로 Done 처리하지 않는다.
Epic body에는 자식 Task checklist와 공통 완료 기준을 둔다. task body에는 parent·dependencies·scope·non-goals·AC·validation·PR slice.
기존 issue를 검색해 중복 생성을 막는다. 실제 issue number는 별도 registry에 연결한다.

## 브랜치·커밋

원격 default `main`에서 시작한다. 각 task는 `codex/hc-xxx-short-description`.
커밋은 `docs(HC-001): ...`, `feat(HC-201): ...`, `fix(HC-...): ...`, `test(HC-...): ...`.
git status와 base를 먼저 확인하고 해당 Task 파일만 stage한다. archive·spike를 활성 코드와 섞지 않는다.
빈 원격의 최초 baseline만 main에 최소 README로 초기화하고, 상세 설계는 별도 branch/Draft PR로 검토한다.
force push·history rewrite·보호 규칙 변경·자동 merge는 요청 없으면 수행하지 않는다.

## PR 분할 계획

| 순서 | Task/PR | 포함 범위 | 선행 조건 |
| --- | --- | --- | --- |
| Design baseline | HC-001~004 / docs(HC-001): establish design and workflow baseline | 구조·규칙·계약·Epic/Task·UI 증거·템플릿만 | 실서비스 조사 |
| Foundation | HC-101 / chore(HC-101): bootstrap solution and Windows CI | 프로젝트·참조·빌드·실행·테스트 기반, 업무 기능 없음 | 기준 PR 검토·ADR 선택 |
| MVVM | HC-102 / feat(HC-102): establish navigation and screen lifetime | 분리 ViewModel·DI·취소·오류 경계 | HC-101 |
| UI foundation | HC-103 / feat(HC-103): implement approved shell and resources | 토큰·화면 shell·접근성, 업무 write 없음 | HC-004, HC-102 |
| Domain | HC-201 / feat(HC-201): model medication schedules and records | 불변식·수명·clock·테스트 | HC-003, HC-101 |
| Persistence/API | HC-202 / feat(HC-202): persist plans, records and operations | SQLite transaction·DTO·조회/쓰기·계약 테스트 | HC-201 |
| Recovery | HC-203 / feat(HC-203): handle unknown outcomes and retry | stable ID·operation query·restart·conflict | HC-202 |
| Registration | HC-204 / feat(HC-204): register medication through WPF | 직접 입력·일정·확인·API 저장, 실패 초안 유지 | HC-103, HC-203 |
| Intake journey | HC-205 / feat(HC-205): record and end medication schedules | 날짜 조회·체크·취소·종료·통합 증거 | HC-204 |
| Follow-ups | HC-301~402 | 체성분·증상·품질의 각 Task PR | 복약 흐름 완료 |

한 PR이 동작·화면·저장 계약 변경까지 검토하기 어려울 정도면 같은 Task 안에서 하위 PR로 나눈다.
기능을 서버까지 완성하는 과정은 여러 작은 PR로 이어질 수 있다. 거대한 단일 'MVP 완성' PR은 만들지 않는다.

## 검증·merge

docs-only: diff·문서 링크·Task mapping·미확인 사실·source 범위 검사. 코드 테스트 성공을 꾸며 쓰지 않는다.
code: Windows Release restore/build, 관련 자동 테스트, API 계약/실패 시나리오, UI PR이면 실제 WPF 캡처/키보드.
CI 이름은 HC-101에서 `build-and-test`로 정의하고 TRX/coverage·UI evidence를 artifact로 보존한다. 임의 커버리지 % gate 없음.
branch protection에 required check를 권장하되 사용자가 요청하기 전 repository security 설정을 바꾸지 않는다.
Draft → checks·설명·증거 완료 → review. merge는 저장소 소유자 결정 또는 명시적 위임에 따른다.
