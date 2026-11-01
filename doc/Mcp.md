# 로컬 MCP 이벤트 구성

## 범위와 원본

`https://github.com/EomTaeWook/Mcp.Core.git`를 최상위 `Mcp.Core` 서브모듈로 참조한다. JSON-RPC·HTTP·요청 액터는 공통 Core가 담당하고 이벤트 편집·이미지·UI·저장·앱 수명은 Diadem이 담당한다. 공통 Core에는 기존 텍스트 결과와 호환되는 PNG 이미지 결과만 추가한다.

일반 실행에서 MCP는 닫힌 상태로 시작한다. 설정의 MCP 영역에서 포트를 입력해 열기·닫기를 요청하거나 `--mcp` 인수로 시작 시 열 수 있다. 포트 원본은 기존 Config의 `McpPort`이며 기본값은 8192, 허용 범위는 1024~65535다. 기존 설정 파일에 항목이 없으면 기본값을 사용하고 설정 복사·저장에도 같은 값을 전달한다. 시작 인수 `--mcp-port=8193`은 해당 시작에서 저장된 포트를 덮어쓰며 파일을 수정하지 않는다. 설정의 열기는 입력한 포트를 즉시 사용하고 기존 저장 버튼은 다음 실행을 위한 포트를 저장한다. 원격 주소는 바인딩하지 않는다. 선택적 `DIADEM_MCP_TOKEN` 환경 변수는 Core의 Bearer 인증에 전달하고 설정 파일이나 로그에 저장하지 않는다.

설정의 버튼도 기존 UIManager 큐에서 서버의 실제 시작·종료를 기다린다. 상태와 열린 서버의 주소는 MacroMcpService가 현재 서버에서 읽어 제공하고 서버 상태 변경을 설정 화면에 알린다. 설정 창을 닫아도 서버는 유지하며 이벤트 편집은 설정 창을 닫은 뒤 허용한다. 설정을 다시 열면 현재 상태를 표시하고 구독은 창 종료 때 해제한다. 서버의 열림 상태는 사용자 설정 파일에 저장하지 않는다. 앱 종료는 기존 MCP 종료 경계를 유지한다.

서버가 열려 있으면 포트 입력은 잠그고 실제 서버의 포트를 표시한다. 닫은 뒤 수정하고 다시 열어 적용한다. 다른 포트로 열린 서버를 재사용한 것처럼 응답하지 않으며 UI와 시작 경계에서 포트 범위를 확인한다. 설정의 저장과 열기는 빈 값·정수가 아닌 입력도 거절한다.

## 이벤트와 저장 소유권

`EventEditingService` 하나가 현재 이벤트 목록과 기존 `FileService`·`CacheDataManager`를 연결한다. UI ViewModel과 MCP는 같은 ObservableCollection을 사용한다. 별도 MCP 저장 파일·이벤트 번호·캐시를 만들지 않으며 기존 `SavePath`, `save.dll`, `img` 파일과 구형 바이너리 읽기를 유지한다.

UI의 생성·복사·삭제·드래그 이동·저장을 이 경계로 연결한다. MCP 설정 수정은 현재 모델의 복사본에 입력을 적용하고 모두 유효할 때 기존 모델에 반영한다. 변경 뒤 같은 저장 경로에 저장한다. 저장 실패는 적용된 메모리 변경과 저장 실패를 구분해 반환하며 성공으로 보고하지 않는다.

## 실행 문맥과 수명

MCP 핸들러는 Dispatcher를 거쳐 기존 UIManager 큐에서 실제 완료를 기다린다. 접수 성공을 작업 성공으로 반환하지 않는다. 접수 후 취소된 작업은 실행 전에 취소를 확인한다. 매크로 실행·캡처·옵션 편집·설정·업데이트·종료 중에는 변경·캡처·검색 요청을 거절한다. 상태·이벤트 목록 조회는 종료를 제외하고 허용한다.

캡처·NCC는 UI 큐가 작업 순서를 소유한 상태로 백그라운드에서 실행하고 실제 완료를 기다린다. UI와 MCP가 같은 Bitmap을 동시에 처리하지 않는다. 앱 종료는 MCP 접수를 중단하고 요청에 취소를 전달한 뒤 UI 작업과 매크로의 실제 종료를 기다리고 이미지·프로세스를 해제한다.

## 공개 작업

