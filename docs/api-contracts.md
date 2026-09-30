# API 계약 초안

설계 PR에서 검토할 v1 계약. endpoint와 DTO는 구현·배포되지 않았다. Demo는 합성 데이터·loopback만 지원한다.
UTF-8 JSON camelCase. 날짜 YYYY-MM-DD, 시각 HH:mm, timestamp ISO8601 UTC(Z), decimal 수치+unit.
도메인 entity 직접 serialization을 금지한다. 입력은 unknown lifecycle/authority fields를 받지 않는다.

| Method/Path | 요청 | 응답 |
| --- | --- | --- |
| GET /api/v1/health | 없음 | demo 모드·버전·ready 상태 |
| GET /api/v1/medication-schedules?date=YYYY-MM-DD | 선택 날짜 | date/timeZone, plan count, occurrence projections, query timestamp |
| POST /api/v1/medication-plans | operationId, entry(name/kind/note/source), schedule(start/end/days/times/timeZone), notificationRequested | 201 + plan ID/version, operation receipt |
| POST /api/v1/intake-records | operationId, occurrenceId, taken, expectedVersion | 200 + committed record/version·UTC 기록 시각 |
| POST /api/v1/medication-plans/{id}/end | operationId, expectedVersion | 200 + 종료 상태/version·영수증 |
| GET /api/v1/operations/{operationId} | 원래 작업 ID | found+committed receipt 또는 404. 요청 저장 완료 이전 404는 실패 확정 증거가 아님 |
| GET /api/v1/medication-plans/{id}/history | cursor, pageSize≤100 | immutable history projections·next cursor |

일정 수정 endpoint·체성분·증상 계약은 해당 Task 전에 추가한다. 수정 기능은 일정 version·effective date·원본 snapshot 정책을 따라야 한다.
조회 DTO는 occurrence ID, plan summary, 날짜·시각·timezone, record status/version/recordedAtUtc, canChange/reason을 포함한다.
직접 입력한 약에 합성 의학적 효능·주의사항을 자동 채우지 않는다.

## 오류 및 동시성

400 validation, 404 missing resource, 409 version 또는 idempotency conflict, 503 temporarily unavailable.
오류는 ProblemDetails(application/problem+json): type/title/status/code/traceId와 안전한 field errors. payload·stacktrace·사용자 이름 없음.
409 두 유형을 code로 구분하고 클라이언트는 최신 조회·명시적 다시 선택을 제공한다.
명령 operationId는 GUID, payload fingerprint는 필드 순서가 아닌 canonical 의미값으로 계산한다.
operation 처리의 retention과 재시도 기간은 동일해야 한다. 첫 Demo는 receipt를 데이터셋 수명 동안 유지한다.
list cursor ordering·대량 조회 한도·삭제 후 receipt 동작은 API Task에서 테스트한다.

## 데모 장애 주입

업무 DTO에 failure flag를 넣지 않는다. 명시적 Demo 설정에서만 adapter/test harness가 저장 전 실패·commit 후 응답 유실·지연·offline을 재현한다.
데모 제어는 실제 제품 탐색/폼과 분리된 개발 도구에 둔다. Production-safe 설정에서는 비활성화한다.
실제 사용자의 의료 데이터·token을 요청하거나 외부 네이버/인바디로 전송하지 않는다.
