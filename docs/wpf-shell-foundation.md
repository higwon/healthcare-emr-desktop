# HC-103 Native WPF UI Foundation

PR #26 디자인은 merge commit `eaf5a0c3a6ba18cc757d0549a6720d681735c14a`로 승인됐다.
Task #12는 구현 진행 중이며 완료하지 않는다. 이번 branch는 승인된 디자인의 WPF 구현 slice다.

## 구현 계약

- Shell이 세 개의 presentation preview VM을 소유한다. 기본 ListBox의 선택으로 ContentControl/DataTemplate 화면을 전환한다. 기존 readiness 화면/수명은 별도 개발 도구로 유지한다.
- 합성 fixture는 Desktop presentation 자료이며 Domain/API 모델이 아니다. 기록 필터/검색/기간/상태 전환은 로컬 preview다. 실제 query/paging/race는 HC-104다.
- Timeline은 기본 ListBox/Item을 사용한다. 방향키 선택은 목록 초점을 유지하고 Enter는 상세에 진입한다. Escape/상세 닫기는 원래 선택 행으로 돌아간다. 필터 제외 시 선택 해제, 첫 행 자동 선택 없음.
- Trend는 지표·표·선택 상세를 제공한다. 결측 값 영역은 `측정값 없음`, 수치만 단위와 함께 표시한다. chart는 HC-302에서 구현한다.
- Window scope에 Foundations → LightTheme → Controls → ViewTemplates를 합친다. View 특수 스타일은 View scope, Light만 구현한다.
- 초기/최소 Window 크기는 실제 work area보다 크지 않게 제한한다. 좁은 Timeline은 목록/상세를 세로 배치하고 각 영역을 bounded viewport로 유지한다. 1024×680은 최소 Window 상수가 아니다.
- High Contrast는 Window scope semantic brush를 시스템 brush로 매핑한다. 시스템 변경 구독은 Closed에서 해제한다. 실제 OS High Contrast 검증과 resource 적용 테스트는 별개다.
- correctness 테스트에는 캡처 I/O를 넣지 않는다. 별도 Evidence 테스트에서 native WPF 렌더 캡처와 환경 정보를 생성한다. bitmap DPI/인위적 크기 변경은 실제 OS DPI 검증으로 표시하지 않는다.

## 검증 기록

구현 후 실제 실행 결과·CI·환경·캡처를 이 문서에 기록한다. 현재 새 UI의 DPI/keyboard/UIA/Binding/High Contrast는 미검증이다.
CI와 로컬 synthetic work-area/reflow 테스트가 실제 monitor DPI matrix를 대신하지 않는다. 필수 미검증 항목이 있으면 Task OPEN을 유지한다.
