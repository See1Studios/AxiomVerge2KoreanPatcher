# Axiom Verge 2 한글화 구현 계획서 (BepInEx 기반)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Axiom Verge 2의 실행 파일을 수정하지 않고 BepInEx 모드 로더를 사용하여 런타임에 한글 텍스트와 폰트를 주입합니다.

**Architecture:** BepInEx 5.x (x86)를 기반으로 Harmony 후킹을 사용하여 `TitleContainer.OpenStream` 메서드를 가로챕니다. 이를 통해 게임이 내부 리소스를 요청할 때 외부 폴더의 한글 데이터를 반환하도록 리다이렉션합니다.

**Tech Stack:** BepInEx 5.4.x (x86), Harmony, C#, .NET Framework 4.x, Python (for build/patch scripts)

---

### Task 1: BepInEx 5.x 환경 구축 (Steam Deck/Linux)

**Files:**
- Create: `BepInEx/` (directory and core files)
- Create: `winhttp.dll` (proxy)
- Modify: Steam Launch Options

- [ ] **Step 1: BepInEx 5.x (x86) 바이너리 준비**
  - BepInEx 5.4.21 x86 버전을 다운로드하여 게임 루트 폴더에 압축 해제합니다.
  - (`winhttp.dll`, `doorstop_config.ini`, `BepInEx/` 폴더가 생성되어야 함)

- [ ] **Step 2: BepInEx 구성 설정**
  - `BepInEx/config/BepInEx.cfg` 파일을 생성하기 위해 게임을 한 번 실행하거나, 기본 설정 파일을 생성합니다.
  - 로그 창 활성화: `[Logging.Console]` 섹션의 `Enabled = true` 확인.

- [ ] **Step 3: Steam 시작 옵션 적용**
  - 스팀의 시작 옵션에 다음 명령어를 추가합니다: `WINEDLLOVERRIDES="winhttp=n,b" %command%`

- [ ] **Step 4: 로드 확인**
  - 게임을 실행하고 `BepInEx/LogOutput.log` 파일이 생성되며 BepInEx 버전 정보가 찍히는지 확인합니다.

---

### Task 2: 한글 리다이렉션 플러그인 (KoreanPatch) 제작

**Files:**
- Create: `src/KoreanPatch.cs` (C# 소스 코드)
- Create: `build_plugin.sh` (컴파일 스크립트)

- [ ] **Step 1: Harmony 후킹 코드 작성**
  - `TitleContainer.OpenStream`을 후킹하여 파일명을 가로채고, `./BepInEx/plugins/KoreanPatch/Text/`에 해당 파일이 있으면 그 파일의 `FileStream`을 반환하도록 작성합니다.

```csharp
using System.IO;
using BepInEx;
using HarmonyLib;
using Microsoft.Xna.Framework;

[BepInPlugin("com.deck.axiomverge2.korean", "Axiom Verge 2 Korean Patch", "1.0.0")]
public class KoreanPatch : BaseUnityPlugin {
    void Awake() {
        var harmony = new Harmony("com.deck.axiomverge2.korean");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(TitleContainer), "OpenStream")]
class OpenStreamPatch {
    static bool Prefix(string name, ref Stream __result) {
        string customPath = Path.Combine(Paths.PluginPath, "KoreanPatch", name);
        if (File.Exists(customPath)) {
            __result = new FileStream(customPath, FileMode.Open, FileAccess.Read);
            return false; // 원본 메서드 실행 안 함
        }
        return true; // 원본 메서드 실행
    }
}
```

- [ ] **Step 2: 컴파일 및 빌드**
  - `FNA.dll`과 `BepInEx` 라이브러리를 참조하여 `KoreanPatch.dll`을 빌드합니다.
  - (스팀 덱에 컴파일러가 없을 경우, 사전 빌드된 바이너리를 사용하거나 원격 빌드 환경을 제안합니다.)

---

### Task 3: 폰트 및 번역 데이터 적용

**Files:**
- Create: `BepInEx/plugins/KoreanPatch/Text/Dialogue.csv`
- Create: `BepInEx/plugins/KoreanPatch/Content/Fonts/NotoSansMonoCJKJpRegular16pt.xnb`

- [ ] **Step 1: 한글 지원 폰트 이식**
  - 기존 `Content/Fonts/NotoSansMonoCJKJpRegular16pt.xnb`와 동일한 이름을 가진 한글 지원 폰트 에셋을 `KoreanPatch` 폴더 구조에 맞게 배치합니다.

- [ ] **Step 2: 한글 번역 데이터(Dialogue.csv) 배치**
  - 아까 수정한 `Dialogue_mod.csv`를 `Dialogue.csv`로 이름을 바꿔서 `Text/` 폴더에 넣습니다.

---

### Task 4: 최종 검증 및 패키징

- [ ] **Step 1: 인게임 확인**
  - 언어를 일본어(日本語)로 선택했을 때 한글 텍스트와 폰트가 정상 출력되는지 확인합니다.

- [ ] **Step 2: 배포 패키지 구성**
  - `BepInEx/`, `winhttp.dll`, `doorstop_config.ini`, `install_guide.txt`를 포함한 압축 파일을 만듭니다.
