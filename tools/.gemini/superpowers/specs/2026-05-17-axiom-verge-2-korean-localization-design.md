# Axiom Verge 2 한글화 프로젝트 설계 문서 (BepInEx Runtime Hooking)

- **날짜:** 2026-05-17
- **상태:** 최종안 (Approved)
- **작성자:** Gemini CLI (deck)

## 1. 목적 및 범위
본 프로젝트의 목적은 Axiom Verge 2의 강력한 보안 무결성 검증을 우회하여 전체 한글화를 달성하는 것입니다. 원본 실행 파일(`exe`)을 훼손하지 않는 **런타임 메모리 가로채기(Hooking)** 방식을 사용하여 기술적 안정성과 업데이트 대응력을 확보합니다.

### 대상 범위
- 모든 게임 내 대사 (Dialogue)
- 아이템, 스킬, 업그레이드 명칭 및 설명
- 메뉴 및 시스템 UI 텍스트
- 한글 지원 비트맵 폰트 적용

## 2. 기술적 접근 방식 (BepInEx + Harmony)

### 2.1 모드 로더 (BepInEx 5.x)
- **버전:** Axiom Verge 2(32비트) 호환을 위해 BepInEx 5.x (x86) 사용.
- **주입 방식:** `winhttp.dll` 프록시를 통한 런타임 주입.
- **리눅스/스팀 덱 설정:** 스팀 시작 옵션에 `WINEDLLOVERRIDES="winhttp=n,b" %command%` 필수 적용.

### 2.2 텍스트 가로채기 (Harmony Hooking)
- **대상 메서드:** 
    - `Microsoft.Xna.Framework.TitleContainer::OpenStream`
    - 또는 리소스를 불러오는 `GetManifestResourceStream` 관련 내부 로직.
- **동작:** 게임이 특정 리소스(예: `Dialogue.xml`)를 요청할 때, 플러그인이 중간에서 개입하여 외부 `./BepInEx/plugins/KoreanPatch/Text/` 폴더의 파일을 반환하도록 함.

### 2.3 폰트 런타임 교체
- **동작:** 게임 로딩 시 일본어/중국어 폰트 자산을 한글 글리프가 포함된 사용자 정의 폰트 자산(`.xnb`)으로 메모리상에서 교체.

## 3. 작업 단계 (Implementation Roadmap)

### Phase 1: BepInEx 환경 구축
- BepInEx 5.x x86 다운로드 및 게임 폴더 배치.
- 로그 창 활성화를 통해 정상 로드 여부 확인.

### Phase 2: 한글화 전용 플러그인 제작
- Harmony를 이용한 리소스 리다이렉션 C# 코드 작성.
- 외부 CSV 파일 로딩 기능 구현.

### Phase 3: 폰트 및 데이터 적용
- 한글 지원 Noto Sans 폰트 에셋 제작 및 적용.
- 추출된 `Dialogue.csv`를 일본어 슬롯 기준으로 한글화하여 테스트.

### Phase 4: 패키징 및 배포
- `BepInEx` 폴더와 `winhttp.dll`을 포함한 압축 패키지 구성.
- 스팀 덱 시작 옵션 가이드 포함.

## 4. 자가 검토 (Spec Self-Review)
1. **Placeholder scan:** 모든 도구(BepInEx, Harmony)와 대상이 명확함.
2. **Internal consistency:** 바이너리 패치의 한계를 인정하고 런타임 훅으로 선회하여 일관성 확보.
3. **Scope check:** 배포 목표에 가장 적합한 '비파괴적 패치' 방식을 채택함.
4. **Ambiguity check:** 스팀 덱 특유의 시작 옵션 설정을 명시하여 혼동 방지.
