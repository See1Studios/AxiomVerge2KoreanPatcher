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

# ── 인수 검증 ─────────────────────────────────────────────
if [ -z "$GAME_DIR" ]; then
    echo "오류: 게임 폴더 경로를 인수로 지정하세요."
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
# TODO: 실행파일 구조 확인 후 구현 필요
#
# Linux 실행파일 유형에 따라 아래 중 하나:
#   A) .NET 6+ 단일 실행파일 (self-contained):
#      dotnet 도구를 이용해 임베디드 리소스 교체
#
#   B) Mono 어셈블리 (AxiomVerge2.exe / AxiomVerge2.dll):
#      mono / monodis 를 이용하거나
#      dotnet-script + Mono.Cecil 로 처리
#
#   게임 폴더 구조를 확인 후 이 섹션을 완성하세요.
#
# 예시 (구조 파악 후 교체):
# EXE=$(find "$GAME_DIR" -maxdepth 1 -name "AxiomVerge2*" -not -name "*.sh" | head -1)
# echo "  대상 실행파일: $EXE"
echo "  ⚠️  실행파일 주입은 아직 구현되지 않았습니다."
echo "     게임 폴더 구조를 확인 후 스크립트를 완성하세요."

echo ""
echo "=== 완료 ==="
echo "폰트 패치가 적용되었습니다."
echo "번역 주입은 수동으로 완료해야 합니다 (위 TODO 참고)."
