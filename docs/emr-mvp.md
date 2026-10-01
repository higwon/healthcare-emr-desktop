> 2026-10-01 재설계: **아래는 이전 범위/조사/구현 이력이다. 현재 제품·화면·도메인/API 방향·Task 순서는 [Windows 헬스케어 재설계](healthcare-windows-baseline.md)가 우선한다.** EMR·범용 Timeline/Trend 구현을 재개하지 않는다. 이 문서의 과거 완료 조건/추천/endpoint를 새 기능 계약으로 자동 채택하지 않는다.

# EMR MVP · RESTful API 연동 기준

2026-10-01 사용자 목표 변경 및 범위 선택 반영.
목표는 **WPF EMR 구현 + 실제 RESTful API 연동 테스트**다.
첫 흐름: **환자 조회 → 진료 이력 → 진료 기록 작성·저장 → 재조회 확인**.

이 문서는 기존 Healthcare Data Exploration/Medication 계획보다 우선한다.
기존 조사·시안·완료된 PR은 이력으로 보존한다. WPF/MVVM/수명·계층·코딩 규칙은 유지한다.
커스텀 Timeline/Trend, 대량 데이터 benchmark, 복약은 보류한다.
실제 100/150% DPI·혼합 모니터·High Contrast·물리 키보드·screen reader 미검증은 기록으로 남기되 다음 EMR 구현을 차단하지 않는다. 미검증 항목을 통과했다고 표시하지 않는다.

## MVP 사용자 흐름과 UI

합성 환자 여러 명을 조회하고 선택한다. 좌측 환자 검색/목록, 중앙 해당 환자의 진료 이력, 우측 선택 기록 상세 또는 편집을 사용한다.
환자 문맥(이름·합성 환자번호)을 이력/편집에 계속 표시한다. 기존 preview 화면을 환자 EMR 완성 화면으로 주장하지 않는다.

- 환자: 읽기만. 이름/환자번호 검색, 목록 paging, 환자 상세.
- 진료 이력: 해당 환자 기록만, 진료일 내림차순 + ID 순서, paging, 상세.
- 진료 기록: 진료일·주호소·평가·계획 입력, 저장, 저장된 내용 재조회 및 재실행 후 유지 확인.
- 서버 원본을 기준으로 UI가 성공을 표시한다. 입력 중에는 원본 DTO와 편집 buffer를 분리한다.
- 환자/화면 전환 중 미저장 편집은 버리기 확인을 거친다. 저장 중 중복 실행을 막는다.
- 필수 상태: Loading/Empty/Error/Retry, 입력 validation, 저장 중/성공/실패/결과 미확인, version conflict.

접수·처방·청구·검사 주문·진료 확정/서명·삭제·환자 등록은 이번 범위에 넣지 않는다.
데모는 합성 데이터와 loopback API를 사용한다. 실제 병원 EMR 제품과 네이버의 비공개 EMR 기능/계약을 동일시하지 않는다.

## 최소 모델 / 불변 규칙

Patient: 불변 ID, 합성 환자번호, 표시 이름. 환자 목록은 읽기 projection이다.
EncounterNote: ID, PatientId, visitDate(YYYY-MM-DD), chiefComplaint, assessment, plan, version.
첫 버전은 자유 텍스트 진료 노트다. 진단 코드 체계·의학적 판정·처방 계산을 추가하지 않는다.

- 환자 존재 확인, PatientId는 생성 후 변경 불가. 다른 환자의 URL로 조회/수정 시 404.
- visitDate와 chiefComplaint는 필수. 텍스트 길이는 chiefComplaint 500, assessment/plan 각 4000자.
- ID는 GUID, version은 양의 정수이며 저장 성공마다 증가한다. 생성 expectedVersion=0, 수정은 읽은 version 필수.
- UTC 저장 시각은 서버에서 정하고 진료일과 구분한다. Domain에 WPF/HTTP/DB/DTO를 넣지 않는다.
- API DTO, Domain, WPF 편집 buffer를 분리한다. VM은 Application port를 사용하고 HttpClient/DB에 직접 의존하지 않는다.
- 장기 서버 프레임워크를 만들지 않는다. API 저장은 SQLite를 우선 검토하고 서버 전용 provider를 공유 netstandard 계층에 강제하지 않는다. 선택 패키지/경계는 API Task에서 확정한다.

## REST v1 계약

UTF-8 JSON camelCase. 합성 seed는 재시작마다 기존 사용자 저장을 덮어쓰지 않는다.
DTO는 실제 구현 Task에서 명시하고 OpenAPI와 contract test를 함께 갱신한다.

