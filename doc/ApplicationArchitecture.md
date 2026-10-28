# AppPlayerMacro 구성

## 적용 기준

현재 사용자 지시와 [작업 규칙](WorkingRules.md)을 우선하며 `D:\Source\Paper-Toss\Blueprints`를 규칙 원본으로 사용한다. [공통 컨벤션](../../Paper-Toss/Blueprints/ReusableArchitecture/Common/CodingConvention.md), [협업 규칙](../../Paper-Toss/Blueprints/ReusableArchitecture/Common/Collaboration.md), [일반 앱 아키텍처](../../Paper-Toss/Blueprints/ReusableArchitecture/Application/ApplicationArchitecture.md)와 [아키텍처 리뷰 스킬](../../Paper-Toss/Blueprints/.agents/skills/reusable-architecture-review/SKILL.md)을 따른다. Bough에서는 SDK 프로젝트와 App/Core 및 Shared 솔루션 구성을 참고한다. 화면 배치와 기능은 기존 매크로 화면을 기준으로 한다.

기존 ViewModel·Controller 연결을 유지하며 별도 MVP 전환, 저장소와 캐시는 도입하지 않는다. 버튼 작업은 기존 `UIManager`의 UI 스레드 큐에서 접수 순서대로 처리한다. 기존 UI의 서비스 접근·알림 경계를 유지하고 Core에서 App을 참조하던 부분은 생성자 주입과 캡처 창 핸들 전달로 연결한다. 새 코드에는 nullable 비활성화, 블록 네임스페이스와 단일 타입 파일을 적용한다. Avalonia 이름 생성기는 비활성화하고 코드비하인드에서 XAML 로드와 컨트롤 연결을 수행한다. 앱 업데이트와 단일 실행 파일 배포는 [앱 업데이트](ApplicationUpdate.md)를 따른다.

## 솔루션과 책임

| 프로젝트 | 책임 | 참조 |
| --- | --- | --- |
| `Macro.App` | Avalonia 시작, 화면·창 수명, 지역화, 후원·웹 표시, 정식 릴리스 조회·다운로드·설치·재시작 | Core, DataContainer, Utils |
| `Macro.Core` | 매크로 실행, 키보드·마우스 입력, 이미지 검색·캡처, 설정·트리거 모델, 저장 파일 읽기·쓰기 | DataContainer, Utils |
| `DataContainer` | 기존 Excel 생성 템플릿과 로더 | .NET |
| `Utils` | Win32 연동, 공용 좌표·상수, JSON·이미지 파일 변환 | .NET |

`Macro.slnx`는 App/Core를 최상위에 두고 DataContainer와 Utils를 Shared에 둔다. Core는 Avalonia와 App을 참조하지 않는다. 대상은 .NET 10이며 캡처·입력·이미지와 내장 웹 표시 때문에 Windows 전용이다. 실행 파일의 어셈블리 이름은 `Macro`를 유지한다.

Windows x64 자체 포함 단일 실행 파일은 `WindowsX64.pubxml`과 SDK 게시 흐름으로 구성한다. 필요한 템플릿·로그 기본 설정은 내장하며 사용자 설정과 저장 데이터는 기존 정책을 유지한다. 배포 산출물 생성과 릴리스 게시는 사용자 요청 범위에서 수행한다.

## 시작과 실행

`Program`은 전용 업데이트 보조 인수를 먼저 처리하며 일반 실행에서 `TemplateDataLoader`로 생성 템플릿을 준비한 뒤 Avalonia 수명을 시작한다. `TemplateDeserializer`는 내장 JSON을 생성 템플릿으로 역직렬화한다. Debug는 실행 파일 옆 `Datas` JSON을 우선하고 Release는 App assembly의 `Macro.Datas.*` 내장 리소스만 사용하며 외부 JSON을 출력·게시하지 않는다. `App.Initialize`가 로그와 XAML을 준비하고 프레임워크 초기화 완료 경계에서 기존 설정과 App/Core DI를 연결한다. `MainWindow`가 화면과 UI 코루틴 타이머를 연결한다. 기존 언어 라벨은 App의 `LabelLocator`와 템플릿 확장 메서드가 표시한다. 정상 UI 종료 후의 업데이트 승인은 Program에서 처리한다.

