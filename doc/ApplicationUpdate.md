# 앱 업데이트

설정의 앱 업데이트 영역에서 현재 버전과 최신 정식 버전을 확인한다. **업데이트 확인**은 사용자가 누를 때만 조회한다. **업데이트 후 재시작**은 다운로드·SHA-256 확인·설치 준비와 정상 종료 후 교체를 수행한다. 조회·다운로드는 취소할 수 있고 **릴리스 열기**는 공식 릴리스 목록을 기본 브라우저에서 연다. 시작 시 자동 조회·설치와 베타 포함 설정은 제공하지 않는다.

## 배포 원본

원본은 현재 공식 저장소 `EomTaeWook/DiademMacro`의 `https://api.github.com/repos/EomTaeWook/DiademMacro/releases/latest`로 고정한다. 기존 `AppPlayerMacro`·`EmulatorMacro` 주소는 이 저장소로 이전되어 있다. 공개 API에 사용자 인증 토큰이나 임의의 업데이트 주소를 전달하지 않는다.

게시된 정식 릴리스만 사용하며 `draft`와 `prerelease`는 제외한다. 태그는 `3.1.0` 또는 `v3.1.0`과 같은 세 부분 버전이어야 한다. 현재 앱의 `AssemblyInformationalVersion`보다 높은 버전만 설치하며 같은 숫자의 사전 공개 버전에서 정식 버전으로의 전환은 허용한다.

Windows x64 자산 이름은 `Macro-{tag}-win-x64.exe` 또는 `Macro.exe`다. 해당 이름과 일치하는 자산은 하나여야 하며 업로드 상태, 크기, 공식 저장소의 정확한 다운로드 경로와 `sha256:<해시>` digest를 확인한다. ZIP이나 digest 없는 자산은 자동 교체하지 않는다. 조회 실패·정식 릴리스 없음·최신 버전·설치 불가 사유를 구분해 표시한다.

같은 릴리스의 Mac 배포 파일은 `Macro-{tag}-osx-arm64.zip`, `Macro-{tag}-osx-x64.zip`과 각 파일의 `.sha256`이다. ZIP에는 자체 포함 `Diadem Macro.app` 전체를 넣고 Mac 메타데이터와 실행 권한을 보존한다. Mac 자동 업데이트는 ZIP 자산의 GitHub `sha256:` digest를 사용하며 digest가 없는 자산은 자동 설치하지 않는다. Mac ZIP과 해시 파일은 Windows 자동 업데이트 자산 선택 대상이 아니다.

App의 DI 등록에서 Windows 빌드는 `WindowsApplicationUpdatePlatform`, Mac 빌드는 `MacOSApplicationUpdatePlatform`을 `IApplicationUpdatePlatform`으로 주입한다. 대상 프레임워크별로 반대 플랫폼 구현을 컴파일에서 제외하고 기존 Dignus Injectable 등록을 사용한다. 공통 서비스는 릴리스 조회·버전 비교·업로드 상태·공식 다운로드 경로·크기·SHA-256 확인을 담당한다. 플랫폼 구현은 자산 이름·자동 설치 가능 조건·보조 사본 준비·교체·복원·재시작을 담당한다. Windows는 기존 EXE 자동 업데이트와 개발 출력 차단 조건을 유지하며 Mac은 실행 중인 앱 프로세스의 ARM64·x64에 맞는 ZIP을 내려받아 `.app` 전체를 교체한다. 배포 파일명과 릴리스 게시 방식은 변경하지 않는다.

## 단일 실행 파일

`Macro.App/Properties/PublishProfiles/WindowsX64.pubxml`을 사용한다. 자체 포함 Windows x64 단일 파일, 네이티브 라이브러리 추출, 내장 디버그 정보와 트리밍 비활성화를 설정한다. 기본 SDK publish 출력의 `Macro.exe`가 배포 파일이며 별도 영구 패처는 제공하지 않는다. 기존 다중 파일 Release 복사 후처리 대신 SDK 게시 흐름을 사용한다.

`PublishSingleFile=true`일 때 `MacroPublishedSingleFile` 어셈블리 메타데이터를 생성한다. 자동 설치는 이 표시, 지원 CPU, 디버거 상태, 실제 실행 경로와 개발용 `Macro.dll`·`Macro.deps.json`의 부재를 확인한다. Windows는 x64 EXE, Mac은 macOS 14 이상 ARM64·x64의 `.app/Contents/MacOS/Macro` 배포 실행을 허용한다. Mac AppTranslocation 실행 경로는 차단하고 앱을 일반 설치 폴더로 옮겨 실행하도록 안내한다. `dotnet.exe`와 일반 IDE 출력을 교체하지 않는다.

