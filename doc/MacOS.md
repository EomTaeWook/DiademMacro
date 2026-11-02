# macOS 구현 경계

## 범위

Windows의 기존 캡처·메시지 입력·이미지 처리·저장 호환을 유지한다. macOS 14 이상에서는 같은 Avalonia 화면과 이벤트 실행·검색·저장 경로를 사용하고 창 조회·캡처·입력·권한·웹 호스트만 OS별 구현으로 연결한다. 일반적인 DI 전환이나 MVP 재설계는 하지 않는다.

## 플랫폼 선택과 값

Core의 IDesktopPlatform과 IScreenCaptureManager를 소비자 생성자에 주입한다. 기존 IMouseInput·IKeyboardInput도 유지한다. Windows 대상에서는 기존 Win32 구현을, net10.0 macOS 대상에서는 Apple 구현을 컴파일·등록한다. 별도 이벤트 저장소·프로세스 캐시·이벤트 ID를 만들지 않는다. Process의 PID와 기존 IntPtr 창 값은 각 OS의 HWND 또는 CGWindowID를 나타낸다.

Windows 이미지는 기존 System.Drawing.Bitmap을 유지한다. macOS 대상의 Bitmap 별칭은 BGRA 픽셀을 소유하는 PortableBitmap이며 기존에 승인된 Avalonia 디코더·PNG 인코더를 사용한다. Utils의 macOS 이미지 구현에 한해 Avalonia 이미지 의존성을 채택하며 Core의 컨트롤러·모델은 Avalonia 창이나 UI 객체를 참조하지 않는다. PNG 파일·save.dll JSON 속성·해시 이미지 이름과 config.json은 기존 소유자를 유지한다.

## macOS 네이티브 경계

MacOSNative는 제품에 포함하는 libDiademMac.dylib의 C ABI만 호출한다. Swift 구현은 AppKit·ApplicationServices·CoreGraphics·ScreenCaptureKit·WebKit의 공개 API를 사용한다. Xcode 도구로 macOS에서 네이티브 라이브러리를 빌드하며 Mac 배포 프로필은 .app 안에 실행 파일과 라이브러리·Info.plist를 배치한다.

ScreenCaptureKit의 SCScreenshotManager가 창·화면 PNG를 반환한다. 콜백 데이터는 콜백 중에 관리 메모리로 복사하며 완료·취소·오류와 네이티브 요청의 GCHandle 해제를 구분한다. 캡처 요청은 UI 큐에서 실제 작업을 기다리고 취소 이후 늦은 이미지를 모델에 반영하지 않는다. macOS는 표시 중인 창을 대상으로 하며 Windows 최소화 DWM 동작을 지원한다고 표시하지 않는다.

Mac 창·모니터 위치와 기록된 마우스 좌표는 CoreGraphics의 논리 화면 좌표다. 캡처 선택 영역·ROI·이미지 검색 좌표는 캡처 픽셀을 사용하며 선택 영역에는 Avalonia RenderScaling을 적용한다. 캡처 화면 크기와 창 크기의 비율로 검색 좌표를 물리 입력 좌표로 변환한다. 모니터 배열의 열거 순서로 화면을 대응시키지 않는다.

