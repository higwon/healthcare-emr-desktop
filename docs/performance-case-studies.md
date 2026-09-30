# 성능·메모리 Case Study 기준

상태: 계획·미측정. 실제 WPF 성능 문제 2~3건과 실제 profiler Memory Case Study 1건을 핵심 산출물로 삼는다.
HC-102 진단/수명 기반부터 HC-103/104/301/302/204/205 baseline·원자료를 누적하고 HC-401 앱 전체 재검증, HC-402 README에 연결한다.
각 UI Task의 DPI·keyboard/Automation·Binding·대량 데이터 측정은 Done 조건이다.

## 기록 형식

1. Problem: 행동·dataset·증상·재현 절차.
2. Hypothesis: container/binding/layout/render/query/dispatcher의 의심 경계.
3. Measurement: OS·CPU/GPU/RAM·DPI·SDK/TFM·Release·commit·fixture seed/version·warmup·반복 수·도구/설정·raw artifact.
4. Root Cause: profiler stack/allocation/retention evidence와 대안 가설 배제.
5. Solution: 수정 PR·tradeoff·기능/접근성 회귀 확인.
6. Before / After: 동일 환경/fixture/절차·분포/변동/한계·원자료 링크.
7. 재현 가이드·관련 Task/PR·README 연결.

query latency·UI 적용·첫 표시·scroll/selection·Measure/Arrange·render 비용을 구분한다.
frame/layout/render는 도구가 실제 측정하는 지표를 명시한다. HTTP latency를 frame time으로 표현하지 않는다.
초기 조사 후보: 10,000건 scroll의 container recycling, Trend resize/hover invalidation, query 결과 dispatcher 적용.
성능 예산은 첫 baseline 후 환경·대표 데이터·행동 기준으로 정한다. 목표와 측정값을 구분한다.
실제 병목을 조사한다. 결함을 일부러 넣거나 가짜 수치를 만들지 않는다. 문제 없는 baseline은 관찰 한계를 남기고 문제 해결 사례 수를 채웠다고 주장하지 않는다.

## Memory Case Study

반복 detail 열기/닫기·navigation·subject 변경·Loaded/Unloaded·지연 중 scope 종료를 재현한다.
100회 detail 반복 전후 생존 ViewModel·retained bytes·GC roots·안정화 여부가 후보이며 정상 cache와 반복 수는 baseline 후 확정한다.
event/static event·DispatcherTimer·CollectionChanged·messenger·callback·token registration의 root/ownership을 실제 profiler snapshot으로 추적한다.
강제 GC만으로 누수 없음을 입증하지 않는다. 발견 시 구독/해제·timer stop·scope 종료·generation guard 수정 전후 비교.
로그/캡처에는 의료 본문·이름·token 없이 fixture만 사용한다.

## 현재 증거

성능 문제 해결 사례 0건·Memory Case Study 미측정. 현재 PR은 문서만 포함하며 실행·프로파일링 결과를 제공하지 않는다.
