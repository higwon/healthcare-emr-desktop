# Epic / Task 계획

2026-09-30. 구현 scope의 source of truth. 실제 진행 상태는 GitHub issue에서 관리한다.
범위 변경은 이 문서와 issue AC를 함께 갱신한다. 이전 EMR/MED/WB ID 초안은 보류하며 새 작업 ID는 HC-XXX로 통일한다.
현재는 E01 설계 문서 초안과 시안이 있고 리뷰 전이다. E02 이후 실행 작업은 모두 Backlog다.
작업을 생성했다는 이유로 Done 표시하지 않는다. Ready/Done·PR 순서는 development-workflow를 따른다.

## HC-E01 설계·도메인·개발 운영 기준

목표: 구현 전에 제품 범위·계층·계약·UI·코드·GitHub 운영 기준을 검토한다.
Epic 완료: 모든 자식 Task 수용 기준·검증·관련 PR merge 확인.

### HC-001 저장소 운영·Epic/Task·PR 기준

- 선행: 없음
- 범위: README·AGENTS·workflow·이슈 템플릿·Epic/Task 매핑
- PR: Design baseline
- 검증: diff·문서 링크·이슈/PR 매핑 검사

수용 기준:

- [ ] 실제 GitHub Epic/Task와 문서 ID가 연결됨
- [ ] Ready/Done·브랜치·PR 분할·검증 규칙과 보류 초안 처리 명시
- [ ] 실행 코드가 설계 PR에 포함되지 않음

### HC-002 아키텍처·런타임·MVVM ADR

- 선행: HC-001
- 범위: 계층·참조·net48/netstandard2.0/net10 후보·패키지 비교
- PR: Design baseline
- 검증: Microsoft 문서·로컬 환경 조사; 실행 spike는 별도 검토

수용 기준:

- [ ] 호환성·Windows/CI targeting pack·패키지·테스트 runner의 검증 계획 기록
- [ ] Domain·Application·DTO·ViewModel 경계 정의
- [ ] ADR 제안/확정과 검증 결과를 구분

### HC-003 복약 도메인·API·복구 계약

- 선행: HC-002
- 범위: 날짜·요일·시각·종료·버전·멱등성·ProblemDetails
- PR: Design baseline
- 검증: 계약 사례·불변식·경계값 리뷰

수용 기준:

- [ ] 계획·occurrence·기록·receipt·출처와 시간대 정의
- [ ] 수정 후 과거 보존·종료 경쟁·미확인 결과·restart 정책 명시
- [ ] API DTO·에러·작업 조회·transaction 검증 시나리오 존재

### HC-004 UI 상태·WPF 매핑·코딩 규칙

- 선행: HC-003
- 범위: 홈/복약 시안·토큰·state machine·수명·C#/XAML 규칙
- PR: Design baseline
- 검증: 시안 증거·규칙/수용 기준 비교

수용 기준:

- [ ] 정상·빈·로딩·실패·unknown·긴 이름·키보드 상태 정의
- [ ] 시안 구현 범위와 WPF DPI 미검증을 구분
- [ ] View/ViewModel/Control 경계와 focus/dispose 규칙 명시

## HC-E02 WPF 기반·MVVM·디자인 시스템

목표: 업무 기능과 분리된 실행·CI·수명·테마 기반을 구축한다.
Epic 완료: 모든 자식 Task 수용 기준·검증·관련 PR merge 확인.

### HC-101 솔루션·프로젝트 참조·Windows CI

- 선행: HC-002, HC-003, HC-004
- 범위: 실행 기반과 test projects, CI artifact
- PR: Foundation PR
- 검증: clean checkout·Release CI·기본 실행 smoke

수용 기준:

- [ ] 계층 참조와 선택 TFM에 맞는 clean restore/Release build
- [ ] 최소 WPF 실행과 net48 테스트 경로 확인
- [ ] Windows build-and-test 및 TRX artifact; 업무 기능 미포함

### HC-102 MVVM·탐색·화면 수명·설정 기반

- 선행: HC-101
- 범위: composition root·분리 VM·command·scope·safe errors·demo options
- PR: MVVM PR
- 검증: command/state/lifetime 테스트·기본 shell 실행

수용 기준:

- [ ] ViewModel의 WPF View/HTTP/DB 직접 의존 없음
- [ ] 중복 실행·조회 취소·stale response·종료 후 callback 차단
- [ ] 구독 해제·초기화 guard·Demo 명시 설정 테스트

### HC-103 승인된 UI shell·리소스·접근성

- 선행: HC-004, HC-102
- 범위: 리소스 토큰·화면 분리·목록/상세·입력/포커스
- PR: UI foundation PR
- 검증: WPF smoke·UI 캡처·DPI 검증 범위 기록

수용 기준:

- [ ] 정해진 최소/기본 창에서 wrap·selection·scroll 검증
- [ ] Tab/Shift+Tab/Enter/Escape·초점 복귀·Automation 명칭
- [ ] 대표 WPF 캡처·Binding 오류 검사; 실제 업무 write 없음

## HC-E03 복약관리 수직 구현과 복구

목표: 직접 등록부터 일정·복용 기록·종료를 영속 API와 WPF에서 완성한다.
Epic 완료: 모든 자식 Task 수용 기준·검증·관련 PR merge 확인.

### HC-201 일정·기록·종료 도메인 구현

- 선행: HC-003, HC-101
- 범위: 불변 모델·clock·요일/복수 시각·기간·schedule version
- PR: Domain PR
- 검증: 결정적 Domain 테스트·표 기반 사례

수용 기준:

