# HC-103 탐색 Shell 설계안

2026-09-30. Task [#12](https://github.com/higwon/healthcare-emr-desktop/issues/12).
**디자인 리뷰 제안**이며 WPF 구현/Task 완료 증거가 아니다.
기존 Medication 시안을 전체 Shell 기준으로 재사용하지 않았다.
[조작 가능한 시안](../design/prototypes/health-explorer.html)과 [캡처](../design/evidence/hc103/overview-1280.png)를 함께 검토한다.

## 제품 정보 구조

| 화면 | 사용자의 질문 | 우선 정보 | 다음 행동 |
| --- | --- | --- | --- |
| 건강 요약 / EXP-01 | 이 기간에 어떤 기록이 있나? | 선택 프로필·기간, 기록 건수, 최근 기록, 최신 측정값·단위·날짜 | 특정 기록 상세 또는 지표 추이 |
| 기록 탐색 / EXP-02 | 원하는 기록과 그 출처는? | 기간·유형, 날짜 순 목록, 선택 제목·시각·출처·요약 | 상세 닫기·같은 측정 추이 |
| 지표 추이 / EXP-03 | 측정값이 어떻게 변했나? | 단일 지표·단위, 시간축, 결측, 같은 자료의 표, 선택 측정 | 원래 기록으로 복귀 |

왼쪽 탐색은 위 세 화면으로 한정한다. 상단은 프로필/데모 표시, 화면 제목 옆은 기간이다.
프로필 선택 기능/계정/실제 로그인은 이번 범위가 아니므로 작동하지 않는 계정 메뉴를 넣지 않는다.
Medication 쓰기, 인바디 로그인, 병원 접수/처방/청구, AI 건강 판정, 장식용 건강 점수는 넣지 않는다.
수치는 합성 표시 자료다. 실제 NAVER 기능/디자인/연동으로 주장하지 않는다.
Source와 합성 표시는 작은 문구로 유지하고 개발 용어는 설계 시안 검토 영역에만 둔다.

## 배치·창 크기 제안

| 항목 | 제안 | 제한/구현 확인 |
| --- | --- | --- |
| 기본 client 영역 | 1280×800 DIP | 브라우저 viewport와 native Window 외곽 크기는 다름 |
| 최소 client 검토 영역 | 1024×680 DIP | 최종 MinWidth/MinHeight는 chrome·work area 포함 실제 WPF에서 확인 |
| 왼쪽 탐색 | 184 DIP, 좁은 배치 160 DIP | 탐색 글자 줄임/아이콘만 표시하지 않음 |
| 본문 | Auto/* Grid + 22~28 DIP padding | 픽셀 절대 좌표/전체 고정 크기 카드 없음 |
| 넓은 기록 탐색 | 목록 * / 상세 최소 285 DIP + 18 DIP 간격 | 선택 상세는 bounded 영역에서 wrap/scroll |
| 좁은 기록 탐색 | 목록 위 / 상세 아래 | 목록/상세를 각각 bounded viewport로 유지, 외부 ScrollViewer로 목록 virtualization 파괴 금지 |
| 요약 | 기록 요약 3개, 아래 최근 기록/측정값 | 글자 확대·긴 값에서 reflow; 글자를 줄여 억지로 맞추지 않음 |

좌측 탐색/상단 context는 유지하고 본문이 scroll한다.
브라우저 시안의 전체 세로 scroll과 CSS breakpoint를 WPF panel/가상화 구현으로 그대로 복제하지 않는다.
실제 screen working area가 제안 크기보다 작으면 첫 Window 위치/크기를 usable area에 맞추는 정책을 구현 시 검증한다.
DPI를 올렸을 때 창이 화면 밖으로 나가는 상태를 합격으로 처리하지 않는다.
최소 영역 이하가 필요한 monitor 조건은 지원 범위·reflow·예외를 실제 증거와 함께 확정한다.
512px 모바일 시안을 Windows 최소 크기의 근거로 삼지 않는다.

## 시각 리소스

| 의미 | 토큰 제안 |
| --- | --- |
| 배경 / 표면 | #F5F7F9 / #FFFFFF |
| 본문 / 보조 | #202D35 / #61737F |
| 구분선 | #E2E8EC |
| 선택 / 주 행동 | #087B60 / 연한 선택 #E8F4EF |
| 오류 | #A53339 + 오류 문구 |
| Keyboard focus | 3 DIP 파란 outline; 시스템 High Contrast에서는 시스템 highlight |
| Typography | 기본 14, 보조 12, 화면 제목 26, section 17 DIP |
| Geometry | spacing 4/8/12/16/20/24/28, surface radius 10, input 6~7 DIP |

가독성/contrast는 WPF 실제 텍스트·시스템 High Contrast에서 추가 확인한다. 아직 접근성 완료를 주장하지 않는다.
선택은 색 + 테두리 + selected state, 오류는 색 + 작업별 문구로 구분한다.
초록색으로 정상/건강 판정을 내리지 않는다. 단위·측정 날짜는 값 가까이에 배치한다.
Button 최소 높이/패딩은 토큰이며 내용에 따라 커진다. 제목/행 높이를 고정해 긴 이름을 자르지 않는다.
ControlTemplate은 공통 버튼/선택 항목의 실제 상태 차이에만 최소 사용하고 모든 기본 Control을 다시 만들지 않는다.

## 조회·선택·복귀 계약

- 화면과 query context(프로필·기간·유형/지표), 선택 event/point identity를 분리한다.
- Idle/취소는 아직 조회 안 함, LoadedEmpty는 성공했지만 자료 없음이다.
- Loading은 조회 중 표시 + 취소, 초기 Error는 자료 없음 + 안전한 오류/명시적 재시도다.
- Stale은 이전 기간/필터를 명시한다. 새 선택 조건의 결과처럼 보이지 않게 한다.
- append 실패는 기존 목록 유지와 해당 cursor 재시도이며 HC-104에서 구현한다.
- 필터에서 빠진 선택은 해제한다. 상세 닫기가 late response로 뒤집히지 않는다.
- 선택 기록의 출처/시각은 상세에 보존한다. 선택한 측정의 단위와 시각은 그래프·표·상세에서 같아야 한다.
- null은 결측/빈 표식이다. 0으로 바꾸거나 결측 구간을 선으로 연결하지 않는다.
- 시안의 최근 24건과 표시 6행은 표시 예시다. 실제 전체 fixture/API paging 계약으로 간주하지 않는다.
- 시안의 검색은 화면 내 합성 기록 찾기다. 서버 검색 API/필터 계약을 추가하지 않는다.
- 브라우저 재시도는 검토 상태 전환이며 실제 네트워크/오류 복구 증거가 아니다.

## WPF 구현 범위와 확장 순서

이 PR은 HTML 시안·설계·브라우저 검토만 포함한다. src/tests의 동작은 변경하지 않는다.

다음 HC-103 WPF PR은 Window를 host로 유지하고 UserControl/DataTemplate·공통 context·탐색 선택·리소스·키보드/Automation을 구현한다.
검토용 합성 presentation fixture는 UI preview로 명시하고 Domain entity/새 API/read port를 선구현하지 않는다.
실행 가능한 목록/선택/상세·표를 제공하며 미래 화면 버튼을 빈 placeholder로 라우팅하지 않는다.
HC-103의 Trend preview는 표/선택/동일 단위 표현까지다. 시안의 그래프는 최종 표현 목표이며 WPF chart 구현 완료를 의미하지 않는다.
Healthcare API(query/paging/stale/context)는 HC-104, HealthTimelineControl은 HC-301, HealthTrendControl은 HC-302다.
최종 Control의 rendering/downsampling/가상화는 기본 WPF 측정 후 정한다.
실제 API와 UI fixture 상태는 구분한다. readiness API의 성공을 건강 데이터 조회 성공으로 표시하지 않는다.

### ResourceDictionary 계획

1. Foundations.xaml: 간격·글자·반경 등 theme 중립 값.
2. LightTheme.xaml: 의미별 brush tokens.
3. Controls.xaml: 위 tokens를 사용하는 최소 Button/TextBox/ComboBox/ListBoxItem style·focus.
4. ViewTemplates.xaml: 실제 화면 VM → UserControl 매핑.

Merge 순서와 의존을 위 순서로 단방향 유지한다.
색/token은 DynamicResource로 참조하고 High Contrast에 시스템 brush를 적용한다.
같은 key 중복 선언을 편의 override로 남발하지 않는다. 특정 view의 스타일은 view scope에 둔다.
global IServiceProvider/Ioc, INavigationService/ScreenViewModelBase/AsyncLoadViewModel은 추가하지 않는다.
새 화면의 실제 중복/책임이 확인되면 그때 작은 추상화를 검토한다.

### Keyboard·Automation 계획

- Tab/Shift+Tab: 탐색 → context → 화면 필터 → 목록 → 상세/표의 자연스러운 순서.
- 탐색은 기본 selection control 의미를 유지한다. 키보드 활성화는 Enter/Space, 현재 선택은 Automation state에 전달한다.
- 기록 목록은 방향키 선택, Enter 상세 진입. 상세 내 Escape는 닫기 후 원래 event identity의 행으로 복귀한다.
- 필터로 그 행이 없어졌다면 목록/필터로 돌아가며 임의의 첫 기록을 자동 선택하지 않는다.
- 화면 전환 시 이전 화면의 초점 identity를 보존한다. query 완료/retry가 현재 사용자 초점을 빼앗지 않는다.
- 안내 dialog는 모달 focus cycle과 닫기 후 호출 버튼 복귀를 확인한다.
- Navigation/ListItem/Button/ComboBox 등의 기본 AutomationPeer를 우선 사용한다.
- Name/role/selected/enabled를 실제 UIA로 검증한다. tooltip만 접근 가능한 이름으로 사용하지 않는다.
- event/Loaded/Unloaded 해제와 실제 화면 scope Dispose를 구분한다.

### 테스트·진단 인계

Behavior/Binding correctness test와 evidence capture test를 분리해 파일 I/O 실패를 바인딩 실패로 보고하지 않는다.
ClientDiagnostics는 구체 타입을 유지한다. QueryCompleted는 success 전용이 아니므로 이름 변경 시 QueryFinished로 통일하되 별도 회귀 확인한다.
이 디자인 PR에서 진단/command 구현을 바꾸지 않는다.
100/150/200% 실제 DPI × 최소/기본 client 영역, keyboard/focus, Automation name/role/state, Binding/resource 오류가 WPF PR의 필수 증거다.
브라우저 CSS 확대나 scaled bitmap을 실제 monitor DPI evidence로 표시하지 않는다.
스크린리더/High Contrast/monitor 이동은 검증 환경과 제한을 기록하며 미확인 항목을 체크하지 않는다.

## 실제 브라우저 검토

Codex in-app browser, localhost 시안. 브라우저 viewport는 CSS px이며 native WPF DIP 검증이 아니다.

| 검토 | 실제 결과 |
| --- | --- |
| 1280×800 요약/목록·상세/추이 | 캡처·정보 구조 확인; 긴 본문은 세로 scroll |
| 1024×680 긴 제목 | 상세 아래 재배치, 선택 1개 유지, document 가로 넘침 없음 |
| 기록 상세 Escape | 닫기 후 body-3 원래 행 초점 복귀 |
| 체성분 상세 → 추이 | 화면 전환과 선택 측정 표시 확인 |
| 결측 표 행 선택 | 값 없음 표시, 선 gap 유지, 0으로 표시하지 않음 |
| 유형 필터 검사 | 합성 행 1개 표시 |
| 조회 오류 → 재시도 | 정상 시안으로 전환 |
| Loading 취소 | Empty가 아닌 Idle/취소 문구 표시 |
| Stale | 선택 최근 1개월/표시 이전 최근 3개월 구분 |
| 안내 dialog Escape | 닫기와 demo-info 버튼 초점 복귀 |
| Script/console | JS syntax 검사 통과, 확인한 브라우저 error log 없음 |

캡처는 [design/evidence/hc103](../design/evidence/hc103)에 보관한다.
브라우저 탭의 대표 정상/긴 이름/오류/이전 조건 이미지이며 WPF/성능/메모리 evidence는 아니다.
전체 Windows/DPI/Automation/스크린리더/High Contrast는 **아직 미검증**이다.

## 공식 구현 근거

이 문서의 화면/토큰/배치 수치는 자체 제안이다. WPF 플랫폼 의미는 다음 공식 자료를 기준으로 한다.

- [Merged ResourceDictionary lookup/merge](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/systems/xaml-resources-merged-dictionaries): 순서와 lookup 규칙.
- [Keyboard/logical focus](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/focus-overview): focus scope와 실제 keyboard focus 구분.
