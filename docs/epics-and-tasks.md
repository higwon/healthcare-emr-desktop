> 2026-10-01 목표 변경: 현재 제품 범위·도메인·API·실행 우선순위는 [EMR MVP](emr-mvp.md)가 우선한다. 아래 탐색/복약 계획은 이전 기준이며 후속 구현을 바로 진행하지 않는다. 남은 Windows 품질 검증은 미검증 기록으로 보존하고 EMR 구현과 병행/후속 처리한다.

# Epic / Task 계획

2026-09-30 리뷰 반영. 구현 범위의 source of truth, 실제 진행 상태는 GitHub issue다.
기존 5 Epic/16 Task의 ID·URL을 보존하고 HC-104 조회 작업 하나만 추가했다. 현재 5 Epic/17 Task.
E01은 PR #22 병합으로 Done이다. HC-101 이후 각 Task의 진행·검증·병합 상태는 GitHub issue와 PR을 따른다. 생성/문서 작성만으로 Done이 아니다. Ready/Done은 development-workflow를 따른다.
HC 숫자는 실행 순서가 아니다. 첫 기능 단계는 HC-104 → HC-301 → HC-302이며 Medication보다 먼저다.

## 공통 UI 완료 조건

Task에 영향을 받는 품질 항목만 검증한다. 변경하지 않은 화면·Control의 이전 증거는 재사용하고 영향/회귀 근거를 PR에 남긴다.

| 종류 | 해당 Task | 완료 조건 |
| --- | --- | --- |
| MVVM / recovery 기반 | HC-102, HC-203 | command/state·취소/stale·구독/scope·실패/충돌/재시작 검증. 화면 변경 없는 PR에 DPI·대량 rendering 요구 없음 |
| UI Foundation | HC-103 | DPI 100/150/200%·최소/기본 창·focus·Automation·Binding 오류 |
| Data UI | HC-104 | UI Foundation + API paging/query/apply·대표 데이터 상태·detail 수명 |
| Custom Control | HC-301, HC-302 | UI Foundation + 직접 fixture benchmark·Measure/Arrange·invalidation·hit test·profiling |
| State Workflow UI | HC-204, HC-205 | 오류/retry/conflict·focus·관련 DPI·Binding·상태/명칭·닫기/재열기. Timeline benchmark 요구 없음 |
| 앱 전체 | HC-401 | 전체 DPI/Windows/접근성·대량 데이터·장시간 실행 matrix 통합 재검증 |
성능/메모리 자료는 [Case Study 기준](performance-case-studies.md)에 연결한다. HC-401은 전체 앱 재검증이며 최초 측정 단계가 아니다.
필수 미검증 상태를 Done으로 표시하지 않는다.

## HC-E01 설계·도메인·개발 운영 기준

목표: 자체 Healthcare Data Client 시나리오와 구조·계약·UI·코드·개발 운영 기준을 검토한다.
Epic 완료: 모든 자식 AC·실제 검증·관련 PR merge 확인.

### HC-001 저장소 운영·Epic/Task·PR 기준

