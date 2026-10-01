# EMR-102 환자 / 진료 기록 조회

Task #30. 선행 #32/#33/#27은 사용자 승인 및 통합 CI 확인 후 병합했다.
WPF 실제 실행은 환자 검색·paging → 환자 선택 → 이력 paging → 기록 상세 흐름을 제공한다. 저장/편집 버튼은 EMR-103까지 넣지 않는다.

- 기존 preview VM/검증 fixture는 회귀용으로 유지하며 실제 composition은 EMR workspace를 첫 화면으로 사용한다.
- VM은 Application IEmrQuery와 기존 IUiDispatcher에 의존한다. HttpClient/DTO parsing은 Infrastructure, 포커스/reflow는 View에 둔다.
- 환자 검색은 Enter/검색 버튼으로 실행하며 page=1로 초기화한다. 페이지 크기는 20이며 이전/다음은 서버 totalCount로 결정한다. 새 검색/페이지에서 선택을 해제하고 다른 환자의 이력/상세를 먼저 비운다.
- 환자 선택은 목록 초점을 유지하며 해당 환자의 이력을 조회한다. 기록 선택은 상세를 조회한다. Enter는 다음 영역, Escape는 이전 선택 행으로 복귀한다. 조회 완료가 초점을 자동으로 빼앗지 않는다.
- 환자/이력/상세는 각각 loading/empty/error/retry 상태를 갖는다. query snapshot과 CTS/generation을 사용한다. 환자 변경/새 query/화면 close는 이전 응답 apply와 busy 변경을 막는다. 취소를 무시하는 provider도 generation 검사로 차단한다.
- DTO mapping은 page context, ID/환자 소유자, 날짜/UTC/version을 확인한다. 오류 원문·payload를 사용자 메시지/진단에 넣지 않는다.
- 기본 크기에서는 세 영역을 나란히 표시한다. 좁은 공간에서는 세로로 재배치하고 각 목록은 bounded viewport를 유지한다. 긴 주호소/평가/계획은 wrap/scroll한다.
- 대표 캡처는 별도 Evidence 테스트에서 실제 API로 합성 기록을 준비하고 조회한 실제 WPF Window를 사용한다. correctness 테스트에 캡처 I/O를 섞지 않는다.

## 검증 계획

실제 API 프로세스와 Infrastructure adapter의 환자/이력/상세 HTTP→모델 매핑, VM query 적용 및 WPF binding을 검증한다.
결정적 fake provider는 취소 무시/응답 순서 역전/dispatcher queue 후 Dispose를 검증하는 데만 사용한다. mock-only 실행을 실제 연동 증거로 주장하지 않는다.
기존 net48 49 + API net10 7 회귀를 유지한다. DPI100/150·mixed-monitor·High Contrast·physical keyboard/screen reader 미검증은 기록을 유지하며 구현을 차단하지 않는다.
