# 제품 범위와 진행 단계

2026-09-30 설계 기준 초안. 가칭 건강노트.
Healthcare Data를 탐색하고 관리하는 WPF Desktop Client로서 장시간 실행·대량 데이터·비동기 통신·Custom Control·Windows 환경 대응을 입증한다.
사용자는 합성 건강 프로필의 기록을 탐색하는 데모 이용자다. Patient/사용자 프로필은 합성 subject이며 실제 진료 업무나 권한 모델을 의미하지 않는다.
공개 소비자 서비스 조사는 도메인/context 참고다. 자체 요구사항은 이 문서·[탐색 명세](health-data-exploration.md)·Epic/Task에서 정의한다.

## 단계

1. 설계·운영 기준: 구조·도메인·API·코딩·UI·Epic/Task·PR 계획을 문서화하고 기준 PR로 검토.
2. 기반: buildable solution, CI, MVVM·lifetime·설정·테마. 이 단계에서 업무 기능을 몰래 같이 구현하지 않는다.
3. 데이터 탐색: Overview·Timeline·Trend, 합성 API async loading, Custom Control과 초기 성능·메모리 측정.
4. Medication 수직 구현: 직접 등록, 요일·시각·기간, 조회·체크·취소·종료와 제한된 실패·충돌 복구.
5. Windows 품질: 각 UI PR의 DPI·접근성·성능·수명 증거를 앱 전체에서 재검증하고 설치·데모·Case Study로 정리한다.

## MVP 경계

Overview·Timeline·Trend 조회가 우선이다. Blood Test, Health Checkup, Visit, Medication, Body Measurement, Symptom 이벤트를 합성 fixture로 탐색한다.
측정값·참고 구간은 합성 자료 표시와 출처를 동반하며 의료 판정·건강 점수·치료 권고를 생성하지 않는다.
증상 질문·체성분 입력 전용 화면은 후속으로 보류한다. Medication 단계에서는 직접 등록만 제공한다. 실제 의약품 검색·OCR·AI 판정·네이버/인바디 인증은 구현 가능성과 계약을 확인한 후 별도 Task로 추가한다.
선택 요일·하루 복수 시각·시작/종료일은 복약 MVP에 포함한다. 복용량 추천·처방 변경은 없다.
온라인 쓰기를 기본으로 하고 오프라인은 명시된 캐시 조회만 허용한다. 응답 미확인 쓰기와 새 입력을 구분한다.
실제 개인 데이터·다중 사용자 운영·인증·병원 공유는 MVP에 포함하지 않는다. 데모 서버는 loopback와 합성 데이터에 한정한다.
건강미션·종합 진단은 공식 도움말의 업데이트 예정 표기를 현재 출시 기능으로 바꾸지 않는다.

## 완료 증거

문서 링크 → Task → PR → 테스트/시각 검증 → 재현 데모를 추적한다.
UI Task마다 지원 창 크기, 100/150/200% DPI, 키보드·Automation·Binding 오류·대량 데이터·layout/rendering 측정을 Done 조건에 포함한다.
성능 Case Study 2~3건과 실제 profiler Memory Case Study 1건을 [측정 기준](performance-case-studies.md)에 따라 누적한다. 숫자를 미리 채우거나 결함을 일부러 넣지 않는다.
기능 완료와 실제 연동·운영 가능성·규제 적합성 주장을 분리한다.
지금은 브라우저 UI 시안만 검증했다. WPF 빌드·실행·테스트 완료 상태가 아니다.
