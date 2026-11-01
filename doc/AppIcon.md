# 앱 아이콘

## 제작자 공통 스타일

Skein·Bough·Eidolon의 현재 적용 아이콘을 참고해 Diadem을 같은 제품군 스타일로 표현한다. 짙은 네이비 배경과 오른쪽 아래의 크림색 제작자 마크를 공유하고, Diadem의 심볼은 금색 왕관과 재생 삼각형을 결합한다. 제작자 마크는 비스듬한 세 막대와 아래 연결선으로 구성한다.

참고 자산은 `D:\Source\Skein\Client\Skein.App\Assets\Skein.png`, `D:\Source\Bough\Bough.App\Assets\AppIcon.png`, `D:\Source\Eidolon\Eidolon.App\Assets\AppIcon.png`다. 다른 프로젝트의 파일은 변경하지 않는다.

## 적용 경로

- `Macro.App/Assets/AppIcon.png`: 내장 imagegen 도구가 생성한 최종 PNG를 그대로 사용한다. Bough와 같은 불투명 네이비 배경이다.
- `Macro.App/Assets/AppIcon.ico`: 같은 PNG로 16·20·24·32·40·48·64·96·128·256px 크기의 아이콘을 생성한다. .NET 기본 이미지 API를 사용한다.
- 프로젝트의 `ApplicationIcon`과 메인 창의 기존 ICO 경로를 유지한다. 메인 화면 왼쪽의 아이콘은 같은 PNG를 사용하며 기존 48px 크기와 둥근 모서리를 유지한다.
- README의 실제 이벤트 화면 캡처는 유지한다.

기존 PNG·ICO는 `artifacts/AppIcons/before-creator-suite-b73082b568874ccf9392bc11913880ed`에 백업했다. 생성·변환과 소스 연결을 수행했으며 앱 빌드·테스트·실행·실제 창 표시와 배포는 수행하지 않았다.

## 최종 생성 지시

내장 imagegen 도구를 사용했다. CLI/API fallback은 사용하지 않았다. 참고 이미지 순서는 Skein, Eidolon, Bough다.

```text
Use case: logo-brand. Generate a FINAL sibling icon for Diadem Macro in the same creator family as the three references: Image 1 Skein, Image 2 Eidolon, Image 3 Bough. References specify style and the shared lower-right creator signature, not subject. The entire square canvas MUST be 100% OPAQUE, one perfectly uniform solid midnight navy #172132, edge to edge. This is essential: NO transparency anywhere, NO black patches, holes, vignette, darker islands, background texture, gradient, outer border, padding in another color, or rounded-corner cutout. Like the Bough reference, opaque navy is the full background. Draw exactly two elements. Main element: a bold, beautifully balanced THREE-POINT golden amber DIADEM crown, a compact flat geometric symbol with one broad gently curved base and a right-facing navy PLAY triangle cut out at its lower center. Use at most two FLAT solid accent fills, amber #FFAA22 and pale warm gold #FFD877, crisp smooth filled shapes. Three simple crown points, strong silhouette, no jewels, no dots, no ornate flourishes, no metallic treatment, no gear, no letters. Center it slightly above the canvas midpoint, fill about 72% of width, leave an even comfortable 12% margin, keep main crown clear of lower-right maker mark. The second element MUST match the common developer signature visible in ALL references: a small warm-ivory #FFF3DA emblem at lower right, precisely three equal short parallel diagonal bars connected by a short horizontal baseline, one flat cream filled shape, about 14% of canvas width, cleanly separated from the crown. Straight-on flat 2D vector-like branding. Absolutely NO 3D, shading, extrusion, highlights, gradients, shadows, texture, glow, perspective, text, additional symbols, mockup, presentation sheet. One square finished icon, 1024 square or higher. Opaque navy at every background pixel, including inside the play triangle and all spaces around the symbols.
```
