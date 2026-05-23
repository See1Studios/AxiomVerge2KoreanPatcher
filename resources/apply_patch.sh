#!/bin/bash
# ============================================================
# Axiom Verge 2 Korean Patch - Linux Apply Script
# ------------------------------------------------------------
# 사용법: ./apply_patch.sh <게임_폴더_경로>
# 예시:   ./apply_patch.sh ~/.steam/steam/steamapps/common/AxiomVerge2
#
# 준비물 (이 스크립트와 같은 폴더에 있어야 함):
#   Content.zip      - Windows 패처에서 생성된 번역 ZIP
#   Fonts/           - 패치된 폰트 XNB 파일들
# ============================================================

set -e

GAME_DIR="${1}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# SteamOS / Linux 기본 설치 경로 후보 목록
DEFAULT_PATHS=(
    "$HOME/.steam/steam/steamapps/common/Axiom Verge 2"
    "$HOME/.local/share/Steam/steamapps/common/Axiom Verge 2"
    "/run/media/mmcblk0p1/steamapps/common/Axiom Verge 2"
)

# 인수가 비어있다면 기본 경로 탐색
if [ -z "$GAME_DIR" ]; then
    for path in "${DEFAULT_PATHS[@]}"; do
        if [ -d "$path" ]; then
            GAME_DIR="$path"
            echo "[정보] 기본 설치 경로가 감지되었습니다: $GAME_DIR"
            break
        fi
    done
fi

# ── 인수 검증 ─────────────────────────────────────────────
if [ -z "$GAME_DIR" ]; then
    echo "오류: 게임 폴더 경로가 감지되지 않았습니다. 인수로 직접 지정해 주세요."
    echo "사용법: $0 <게임_폴더_경로>"
    exit 1
fi

if [ ! -d "$GAME_DIR" ]; then
    echo "오류: 게임 폴더를 찾을 수 없습니다: $GAME_DIR"
    exit 1
fi

CONTENT_ZIP="$SCRIPT_DIR/Content.zip"
FONTS_DIR="$SCRIPT_DIR/Fonts"

if [ ! -f "$CONTENT_ZIP" ]; then
    echo "오류: Content.zip이 없습니다. Windows 패처에서 먼저 패치를 실행하세요."
    exit 1
fi

echo "=== Axiom Verge 2 Korean Patch (Linux) ==="
echo "게임 폴더: $GAME_DIR"
echo ""

# ── Step 1: 폰트 XNB 복사 ─────────────────────────────────
GAME_FONTS_DIR="$GAME_DIR/Content/Fonts"
if [ -d "$FONTS_DIR" ] && [ -d "$GAME_FONTS_DIR" ]; then
    echo "[1/2] 폰트 패치 중..."
    for xnb in "$FONTS_DIR"/*.xnb; do
        [ -f "$xnb" ] || continue
        fname=$(basename "$xnb")
        # 원본 백업 (최초 1회)
        if [ ! -f "$GAME_FONTS_DIR/${fname}.original" ]; then
            cp "$GAME_FONTS_DIR/$fname" "$GAME_FONTS_DIR/${fname}.original" 2>/dev/null || true
        fi
        cp "$xnb" "$GAME_FONTS_DIR/$fname"
        echo "  ✓ $fname"
    done
else
    echo "[1/2] 폰트 폴더를 찾을 수 없습니다. 건너뜁니다."
fi

# ── Step 2: 실행파일에 Content.zip 주입 ────────────────────
echo "[2/2] 번역 데이터 주입 중..."
CLI_TOOL="$SCRIPT_DIR/AV2Patcher.CLI"

if [ -f "$CLI_TOOL" ]; then
    chmod +x "$CLI_TOOL"
    echo "  실행파일 주입 도구 실행 중..."
    "$CLI_TOOL" "$GAME_DIR/AxiomVerge2.exe" "$CONTENT_ZIP"
else
    echo "오류: 실행파일 주입 도구가 없습니다: $CLI_TOOL"
    exit 1
fi

echo ""
echo "=== 완료 ==="
echo "번역 데이터 및 폰트 패치가 성공적으로 적용되었습니다!"
