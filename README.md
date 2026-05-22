# Axiom Verge 2 한국어 패처

**Axiom Verge 2** 의 한국어 번역 패치를 적용하는 WPF 도구입니다.  
게임 실행파일에 내장된 텍스트 리소스와 폰트 XNB 파일을 교체하여 한국어를 표시합니다.

> 현재 개발 중입니다. 폰트 렌더링 및 번역 작업이 진행 중입니다.

---

## 기능

- 게임 실행파일(`AxiomVerge2.exe`)에 번역된 CSV를 주입
- 한국어 비트맵 폰트(`.fnt` / `.png`)로 게임 폰트 XNB 교체
- 원본 파일 자동 백업 및 복원
- 폰트 슬롯별 커스텀 폰트 지정 및 오프셋 조정
- 패치 전 원본 폰트 텍스처 미리보기
- Linux 버전 패치용 패키지 내보내기 (개발 중)

---

## 사용법

### 요구사항

- Windows 10 이상
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### 패치 적용

1. **`AV2Patcher.Modern.exe`** 실행
2. **EXE 경로** 에 `AxiomVerge2.exe` 선택
3. `Extract Originals` — 원본 폰트 텍스처 추출 및 백업
4. `Translations/` 폴더에 번역 CSV 파일 배치
5. 각 폰트 슬롯에서 원하는 한국어 폰트 선택
6. `Apply Patch` 클릭

### 원본 복원

`Restore Original` 버튼을 누르면 최초 패치 시 저장된 `.origin` 파일로 되돌립니다.

---

## 커스텀 폰트 추가

`resources/CustomFonts/` 폴더에 `.fnt` + `.png` 쌍을 추가하면  
빌드 시 자동으로 패처 `CustomFonts/` 디렉터리에 복사됩니다.

BMFont 형식의 비트맵 폰트를 지원합니다.

---

## 프로젝트 구조

```
AxiomVerge2KoreanPatcher/
├── AxiomVerge2KoreanPatcher.sln
├── src/
│   └── AV2Patcher.Modern/          # WPF 패처 소스
│       ├── Core/
│       │   ├── Config.cs           # 설정 및 폰트 매핑
│       │   └── FontBuilder.cs      # XNB 빌드 / 언팩
│       ├── MainWindow.xaml(.cs)
│       └── AV2Patcher.Modern.csproj
└── resources/                      # 빌드 리소스 (단일 소스)
    ├── CustomFonts/                 # 한국어 폰트 (.fnt/.png)
    ├── Tools/                       # xnbcli (XNB 언팩 도구)
    ├── Translations/                # 번역 CSV
    └── apply_patch.sh               # Linux 패치 스크립트 (WIP)
```

---

## 개발 환경 설정

```bash
git clone https://github.com/See1Studios/AxiomVerge2KoreanPatcher.git
cd AxiomVerge2KoreanPatcher
dotnet build src/AV2Patcher.Modern/AV2Patcher.Modern.csproj
```

**요구사항**: .NET 10 SDK, Windows (WPF)

---

## 라이선스

[MIT License](LICENSE)
