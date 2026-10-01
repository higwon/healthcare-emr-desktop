> 2026-10-01 재설계: 제품·Feature·실행 우선순위는 [Windows 헬스케어 재설계](healthcare-windows-baseline.md)를 따른다. 아래 공통 계층·런타임·C#/WPF·수명·검증 규칙은 유지하되, 이전 Feature 모델/endpoint/이슈 순서/시안 승인을 새 제품 기준으로 사용하지 않는다.

# 설계 결정 기록

2026-09-30. 설계 기준 PR #22를 병합했다. 각 ADR의 실행 검증 수준과 미결정 범위를 분리한다.

## ADR-001 Windows Client와 서버 런타임

선택(HC-101): Desktop .NET Framework 4.8 WPF, Domain/Application/Infrastructure netstandard2.0, API .NET 10.
로컬 Release·net48 STA/프로세스 startup·readiness API 검증 통과. clean checkout Windows CI는 HC-101 PR에서 확인한다. 제품 기능/지원 OS·DPI 검증을 확정한 것은 아니다.
SDK baseline은 global.json의 10.0.100 + latestFeature, CI는 10.0.x. SDK를 로컬 패치에 고정하지 않는다.
net48 테스트는 MSTest.TestFramework/TestAdapter 4.4.1 + Microsoft.NET.Test.Sdk 18.10.1, 참조 어셈블리는 Microsoft.NETFramework.ReferenceAssemblies 1.0.3. lockfile로 복원 버전을 관리한다.
이유: 채용 요구의 .NET Framework·WPF를 직접 다루면서 서버 런타임을 분리한다. 로컬에는 4.8 참조 어셈블리와 .NET10 SDK가 있다.
비교: net481은 별도 targeting pack·지원 OS 차이 검토가 필요하고, 현대 .NET WPF는 개발 도구가 좋지만 Framework 경험 증거가 약해진다.
검증: 패키지의 net48/netstandard2.0 지원, net48 테스트 실행기, CI targeting pack, 설치 선행 조건, 지원 Windows 목록.
Microsoft 출처: https://learn.microsoft.com/dotnet/framework/get-started/system-requirements 및 https://learn.microsoft.com/dotnet/core/releases-and-support.
MSTest 선택 근거·소스·재현 명령은 [실행 가이드](foundation-build.md)를 따른다. MVVM 패키지 선택은 ADR-002/HC-102에서 계속 검증한다.

## ADR-002 MVVM과 UI 라이브러리

확정(HC-102): 기본 WPF + 자체 ResourceDictionary/Control 방향을 유지하고,
CommunityToolkit.Mvvm **8.4.2**의 ObservableObject/RelayCommand/AsyncRelayCommand를 직접 사용한다.
generator·global Ioc·messenger·범용 BaseViewModel은 이번 단계에서 사용하지 않는다.
Toolkit은 MVVM 코드 도구이며 제품 UI 구현을 대신하지 않는다.

선택 이유: 검증된 notification/command 구현을 재사용하면서 별도 MVVM framework를 만들지 않는다.
수동 composition root에서 실제 readiness query·Shell·화면 수명을 연결한다.
현재의 navigation은 Shell의 화면 열기/닫기이며 INavigationService/IDialogService/IDiagnostics facade는 없다.
IUiDispatcher는 실제 background 응답의 UI apply를 위해 단일 메서드로 도입했다.
예외/중복 실행/취소/stale/Disposed guard는 라이브러리에 맡겼다고 가정하지 않고 ViewModel 상태 테스트로 검증한다.
AllowConcurrentExecutions의 이유와 UI thread 소유 계약은 [HC-102 구현 계약](mvvm-foundation.md)에 기록했다.

netstandard2.0 자산과 전이 의존성(Microsoft.Bcl.AsyncInterfaces 10.0.1 등)을 lockfile에 고정했다.
전이 패키지 버전은 Desktop runtime을 .NET 10으로 바꾼다는 의미가 아니다.
로컬 net48 Release build(경고 0·오류 0), 실제 net48 테스트 34개, compiled executable startup으로 호환성을 확인했다.
원격 Windows CI는 HC-102 PR checks에서 별도로 확인한다. generator 호환성을 검증한 것으로 확대하지 않는다.
대안: 최소 수동 command는 의존성을 줄이지만 실행/notification semantics를 직접 유지해야 한다.
UI 품질·Control은 HC-103/301/302에서 WPF 자체 구현으로 증명한다.
[공식 패키지/TFM](https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.2),
[공식 async command 동작](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand).

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
