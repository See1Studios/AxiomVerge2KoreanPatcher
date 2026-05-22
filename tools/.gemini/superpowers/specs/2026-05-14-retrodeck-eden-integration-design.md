# RetroDECK ES-DE Switch Emulator (Eden) Integration Design

- **Date:** 2026-05-14
- **Topic:** ES-DE Alternative Emulators 목록에 Eden 추가

## 1. 목적
RetroDECK 초기화 이후, EmuDeck 의존성을 제거하고 독립적인 `Eden.AppImage`를 사용하여 ES-DE UI 상에서 Switch 에뮬레이터를 선택하고 실행할 수 있도록 설정합니다.

## 2. 핵심 아키텍처
RetroDECK의 커스텀 시스템 기능을 활용하여 기본 시스템 구성을 유지하면서도 원하는 에뮬레이터를 추가합니다.

- **Config File:** `/run/media/deck/SD/retrodeck/ES-DE/custom_systems/es_systems.xml`
- **Emulator:** `/run/media/deck/SD/retrodeck/tools/Eden.AppImage`
- **ROM Path:** `/run/media/deck/SD/retrodeck/roms/switch`
- **Data Path:** `/run/media/deck/SD/retrodeck/storage/eden_data` (향후 래퍼 스크립트로 처리 가능)

## 3. 구현 상세
`es_systems.xml`에 다음과 같은 구조를 추가하여 ES-DE가 `Eden`을 대체 에뮬레이터로 인식하게 합니다.

```xml
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

## 4. 성공 기준
1. ES-DE 실행 시 'switch' 시스템이 활성화됨.
2. [Other Settings] -> [Alternative Emulators] 메뉴에서 `Eden`이 선택 가능함.
3. 게임 실행 시 `Eden.AppImage`가 호출됨.

## 5. 리스크 관리
- **Flatpak Sandbox:** RetroDECK은 Flatpak으로 실행되므로 SD 카드 경로(`run/media/deck/...`)에 대한 접근 권한이 필요합니다. (RetroDECK 기본 권한으로 해결됨)
- **Settings Conflict:** `~/.config/eden` 전역 설정과 충돌할 수 있으나, 일단은 실행 확인을 우선합니다.