설치된 Avalonia.Native 12.1.4의 저장소 커밋은 2d79ba55fae1b1e71c69f93a668080c9029e6a31이다. 해당 [Mac 화면 구현](https://github.com/AvaloniaUI/Avalonia/blob/2d79ba55fae1b1e71c69f93a668080c9029e6a31/native/Avalonia.Native/src/OSX/Screens.mm)은 NSScreen의 논리 frame을 Bounds로 전달하고 Screen Scaling은 1로 둔다. 따라서 Mac 오버레이 크기를 Windows처럼 RenderScaling으로 다시 나누지 않는다. 이것은 API 소스 근거이며 Retina 기기 실행 검증은 아니다.

macOS 입력은 사용자가 허용한 접근성 권한으로 실제 포인터·키보드 이벤트를 전송한다. Win32 백그라운드 메시지 입력을 지원한다고 가장하지 않는다. 창 활성화와 대상 확인 후 물리 입력을 수행하고 취소·실패에도 누른 버튼·수정 키를 해제한다. 화면 캡처 권한과 입력 권한은 OS가 소유하며 앱이 자동 승인하거나 설정 파일에 권한을 저장하지 않는다.

## 검증과 배포

배포는 사용자가 지정한 기존 3.3.1 릴리스에 Windows EXE와 Mac .app ZIP을 함께 게시한다. Windows 산출물은 로컬 SDK 게시로 만들고 Mac ARM64·x64 산출물은 수동 실행하는 GitHub Actions의 macOS 러너에서 만든다. 버전은 App 프로젝트를 원본으로 사용한다. 기존 태그를 강제로 이동하거나 새 릴리스를 만들지 않는다.

게시 산출물 생성과 앱 실행 검증은 구분한다. 앱 시작·권한 승인·Retina/다중 모니터·Unity 입력·구형 저장 파일 호환과 업데이트 설치는 직접 검증하지 않는다. 기존 Windows 자동 업데이트 계약은 유지하고 macOS 자동 교체 설치를 제공한다고 표시하지 않는다.

## 데이터와 권한 UI

Mac 시작 시 사용자 Application Support/DiademMacro를 작업 디렉터리로 정하고 그 아래에 config.json·기본 Save·logs를 둔다. Windows는 기존 위치를 유지한다. 경로 결합은 OS 구분자를 사용한다. 기존 Windows 설정의 절대 경로를 Mac 경로로 임의 변환하지 않는다.

설정의 macOS 권한 카드는 Mac에서만 표시한다. 입력 제어와 화면 캡처를 각각 OS에 요청하고 창이 다시 활성화되면 실제 권한 상태를 조회한다. 권한은 Config에 저장하지 않는다. 문구 원본은 Excel/Label.xlsx이며 공식 ExcelToJson 도구로 생성한다. Mac의 Mouse·Image·RelativeToImage와 같은 이미지 드래그는 실제 포인터를 사용한다. 기존 HardClick 직렬화 값과 Windows에서의 의미는 유지한다. LWIN/RWIN은 Mac Command, CTRL은 Control, ALT는 Option으로 연결한다. 미지원 키는 오류로 처리한다.

## Mac 개발 환경의 게시 경로

Mac과 .NET 10 SDK, macOS 14 SDK와 Swift 5.9 이상을 제공하는 Xcode 도구가 필요하다. App 프로젝트를 Mac에서 빌드하면 Swift 소스가 대상 아키텍처의 libDiademMac.dylib로 컴파일되고 실행 파일 옆에 복사된다. Windows에서는 Mac 네이티브 빌드를 실행하지 않는다.

Mac에서 다음 프로필 중 하나로 게시한다. GitHub Actions의 Publish macOS packages 워크플로도 같은 프로필을 사용하며 산출물을 Actions artifact로 보관한다. 릴리스 업로드는 이 워크플로에서 자동 수행하지 않는다.

```sh
dotnet publish Macro.App/Macro.App.csproj -c Release -r osx-arm64 -p:PublishProfile=MacOSArm64
dotnet publish Macro.App/Macro.App.csproj -c Release -r osx-x64 -p:PublishProfile=MacOSX64
```

게시 타깃은 자체 포함 실행 파일과 네이티브 라이브러리를 Diadem Macro.app/Contents/MacOS 아래로 복사하고 Info.plist에 버전·앱 식별자를 설정한다. 마지막 서명은 로컬 ad-hoc 서명이며 Developer ID 서명·공증·Gatekeeper 통과나 권한 유지가 검증된 배포물이 아니다. .app 전체가 배포 단위이며 ditto로 Mac 메타데이터와 실행 권한을 보존하는 ZIP을 만든다. 게시 명령은 앱을 실행하지 않는다.

3.3.1의 Mac ARM64·x64 게시·패키징은 [Actions 실행 41436121067](https://github.com/EomTaeWook/DiademMacro/actions/runs/41436121067)에서 성공했다. Windows와 Mac 배포 코드 커밋은 a0d0dfc24588d92c96b9b3b8900c5ad06190bc38이며 기존 태그는 이동하지 않는다. Mac ZIP의 해시, 필수 .app 파일과 앱 실행 파일의 CPU 타입은 확인했다. 앱 실행이나 권한 승인·캡처·입력의 동작 검증은 수행하지 않았다. 자산별 해시는 [3.3.1 릴리스 기록](releases/3.3.1.md)에 둔다.
