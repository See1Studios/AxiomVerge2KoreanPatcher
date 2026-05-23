# Changelog

## [Unreleased]

### 추가
- 번역 편집기(Translation Editor) 내장 — CSV를 앱 내에서 직접 편집 가능
- **폰트 빌드 모드** 추가
  - **원본 병합 (Append)** — 원본 글자를 보존하고 한국어 글리프를 하단에 병합
  - **완전 대체 (Replace)** — 원본 글리프를 전부 커스텀 폰트로 교체
- **원본 문자표 뷰어** — Extract Originals 후 원본 폰트의 charset.txt를 버튼 클릭으로 확인
- **빌드본 문자표 뷰어** — Build 후 생성된 charset.txt를 버튼 클릭으로 확인
- **빌드본 텍스처 뷰어** — Build 후 생성된 병합 PNG를 기본 이미지 뷰어로 열기
- `Extract Originals` 버튼 — EXE 경로 유효 시에만 활성화
- 패치 단계 표시 pill (Step 1→2→3) UI
- 각 폰트 슬롯에 XOffset, XAdvance 조정 컨트롤 추가
- Linux 패치 패키지 자동 생성 (`ExportPackage/Content.zip` + `Fonts/`)
- `(원본 유지)` 폰트 선택 옵션 — 해당 슬롯을 패치하지 않고 원본 복원
- 다크/라이트/시스템 테마 전환

### 변경
- 폰트 디렉터리 구조 개편
  - `Fonts/Originals/` — 원본 XNB 백업 및 언팩 파일
  - `Fonts/Korean/` — 커스텀 한국어 폰트
- 리소스 단일화: `resources/Fonts/Korean/`, `resources/Tools/`, `resources/Translations/`
- GDI+ 렌더링 정밀도 개선 — `NearestNeighbor` + `GraphicsUnit.Pixel` 적용으로 픽셀 아트 폰트 1px 깨짐 방지
- 템플릿 베이스라인 Y를 중앙값으로 자동 계산하여 한글 수직 정렬 기준 통일
- `Extract Originals` — 항상 백업 XNB에서 언팩 (이미 패치된 상태에서도 원본 텍스처 정확히 복원)
- 빌드된 문자표 파일명 수정 (`._charset.txt` → `_charset.txt`)

### 수정
- 폰트 슬롯 확장: **`Uni0553_6pt.xnb`** 추가
  - 스피드런 설명 텍스트에 사용되는 폰트로, 원본에 한글 글리프가 없어 한글이 전부 `?`로 표시되던 문제 해결
- `Moire16pt.xnb` 슬롯 제거 (실제 게임 내 한글 텍스트에 미사용)

---

## 이전 변경사항

### 2026-05-22 — WPF 리팩터 및 리소스 통합
- WPF 패처 전면 리팩터
- 리소스를 `resources/` 단일 폴더로 통합
- `Fonts/Originals/` 도입 (게임 폴더에서 분리)
- `ExportPackage/` Linux 패치 준비 자동화
- `AV8ptMonogame.xnb` 슬롯 제거 (CJK 텍스트 미사용)

### 초기 커밋
- 기본 WPF 패처 구현 (CSV 주입 + XNB 폰트 교체)
