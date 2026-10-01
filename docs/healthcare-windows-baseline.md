# 네이버 헬스케어 웹 기반 Windows 앱 · 재설계 기준

2026-10-01 사용자 지시: 네이버 헬스케어 웹 이미지 및 기능을 기반으로 Windows 앱을 만든다.
상태: **전면 재검토 제안 / 설계 리뷰 대상**. 실행 코드 변경·기능 구현 완료를 의미하지 않는다.
설계 Task: [CON-001 #35](https://github.com/higwon/healthcare-emr-desktop/issues/35).
현재 제품 범위·화면·도메인·API 방향·개발 순서는 이 문서를 따른다.
EMR MVP와 Overview/Timeline/Trend 중심 계획을 대체하며 이전 문서는 이력으로만 보존한다.

## 1. 제품 목표와 기준의 우선순위

개인이 자신의 건강 정보를 확인하고 복약과 증상 기록을 관리하는 WPF Windows 앱이다.
첫 실행은 **헬스케어 홈**이다. 홈에서 바디리포트·복약관리·증상체크로 이동한다.
환자 목록·의료진 진료 기록·처방·청구 화면은 제품 범위에서 제외한다. 홈 안의 병원 예약은 외부 서비스 연결이며 진료 업무가 아니다.
지원 직무는 EMR Windows Client 개발이지만, 그 이름에서 비공개 병원 업무를 추정해 제품 요구를 추가하지 않는다.
포트폴리오는 소비자 건강 서비스의 WPF 구현, REST 연동, 상태 관리와 Windows 품질을 증명한다.

| 근거 | 채택하는 내용 | 채택하지 않는 추정 |
| --- | --- | --- |
| 사용자 제공 홈 이미지 2장 | 기능 바로가기, 두 열 카드, 정보 위계, 비연동·복약 없음·예약 없음, 예정 기능 표시 | 로그인 후 상세 UI와 서버 계약 |
| 이번에 다시 읽은 공식 복약 도움말 | 약/영양제 등록 방식, 시간 알림, 사용자의 복용 여부 기록, 정보 확인 | 등록 폼의 모든 필드, 수정·중복·날짜 처리 정책 |
| 기존 바디리포트 조사 | 날짜별 측정·지표·연동 상태 설계의 참고 | 이번에 상세 도움말까지 재검증했다는 주장 |
| 홈 증상체크 카드·기존 조사 | 증상 진입, 인기 증상, MY증상기록 | 의료 질문 분기·질병 판정 알고리즘 |
| 자체 설계 | Windows 탐색·입력·실패 복구·자체 API | NAVER/InBody 공식 API와 동등하다는 주장 |

자료 충돌 시 최신 사용자 지시 → 확인한 이미지/공식 기능 → 자체 정책 순서다.
상세 화면을 확인하지 못한 항목은 자체 설계로 표시한다. 이전 브라우저 시안은 재사용 후보이지 새 UI 승인 증거가 아니다.

## 2. 기능 범위

핵심 기능 세 개와 홈을 완결된 사용자 흐름으로 만든다. 기술 성능 과제를 이유로 제품 구조를 변경하지 않는다.

| 기능 | Windows 앱의 범위 | 데이터/외부 연동 경계 |
| --- | --- | --- |
| 헬스케어 홈 | 바로가기, 오늘 복약, 최근 체성분·연동 상태, 증상 기록 진입, 보조 카드 | 카드별 loading/empty/error/last updated. 일부 실패해도 다른 카드 사용 가능 |
| 바디리포트 | 측정일 목록·선택, 체중/골격근량/체지방률/체지방량, 기간별 추이, 결측, 연동·새로고침 상태 | 첫 구현은 명시된 InBody 연동 시뮬레이터와 자체 REST API. 실제 인증/수집 API는 미확인 |
| 복약관리 | 직접 등록, 일정, 날짜별 체크·취소, 기록, 관리 종료 | 실제 SQLite 저장·REST 요청·재조회. 검색은 별도 합성 카탈로그가 있을 때만 제공 |
| 증상체크 | 증상 선택·입력, 질문/답변, 돌아가기, 결과 요약·MY증상기록 저장/조회 | 질문 흐름은 버전 있는 데모 자료. 결과는 입력 요약이며 질병 진단·치료 권고를 만들지 않음 |
| 만보기 | 홈의 걸음 요약·데이터 없음·외부 서비스 진입 | PC 자체 보행 측정 없음. 합성 걸음 자료는 데모 출처 표시 |
| 병원 예약 | 일정 요약·예약 서비스 외부 연결 | 자체 예약·진료·처방 서버를 만들지 않음. 일정 자료가 없으면 빈 상태 |
| 생활·보건 지수 | 홈 보조 카드, 지역/기준시각/출처 표시 | API·이용 조건 확인 전 데모 자료만. 미확인 수치를 실제 현재 지수로 표시하지 않음 |
| 건강 콘텐츠·건강판·보험금 신청 | 보조 링크로 검토, 핵심 기능 뒤 우선순위 | 출처 확인한 외부 연결만. 클립 영상·썸네일을 수집해 자체 콘텐츠처럼 쓰지 않음 |
| 건강미션 | 이미지의 예정 기능으로 분류 | MVP에서 제외. 작동하지 않는 주요 버튼으로 넣지 않음 |

**정식 목표**는 홈+바디리포트+복약관리+증상체크다. 첫 수직 구현은 홈→복약 직접 등록→체크→홈 반영이다.
그다음 바디리포트, 증상체크 순서다. 첫 slice만 끝내고 전체 목표 달성이라고 하지 않는다.
모든 핵심 화면은 자체 API를 실제 HTTP로 조회한다. WPF 내부 fixture만 사용하는 시연은 REST 완료 증거가 아니다.

## 3. 화면 구조와 사용자 흐름

상위 메뉴: **홈 / 바디리포트 / 복약관리 / 증상체크**. 설정은 별도 유틸리티다.
범용 기록 탐색·지표 추이는 바디리포트의 내부 기능이다. 진료노트·환자 선택 메뉴는 없다.
화면 이동 시 선택 날짜·스크롤 문맥을 유지한다. 등록/질문 초안이 있으면 유지 또는 명시적 폐기를 제공한다.
화면 전환으로 이전 요청이 새 화면을 덮거나 입력이 사라져서는 안 된다.

### HOME-01 헬스케어 홈

- 상단: 자체 제품명 건강노트, 기능 바로가기, 설정. NAVER 로고를 자체 앱의 제품 로고로 사용하지 않는다.
- 넓은 창: 사용자 이미지처럼 두 열 카드. 왼쪽은 오늘의 걸음→증상체크→예약, 오른쪽은 바디 브리핑/최근 측정→오늘 복약→생활·보건 지수. 보조 콘텐츠는 아래쪽.
- 사용 빈도가 높은 복약 행동과 체성분 확인은 카드 제목·핵심 값·한 개의 주요 행동으로 읽히게 한다. 카드 전체 클릭과 내부 버튼이 경쟁하지 않게 한다.
- 좁은 창: 읽는 순서대로 한 열로 재배치. 본문 스크롤, 카드 내부 잘림·중첩 금지.
- 바디 비연동: 최근 수치 대신 연동 안내. 복약 계획 없음과 오늘 예정 없음은 별도 문구·행동. 예약 없음은 정상 빈 상태다.
- 각 카드 조회를 독립 관리한다. 갱신 실패 시 마지막 정상 값과 기준시각을 남기고 재시도를 가까이 제공한다.

### BODY-01~03 바디리포트

연동 상태/데모 출처→측정일 선택→핵심 지표→추이·상세 순서.
체중 kg, 근육량 kg, 체지방량 kg, 체지방률 %를 분리한다. 같은 단위라는 이유로 다른 지표를 한 축에 합치지 않는다.
차트에는 선택 값·날짜·단위와 표 대안을 제공한다. 결측을 0으로 그리거나 임의로 선을 잇지 않는다.
연동 시뮬레이터는 연결/갱신 실패/재연결 필요를 재현하되 실제 계정 비밀번호를 요구하지 않는다.
AI 브리핑이 실제 제공자 결과가 아니면 AI 분석 완료로 표시하지 않는다. 초기에는 측정 요약으로 충분하다.

### MED-01~05 복약관리

날짜별 일정→직접 등록→기간/요일/시간→확인·저장→일정 재조회→체크/취소→홈 반영.
등록은 이름·약/영양제·선택적 메모와 일정으로 시작한다. 복용량 권고·약 효능 자동 생성 없음.
사용자 체크 상태는 복용 인증이 아니다. 저장 중/확인된 완료/실패/결과 미확인을 시각·문구로 구분한다.
종료는 확인 후 수행하고 기존 기록을 보존한다. 일정 수정은 MVP에서 제외하고 필요 시 별도 설계한다.
알림은 후속 Windows 작업으로 분리한다. 초기 앱은 알림 전달을 보장하는 설정을 제공하지 않는다.

### SYM-01~04 증상체크

증상 선택/입력→데모 질문→답변 확인→기록 저장→MY증상기록.
앞 질문 답변 변경으로 무효가 되는 뒷 답변은 재확인한다. 질문 세트 버전을 기록과 함께 보관한다.
도중 취소/뒤로/재진입 정책을 시안에서 확인한다. 결과 화면은 입력 요약·기록이며 임의 질병 확률을 보여주지 않는다.
실제 의료기관 정보는 확인된 외부 검색 연결로만 제공한다. 자체 진단 엔진은 별도 자료·계약 없이 만들지 않는다.

## 4. UI 품질과 디자인 검토

사용자 이미지의 카드 구성·둥근 모서리·짧은 문구·녹색 주요 행동·청록 건강 아이콘·충분한 여백을 기준으로 한다.
**다크 시안을 먼저 검토**한다. 이는 사용자 이미지에 맞춘 제안이며 원 서비스의 기본 테마를 단정하지 않는다.
기존 라이트 탐색 화면을 홈 승인 시안으로 대체하지 않는다. 테마 토큰과 High Contrast 대응은 공유한다.
새 외부 UI 라이브러리를 추가하지 않고 WPF ResourceDictionary와 기본 Control을 우선 사용한다.

검토 후보: 기본 1280×800 DIP, 최소 960×640 DIP, 기본 글자 14 DIP, 보조 12~13 DIP, 제목 24~28 DIP, 카드 간격 16 DIP.
최소 크기와 열 전환점은 홈/등록 시안의 긴 문자열·작은 창 검토 후 확정한다. 기존 720×520 정책을 자동 승계하지 않는다.
아이콘은 repo-native vector를 우선하고 이모지에 상태·정보를 의존하지 않는다.
모든 버튼은 동작 또는 명시된 외부 연결을 가진다. 데모 구분은 데이터/연동 근처에 두고 제품 메뉴에 개발용 장애 도구를 섞지 않는다.

설계 승인 증거: 홈 정상/빈/부분 실패, 복약 등록/저장 실패/완료, 바디 비연동/측정/결측, 증상 질문/기록과 좁은 창 시안.
시안에 기본 Tab 순서·주요 Enter/Escape·초점 복귀·선택 상태를 명시한다.
구현 완료 증거: 실제 WPF 캡처·Binding 오류·키보드/Automation·관련 DPI. 브라우저 캡처로 WPF 검증을 대체하지 않는다.
제품화 수준은 기능 버튼·상태·정보 위계·크기 변경·입력 복구까지 포함한다. 예쁜 정지 화면만으로 Done 처리하지 않는다.

## 5. 구조·도메인·코딩 규칙

유지: net48 WPF, netstandard2.0 Domain/Application/Infrastructure, net10 API, 기존 Windows CI·MVVM Toolkit·수동 composition.
Domain에 WPF/HTTP/DB/DTO/로그 금지, VM에 Window/HttpClient/DB 직접 의존 금지. DTO·도메인·편집 buffer를 분리한다.
기존 coding-rules의 비동기·해제·리소스·테스트 규칙을 유지한다. 범용 BaseViewModel·Messenger·DI framework를 먼저 만들지 않는다.

| 영역 | 모델/규칙 |
| --- | --- |
| 홈 | HomeCard 읽기 projection. 복약/측정/증상 도메인 원본을 모두 담는 거대 HealthEvent가 아님 |
| 복약 | MedicationEntry, MedicationPlan, DoseOccurrence, IntakeRecord. 이름 trim 1~80자, 메모≤200, 기간 양끝 포함, 요일≥1, 하루 시각 1~12 중복 없음 |
| 복약 시간 | 초기 Asia/Seoul 고정. 일정 local date/time과 저장 UTC 분리. 미래 날짜 체크 금지, 오늘/과거 체크·취소 허용, 종료 후 변경 금지 |
| 복약 보존 | 종료 후 기존 일정 snapshot·기록 유지. 재시작은 새 계획. 같은 occurrence에는 현재 체크 상태 하나·version |
| 측정 | BodyMeasurement + metric/value/unit/missingReason/source. 같은 측정의 지표 identity·측정 시각·제공자 구분. 연동 상태는 측정값과 분리 |
| 증상 | SymptomSession, QuestionSetVersion, AnswerSnapshot, SymptomRecord. 저장 시 질문·답변 snapshot 보존. 개인정보 필드와 답변 원문 로그 금지 |
| 외부 자료 | 걸음/예약/지수는 제공자·기준시각·데모 여부를 가진 읽기 자료. Patient/EncounterNote로 매핑하지 않음 |

읽기: CancellationToken+generation+현재 선택 문맥을 UI apply 직전에 검증한다. 기존 readiness와 dispatcher 기반을 재사용한다.
쓰기: stable ID·불변 입력 snapshot·expectedVersion·중복 실행 차단. 취소는 서버 롤백을 뜻하지 않는다.
Window/화면 실제 수명 종료에 취소·해제, 일시 Unloaded에서 VM Dispose 금지. 홈 재방문 시 중복 구독·타이머를 만들지 않는다.
첫 기능에서는 화면 이동 서비스를 추상화할 필요가 확인될 때만 최소 형태로 도입한다.

## 6. 자체 REST API 재설계 방향

아래는 **설계 후보**다. 다음 계약 Task에서 DTO·OpenAPI·오류·날짜/버전·영속 정책을 확정한 뒤 코드를 작성한다.
NAVER/InBody의 endpoint 또는 접근 권한으로 해석하지 않는다. 기존 EMR endpoint를 이름만 바꿔 재사용하지 않는다.
합성 단일 사용자·loopback 데모, SQLite 영속 저장을 기본으로 한다. 원래 EMR DB는 별도 파일로 보존하고 소비자 앱 데이터로 자동 변환하지 않는다.

| 기능 | 후보 계약 |
| --- | --- |
| 홈 | GET /api/v1/home?date=YYYY-MM-DD → 카드별 상태·요약·updatedAt·source |
| 복약 일정 | GET /api/v1/medication-schedules?date=YYYY-MM-DD → 선택 날짜·timezone·occurrences·recordVersion |
| 등록 | PUT /api/v1/medication-plans/{clientGeneratedId} → expectedVersion=0·entry·schedule; 201+저장 representation |
| 체크/취소 | PUT /api/v1/intake-records/{occurrenceId} → taken·expectedVersion; 저장 representation |
| 종료 | PUT /api/v1/medication-plans/{id}/status → ended·expectedVersion; 종료 상태 |
| 바디 | GET /api/v1/body-measurements → page/pageSize≤100; GET /{id} → 지표 상세. 기간/metric 추이는 별도 bounded 조회 |
| 연동 데모 | GET /api/v1/body-connection 및 명시된 데모 연결/새로고침 command; 실제 인증과 구분 |
| 증상 | GET /api/v1/symptom-question-sets/{id}; PUT /api/v1/symptom-records/{clientGeneratedId}; GET 목록/상세 |

UTF-8 JSON camelCase, date YYYY-MM-DD, time HH:mm, UTC ISO8601, 수치와 단위·결측 구분.
오류: 400 field validation, 404 resource missing, 409 version conflict, 503 unavailable. 기술 오류 원문은 UI에 출력하지 않는다.
응답 유실은 OutcomeUnknown. 같은 자원 ID GET→확인된 서버 상태·version과 원래 snapshot 비교→명시적 재시도.
GET 실패/404만으로 저장 실패를 확정하지 않는다. 한 자원 쓰기는 미확인 중 잠그고 원래 ID·입력을 유지한다.
바디 연결처럼 자원 상태 GET만으로 완료를 판정할 수 없는 작업은 계약 Task에서 작업 상태 조회 필요성을 따로 결정한다.
범용 operations/receipt/offline replay framework를 기본 도입하지 않는다. 기록/history/idempotency 필요성은 작업별로 증명한다.
앱 재시작의 미확인 쓰기 복구에는 최소 pending command 보존이 필요하다. 저장 위치·원문 로그 금지·정리 시점은 계약 Task AC에 포함한다.

## 7. 재사용·중단 판단

| 현재 자산 | 처리 |
| --- | --- |
| HC-101/102, CI, readiness, dispatcher, 안전한 diagnostics | 재사용. 실제 성공 증거는 기존 PR에 남김 |
| PR #27 리소스·기본 Control·수명/STA 검증 | 재사용 후보. Overview/Timeline/Trend 화면은 새 제품 구조로 승인된 것이 아님 |
| merged EMR API/Domain | 개발 이력 보존. 소비자 기능의 모델·계약·완료 증거로 사용하지 않음 |
| Draft PR #34, EMR-102 #30, EMR-103 #31, Epic #28 | 새 목표로 진행 중단. PR 병합·진료 편집 구현 없음 |
| 기존 Medication 문서·health-daily 시안 | 참고. 새 홈과 3개 핵심 기능에 맞춰 다시 리뷰 |
| HC-104/301/302 탐색·Custom Controls Task | 실행 보류. Timeline 10,000건 benchmark를 소비자 기능 AC에 복붙하지 않음 |
| Windows 품질·배포 Task | 목적 유지. 실제 화면 확정 후 영향 있는 검증 항목으로 다시 계획 |

기존 이슈/PR 삭제·재번호 부여 없음. 완료 이력과 현재 실행 계획을 구분한다.
사용자 파일 `src/HealthNote.Api/Properties/launchSettings.json`은 미추적 변경으로 발견했으며 수정·stage하지 않는다.

## 8. Epic·Task·PR 재계획

새 설계 리뷰가 끝날 때까지 구현 Task를 Ready 처리하지 않는다. 기존 기능 Task는 아래 계획으로 자동 재개하지 않는다.

| 순서 / 새 ID | 범위 | 주요 수용 기준 / PR |
| --- | --- | --- |
| CON-001 재설계 기준 | 제품 범위·근거·UI·도메인/API 후보·이슈 전환 | 현재 문서 상충 우선순위 제거, 보류 기록, 실행 코드 없는 Design Draft PR |
| CON-002 홈 및 핵심 화면 시안 | 홈+복약+바디+증상, 정상/빈/실패/좁은 창 | 사용자 이미지와 화면 매핑, 자체 상세 설계 구분, 디자인 리뷰 전 코드 구현 없음 |
| CON-003 복약 계약·Domain·API | 직접 등록·일정·체크·취소·종료·결과 확인 | DTO/OpenAPI 먼저, 자정/기간/종료/중복/409/응답 유실/재시작 실제 HTTP·DB 검증 |
| CON-004 WPF 홈·복약 수직 흐름 | 홈 첫 실행→등록→조회→체크→홈 갱신 | API→VM→실제 WPF, 초안·미확인 결과 보존, stale 방어, 키보드·Binding·대표 캡처 |
| CON-005 바디리포트 | 연동 데모→측정일→지표/추이/표 | 계약 선행, 실제 HTTP, 결측·단위·날짜 race·갱신 실패, native WPF 캡처 |
| CON-006 증상체크 | 데모 질문→답변→요약→저장/기록 | 자료 버전·분기 변경·초안, 실제 저장/재조회, 임의 진단 없음 |
| CON-007 보조 카드·알림 | 걸음·예약·지수·외부 연결, Windows 알림 검토 | 자료 출처/API 이용 조건, OS 알림 권한·앱 종료/절전 정책. 계약 미확인 연동을 완료로 주장하지 않음 |
| CON-008 Windows 완성도 | 전체 사용자 흐름·설치·재실행·측정·문서 | 영향 있는 DPI/키보드/Automation·메모리·성능 조사·설치 결과와 미검증 한계 |

Epic 구성 제안: Consumer Design(CON-001/002), Core Healthcare(CON-003~006), Windows Completion(CON-007/008).
이번 PR에서는 재설계 Task만 생성하고, 후속 Epic/Task는 설계 리뷰 후 GitHub에 정확한 AC·선행 이슈를 붙여 생성한다.
새 ID는 실행 순서를 돕는 이름이며 기존 HC/EMR ID의 삭제·대체 번호가 아니다.
각 Task를 작은 Draft PR로 구현한다. 계약+실행 변경이 커지면 해당 Task 아래 API/WPF PR을 분리한다.
사용자 승인에 따라 리뷰 문제 없고 최신 통합 CI 통과한 PR은 병합 가능하지만, 새 설계를 미검토 상태에서 병합하지 않는다.

## 9. 검증과 남은 확인

설계 단계: 범위 충돌·링크·이슈/PR 상태·화면/기능 추적을 검사한다. 실행 코드를 바꾸지 않으므로 build/test를 성공 증거로 새로 주장하지 않는다.
기능 단계: 실제 loopback 서버·HTTP·SQLite 저장/재시작, net48 VM/STA, 실제 WPF 캡처를 사용한다. 테스트용 프로세스/DB만 정리한다.
성능은 실제 화면의 가설·측정·결론으로 조사한다. 발견된 병목만 최적화하고 Custom Control/downsampling은 필요가 확인될 때 도입한다.
API paging integration과 Control 직접 synthetic benchmark는 분리한다. 모든 UI Task에 대량 데이터·전 DPI matrix를 반복 요구하지 않는다.
기존 DPI100/150·혼합 모니터·High Contrast·OS 입력/스크린리더 미검증 기록은 유지한다. 기능 진행을 막지 않되 최종 완료 표에서는 구분한다.

미확인: 로그인 후 바디/복약/증상 상세 최신 화면, 실제 제공자 API 권한, 약 정보 데이터 이용 조건, 건강 지수 API, Windows 알림 전달 정책.
현재 자료로 홈과 자체 상세 화면의 설계는 진행 가능하다. 미확인 외부 연결은 데모 adapter로 분리하고 실제 연동이라고 부르지 않는다.

## 출처와 재확인 수준

- 사용자 제공 네이버 헬스케어 홈 이미지 2장: 현재 대화 첨부. [서비스 홈](https://healthcare.naver.com/). 공개 페이지 접근만으로 로그인 후 홈을 새로 검증한 것은 아니다.
- 2026-10-01 본문 재확인: [복약 소개](https://help.naver.com/service/30051/contents/24743?lang=ko&osType=COMMONOS), [등록 방법](https://help.naver.com/service/30051/contents/24744?lang=ko&osType=COMMONOS).
- 바디리포트 [소개](https://help.naver.com/service/30051/contents/25197?lang=ko&osType=COMMONOS), [연동 FAQ](https://help.naver.com/service/30051/contents/25198?lang=ko&osType=COMMONOS): 이번 web 조회 접근 실패. 세부 조건은 이전 조사 이력이며 새 계약으로 자동 채택하지 않는다.
- [증상체크 도움말](https://help.naver.com/service/30051/contents/23689?lang=ko): 이번 조회는 메뉴만 노출. 질문 분기·결과 화면의 신규 검증 자료가 아니다.
- [이전 서비스 조사](service-flow-research.md), [이전 소비자 범위](consumer-healthcare-scope.md): 참고 이력.
