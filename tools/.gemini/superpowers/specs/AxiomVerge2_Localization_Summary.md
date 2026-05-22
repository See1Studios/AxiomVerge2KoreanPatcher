# Axiom Verge 2 한글화 프로젝트 요약 및 인계 문서 (Handoff Document)

## 1. 프로젝트 개요
- **목표:** Axiom Verge 2 전체 한글화 및 배포 환경 구축
- **엔진:** FNA / MonoGame (.NET C# 기반)
- **대상 파일:** `AxiomVerge2.exe` (게임 로직 및 텍스트 데이터 포함), `Content/Fonts/` (폰트 데이터)

## 2. 텍스트 및 데이터 구조 분석 결과
- **텍스트 데이터:** 외부 폴더가 아닌, `AxiomVerge2.exe` 내부에 `OuterBeyond.EmbeddedContent.Content.zip` 형태의 임베디드 리소스로 압축되어 있습니다.
- **주요 파일:** 
  - `Text/Dialogue.csv` (대사 데이터)
  - `Text/UI.csv` (메뉴 및 UI 텍스트)
- **언어 슬롯:** 기본적으로 한국어 슬롯이 없으므로, 기존의 `Japanese` 또는 `Chinese` 컬럼을 한국어로 대체하여 사용하는 것이 가장 효율적입니다.
- **폰트:** `Content/Fonts/NotoSansMonoCJKJpRegular16pt.xnb` 등 기존 폰트 에셋이 이미 다국어(CJK)를 지원하기에, 폰트 구조 호환성이 확보되어 있습니다.

## 3. 스팀 덱(Linux)에서의 실패 원인 및 교훈
- **바이너리 패치 실패:** 헥스 에디터나 파이썬 스크립트를 통한 단순 1바이트 변조나 ZIP 파일 덮어쓰기는 모두 실패했습니다. `.NET` 어셈블리의 강력한 이름 서명(Strong Name)과 무결성 검증 때문입니다.
- **경로 가로채기 한계:** BepInEx 모드 로더 주입이나 `LD_PRELOAD`와 같은 시스템 레벨 훅도 리눅스 네이티브 실행 환경 및 스팀 덱 특유의 샌드박스로 인해 적용이 어려웠습니다.
- **엔진의 한계:** 게임 내부 코드(`TitleContainer.OpenStream`)가 외부 폴더를 아예 확인하지 않고, 오직 내부 리소스(`GetManifestResourceStream`)만 호출하도록 하드코딩되어 있습니다.

## 4. 최종 해결책: 윈도우 PC + dnSpy를 이용한 코드 리다이렉션
실행 파일의 바이너리를 훼손하지 않으면서 로직만 깔끔하게 수정하기 위해, 윈도우 환경에서 .NET 디컴파일러인 **dnSpy**를 사용합니다.

### 🛠️ 작업 가이드 (윈도우 환경에서 수행)
1. **dnSpy 다운로드:** [dnSpy GitHub 릴리즈](https://github.com/dnSpy/dnSpy/releases)에서 최신 버전을 다운로드 및 실행합니다.
2. **파일 로드:** 스팀 덱에서 가져온 원본 `AxiomVerge2.exe`를 dnSpy에 드래그 앤 드롭합니다.
3. **타겟 메서드 검색:** `Microsoft.Xna.Framework.TitleContainer` 클래스의 `OpenStream(string name)` 메서드를 찾습니다.
4. **코드 수정 (Edit Method):**
   - 메서드 시작 부분에 다음 한 줄을 추가합니다.
   ```csharp
   if (System.IO.File.Exists(name)) return new System.IO.FileStream(name, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
   ```
5. **컴파일 및 저장:** Compile 후, `File -> Save Module...`을 눌러 저장합니다.

## 5. 향후 작업 (패치 적용 후)
1. 수정된 `AxiomVerge2.exe`를 게임 폴더에 덮어씌웁니다.
2. 게임 폴더 내에 `Content/Text/` 경로를 생성합니다.
3. 번역이 완료된 `Dialogue.csv`, `UI.csv` 등을 해당 폴더에 배치합니다. (인코딩: UTF-8 BOM)
4. 게임을 실행하여 한글이 정상 출력되는지 확인합니다.
