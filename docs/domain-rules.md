> 2026-10-01 재설계: **아래는 이전 범위/조사/구현 이력이다. 현재 제품·화면·도메인/API 방향·Task 순서는 [Windows 헬스케어 재설계](healthcare-windows-baseline.md)가 우선한다.** EMR·범용 Timeline/Trend 구현을 재개하지 않는다. 이 문서의 과거 완료 조건/추천/endpoint를 새 기능 계약으로 자동 채택하지 않는다.

# Healthcare Data · Feature별 도메인 규칙

자체 제품 규칙이며 의료 지침이 아니다. [공식 기능 조사](service-flow-research.md)와 구분한다.

## 탐색 모델 경계

HealthEvent: Id·SubjectId·OccurredAt(UTC)·Type·Title·Summary·Source·DetailId의 읽기 projection이다.
동일 시각은 Id로 순서를 고정한다. 표시 시간대는 query context로 주며 '최근'의 기준도 IClock과 함께 명시한다.
이벤트 타입별 상세 모델과 측정 series는 별도다. Blood Test·Visit 등의 합성 상세는 처방/진료 workflow가 아니다.
MeasurementPoint: metric·observedAtUtc·decimal? value·unit·missingReason·source·optional reference range.
결측은 0으로 대체하지 않는다. unit 불일치 series를 혼합하지 않으며 참고 구간은 해당 점의 합성 데이터로 표시한다.
필터·페이지·선택은 조회 상태다. 원본 event를 화면 필터 변경으로 수정하지 않는다. 자세한 계약은 [탐색 명세](health-data-exploration.md)를 따른다.

## Medication 모델 경계

MedicationEntry: 사용자 입력 이름·종류·메모와 출처. 직접 등록 이름은 검증된 약 설명이 아니다.
MedicationPlan: Entry 참조, 날짜 범위·요일·시각·Asia/Seoul·알림 요청·일정 버전·관리 종료 시각.
DoseOccurrence: Plan ID + schedule version + local date + local time의 고유 식별. 여러 약이 같은 시각이어도 다른 occurrence.
IntakeRecord: occurrence의 현재 체크 상태·버전과 변경 이력. 체크는 실제 복용의 인증이 아니다.
작업 결과 저장: operation ID·command 유형·정규화된 입력·처리 결과·resource version을 보관하는 서버 adapter 책임이다. Domain의 중심 모델이나 범용 receipt framework로 만들지 않는다.
측정 기록: 지표·decimal 값·단위·측정 시각·source·결측 이유. UI 계산과 의료 판정을 분리한다.

## Medication 불변식

- 이름: trim 후 1~80자, 메모 최대 200자. 종류는 약/영양제. 한글·공백·긴 이름을 지원한다.
- 날짜는 시간과 분리한 값 객체, 시각은 분 단위 값 객체다. JSON에서 YYYY-MM-DD·HH:mm 형식을 엄격히 검증한다.
- 시작일 ≤ 종료일. 종료일 포함. 요일은 한 개 이상·중복 제거, 시각은 1~12개·중복 불가·정렬.
- 계획 기간과 요일 조건을 만족해야 occurrence가 생긴다. 누락 기록은 0/미복용/완료로 바꾸지 않는다.
- 예정 시각·날짜는 Asia/Seoul, 서버 commit·사용자 기록 시각은 UTC. 기록 시각과 실제 복용 시각을 혼동하지 않는다.
- 미래 날짜 체크를 막는다. 오늘·과거 체크와 취소는 허용하고 변경 이력을 유지한다.
- 계획 수정은 다음 날부터 새 schedule version에 적용한다. 오늘·과거 occurrence와 기록을 다시 계산해 덮지 않는다.
- 관리 종료는 새 체크와 알림을 막는다. 기존 record와 당시 occurrence snapshot은 유지한다. 재시작은 새 계획으로 등록한다.
- 알림 끄기는 계획 종료가 아니다. 요청 설정과 OS 권한·실제 전달 상태를 분리한다.
- 미기록 시점의 변경이 종료와 경쟁하면 서버 transaction 순서가 판정한다. 종료 이후 commit된 새 체크를 거부한다.

## 쓰기와 실패

1. 유효한 변경은 명령 snapshot과 새 operation ID를 만든다.
2. 같은 ID·같은 정규화된 의미 입력는 같은 결과를 반환하고 추가 effect를 만들지 않는다.
3. 같은 ID·다른 payload는 409. expected version 불일치는 409이며 최신 값을 확인하고 새 명령을 만든다.
4. 업무 변경·record history·최소 작업 결과는 같은 transaction에서 commit한다. 작업 결과만 남거나 기록만 남는 부분 성공을 허용하지 않는다.
5. timeout·응답 유실은 실패 확정이 아니다. 결과 미확인으로 표시하고 operation 조회/같은 요청 재시도.
6. 사용자 재시도는 원래 ID를 보존한다. 앱 재시작에는 보관한 ID로 상태를 조회하고 명시적 재시도를 제공한다. 자동 전송·영속 offline queue는 만들지 않는다.
7. offline 신규 쓰기는 첫 MVP에서 금지한다. 읽기 캐시의 시각·stale 상태를 명시한다.

## 체성분·증상

측정값이 없는 이유(기기 미지원/미측정/참고 자료 없음)를 구분하며 차트 결측 구간을 임의 연결하지 않는다.
단위가 다른 값을 섞어 추이를 계산하지 않는다. AI 해석은 검증된 제공자 계약이 없으면 합성 시연 문구로만 사용한다.
증상 입력·질문·기록 UI와 의학적 판단 엔진을 분리한다. 가족 언급은 가족 계정 열람 권한을 의미하지 않는다.

테스트 예: 한국 자정 직전/후, 윤일, 기간 양 끝, 요일·중복 시각, 미래 체크, 종료·체크 경쟁, 일정 수정 뒤 과거 유지, 재시작 중복, version conflict.
