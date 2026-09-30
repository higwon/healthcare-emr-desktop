# UI 구현 기준

실서비스 기능의 근거는 service-flow-research, 조작 시안의 범위는 ui-prototype-review를 따른다.
브라우저 시안은 화면 검토 도구이며 WPF 품질 증거가 아니다.

## 화면·토큰

HC-01 오늘의 건강: 복약 중심 카드, 체성분 연동 상태와 증상 기록 진입.
MED-01 일정: 날짜 선택·예정 항목·완료/미기록·선택 상세.
MED-02~04 등록: 이름/종류 → 기간/요일/복수 시각/메모 → 확인.
MED-05 상세/종료: 출처·일정·기록·버전·명시적 종료 확인.
MED-06 날짜별 기록: 현재 선택 날짜와 server query context 일치.

시안 토큰: 기본 14 DIP, 보조 12 DIP, 제목 27~29 DIP, 카드 간격 16 DIP, radius 16 DIP.
녹색은 주요 행동·선택·완료, 오류는 붉은 계열+문구. 성공 알림까지 오류 색으로 표시하지 않는다.
Window min size·정보 밀도·줄바꿈 기준은 HC-004에서 desktop용으로 확정한다. prototype의 320px 배치를 WPF 최소 창 폭으로 자동 채택하지 않는다.
라이트 우선 구현, 다크는 후속 명시 범위. system high contrast 지원과 keyboard focus는 기본이다.

## 상태 계약

Idle/Loading/LoadedEmpty/Loaded/Error/Stale의 조회 상태를 명시한다.
Editing/Validating/Saving/Committed/Rejected/OutcomeUnknown의 쓰기 상태를 구분한다.
Saving은 일시 busy, OutcomeUnknown은 작업 identity가 있는 복구 상태. 입력을 잃거나 다른 payload로 원래 요청을 덮지 않는다.
오류 메시지는 해당 작업 가까이에 둔다. 위험과 다음 행동을 짧게 표현하고 internal exception·operation hash를 사용자 문구에 넣지 않는다.
등록 초안에서 닫기·뒤로·창 종료 시 보존/폐기 정책을 정의한다. 결과 미확인 쓰기를 닫기만으로 완료·롤백 처리하지 않는다.
조회 데이터 없음·권한/연결 문제·등록된 계획 없음·그 날짜 일정 없음은 다른 상태다.

## UI PR 증거

- 정상·빈·지연·실패·재시도·긴 이름·많은 항목·비활성 상태의 대표 캡처.
- 지원 최소 창·기본 창·최대 창, DPI 100/150/200%, 모니터 이동 결과.
- Tab/Shift+Tab/Enter/Escape, selection, 초점 복귀, 닫기/재열기.
- Automation name/role/state, 색 없이 식별 가능한 상태, 스크린리더 smoke 범위.
- Binding trace 오류가 없고 runtime에서 필수 리소스 누락이 없는지 확인.
- 측정 환경과 미검증 조건을 적는다. 프로덕션 수준 목표와 실제 확인한 증거를 구분한다.

예시 데이터·장애 제어는 Demo 도구에 둔다. 실제 제품 화면에 장식용 점수·가짜 AI 건강 판정·작동하지 않는 버튼을 넣지 않는다.
