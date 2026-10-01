> 2026-10-01 재설계: **아래는 이전 범위/조사/구현 이력이다. 현재 제품·화면·도메인/API 방향·Task 순서는 [Windows 헬스케어 재설계](healthcare-windows-baseline.md)가 우선한다.** EMR·범용 Timeline/Trend 구현을 재개하지 않는다. 이 문서의 과거 완료 조건/추천/endpoint를 새 기능 계약으로 자동 채택하지 않는다.

# EMR-101 API 구현 계약

Task #29, 설계 PR #32에 이어지는 API slice. WPF 기능 변경 없음.

- 서버 SQLite는 Microsoft.Data.Sqlite 10.0.12를 고정하고 API/Persistence에 둔다. 공용 netstandard 계층에는 provider 의존 없음. EF/범용 repository/receipt framework 없음.
- API는 `--Emr:Port=5078` (기본), `--Emr:DatabasePath=<path>` (기본 `.local/emr/healthnote.db`)로 실행한다. 항상 127.0.0.1에만 bind한다. 테스트는 OS가 배정하는 port=0과 별도 임시 DB를 사용한다.
- 환자 seed는 세 명의 합성 환자이며 INSERT OR IGNORE로 보존한다. 진료 기록 seed 없음; 첫 이력은 빈 목록이다. schema v1이며 다른 user_version은 시작 실패한다.
- page 기본 1, pageSize 기본 20, 허용 page≥1/pageSize 1..100. 정수 overflow 없는 offset 계산. 환자 검색은 trim한 100자 이내 이름/환자번호 literal 부분 일치; `%`/`_`는 wildcard로 해석하지 않는다. 이름 순 Unicode/DB collation은 한국어 검색 사전 정렬을 의미하지 않는다. 환자번호 ASC, 이력 visitDate DESC/id ASC로 고정한다.
- Patient DTO: id/patientNumber/displayName. 이력 DTO: id/patientId/visitDate/chiefComplaint/version. 상세는 assessment/plan/savedAtUtc를 추가한다. 목록 응답은 items/totalCount/page/pageSize.
- PUT DTO: expectedVersion(int≥0, 최대 int.MaxValue−1), visitDate(정확 YYYY-MM-DD), chiefComplaint(필수≤500), assessment/plan(선택≤4000, null은 빈 문자열). ID 빈 GUID 금지. 입력 텍스트는 앞뒤 trim 후 Domain 길이 검사한다. 예상 version 누락 및 알 수 없는 필드는 400이다.
- DateTime UTC 저장 시각은 API가 정하며 Domain에 clock/HTTP/DB를 넣지 않는다. 진료일은 시간대 없는 날짜다.
- immediate SQLite transaction으로 환자/기록 소유자/version 확인과 INSERT/UPDATE를 직렬화한다. 기존 다른 환자의 ID는 404. 생성 expectedVersion=0이면 201/Location/version1, 수정 일치 시 200/version+1. 없는 기록 expectedVersion>0은 409. 같은 ID 중복 PUT도 version 불일치 409; 재시도는 먼저 같은 ID GET으로 확인한다.
- 응답은 camelCase JSON. 오류는 application/problem+json: type/title/status/code/traceId, validation은 errors dictionary. route/query/body 오류도 동일 형식. SQL busy/storage failure는 안전한 503, stacktrace/payload는 응답·로그에 넣지 않는다.
- OpenAPI는 버전 관리 정적 계약 파일이다. 별도 런타임 OpenAPI 패키지는 추가하지 않는다. 실제 HTTP integration test에서 경로·status·주요 응답 계약을 확인한다.
- API 재시작은 동일 DB 경로를 사용한다. readiness는 DB 초기화가 끝난 뒤 응답한다. 기존 smoke는 임시 DB로 격리한다.

## 검증 계획

별도 net10 MSTest 프로젝트에서 실제 dotnet API 프로세스/Kestrel + HttpClient + 임시 SQLite를 사용한다. 포트=0의 실제 listening 주소를 startup 로그에서 읽는다. 자신이 생성한 프로세스만 finally에서 종료한다.
환자 검색/paging/404, 생성·수정·GET, validation no-write, 다른 환자 접근 방지, 409 no-write, 동시 version 충돌, 이력 paging/order, API 재시작 후 저장 유지를 검증한다. 로그/DB는 합성 자료만 포함한다.
기존 net48 regression, 참조 경계, locked restore, Release, API smoke/publish/CI artifacts를 유지한다. 실제 실행 결과와 제한은 검증 후 추가한다.

## 실제 로컬 결과

2026-10-01 Windows 11 build 26200 / SDK 10.0.300 / Release.
참조 경계·locked restore 통과, Release 0 warnings/errors.
net10 Domain/실제 Kestrel HTTP integration: 7 passed / 0 failed / 0 skipped.
기존 main 기반 net48 regression: 34 passed / 0 failed / 0 skipped.
API readiness smoke와 publish 통과. PR #27의 미병합 UI 테스트 49개를 이 브랜치에 포함했다고 주장하지 않는다.

코드 기반은 목표 변경 PR #32이며 구현 PR은 그 브랜치를 base로 삼는 stacked PR이다. #32/#27 모두 사용자 승인 없이 merge하지 않는다.
정확한 head·Windows CI 결과는 해당 구현 PR의 Checks와 리뷰 회신을 따른다.

## 실행과 계약

```powershell
dotnet src/HealthNote.Api/bin/Release/net10.0/HealthNote.Api.dll --Emr:Port=5078 --Emr:DatabasePath=.local/emr/healthnote.db
dotnet test tests/HealthNote.Api.Tests/HealthNote.Api.Tests.csproj -c Release --no-build --logger "trx;LogFileName=emr-api.trx" --results-directory artifacts/test-results
```

[OpenAPI v1](contracts/emr-v1.openapi.json)은 실제 구현된 여섯 operation(GET readiness 포함)의 정적 계약이다. Swagger UI/런타임 endpoint가 아니다.
SQLite API는 작은 서버 요청에서 동기 실행하며 가짜 async/Task.Run 계층을 추가하지 않았다. 쓰기 lock이 10초 내 확보되지 않으면 503으로 반환한다. 대량 데이터 성능·멀티 서버·migration framework는 이번 검증 대상이 아니다.
실제 WPF 환자/이력 연동은 EMR-102, 편집/저장/결과 미확인 UI는 EMR-103이다. 이 PR은 의료 클라이언트 UI 완성이 아니다.

provider 선택과 즉시 transaction 근거: [Microsoft.Data.Sqlite 10.0.12](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.12), [Microsoft transaction 문서](https://learn.microsoft.com/dotnet/standard/data/sqlite/transactions).