- [ ] 한국 자정·윤일·경계일·요일·중복 시각 검증
- [ ] 미래 체크 차단·종료 뒤 과거 유지·다음 날 수정 적용
- [ ] Domain에 WPF/HTTP/DB 없음

### HC-202 영속 저장·API·작업 조회

- 선행: HC-201
- 범위: SQLite·transaction·DTO mapping·v1 endpoints
- PR: Persistence/API PR
- 검증: 실제 임시 DB·API 통합·serialization·restart 테스트

수용 기준:

- [ ] plan/record/history/receipt가 같은 transaction에서 commit
- [ ] 같은 작업 재요청·payload 충돌·expected version 검증
- [ ] 재시작 데이터 보존·ProblemDetails·operation 조회 계약 확인

### HC-203 미확인 결과·재시도·충돌 복구

- 선행: HC-202, HC-102
- 범위: 클라이언트 command snapshot·operation query·재시작 복구
- PR: Recovery PR
- 검증: fault adapter·HTTP/DB·ViewModel 상태 테스트

수용 기준:

- [ ] 저장 전 실패와 commit 후 응답 유실이 구분됨
- [ ] 재시도·restart가 같은 ID를 유지해 effect 중복 없음
- [ ] 409 자동 덮어쓰기 없음·stale query/unknown 중 write 정책

### HC-204 WPF 직접 등록·일정·저장 흐름

- 선행: HC-103, HC-203
- 범위: 이름/종류→기간/요일/복수 시각→확인→API 저장
- PR: Registration PR
- 검증: ViewModel/API 통합·WPF 정상/오류/재시도 캡처

수용 기준:

- [ ] 모든 입력 검증·safe message·저장 중 상호 배제
- [ ] 실패/응답 유실에도 초안 유지·명시적 재시도
- [ ] 서버 확인 후 등록 반영·닫기/이전/초점·긴 이름 검증

### HC-205 날짜별 체크·취소·종료 및 통합 데모

- 선행: HC-204
- 범위: 날짜 조회·선택 상세·기록 변경·종료 확인·flow evidence
- PR: Intake journey PR
- 검증: Domain/API/VM 통합·실제 WPF journey·저장소 재시작

수용 기준:

- [ ] 미래/종료 항목 쓰기 차단·과거 조회/수정 정책 준수
- [ ] 선택/날짜 변경 stale response 차단·동시 변경 처리
- [ ] 등록→조회→체크/취소→종료→restart 데모 재현

## HC-E04 체성분 추이·증상 기록 확장

목표: 검증된 기능과 합성 데이터 계약을 바탕으로 조회·추이·기록 UI를 확장한다.
Epic 완료: 모든 자식 Task 수용 기준·검증·관련 PR merge 확인.

### HC-301 체성분 데이터 계약·추이 Custom Control

- 선행: HC-205
- 범위: 합성 측정·기기/출처·단위·결측·기간·표/차트
- PR: Body data PR 후 Trend control PR로 분할
- 검증: contract·control/STA·시각·대량 데이터 측정

수용 기준:

- [ ] 조회 DTO와 값/단위/결측 이유·기기별 누락 정의
- [ ] 키보드 선택·AutomationPeer·크기/DPI·표 대안 제공
- [ ] 임의 건강 점수/AI 판정 없음·기본 가상화 성능 측정

### HC-302 증상 질문·기록 UI 범위 검증과 구현

- 선행: HC-205
- 범위: 확인된 질문 흐름·합성 기록·조회·draft restoration
- PR: Symptom design PR 후 Record UI PR
- 검증: 범위 리뷰→계약/VM 테스트→WPF 캡처

수용 기준:

- [ ] 실제 도움말/내부 화면 근거와 자체 질문 시나리오 구분
- [ ] 의료 판단 엔진 없이 기록·탐색·빈/오류 상태 제공
- [ ] 증상 개인정보 없는 로그·입력 초안/취소/수명 검증

## HC-E05 Windows 품질·배포·포트폴리오

목표: DPI·접근성·성능·설치·복구와 재현 가능한 증거를 확보한다.
Epic 완료: 모든 자식 Task 수용 기준·검증·관련 PR merge 확인.

### HC-401 DPI·접근성·성능·수명 검증

- 선행: HC-205, HC-301, HC-302
- 범위: 100/150/200% DPI·monitor·keyboard·Automation·large data
- PR: Quality PR; 발견 문제는 작게 분할
- 검증: Windows matrix·screenreader smoke·성능 baseline

수용 기준:

- [ ] 지원 환경·장비·Release·데이터·측정법 기록
- [ ] 모니터 이동·긴 이름·탭순서·읽기·Binding 오류 검증
- [ ] 기본 가상화·지연·반복 탐색 수명 측정; 결과 근거로 수정

### HC-402 설치·진단·재시작 복구·포트폴리오

- 선행: HC-401
- 범위: 설치 선행조건·안전한 진단·배포 artifact·clean-machine demo
- PR: Packaging PR 후 Portfolio evidence PR
- 검증: 깨끗한 Windows 설치·재시작·정상/실패 데모

수용 기준:

- [ ] 설치/업데이트 실패·설정/미확인 작업 보존·복구 확인
- [ ] 로그 allowlist·합성 데이터·Demo 제한 표시
- [ ] 이슈/PR/테스트/스크린샷/ADR을 연결한 재현 가이드

## 공통 제외 범위

실제 네이버/인바디 인증·의약품 API·OCR·AI 진단·처방·병원/가족 공유·운영 보안 인증은 별도 ADR/Task 없이 추가하지 않는다.
실제 사용자 정보·기관 API를 연결하는 일과 합성 데이터 포트폴리오 구현을 구분한다.