App·Core assembly는 각각 한 번 자동 등록한다. `FileService`, 키보드·마우스 입력 구현과 기존 수동 등록 ViewModel은 attribute로 Transient 수명을 선언한다. 입력 구현은 기존 인터페이스로 등록한다. 일반 앱의 수동 등록은 설정 인스턴스·데스크톱 수명·업데이트 HTTP 클라이언트·배포 환경처럼 bootstrap에서 준비한 값에 한정한다. 공개 타입·namespace와 enum 값은 유지하며, 이벤트 인수·프로토콜·enum·변환기는 타입별 파일에 배치한다. `FileService`와 `CoroutineRunner`의 파일은 기존 namespace에 맞는 디렉터리에 둔다.

실행 입력은 `MainWindow`에서 `MacroExecutionController.Start`로 전달한다. 실행기는 생성자에서 받은 순회·배치 컨트롤러 중 설정에 맞는 것을 사용한다. 캡처와 입력은 기존 실행 순서와 취소 토큰을 유지한다. 상태와 미리보기는 Avalonia UI Dispatcher로 전달하며, 중지와 창 종료는 실행 Task의 종료를 기다린다.

`MacroExecutionController.Completion`은 실제 실행 Task를 노출하며 메인 창은 완료·실패를 await한 뒤 실행 표시를 갱신한다. `StopAsync`는 취소를 요청하고 해당 Task가 종료된 뒤 취소 토큰 소스를 정리한다. 실행기가 조회한 Process 인스턴스는 실행기에서 해제하며 선택 화면에서 받은 고정 Process는 빌려 사용한다. 순회·배치 캡처와 배치 복제 이미지의 소유 범위는 처리 메서드이며, ROI 검색에서 원본 이미지가 반환되면 그 원본은 해제하지 않는다. 미리보기 callback은 전달받은 Bitmap을 호출 중에 Avalonia 이미지로 복사하고 UI Dispatcher에는 복사본을 전달한다. 앱 종료는 기존 `CoroutineRunner`의 실행 Task도 기다린 뒤 창을 닫고 기존 위치에서 코루틴을 해제한다.

메인·설정·옵션·업데이트의 버튼 작업과 폴더 선택은 `UIManager.EnqueueButtonAction`으로 연결한다. 큐는 UI 실행 문맥에서 한 작업의 Task 완료를 기다린 뒤 다음 작업을 처리한다. 대기 중 선택·입력이 바뀌지 않도록 큐 상태에 따라 메인의 프로세스·목록, 설정 입력과 옵션 입력을 잠근다. 큐 상태 구독은 창을 닫을 때 해제한다. 매크로 시작은 실행을 시작하고 큐를 반환하며 별도 완료 관찰 Task가 실행·정리 완료를 기다린다. 창 열기는 표시까지 큐에서 처리하고 기존 창 수명을 유지한다. 중지·업데이트 취소·창 닫기와 알림 대화상자의 확인은 진행 중인 작업을 끝낼 수 있는 신호로 즉시 전달한다. 위·아래 이동의 반복 작업도 같은 큐를 사용하며 포인터 해제는 반복을 즉시 중단한다.

매크로 실행 중에는 이벤트 목록과 편집 버튼을 잠그며 편집·드롭 처리 경로도 같은 실행 상태를 반영한다. 두 실행 모드는 프로세스·이벤트·바로가기 순회에서 취소를 확인한다. 설정 검증이나 파일 저장에 실패하면 오류를 표시하고 설정 창을 유지한다. 종료를 시작하면 새 버튼 작업을 받지 않고 대기 중인 클릭을 버리며, 열린 팝업과 실행 중인 비동기 작업을 취소·종료한 뒤 큐·매크로·코루틴의 실제 완료를 기다린다.

후원·광고 HTTP 조회는 `WebApiManager.Request`에서 Task를 반환하고 `AdManager`와 메인 창까지 await로 연결한다. 조회 실패의 기존 default/null 응답과 후원 미확인·빈 광고 URL 분기는 유지한다. 실제 HTTP 응답과 창 수신 동작은 확인하지 않았다.

이미지·이미지 상대좌표 이벤트에서 기존 `HardClick`을 선택하면 `InputEventExecutor`가 캡처 이미지 크기와 대상 루트 창의 위치로 화면 좌표를 계산한다. 대상 창이 최소화됐거나 계산된 클릭 지점이 다른 창에 가려졌으면 클릭을 진행하지 않는다. 커서는 화면 픽셀 좌표로 이동하고 기존 `MouseInput`의 `SendInput`으로 누름·해제를 전달한 뒤 원래 커서 위치로 돌린다. 같은 이미지 드래그에서는 물리 클릭을 함께 선택하지 않는다. 표시 중인 Unity 창의 물리 클릭은 사용자 실행에서 확인했다.

