# Axiom Verge 2 한글화 기술 실증 및 작업 가이드 (Final POC)

## 1. 개요
본 문서는 Axiom Verge 2의 강력한 무결성 검증을 우회하고, 픽셀 아트 엔진에 최적화된 고퀄리티 한글 출력을 달성한 기술적 성과를 기록한다.

## 2. 핵심 기술적 발견
### A. 데이터 주입 (Data Injection)
*   **파일 구조:** `AxiomVerge2.exe` 내부에 `OuterBeyond.EmbeddedContent.Content.zip` 리소스로 원본 데이터가 존재함.
*   **우회 전략:** 실행 파일의 코드(IL)를 수정하면 실행이 차단되므로, `Mono.Cecil`을 사용하여 내부 **리소스 섹션의 데이터만 교체**함.
*   **언어 매핑:** 게임 내 '일본어(Japanese)' 컬럼을 한글 데이터의 저장소로 활용함.

### B. 폰트 시스템 (Font System)
*   **엔진 특성:** FNA/MonoGame 기반이며, `SpriteFont` 객체와 자체 `THFont`(비트맵 방식)를 혼용함.
*   **성공 공식:** 
    1.  규격이 검증된 외부 `SpriteFont`의 **YAML 설정**을 확보 (Stardew Valley 폰트 활용).
    2.  해당 YAML의 캐릭터 맵(Unicode 순서)에 맞춰 **명품 픽셀 폰트(PNG)**를 정밀 도색.
    3.  `XNBExtract`를 통해 다시 패킹하여 게임에 주입.

### C. 가독성 교정 (Refinement)
*   **자간(Kerning):** 각 글자의 실제 픽셀 폭을 측정하여 YAML의 `Vector3` 데이터(Y값)에 강제 주입하여 조밀한 가독성 확보.
*   **수직 정렬(Baseline):** 글자마다 다른 박스 높이를 무시하고, 모든 글자를 특정 수평선(Baseline = Ascent 기준) 위에 배치하여 삐뚤어짐 현상 해결.

## 3. 적용된 폰트 라인업
*   **8pt (작은 글씨):** 갈무리 7 (Galmuri 7)
*   **12pt (일반 대화):** 갈무리 11 (Galmuri 11)
*   **16pt (타이틀/메뉴):** PF 스타더스트 3.0 (PF Stardust)

## 4. 작업용 도구 및 스크립트 (C:\Users\parkj\.gemini\tmp\axiom-verge-2\)
*   `Patcher/Program.cs`: EXE 리소스 교체기 (Mono.Cecil 기반)
*   `stabilize_font.py`: 폰트 정렬 및 자간 교정 이미지 생성기
*   `XNBExtract/`: XNB 분해/조립 도구

## 5. 향후 대사 번역 시 주의사항
1.  **UTF-8 BOM:** 모든 CSV 파일은 반드시 UTF-8 BOM 인코딩으로 저장되어야 함.
2.  **일본어 컬럼:** 번역된 한글은 반드시 CSV의 9번째 컬럼(Japanese)에 배치함.
3.  **캐릭터 범위:** 현재 폰트는 한글 상용 2,350자를 포함하므로, 특수 고어 등은 추가 작업이 필요할 수 있음.
