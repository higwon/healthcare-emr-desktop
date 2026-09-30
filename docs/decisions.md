# 설계 결정 기록

2026-09-30. 아래 ADR은 설계 기준 PR 검토 대상이다. 구현 가능성 spike를 통과한 뒤 확정 상태를 갱신한다.

## ADR-001 Windows Client와 서버 런타임

제안: Desktop .NET Framework 4.8 WPF, Domain/Application netstandard2.0, API .NET 10.
이유: 채용 요구의 .NET Framework·WPF를 직접 다루면서 서버 런타임을 분리한다. 로컬에는 4.8 참조 어셈블리와 .NET10 SDK가 있다.
비교: net481은 별도 targeting pack·지원 OS 차이 검토가 필요하고, 현대 .NET WPF는 개발 도구가 좋지만 Framework 경험 증거가 약해진다.
검증: 패키지의 net48/netstandard2.0 지원, net48 테스트 실행기, CI targeting pack, 설치 선행 조건, 지원 Windows 목록.
Microsoft 출처: https://learn.microsoft.com/dotnet/framework/get-started/system-requirements 및 https://learn.microsoft.com/dotnet/core/releases-and-support.
SDK 패치 번호를 개발자 PC 값으로 임의 고정하지 않고 HC-002에서 CI와 함께 선택한다.

## ADR-002 MVVM과 UI 라이브러리

제안: 기본 WPF + 자체 ResourceDictionary/Control, CommunityToolkit.Mvvm의 net48 호환 버전 후보.
상용 대형 UI library에 의존하지 않는다. Toolkit은 MVVM 기반 코드 도구이며 제품 UI 구현을 대신하지 않는다.
검증: 호환 TFM·C# generator·command 취소·예외·CI 검증. generator가 맞지 않으면 라이브러리 command/ObservableObject 또는 최소 수동 기반을 선택한다.
수동 AsyncCommand를 고르더라도 예외 처리·CanExecute·취소·중복 실행·수명 테스트를 먼저 둔다.
대안과 선택 패키지 버전은 호환성 spike 이후 기록한다. 이전 POS 패키지 규칙을 그대로 복사하지 않는다.

## ADR-003 서버 영속성과 재시도

제안: 첫 탐색 API는 versioned 합성 fixture 조회. 복약 단계에서만 SQLite 로컬 데모 서버 + plan/record/최소 작업 결과 transaction, 작업 조회, stable operation ID를 사용한다.
서버가 저장의 권위자다. UI check와 HTTP 202는 완료 증거가 아니다.
응답 유실은 작업 조회로 확인한 후 같은 command를 재시도한다. 409는 자동 덮어쓰기·무한 retry를 금지한다.
운영 다중 서버·분산 DB·실제 OAuth·개인정보 보관은 현재 범위가 아니다.
검증: 저장 전 실패, commit 후 응답 유실, 재시작 조회·중복·다른 payload 재사용·DB 실패.
범위 축소: canonical hash를 필수 구현으로 고정하지 않고 정규화 입력 비교를 사용한다. 서버 범용 receipt framework·분산 멱등성·offline outbox·자동 재시작 전송은 제외한다. UI가 미확인 결과를 정직하게 보여주는 데 필요한 최소 서버만 만든다.

## ADR-004 초기 Custom Control과 측정

제안: HC-301 Timeline Control은 기본 WPF container recycling을 먼저 검증하는 templated ItemsControl 계열, HC-302 Trend Control은 자체 drawing 계열을 후보로 한다. 외부 Chart/UI library는 사용하지 않는다.
Measure/Arrange·DP invalidation·selection/focus·hit testing·Automation·DPI를 각 Control PR에서 다룬다.
virtualization이 실제 동작하는지 먼저 측정하고, container 비용이 병목일 때만 custom rendering 또는 paging 조정을 선택한다.
오픈소스 WPF 모듈 분석은 관련 구현 PR에서 primary source·license·차이·재구현 근거를 남긴다. 분석 대상을 아직 선정하지 않았다.
검증: [탐색 명세](health-data-exploration.md), [측정 기준](performance-case-studies.md), HC-104/301/302. rendering 경로·downsampling·성능 예산은 실제 baseline 후 확정한다.

## ADR 변경 규칙

상태(제안/확정/대체), 문제·비교·결정·제약·검증·관련 Task/PR을 함께 남긴다.
실험 코드를 작성했다고 설계 확정으로 표시하지 않는다. 관련 계약·Task·README도 함께 갱신한다.
