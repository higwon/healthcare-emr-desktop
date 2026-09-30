# HC-101 실행·검증 가이드

2026-09-30. Task [#10](https://github.com/higwon/healthcare-emr-desktop/issues/10).
설계 PR #22 이후 새 기반 구성이다. 로컬 보류 spike의 업무 코드는 가져오지 않았다.

## 구성·선행조건

Desktop net48 WPF, Domain/Application/Infrastructure netstandard2.0, API net10.0, Desktop.Tests net48.
공유 계층은 현재 빈 assembly다. 기능/DTO/VM skeleton을 미리 추가하지 않는다.
Windows와 .NET 10 SDK, .NET Framework 4.8 호환 런타임이 필요하다. Visual Studio는 .NET 10 SDK 지원 버전을 사용한다.
SDK는 10.0.100 baseline + latestFeature(10.0 SDK feature band 내 roll-forward), CI 설치는 10.0.x.
[.NET Standard 호환성](https://learn.microsoft.com/dotnet/standard/net-standard),
[Framework 참조 어셈블리](https://learn.microsoft.com/dotnet/framework/migration-guide/reference-assemblies),
[SDK 선택](https://learn.microsoft.com/dotnet/core/tools/global-json)을 기준으로 분리했다.
참조 package는 빌드용이며 런타임 설치를 대신하지 않는다. 실제 지원 Windows matrix는 HC-401 검증 대상이다.

테스트 runner는 [MSTest 4.4.1](https://www.nuget.org/packages/MSTest.TestFramework/4.4.1)·동일 Adapter,
[Test SDK 18.10.1](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.10.1)을 net48에서 확인한다.
기존 xUnit v2/MSTest v3 후보는 신규 기반에 채택하지 않았다. 상용 UI/Chart library와 MVVM 패키지는 없다.

## 동일한 로컬·CI 검증 경로

```powershell
./scripts/verify-project-references.ps1
dotnet restore HealthNote.sln --locked-mode
dotnet build HealthNote.sln -c Release --no-restore
dotnet test tests/HealthNote.Desktop.Tests/HealthNote.Desktop.Tests.csproj -c Release --no-build --logger "trx;LogFileName=desktop.trx" --results-directory artifacts/test-results
./scripts/smoke-api.ps1
dotnet publish src/HealthNote.Api/HealthNote.Api.csproj -c Release --no-build -o artifacts/api
```

NuGet.Config는 nuget.org만 사용하고 프로젝트 lockfile을 commit한다. 의도적 패키지 변경 때만 unlocked restore로 lockfile을 갱신하고 PR에서 검토한다.
Windows CI는 clean checkout·locked restore·Release·net48 테스트·API smoke·publish를 실행하고 TRX/API 로그/응답/배포 디렉터리/desktop binaries를 artifact로 보존한다.
push main과 PR에서 실행하며 같은 브랜치 push+PR의 중복 실행을 피한다. 저장소 보호 설정은 변경하지 않았다.

## 실행

```powershell
./src/HealthNote.Desktop/bin/Release/net48/HealthNote.Desktop.exe
dotnet ./src/HealthNote.Api/bin/Release/net10.0/HealthNote.Api.dll
```

API는 http://127.0.0.1:5078 에서 /api/v1/health만 제공한다. 업무 조회·쓰기·DB·외부 연동 없음.
smoke 스크립트는 포트가 이미 사용 중이면 실패하고 기존 프로세스를 종료하지 않는다. 자체 시작한 프로세스만 정리한다.
Desktop --smoke는 눈에 보이는 제품 검토가 아닌 자동 startup 경로다. 투명/비활성 Window의 ContentRendered 뒤 0으로 종료하며 10초 watchdog는 실패로 종료한다.
현재 중앙 텍스트 Window는 실행 확인용이며 제품 UI는 HC-103에서 시안 검토 후 구현한다.

## 실제 로컬 결과·한계

Windows 11 build 26200, SDK 10.0.300, x64 환경:
- 참조 경계 검사·locked restore 통과.
- Release build: 경고 0, 오류 0.
- net48 테스트 2개: compiled XAML root의 STA Measure/Arrange, 실제 exe startup/ContentRendered/종료 통과.
- loopback readiness 응답(mode=demo/version=v1/ready=true)와 API publish 통과.
- 첫 STA 검사는 표시되지 않은 Window의 ActualWidth=0으로 실패했다. Window 크기를 억지로 기대하지 않고 compiled content root를 layout하도록 수정 후 통과.
- clean checkout remote CI 결과는 HC-101 PR checks와 artifact에서 확인한다. 로컬 성공만으로 CI 성공을 주장하지 않는다.

제품 UI·MVVM·DPI·키보드·접근성·성능·메모리 조사·설치 검증은 해당 후속 Task의 증거가 필요하다. startup smoke는 이를 대신하지 않는다.
