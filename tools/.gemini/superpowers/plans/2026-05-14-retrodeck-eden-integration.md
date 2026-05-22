# ES-DE Eden 통합 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** RetroDECK의 ES-DE UI 내 'Alternative Emulators' 목록에 Eden을 추가하여 Switch 게임을 실행할 수 있게 합니다.

**Architecture:** `/run/media/deck/SD/retrodeck/ES-DE/custom_systems/es_systems.xml` 파일을 수정하여 독립적인 `switch` 시스템 정의를 추가하고, `Eden.AppImage`를 직접 호출하도록 설정합니다.

**Tech Stack:** XML, Bash, RetroDECK (ES-DE)

---

### Task 1: custom_systems 디렉토리 및 파일 준비

**Files:**
- Modify: `/run/media/deck/SD/retrodeck/ES-DE/custom_systems/es_systems.xml`

- [ ] **Step 1: 기존 es_systems.xml 내용 확인 및 백업**

명령어:
```bash
cp /run/media/deck/SD/retrodeck/ES-DE/custom_systems/es_systems.xml /run/media/deck/SD/retrodeck/ES-DE/custom_systems/es_systems.xml.bak
cat /run/media/deck/SD/retrodeck/ES-DE/custom_systems/es_systems.xml
```

- [ ] **Step 2: es_systems.xml에 Eden 시스템 정의 추가**

파일 내용을 다음과 같이 수정합니다.
```xml
<?xml version="1.0"?>
<systemList>
  <system>
    <name>switch</name>
    <fullname>Nintendo Switch (Eden)</fullname>
    <path>/run/media/deck/SD/retrodeck/roms/switch</path>
    <extension>.nca .NCA .nro .NRO .nsp .NSP .xci .XCI</extension>
    <command label="Eden">/run/media/deck/SD/retrodeck/tools/Eden.AppImage %ROM%</command>
    <platform>switch</platform>
    <theme>switch</theme>
  </system>
</systemList>
```

- [ ] **Step 3: 파일 저장 및 권한 확인**

명령어:
```bash
ls -l /run/media/deck/SD/retrodeck/ES-DE/custom_systems/es_systems.xml
```

---

### Task 2: Eden AppImage 실행 권한 및 경로 확인

**Files:**
- Verify: `/run/media/deck/SD/retrodeck/tools/Eden.AppImage`

- [ ] **Step 1: AppImage 실행 권한 부여**

명령어:
```bash
chmod +x /run/media/deck/SD/retrodeck/tools/Eden.AppImage
```

- [ ] **Step 2: ROM 디렉토리 존재 확인**

명령어:
```bash
mkdir -p /run/media/deck/SD/retrodeck/roms/switch
ls -d /run/media/deck/SD/retrodeck/roms/switch
```

---

### Task 3: 최종 검증 및 적용

- [ ] **Step 1: ES-DE 설정 반영 확인**

사용자에게 RetroDECK(ES-DE)을 재시작하고 다음 메뉴를 확인하도록 안내합니다.
1. `Other Settings` -> `Alternative Emulators`
2. `switch` 시스템에서 `Eden`이 선택 가능한지 확인

- [ ] **Step 2: 테스트용 ROM 실행 시도 (선택 사항)**

만약 ROM이 있다면 실행하여 `Eden.AppImage`가 정상적으로 뜨는지 확인합니다.