EMR-101 구현 계약 보충: [EMR-101 API](emr-101-api.md)의 구체 DTO·오류·저장 설정과 OpenAPI 파일을 따른다. version 불일치 PUT 재시도는 같은 내용이라도 409이며 원본을 변경하지 않는다.

| Method / Path | 요청 / 결과 |
| --- | --- |
| GET /api/v1/health | 기존 readiness 유지 |
| GET /api/v1/patients | search, page(1부터), pageSize(1..100); items/totalCount/page/pageSize |
| GET /api/v1/patients/{patientId} | patient 상세, 없으면 404 |
| GET /api/v1/patients/{patientId}/encounters | page/pageSize; 진료 이력 projection과 paging metadata |
| GET /api/v1/patients/{patientId}/encounters/{id} | 진료 기록 상세와 version, 없거나 환자 불일치면 404 |
| PUT /api/v1/patients/{patientId}/encounters/{id} | expectedVersion + 편집 필드. 생성은 201+Location+저장 DTO, 수정은 200+저장 DTO |

PUT의 ID는 클라이언트가 편집 시작 시 한 번 생성해 저장 결과 확인까지 유지한다.
동시 요청의 version 검사는 저장 transaction 안에서 원자적으로 수행한다.
400 validation, 404 missing, 409 version conflict; 오류는 ProblemDetails와 안전한 field errors.
PUT는 해당 ID의 표현을 생성/갱신한다. 중복 기록을 생성하는 POST 재시도나 무조건 새 ID 발급을 하지 않는다.
저장 응답이 유실되면 결과 미확인으로 표시하고 같은 ID GET으로 확인한다. GET 실패/404만으로 저장 실패를 확정하지 않는다.
자동 재전송 대신 확인 후 최신 version과 편집 buffer를 비교해 명시적 재시도를 제공한다. 별도 operations/receipt/outbox 프레임워크는 만들지 않는다.
클라이언트 취소는 서버 쓰기 취소를 보장하지 않는다.

## 필수 연동 테스트

- 실제 HTTP 서버를 loopback에서 띄워 HttpClient로 JSON/status/header/validation 계약을 검증한다. 성공 여부와 무관하게 자신이 띄운 프로세스만 정리한다.
- 환자 검색/paging/404, 다른 환자 기록 접근 방지, 이력 정렬·paging·상세.
- 새 기록 PUT→201/Location→GET 동일 값/version, 수정 PUT→200/version 증가→GET.
- 잘못된 입력이 저장되지 않음, 오래된 version 409가 원본을 변경하지 않음.
- API 재시작 후 같은 저장 기록이 유지됨. 테스트별 임시 DB/독립 포트로 격리.
- WPF adapter→VM의 실제 응답 매핑, query cancellation/generation과 환자 전환 race 방어.
- 편집 buffer/미저장 전환, duplicate save 차단, error/retry/conflict/result-unknown 상태를 결정적인 테스트로 검증.
- 최종 WPF 사용자 시나리오: 환자 선택→기록 작성→저장→재조회→재실행 후 확인. mock-only 통과를 REST 연동 완료로 주장하지 않는다.

## 실행 / PR 계획

1. 목표 변경 PR: 이 문서·README·범위/Task 우선순위 반영. 실행 코드 없음.
2. EMR API Task: Domain/계약/저장/REST + 실제 HTTP/restart integration tests. 작은 Draft PR.
3. EMR 조회 Task: 환자·진료 이력·상세 WPF + 실제 API adapter/query 상태 테스트.
4. EMR 편집 Task: 입력/저장/재조회/미저장 전환/충돌·결과 미확인 + end-to-end 데모.

기존 HC-103 PR #27은 독립 리뷰 통과한 WPF 기반 후보다. 사용자 승인 없이 merge하지 않는다.
후속 WPF는 그 기반을 활용하되 기존 미완료 품질 matrix는 별도로 남긴다.
각 PR은 관련 Task, 정확한 head, 실제 검증, 제한을 보고하고 리뷰 댓글에 수정 결과를 회신한다.

## GitHub 작업

- [Epic #28](https://github.com/higwon/healthcare-emr-desktop/issues/28)
- [EMR-101 API #29](https://github.com/higwon/healthcare-emr-desktop/issues/29)
- [EMR-102 조회 UI #30](https://github.com/higwon/healthcare-emr-desktop/issues/30)
- [EMR-103 기록 편집 #31](https://github.com/higwon/healthcare-emr-desktop/issues/31)
