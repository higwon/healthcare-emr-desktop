> 2026-10-01: 계층/참조/수명 규칙은 유지한다. Feature 우선순위와 EMR 모델·API는 [EMR MVP](emr-mvp.md)를 따른다. 아래 탐색/복약 Feature 계획은 이전 범위다.

# 아키텍처 기준

상태: 설계 PR #22 병합. HC-101에서 아래 프로젝트/참조를 구성하며 업무 코드 없이 실행 기반만 검증한다.

| 프로젝트 | 책임 | 참조 |
| --- | --- | --- |
| HealthNote.Domain | 지표·단위·시각의 값 객체, Feature별 불변 규칙(복약 포함) | 없음 |
| HealthNote.Application | 유스케이스, 저장/조회·시계·작업 상태 포트 | Domain |
| HealthNote.Infrastructure | HTTP client, DTO 매핑, 영속성·캐시·진단 adapter | Application, Domain |
| HealthNote.Desktop | WPF View·ViewModel·Control, 앱 수명·DI 구성 | Application, Domain, Infrastructure |
| HealthNote.Api | endpoint·DTO·검증·DI, 서버 트랜잭션 경계 | Application, Domain, Infrastructure |

Domain/Application은 netstandard2.0 후보, Desktop은 net48 후보, API는 net10.0 후보다.
API 전용 저장 어댑터가 공용 타깃과 호환되지 않으면 Server.Infrastructure로 분리하고 ADR을 갱신한다. 최신 서버 패키지를 공용 netstandard2.0 프로젝트에 억지로 넣지 않는다.
서버 엔티티를 그대로 JSON이나 Binding에 노출하지 않는다. 계약 DTO는 endpoint/client 경계에서 매핑한다.
초기에는 별도 공용 Contracts assembly 없이 DTO 호환성을 계약 테스트로 확인한다. 중복 유지 비용이 생길 때 변경 근거를 남긴다.

## 클라이언트 구성

Feature는 Overview / Timeline / Trend / Medication으로 나눈다.
ShellViewModel: 탐색·화면 수명. HealthOverviewViewModel: 요약·관련 탐색.
HealthTimelineViewModel: 기간/종류 필터·페이지·선택 상세. HealthTrendViewModel: 지표·기간·값/단위·선택.
MedicationSchedule/Registration/DetailViewModel은 후속 상태 변경 slice다.
HealthEvent는 여러 상세 모델을 모아 읽는 공통 projection이며 모든 도메인을 담는 거대 entity가 아니다.
HealthEventDetail과 MeasurementSeries는 조회 계약으로 분리한다. Overview도 선택 subject·query context에 묶인다.
IHealthDataQuery, IMedicationQuery/Commands, IOperationQuery, IClock, IUiDispatcher, IDiagnostics를 필요한 Task에서만 추가한다.
모든 화면과 업무를 하나의 MainViewModel에 모으지 않는다.
DI는 실행 composition root에 둔다. static service locator·도메인의 컨테이너 참조를 금지한다.

## 수명과 동시성

화면 scope가 조회 토큰·선택 요청을 소유하고 교체/종료 때 취소·해제한다.
조회는 CancellationToken과 generation 비교를 함께 사용한다. 낡은 날짜/선택 응답이 현재 화면을 덮지 않게 한다.
쓰기는 작업 ID를 가진 immutable command snapshot으로 추적한다. UI 취소는 서버 롤백을 의미하지 않는다.
재시도는 원래 payload·작업 ID를 유지한다. 새 변경은 새 작업이다. 결과 미확인 중 동일 자원 쓰기를 막는다.
UI collection은 dispatcher에서 변경한다. Domain/Application은 Dispatcher·CommandManager를 참조하지 않는다.

## 저장·환경

첫 탐색 slice는 seed/version이 고정된 합성 fixture 조회 API다. cursor paging·기간/종류 필터·bounded trend query를 사용하며 DB가 선행 조건이 아니다.
Timeline/Trend Custom Control을 이 단계에서 구현·측정한다. 서버 조회와 UI 가상화·rendering 경계를 분리한다.
복약 단계의 저장소 후보는 SQLite transaction이다. 계획·기록·최소 작업 결과는 같은 transaction에서 commit한다.
작업 결과는 API 저장 adapter의 기술 데이터이며 공통 Domain entity가 아니다. 재시작 때 미확인 ID 조회를 제공하되 자동 replay queue는 만들지 않는다.
canonical 의미값 동등 비교만 계약에 필요하다. cryptographic hash·receipt framework·분산 실행·offline outbox는 제외한다.
JSON 전체 파일 저장은 계약 초안 검토용 spike이고 운영 저장 방식으로 채택하지 않는다.
Production 설정·인증은 현재 구현 대상이 아니다. Demo를 명시적으로 선택하고 loopback endpoint·합성 데이터 표시를 사용한다.
개인 데이터가 있는 운영 기능을 추가하려면 인증·권한·암호화·보존·삭제·백업 정책 ADR부터 작성한다.

## 테스트 경계

Domain: 날짜/요일/시각/종료 불변식. Application: clock·저장·작업 상태 포트.
Infrastructure/API: paging·단위/결측 serialization·HTTP delay/stale/error, 복약 transaction·fault/restart.
Desktop: query context·selection·command 상호 배제·stale response·dispose, Custom Control STA/layout/render/keyboard/Automation 검증.
초기 diagnostics는 query 시간·layout/render 관찰·반복 탐색 생존 객체의 측정 경계만 제공하며 의료 본문은 수집하지 않는다.
실제 WPF는 STA·실행·UI Automation·시각 검증으로 따로 확인한다. net10 테스트 성공만으로 net48 클라이언트 호환성을 입증하지 않는다.