- 상태·대상 프로세스·이벤트 트리 조회.
- 등록 이미지와 대상 창의 PNG 조회. 대상은 실제 프로세스 ID로 지정한다.
- 이미지 파일 또는 대상 창의 캡처 영역으로 이벤트 생성.
- 이벤트 타입·체크 상태·물리 클릭·키보드·마우스·반복·딜레이·유사도·바로 이동·ROI 설정 변경.
- 부모·자식 이동과 형제 순서 변경. 자기 자신·자손 아래로의 이동은 거절한다.
- 이벤트와 자식 삭제, 현재 구성 저장.
- 기존 이미지·ROI·유사도 계산을 사용하는 단발 검색. 마우스·키보드 입력이나 매크로 반복 실행은 수행하지 않는다.

이벤트 ID는 기존 `ItemIndex`다. 부모 `0`은 최상위 목록, 위치 `-1`은 끝에 추가를 뜻한다. 입력 좌표는 MCP 창 캡처 이미지의 픽셀 기준이며 화면 좌표와 혼용하지 않는다. 부모·자식은 순차 AND이며 형제 전체를 하나의 AND로 결합하지 않는다. 실시간 화면이나 검색 성공을 지속적인 실행 성공으로 해석하지 않는다.

## 도구와 설정 입력

| 도구 | 입력과 결과 |
| --- | --- |
| `diadem_get_status` | 실행·편집 가능 상태와 최상위 항목 수 |
| `diadem_list_processes` | 현재 창이 있는 프로세스의 ID·이름·제목 |
| `diadem_list_events` | 기존 번호와 설정, `children`으로 중첩한 트리 |
| `diadem_get_event_image` | `event_id`의 PNG와 크기 |
| `diadem_capture_window` | `process_id` 창의 PNG와 크기; 활성화·클릭 없음 |
| `diadem_create_event` | `process_id`, `image_path` 또는 `capture_region`, 선택적 `parent_id`·`settings` |
| `diadem_update_event` | `event_id`, 부분 `settings`, 선택적 `image_path`·`process_id` |
| `diadem_delete_event` | `event_id`와 모든 자식을 구성에서 삭제; 이미지 파일 삭제 없음 |
| `diadem_move_event` | `event_id`, `parent_id`, 선택적 `position` |
| `diadem_save_events` | 기존 경로로 저장 완료 결과 |
| `diadem_search_event` | `event_id`, `process_id`; 유사도·임계값·위치·`input_sent: false` |

`settings`에는 `event_type`, `enabled`, `physical_click`, `keyboard_command`, `after_delay_ms`, `similarity`, `repeat_type`, `repeat_count`, `next_event_id`, `same_image_drag`, `max_drag_count`, `relative_position`, `mouse`, `roi`, `clear_roi`를 제공한다. 생략한 필드는 유지한다. 이벤트 타입·반복 타입·마우스 타입은 기존 enum의 이름을 사용하며 `Max`는 받지 않는다.

`relative_position`과 마우스 `point`·`points` 원소는 `{ "x": 0, "y": 0 }` 형식이다. `mouse.type`은 `None`, `LeftClick`, `RightClick`, `Drag`, `Wheel` 중 하나다. `capture_region`과 `roi`는 `{ "left": 0, "top": 0, "width": 100, "height": 50 }` 형식이다. `clear_roi: true`와 `roi`를 한 요청에 함께 보내지 않는다. MCP ROI는 설정 당시 시스템 DPI를 저장해 캡처 픽셀 좌표가 기존 DPI 조정 경로에서 보존되도록 한다.

키보드 이벤트도 이미지가 먼저 일치해야 실행되는 기존 계약을 유지한다. 자식은 부모가 검색 중인 같은 프로세스 창에서 실행된다. 다른 프로세스 이름을 자식에 저장했다고 독립된 창을 검색하는 조건으로 바뀌지 않는다.

변경 성공은 `applied: true, saved: true`를 반환한다. 저장 실패는 `isError`와 `applied: true, saved: false`를 반환하며 같은 생성·삭제 요청을 반복하지 않고 저장을 재시도하도록 설명한다. 잘못된 입력은 복사본에서 거절하며 원본을 갱신하지 않는다. 이미 반영된 메모리 상태를 저장 실패나 응답 취소만으로 자동 복원하지 않는다.

## 검증 경계

최초 구현에서는 빌드·테스트·앱 실행·MCP 연결 재현과 배포를 수행하지 않았다. 이후 요청된 3.3.0 배포에서는 단일 EXE 게시 산출물을 생성한다. 앱 실행·MCP 연결 재현과 테스트는 수행하지 않으며 도구 응답의 완료·저장·취소·실패 의미는 구현 계약이다.

설정의 MCP 열기·닫기와 포트 변경은 3.3.1 배포 대상에 포함한다. 배포를 위한 게시 산출물 생성과 앱 실행·MCP 연결·포트 재바인딩의 런타임 검증은 구분한다. 이 변경의 앱 실행·연결 테스트는 수행하지 않았다.