필수 템플릿 JSON·로그 기본 설정·라이선스를 실행 파일에 포함한다. 단일 파일 배포는 내장 템플릿과 로그 기본 설정을 읽고 일반 개발 실행은 기존 외부 파일을 우선 사용한다. 사용자 `config.json`, 저장 경로, 이미지와 로그 파일은 기존 소유자와 저장 정책을 유지한다. 배포 버전은 프로젝트의 `Version`을 원본으로 사용하고 정식 태그와 일치시킨다.

## 다운로드와 종료

Bough와 Eidolon의 다운로드·검증·보조 프로세스·승인·교체 흐름을 사용한다. 파일은 OS 임시 폴더의 `Macro/application-update/<GUID>`에서 준비한다. 다운로드한 전체 크기와 SHA-256이 일치하는 Windows `verified.exe` 또는 Mac `verified.zip`만 설치 준비에 전달한다. 취소와 실패 정리는 해당 요청이 만든 고정 파일·폴더에만 적용한다.

Mac 임시 폴더는 `realpath`로 정규화해 `/var`와 `/private/var` 별칭 때문에 보조 실행 파일의 고정 경로 확인이 어긋나지 않게 한다. [.NET의 Mac 실행 경로 구현](https://github.com/dotnet/runtime/blob/main/src/native/minipal/getexepath.c)도 실제 경로를 반환한다. 경로 비교는 Mac에서 대소문자를 구분하고 Windows는 기존 비교를 유지한다.

매크로 실행, 옵션 편집이나 캡처·입력 선택이 진행 중이면 설치를 시작하지 않는다. 설치 준비를 시작하면 새 매크로 실행과 편집을 막는다. 설정 화면이 닫히면 현재 조회·다운로드를 취소하고 실제 종료와 정리를 기다린다. 준비한 파일을 재시작 담당에게 넘긴 뒤의 화면 종료는 설치 취소나 검증 파일 삭제 사유가 아니다.

설치 담당은 검증 파일과 현재 실행 파일의 임시 보조 사본을 준비한다. Windows는 `helper.exe`, Mac은 `ditto`로 메타데이터를 보존한 `helper.app` 전체를 준비한다. Mac 복사 작업 중 취소는 복사 프로세스가 끝날 때까지 기다린 뒤 그 요청의 파일을 정리한다. 두 플랫폼의 보조 프로세스는 `--macro-update-helper` 진입점에서 Avalonia·템플릿·사용자 설정·로그를 초기화하지 않는다. 원래 PID·시작 시각·실행 경로·기존 실행 파일 해시를 확인하고 준비 완료를 알린다.

기존 MainWindow 닫기 흐름으로 설정의 비동기 작업과 매크로 종료, 광고 창과 UI 자원을 정리한다. MainWindow의 정리가 완료되고 Avalonia 데스크톱 수명이 정상 반환한 뒤에만 `Program`이 설치를 승인한다. 보조 프로세스는 승인 nonce와 원래 프로세스의 실제 종료를 모두 확인한다. 강제 프로세스 종료·권한 상승·재부팅 예약은 하지 않는다.

## 교체·복원과 사용자 데이터

교체 직전에 고정 경로·링크 여부·기존 실행 파일과 후보의 해시를 다시 확인한다. Windows는 설치 폴더 안의 고유 임시 파일을 사용해 `File.Replace`로 원자적으로 교체하고 기존 파일을 백업한다. Mac은 검증 ZIP의 경로 이탈·심볼릭 링크·파일 수·압축 해제 크기를 확인한 뒤 `ditto`로 압축을 푼다. `.app`의 번들 ID·실행 파일 이름·버전·최소 OS·Mach-O CPU·실행 권한과 `codesign --verify --deep --strict` 결과를 확인한다. Mac 설치 폴더에 새 번들을 복사하고 같은 검사를 거친 뒤 `renamex_np(RENAME_SWAP)`으로 기존 앱과 한 번에 맞바꾼다. 교환을 지원하지 않는 파일 시스템이나 쓰기 권한 부족은 원본을 유지한 실패로 처리한다.

Mac의 번들 교환은 [Apple의 RENAME_SWAP 정의](https://github.com/apple-oss-distributions/xnu/blob/main/bsd/sys/stdio.h)와 [볼륨 지원 문서](https://developer.apple.com/documentation/foundation/urlresourcevalues/volumesupportsswaprenaming)를 따른다. 번들 서명 검사는 [Apple 코드 서명 문서](https://developer.apple.com/library/archive/technotes/tn2206/)의 검증 명령을 사용하며 재서명·quarantine 제거·권한 상승을 하지 않는다.

Windows의 새 프로세스 시작 또는 Mac의 `open` 재실행 요청에 실패하면 원본을 복원해 다시 실행한다. 복원 실패 시 백업은 보존한다. Mac은 LaunchServices의 `open -n -a`로 설치한 `.app`을 연다. 새 앱 전체 기능의 정상 시작을 확인하는 별도 건강 상태 검증은 포함하지 않는다.

Windows 재실행은 원래 작업 디렉터리를 유지한다. Mac은 기존 시작 정책대로 사용자 Application Support/DiademMacro를 사용한다. 상대 `SavePath`와 `config.json` 위치를 보존하며 교체용 임시 파일과 백업은 사용자 데이터 폴더가 아닌 설치 위치에 둔다. 재실행된 앱은 보조 프로세스 종료 뒤 해당 요청 폴더를 정리하고 설정을 연다. 교체·재실행 요청 실패로 원본이 재실행되면 업데이트 영역에 실패를 표시한다.

## 소유권과 생성 데이터

App의 `Infrastructure/Updates`가 조회·다운로드·파일·설치·보조 진입점을 소유하고 `Infrastructure/Models/Updates`가 버전과 해당 요청 데이터를 보관한다. `ApplicationUpdateRestartPresenter`가 MainWindow와 설치 인계를 연결한다. Core는 제품 버전과 업데이트 구현을 참조하지 않는다. 업데이트 버튼은 앱 공통 `UIManager` 큐를 사용하며 별도 업데이트 작업 큐·저장소·설정 파일을 만들지 않는다. 취소와 창 닫기는 실행 중 요청에 즉시 취소를 전달한다.

보조 프로세스는 원본 예외와 스택을 해당 요청 폴더의 `helper-error.log`에 기록한다. 준비 중 종료되면 설치 담당이 이 내용을 내부 예외에 연결한다. 교체·복원·재시작 중 실패한 기록은 재실행된 앱이 로그 준비 후 읽어 앱 진단에 남긴 뒤 요청 폴더와 함께 정리한다. 앱은 이 정리 Task도 종료 시 기다린다. 보조 진입점에서 Avalonia·사용자 설정·앱 로그를 초기화하지 않는 경계는 유지한다.

`AppUpdateViewModel`은 설정 영역의 요청·진행·취소를 소유하고 `AppUpdateView`가 입력을 연결한다. 문구 원본은 `Excel/Label.xlsx`, `Excel/Message.xlsx`이며 공식 `ExportTools/ExcelToJson`으로 JSON을 생성한다. 기존 템플릿 필드 정의는 유지한다. 기존 VersionCheck 설정 값은 파일 읽기·쓰기 호환을 위해 남기며 자동 조회를 실행하지 않는다.

별도 영구 패처는 제공하지 않는다. 기존 Windows ZIP 릴리스에서 전환하는 사용자는 단일 EXE를 수동으로 내려받아 설정·저장 데이터를 유지하고 실행한다. Mac 기존 배포본에는 자동 교체 코드가 없으므로 이 기능이 포함된 `.app`으로 한 번 수동 교체한 뒤부터 상위 버전 자동 업데이트를 사용한다. 릴리스 게시와 배포 산출물 생성은 사용자 요청 범위에서 수행하며 앱 실행·업데이트 설치 재현과는 구분한다.

2026-11-03 플랫폼 DI 분리와 Mac 자동 교체는 구현만 수행했다. 이 변경의 빌드·테스트·앱 실행·업데이트 설치와 릴리스 게시를 수행하지 않았다. Mac 실제 교환·서명 검사·재시작·복원·권한 유지와 Windows 기존 업데이트 회귀는 미검증이다.

2026-11-04 사용자 배포 요청으로 3.4.0의 Windows SDK 게시와 Mac ARM64·x64 게시·패키징을 완료했다. 세 자산은 같은 앱 소스 4719820을 사용한다. 버전·배포 형식·Mac 바이너리 CPU·SHA-256을 확인했으며 Mac 컴파일 경고와 실행 검증의 한계는 [릴리스 기록](releases/3.4.0.md)에 둔다. 이번 게시에서 앱 실행·업데이트 교체·복원 테스트는 수행하지 않았다.
