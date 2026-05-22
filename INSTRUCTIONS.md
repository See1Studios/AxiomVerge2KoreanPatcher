# Axiom Verge 2 Korean Localization Project Instructions

이 파일은 프로젝트의 연속성을 보장하기 위한 핵심 가이드라인입니다. 다른 에이전트나 세션에서 작업을 시작할 때 이 내용을 반드시 숙지하십시오.

## 1. 핵심 철학 및 지시 사항
- **High-Fidelity Hybrid:** 영문 메뉴는 원본의 가변폭 자간을 100% 유지해야 하며, 한글만 추가된 하이브리드 형식을 유지한다.
- **Repeatability:** 매 패치 시 무결성 검사를 할 필요 없이, 버튼 하나로 원본(.origin) 소스에서 재패치가 가능해야 한다.
- **Minimal Injection:** EXE에는 가벼운 CSV 데이터만 주입하고, 폰트(XNB)는 외부 폴더 로드 방식을 우선한다.

## 2. 프로젝트 구조 (Permanent Home)
- src/AV2Patcher_Modern/: 최신 하이브리드 엔진 패처 (WPF)
- data/: UI.csv, master_charset.txt 등 번역 및 폰트 데이터
- 	ools/.gemini/: xnbcli.exe, XNBExtract 등 핵심 조립 도구
- docs/memory/: 상세 기술 로그 및 히스토리

## 3. 기술적 성공 공식 (Troubleshooting)
- **DivideByZeroException 해결:**
  - JSON 내 필드명은 반드시 erticalLineSpacing을 사용해야 함 (erticalSpacing 아님).
  - 모든 글리프의 width와 kerning.y는 최소 1 이상이어야 함.
- **KeyNotFoundException 해결:**
  - 게임은 Japanese 슬롯에서 ASCII, 일본어, 한자, 특수부호를 빈번하게 호출함. 
  - data/master_charset.txt에 포함된 모든 문자가 폰트 맵에 등록되어 있어야 함.
- **EXE Crash 방지:**
  - Mono.Cecil 사용 시 FNA.dll 등 종속성 해결을 위해 게임 디렉토리를 AssemblyResolver에 추가해야 함.

## 4. 작업 프로세스 (How-To)
1. **번역:** data/UI.csv 수정 (반드시 UTF-8 BOM 인코딩 유지).
2. **폰트:** src/AV2Patcher_Modern/CustomFonts/에 PNG/JSON 업데이트.
3. **빌드:** 패처 실행 후 **Patch** 버튼 클릭. 툴이 내부적으로 하이브리드 병합과 주입을 자동 수행함.
4. **원본 보존:** AxiomVerge2.exe.origin은 건드리지 말 것.

## 5. 이전 세션 히스토리
- 기술 실증(POC) 완료 -> 하이브리드 엔진 개발 완료 -> 저장소 구조 정리 완료.
- 현재 상태: 폰트 시스템 완벽 구축됨. 대사 번역 및 추가 폰트(16pt 등) 실험 단계.