화면 스타일은 `App.axaml`과 기존 트리 스타일에서 정의한다. 메인 창은 프로세스·실행 제어와 이벤트 편집 도구를 구분하며 목록에는 실제 항목 수와 빈 목록 안내를 표시한다. 내장 이미지 미리보기는 항상 표시하고 별도 표시 설정은 두지 않는다. 설정의 일반 영역에 있는 로그 폴더 버튼은 현재 작업 디렉터리의 `logs` 폴더를 생성한 뒤 탐색기로 연다. 옵션·설정 창은 관련 입력을 묶고 스크롤 영역 밖에 저장 버튼을 배치한다. 기존 컨트롤 이름·바인딩·이벤트 연결과 ViewModel 책임을 유지하며 추가 문구는 `Excel/Label.xlsx`에서 생성한다.

최소화되지 않은 창은 Core의 `ScreenCaptureManager`가 Windows Graphics Capture의 `CreateForWindow(HWND)`로 우선 캡처한다. 캡처 프레임은 BGRA `SoftwareBitmap`을 `System.Drawing.Bitmap`으로 복사하며, WGC가 지원되지 않거나 프레임 획득에 실패하면 기존 `PrintWindow` 경로를 사용한다. 최소화 창은 WGC 대상에서 제외하고 기존 DWM 썸네일 경로를 유지한다. DWM 캡처에 필요한 숨김 창은 App의 `ApplicationManager`가 소유하며, 창을 표시한 뒤 `ScreenCaptureManager.SetDrawWindowHandle`에 네이티브 핸들을 전달하고 종료 때 해제한다. Core는 창 객체에 접근하지 않는다. 옵션 창과 메인 창의 알림 구독은 창 종료에 연결한다.

메인 창은 다음 실행에 사용할 컨트롤러와 현재 실행 중인 컨트롤러를 구분한다. 설정 저장으로 다음 실행용 참조가 교체되어도 중지·종료는 실행 중인 인스턴스를 기다린다. 실행 표시의 해제는 해당 인스턴스의 완료를 기다리는 시작 경로에서 담당하고, 중지 버튼은 취소 요청과 진행 표시를 담당한다.

## 저장과 의존성

설정의 기존 작업 디렉터리와 `SavePath` 정책, `save.dll` 이름, JSON 속성과 숫자형 enum, `img/img-<sha256>.png` 참조를 유지한다. `Utils.JsonHelper`가 `System.Text.Json`을 사용한다. Bitmap 파일, Drawing.Point의 기존 문자열과 enum 문자열 입력은 해당 변환기가 처리한다.

이전 바이너리 형식은 기존 순서 기반 직렬화 경로를 유지한다. Microsoft의 `System.Runtime.Serialization.Formatters` 호환 패키지와 앱 런타임 설정을 사용한다. `TypeRedirectBinder`는 기존 Macro 어셈블리와 WPF Point를 현재 Core 모델로 연결한다. 저장 데이터를 일괄 변환하거나 덮어쓰는 별도 마이그레이션은 실행하지 않는다.

OpenCV 대신 기존 `OpenCVHelper` API 안에서 .NET Bitmap, 적분 합과 SIMD를 사용하는 정규화 상관 검색을 수행한다. 검색 임계값, 다중 검색 개수, 결과 표시와 ROI 진입점은 유지한다. WebView2 대신 App의 `WebViewControl`이 .NET Windows Forms WebBrowser를 Avalonia 네이티브 컨트롤 호스트에 연결한다. 웹 엔진과 이미지 검색 구현은 바뀌었으며 웹 호환성과 검색 결과·성능의 동등성은 확인하지 않았다.

문구·템플릿 원본은 `Excel`이며 `ExportTools`의 공식 생성 도구를 사용한다. 업데이트 문구는 Label·Message 원본에 추가하고 JSON을 생성한다. 필드 정의와 생성 C# 템플릿은 유지한다.

## 현재 작업 범위

현재 최종 변경분의 빌드·테스트·앱 실행과 변경 후 정적 검증은 수행하지 않았다. 표시 중인 Unity GPU 창 캡처와 물리 클릭은 사용자 실행에서 확인했으며, 구형 바이너리 파일 읽기, 이미지 검색 성능, 최소화 DWM 캡처, 모니터 DPI와 웹 페이지 동작은 미확인이다. 릴리스 배포는 별도 사용자 요청 범위다.