- GitHub: [#6](https://github.com/higwon/healthcare-emr-desktop/issues/6)
- 선행: 없음
- 범위: README·AGENTS·workflow·이슈 템플릿·Epic/Task 매핑
- PR: Design baseline
- 검증: diff·문서 링크·이슈/PR 매핑 검사

수용 기준:

- [ ] 실제 GitHub Epic/Task와 문서 ID가 연결됨
- [ ] Ready/Done·브랜치·PR 분할·검증 규칙과 보류 초안 처리 명시
- [ ] 실행 코드가 설계 PR에 포함되지 않음

### HC-002 아키텍처·런타임·MVVM ADR

- GitHub: [#7](https://github.com/higwon/healthcare-emr-desktop/issues/7)
- 선행: HC-001
- 범위: 계층·참조·net48/netstandard2.0/net10 후보·패키지 비교
- PR: Design baseline
- 검증: Microsoft 문서·로컬 환경 조사; 실행 spike는 별도 검토

수용 기준:

- [ ] 호환성·Windows/CI targeting pack·패키지·테스트 runner의 검증 계획 기록
- [ ] Domain·Application·DTO·ViewModel 경계 정의
- [ ] ADR 제안/확정과 검증 결과를 구분

### HC-003 탐색·복약 도메인·API·실패 계약

- GitHub: [#8](https://github.com/higwon/healthcare-emr-desktop/issues/8)
- 선행: HC-002
- 범위: HealthEvent/detail/series projection·query context·paging·결측·Medication 불변식·제한된 결과 확인
- PR: Design baseline
- 검증: 계약 사례·불변식·경계값 리뷰

수용 기준:

- [ ] 공통 탐색 projection과 Feature domain을 분리하고 시각·단위·결측·cursor 경계 정의
- [ ] 조회 취소·stale 응답과 쓰기 conflict/OutcomeUnknown를 구분
- [ ] 최소 작업 결과·명시적 재시도 범위와 제외된 backend 복잡도를 기록

### HC-004 UI 상태·WPF 매핑·코딩 규칙

- GitHub: [#9](https://github.com/higwon/healthcare-emr-desktop/issues/9)
- 선행: HC-003
- 범위: Overview/Timeline/Trend UI 상태·Custom Control·토큰·수명·C#/XAML 규칙; 기존 복약 시안 참고
- PR: Design baseline
- 검증: 시안 증거·규칙/수용 기준 비교

수용 기준:

- [ ] 정상·빈·로딩·실패·stale·unknown·선택/키보드 상태 정의
- [ ] Task 종류별 DPI·Automation·Binding·데이터/Control 측정 기준 정의
- [ ] 시안과 실제 WPF 증거 구분·성능/메모리 Case Study 계획·Control 경계 정의

## HC-E02 WPF 기반·수명·디자인·진단

목표: 실행/CI·MVVM·화면 수명·shell·측정 경계를 먼저 구축한다.
Epic 완료: 모든 자식 AC·실제 검증·관련 PR merge 확인.

### HC-101 솔루션·프로젝트 참조·Windows CI

- GitHub: [#10](https://github.com/higwon/healthcare-emr-desktop/issues/10)
- 선행: HC-002, HC-003, HC-004
- 범위: 실행 기반과 test projects, CI artifact
- PR: Foundation PR
- 검증: clean checkout·Release CI·기본 실행 smoke

수용 기준:

- [ ] 계층 참조와 선택 TFM에 맞는 clean restore/Release build
- [ ] 최소 WPF 실행과 net48 테스트 경로 확인
- [ ] Windows build-and-test 및 TRX artifact; 업무 기능 미포함

### HC-102 MVVM·탐색·화면 수명·설정 기반

- GitHub: [#11](https://github.com/higwon/healthcare-emr-desktop/issues/11)
- 선행: HC-101
- 범위: composition root·분리 VM·command·scope·safe errors·demo options·진단 경계
- PR: MVVM PR
- 검증: command/state/lifetime 테스트·기본 shell 실행

수용 기준:

- [ ] ViewModel의 WPF View/HTTP/DB 직접 의존 없음
- [ ] 중복 실행·조회 취소·stale response·종료 후 callback 차단
- [ ] event/static event/timer/CollectionChanged 해제·초기화 guard·Demo 설정 검증
- [ ] 안전한 진단에 query/apply 타이밍·화면 scope 생존을 관찰할 경계 제공


### HC-103 탐색 UI shell·리소스·접근성

- GitHub: [#12](https://github.com/higwon/healthcare-emr-desktop/issues/12)
- 선행: HC-004, HC-102
- 범위: Overview/Timeline/Trend 탐색 host·리소스·목록/상세 패턴·포커스; 브라우저 복약 홈 재사용 승인 아님
- PR: UI foundation PR
- 검증: WPF smoke·UI 캡처·DPI 검증 범위 기록

수용 기준:

- [ ] 정해진 최소/기본 창에서 wrap·selection·scroll 검증
- [ ] Tab/Shift+Tab/Enter/Escape·초점 복귀·Automation 명칭
- [ ] 대표 WPF 캡처·Binding 오류 검사; 실제 업무 write 없음
- [ ] 100/150/200% DPI·최소/기본 창·keyboard/focus·Automation name/role/state·Binding 오류 확인

## HC-E03 Healthcare Data 탐색·Custom Controls

목표: Overview·Timeline·Trend와 합성 API async 상태·초기 Custom Control·실제 성능/수명 측정을 구현한다.
Epic 완료: 모든 자식 AC·실제 검증·관련 PR merge 확인.

### HC-104 Health Overview·Timeline 합성 API 조회

- GitHub: [#23](https://github.com/higwon/healthcare-emr-desktop/issues/23)
- 선행: HC-003, HC-103
- 범위: 고정 seed/version fixture·overview/events/detail API·paging/filter·VM query state·기본 탐색 화면
- PR: 조회 계약/API PR → Overview/Timeline 상태 UI PR
- 검증: API/VM contract·결정적 지연 race·WPF 탐색 증거·Release 측정

수용 기준:

- [ ] Overview→Timeline→상세 탐색, 기간/타입/subject context·cursor ordering 계약 검증
- [ ] Loading/Empty/Error/Stale·retry·취소·지연 순서 역전 시 선택/화면 보호
- [ ] 100/1,000/10,000건 서버 fixture에서 pageSize≤100 실제 paging·페이지 단위 query/UI apply baseline·상세 scope 관찰. 100페이지 누적은 필수 아님
- [ ] HC-301과 같은 첫 기능 단계에 자체 Control을 완성하며 기본 목록만으로 E03 종료하지 않음
- [ ] 100/150/200% DPI·최소/기본 창·keyboard/focus·Automation·Binding 오류·대표 상태 캡처
- [ ] API pageSize≤100 통합 query/UI apply 측정·detail 반복 탐색 수명 확인; Control benchmark와 별도

### HC-301 Health Timeline Custom Control·초기 성능

- GitHub: [#18](https://github.com/higwon/healthcare-emr-desktop/issues/18)
- 선행: HC-104
- 범위: templated Timeline Control·grouping·selection·기본 recycling 측정·layout/invalidation
- PR: Timeline Control PR → 측정에서 발견된 문제의 개선 PR
- 검증: Control STA·UI Automation·layout/render profiler·재현 script/fixture

수용 기준:

- [ ] DP·템플릿·Measure/Arrange·선택/focus·hit testing·AutomationPeer 제공
- [ ] query pagination과 UI 가상화 분리·실제 container recycling 동작 검증
- [ ] Control benchmark는 API 없이 1,000/10,000 synthetic items 직접 공급·scroll/layout/render·원자료/가설 기록
- [ ] 외부 UI library 없음·첫 기능 단계에서 실제 자체 Control 통합
- [ ] 100/150/200% DPI·최소/기본 창·keyboard/focus·Automation·Binding 오류
- [ ] API 없이 1,000/10,000 fixture를 Control에 직접 공급하여 scroll/layout/render·container recycling 측정

### HC-302 Health Trend Custom Control·측정 시각화

- GitHub: [#19](https://github.com/higwon/healthcare-emr-desktop/issues/19)
- 선행: HC-301
- 범위: HealthTrendControl drawing·지표/기간·unit/missing/reference range·표 대안
- PR: series API/VM PR → Trend Control PR → 측정 기반 개선 PR
- 검증: series contract·Control STA·UI Automation·DPI·profiler 원자료

수용 기준:

- [ ] DP·Measure/Arrange·resize·hover·hit testing·keyboard selection·AutomationPeer 제공
- [ ] 결측 gap·단위·점별 합성 참고 구간·원본 표·의료 판정 없음
- [ ] 최대 10,000점 rendering/layout·선택·invalidation baseline, downsampling은 측정 후 결정
- [ ] Timeline/Trend Performance Investigation의 가설·측정·결론·한계 누적, 발견된 병목은 최적화
- [ ] 100/150/200% DPI·최소/기본 창·keyboard/focus·Automation·Binding 오류
- [ ] 직접 공급한 1,000/10,000 point fixture의 layout/render·hover/selection·invalidation profiling

## HC-E04 Medication 상태 변경·실패 복구

목표: 후속 Feature로 복약 등록·일정·기록·종료와 최소 서버 기반 실패/충돌 UX를 검증한다.
Epic 완료: 모든 자식 AC·실제 검증·관련 PR merge 확인.

### HC-201 일정·기록·종료 도메인 구현

- GitHub: [#13](https://github.com/higwon/healthcare-emr-desktop/issues/13)
- 선행: HC-003, HC-302
- 범위: 불변 모델·clock·요일/복수 시각·기간·schedule version
- PR: Domain PR
- 검증: 결정적 Domain 테스트·표 기반 사례

수용 기준:

- [ ] 한국 자정·윤일·경계일·요일·중복 시각 검증
- [ ] 미래 체크 차단·종료 뒤 과거 유지·다음 날 수정 적용
- [ ] Domain에 WPF/HTTP/DB 없음

### HC-202 영속 저장·API·작업 조회

- GitHub: [#14](https://github.com/higwon/healthcare-emr-desktop/issues/14)
- 선행: HC-201
- 범위: SQLite·transaction·DTO mapping·복약 endpoints·최소 작업 결과 조회
- PR: Persistence/API PR
- 검증: 실제 임시 DB·API 통합·serialization·restart 테스트

수용 기준:

- [ ] plan/record/history/최소 작업 결과가 같은 transaction에서 commit
- [ ] 같은 작업 재요청·정규화 입력 충돌·expected version 검증
- [ ] 재시작 보존·ProblemDetails·operation 조회; 범용 receipt/hash framework 없음

### HC-203 미확인 결과·재시도·충돌 복구

- GitHub: [#15](https://github.com/higwon/healthcare-emr-desktop/issues/15)
- 선행: HC-202, HC-102
- 범위: immutable command snapshot·작업 조회·명시적 재시도·재시작 미확인 상태 확인
- PR: Recovery PR
- 검증: fault adapter·API/VM 상태·재시작 작업 조회 검증

수용 기준:

- [ ] 저장 전 실패와 commit 후 응답 유실을 UI 상태에서 구분
- [ ] 명시적 retry/restart 조회는 같은 ID를 유지·자동 replay/offline queue 없음
- [ ] 409 자동 덮어쓰기 없음·unknown 중 동일 자원 write 차단


### HC-204 WPF 직접 등록·일정·저장 흐름

- GitHub: [#16](https://github.com/higwon/healthcare-emr-desktop/issues/16)
- 선행: HC-103, HC-203
- 범위: 이름/종류→기간/요일/복수 시각→확인→API 저장
- PR: Registration PR
- 검증: ViewModel/API 통합·WPF 정상/오류/재시도 캡처

수용 기준:

- [ ] 모든 입력 검증·safe message·저장 중 상호 배제
- [ ] 실패/응답 유실에도 초안 유지·명시적 재시도
- [ ] 서버 확인 후 등록 반영·닫기/이전/초점·긴 이름 검증
- [ ] 오류/retry/conflict·focus·관련 화면 DPI 100/150/200%·Binding·Automation 상태·닫기/재열기 검증

### HC-205 날짜별 체크·취소·종료 및 통합 데모

- GitHub: [#17](https://github.com/higwon/healthcare-emr-desktop/issues/17)
- 선행: HC-204
- 범위: 날짜 조회·선택 상세·기록 변경·종료 확인·flow evidence
- PR: Intake journey PR
- 검증: Domain/API/VM 통합·실제 WPF journey·저장소 재시작

수용 기준:

- [ ] 미래/종료 항목 쓰기 차단·과거 조회/수정 정책 준수
- [ ] 선택/날짜 변경 stale response 차단·동시 변경 처리
- [ ] 등록→조회→체크/취소→종료→restart 데모 재현
- [ ] 오류/retry/conflict·focus·관련 화면 DPI 100/150/200%·Binding·Automation 상태·닫기/재열기 검증

## HC-E05 Windows 품질 재검증·배포·포트폴리오

목표: 초기부터 누적한 증거를 앱 전체에서 재검증하고 Performance Investigation 2~3건·Memory Case Study 1건·설치/재현 자료로 정리한다.
Epic 완료: 모든 자식 AC·실제 검증·관련 PR merge 확인.

### HC-401 앱 전체 Windows 품질 재검증·Case Study

- GitHub: [#20](https://github.com/higwon/healthcare-emr-desktop/issues/20)
- 선행: HC-205, HC-301, HC-302
- 범위: 각 UI PR 증거를 앱 전체에서 재검증·장시간 탐색·Memory/Performance Case Study 통합
- PR: Quality PR; 발견 문제는 작게 분할
- 검증: Windows matrix·screenreader smoke·CPU/layout/render·memory profiler 재현

수용 기준:

- [ ] 각 UI Task에서 누적한 DPI/keyboard/Automation·Binding·대량 데이터 측정을 앱 전체에서 재검증
- [ ] Performance Investigation 2~3건: 가설·측정·결론·한계·원자료. 병목 확인 시 Root Cause→Solution→Before/After, 미확인도 조사 결과로 기록
- [ ] 실제 profiler Memory Case Study 1건: 반복 상세 열기/닫기·생존 VM/구독/timer/callback root 확인
- [ ] 지원 환경·Release·seed·측정법·미해결 결과 기록; 가짜 숫자/의도적 결함 없음
- [ ] 전체 DPI 100/150/200%·창/모니터·keyboard/Automation·Binding·대량 데이터·장시간 수명 matrix 재검증

### HC-402 설치·진단·재시작 복구·포트폴리오

- GitHub: [#21](https://github.com/higwon/healthcare-emr-desktop/issues/21)
- 선행: HC-401
- 범위: 설치 선행조건·안전한 진단·배포 artifact·clean-machine demo
- PR: Packaging PR 후 Portfolio evidence PR
- 검증: 깨끗한 Windows 설치·재시작·정상/실패 데모

수용 기준:

- [ ] 설치/업데이트 실패·설정/미확인 작업 보존·복구 확인
- [ ] 로그 allowlist·합성 데이터·Demo 제한 표시
- [ ] README에 실제 성능/메모리 결과·PR/원자료·재현 가이드 연결

## 보류·제외 범위

HC-301은 기존 체성분 후속 Task에서 초기 Timeline Control로, HC-302는 증상 입력 Task에서 초기 Trend Control로 조정했다. 기존 구현/완료 증거는 없으며 삭제/완료 처리하지 않았다.
Body Measurement·Symptom은 Timeline의 합성 event/detail로 포함하되 전용 입력·질문 engine은 후속 범위다.
실제 기관/네이버/인바디 인증·OCR·의료 판정·처방·병원/가족 공유·운영 보안·분산 서버·offline outbox·자동 replay는 별도 ADR/Task 없이 추가하지 않는다.
