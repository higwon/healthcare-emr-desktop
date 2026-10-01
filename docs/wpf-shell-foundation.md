# HC-103 Native WPF UI Foundation

PR #26 디자인은 merge commit `eaf5a0c3a6ba18cc757d0549a6720d681735c14a`로 승인됐다.
Task [#12](https://github.com/higwon/healthcare-emr-desktop/issues/12)는 **OPEN / In Progress**다.
이번 구현은 승인된 디자인의 WPF foundation slice이며 미검증 필수 조건을 완료로 표시하지 않는다.

## 구현 계약

- Shell이 Overview/Timeline/Trend presentation VM을 소유한다. 기본 ListBox 선택과 ContentControl/DataTemplate로 화면을 전환한다. 기존 readiness 명령과 수명은 접힌 개발 도구에서 유지한다.
- 합성 fixture는 Desktop presentation 자료다. 기간/타입/검색/empty/loading/error/stale는 로컬 preview이며 실제 API/query/paging/race는 HC-104다. loading 취소와 error retry도 preview 상태 전환이다.
- Timeline은 기본 ListBox/Item이다. 방향키 선택은 목록 초점을 유지하고 Enter는 상세 닫기 버튼으로 진입한다. Escape는 상세를 유지하며 원래 행으로 돌아간다. 닫기는 상세를 닫고 복귀한다. 필터 제외 시 선택 해제, 첫 행 자동 선택 없음.
- Trend는 지표·측정값 표·선택 상세를 제공한다. 결측은 표에서 `결측`, 상세에서 `측정값 없음`이며 값 영역에 단위를 붙이지 않는다. Chart는 HC-302다.
- Window scope에서 Foundations → LightTheme → Controls → ViewTemplates를 합친다. Foundations는 폰트/간격/반경, LightTheme은 semantic brush, Controls는 필요한 공통 스타일, ViewTemplates는 VM/View 매핑이다. Button/ListBoxItem만 필요한 상태 표현을 templating하며 기본 peer/selection 동작을 사용한다. DI/navigation/theme framework나 custom control은 추가하지 않았다.
- 공통 dictionary와 Window에 부착되기 전 생성되는 UserControl 사이의 리소스는 DynamicResource로 연결한다. 초기 runtime 테스트에서 cross-dictionary StaticResource의 Padding/FontSize lookup 실패를 발견해 수정했다. compiled XAML 성공만으로 runtime 리소스 검증을 대신하지 않는다.
- High Contrast 시 Window semantic brush를 SystemColors의 Window/WindowText/Highlight/HighlightText 키로 매핑한다. disabled는 GrayText 키를 사용한다. 시스템 변경 구독은 Closed에서 해제한다. **실제 OS High Contrast 결과는 미검증**이며 DynamicResource 사용을 성공 증거로 주장하지 않는다.
- Behavior/Binding correctness에는 캡처 I/O가 없다. 별도 Evidence 테스트가 각 화면의 독립 Window를 실제로 표시한 뒤 native WPF content render와 환경 파일을 생성한다.

## Window sizing 결정

PerMonitorV2 manifest를 사용한다. 초기 outer Window는 1280×800 DIP를 목표로 하되 현재 monitor work area보다 24 DIP 작게 제한하고 그 영역에 중앙 배치한다.
최소 outer Window는 720×520 DIP를 목표로 하되 작은 work area에서는 같은 방식으로 줄인다.
1024×680 디자인 영역을 Window 최소값으로 복사하지 않았다. 720×520 결정 근거는 아래 실제 200% 환경의 reflow/viewport 검증이다. 다른 DPI 전체 검증은 남아 있다.

Timeline View 폭 820 DIP 미만이면 목록/상세를 세로로 배치한다. View 높이 440 DIP 미만이면 중복 화면 제목을 접고 선택 navigation과 Automation Name으로 문맥을 유지한다.
각 영역은 bounded viewport이고 상세는 별도로 scroll한다. 좁은 상세 padding은 12 DIP이며 닫기 버튼이 보인다. 긴 목록 제목은 명시적으로 wrap하고 pixel scroll로 큰 행을 탐색한다.
Overview는 좁아지면 최근 기록/최근 측정 패널을 세로 배치한다. Window chrome과 taskbar를 포함한 실제 위치/크기는 runtime에서 검사한다.
합성 960×540 work-area sizing 테스트는 정책 경계 검사이며 실제 DPI/모니터 검증이 아니다.

## 실제 로컬 검증

2026-10-01, Windows 11 build 26200, CLR 4.0.30319.42000, SDK 10.0.300, Release.
캡처 실행 소스 commit: `71ad371142f9c4c37bf00f9d1dec93551072ce94`.
이후 evidence/documentation commit에는 실행 코드 변경이 없다.

| 항목 | 실제 결과 / 한계 |
| --- | --- |
| 실제 DPI | 192×192 DPI = 200%. bitmap 확대 시뮬레이션 아님 |
| screen / work area | 1920×1080 / 1920×1032 DIP |
| 기본 outer / client | 1280×800 / 1267×764.5 DIP |
| 최소 outer / client | 720×520 / 707×484.5 DIP |
| layout | 실제 Window 위치가 work area 안에 있음; 최소 창에서 긴 목록 제목 wrap, 상세 stacked reflow, 목록/상세 bounded viewport, 닫기 interaction 공간 확인 |
| keyboard / focus | 실제 STA Window의 WPF Down routed event → 선택/상세 변경·목록 초점 유지, Enter → 상세, Escape → 선택 행 복귀, Next/Previous focus traversal → 닫기/선택 행, 필터 제외 → 선택 해제 |
| 외부 입력 한계 | computer-use 읽기에서 실제 exe 화면/UIA tree 확인. 클릭은 사용자 입력 감지 guard로 중단되어 OS 입력 주입/물리 Tab·Shift+Tab 전체 경로는 미완료 |
| 기본 AutomationPeer | navigation/record Name·List/ListItem role·enabled·selected provider, ComboBox role와 검색 TextBox 이름을 실제 WPF peer에서 확인. custom peer 없음; screen reader smoke 미검증 |
| Binding/resource | 세 화면 표시·전환과 Trend 결측/지표 변경에서 runtime Binding trace 오류 0; 필수 brush/converter lookup 성공 |
| High Contrast | 시스템 색 키 대응 구현. 로컬 OS High Contrast=false이며 실제 활성 상태의 text/background/selection/focus/border/disabled/error 미검증 |
| build/tests | Release 경고 0 / 오류 0; 기존 34 유지 + 신규 14 = net48 48 passed / 0 failed / 0 skipped |
| 기타 기존 경로 | 참조 경계·locked restore·API readiness smoke·API publish 통과. 실제 healthcare 조회/쓰기는 없음 |

신규 14개는 presentation 동작 7, STA WPF correctness 6, 별도 capture Evidence 1개다.
선택 identity 유지, 자동 첫 선택 금지, 닫힌 상세 유지, 합성 상태 전환, 결측 의미, runtime 리소스/peer/focus, 실제 최소 창과 work-area 정책을 검사한다.
최소 창 목록 제목 clipping은 실제 캡처에서 발견해 wrap을 수정하고 multi-line 높이/폭 검사로 검증했다.

## Evidence와 CI

[대표 WPF 캡처 7장과 원시 환경](../design/evidence/hc103-wpf/README.md)을 확인한다.
이 PNG들은 실제 WPF Window의 content render이며 OS desktop/chrome screenshot은 아니다.
브라우저 시안을 재사용하지 않았고 각 PNG에 DPI/client/work area/build/source commit TXT를 함께 보존했다.

Windows CI는 기존 locked restore, Release build, net48 tests, API smoke, publish, artifacts 경로를 유지한다.
실제 해당 head 결과는 구현 PR의 Checks/run 링크를 따른다. CI desktop 환경은 물리 monitor DPI matrix의 증거가 아니다.

## 미검증 / 후속 범위

- 실제 100%/150% DPI, 다중 모니터 이동/혼합 DPI, 작은 실제 work area에서의 전체 화면 matrix.
- OS 입력으로 하는 전체 Tab/Shift+Tab/Up/Down/Enter/Space/Escape 탐색, screen reader smoke.
- 실제 Windows High Contrast 전체 상태.
- HC-104 실제 API/paging/query/cancellation/stale. HC-301/302 custom controls 및 직접 fixture benchmark/rendering.
- Medication/persistence/server write/AI/Dark theme는 이번 범위가 아니다.

HC-103은 위 필수 품질 검증 및 사용자 PR 검토/merge 전까지 Done으로 처리하지 않는다.
