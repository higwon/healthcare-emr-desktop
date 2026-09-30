# 저장소·이슈·PR 운영

범위 source of truth는 epics-and-tasks.md, 운영 상태는 GitHub issue다. 문서에 상세 진행 로그를 중복해서 쌓지 않는다.
설계 기준 PR #22를 검토·병합했다. 이후 Task 종류별 AC를 충족하는 작은 구현 PR로 진행한다.

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
| Design baseline | HC-001~004 / PR #22 merged | 설계·계약·규칙·Task만 | 설계 리뷰 |
| 구현 PR 1 | HC-101 / Solution·Windows CI | 프로젝트/참조·TFM 검증·Release build/test·최소 실행, 기능 없음 | 기준 검토·ADR 선택 |
| 구현 PR 2 | HC-102 / MVVM·수명·진단 | 분리 VM·DI·취소/generation·오류·구독/해제·안전한 진단 | HC-101 |
| 구현 PR 3 | HC-103 / 탐색 shell·리소스 | 탐색 host·focus/Automation·DPI·layout baseline, write 없음 | HC-004, HC-102·탐색 UI 시안 검토 |
| 첫 기능 A | HC-104 | 합성 조회 API PR → Overview/Timeline paging·상태 UI PR | HC-103 |
| 첫 기능 B | HC-301 | 자체 Timeline Control PR → 실제 측정 기반 개선 PR | HC-104 |
| 첫 기능 C | HC-302 | series API/VM PR → 자체 Trend Control PR → 측정 기반 개선 PR | HC-301 |
| Medication Domain | HC-201 | 복약 불변식·clock·테스트 | HC-003, HC-302 |
| Medication API | HC-202 | SQLite·최소 작업 결과·DTO/transaction | HC-201 |
| Medication Recovery | HC-203 | unknown·작업 조회·명시적 retry·409·재시작 확인 | HC-202 |
| Medication UI | HC-204 → HC-205 | 등록·일정·기록·종료·실패 UX | HC-203 |
| 전체 재검증/증거 | HC-401 → HC-402 | 누적 품질 재검증·Case Study·packaging·README | 탐색·복약 완료 |

한 PR이 동작·화면·저장 계약 변경까지 검토하기 어려울 정도면 같은 Task 안에서 하위 PR로 나눈다.
기능을 서버까지 완성하는 과정은 여러 작은 PR로 이어질 수 있다. 거대한 단일 'MVP 완성' PR은 만들지 않는다.
첫 3개는 기반 PR, 첫 기능 단계는 HC-104/301/302다. 자체 Control이 실제로 완성된 초기 결과물 없이 Medication 단계로 넘어가지 않는다.
HC 숫자는 기존 식별자를 보존한 것이며 실행 순서가 아니다. HC-301/302는 HC-201보다 먼저 수행한다.

## 검증·merge

docs-only: diff·문서 링크·Task mapping·미확인 사실·source 범위 검사. 코드 테스트 성공을 꾸며 쓰지 않는다.
code: Windows Release restore/build, 관련 자동 테스트, API 계약/실패 시나리오, UI PR이면 ui-guide/epics-and-tasks의 종류별 검증 범위를 적용한다. 변경한 화면·Control의 증거만 갱신하고 영향 없는 이전 증거는 근거를 남겨 재사용한다.
[측정 기준](performance-case-studies.md)에 원자료를 연결하고 필수 미검증 상태에서는 Done으로 전환하지 않는다.
CI 이름은 HC-101에서 `build-and-test`로 정의하고 TRX·해당 UI evidence(coverage는 의미 있는 업무 테스트 도입 후)를 artifact로 보존한다. 임의 커버리지 % gate 없음.
branch protection에 required check를 권장하되 사용자가 요청하기 전 repository security 설정을 바꾸지 않는다.
Draft → checks·설명·증거 완료 → review. merge는 저장소 소유자 결정 또는 명시적 위임에 따른다.
