import csv
import io

translations = {
    "START": "시작",
    "OPTIONS": "옵션",
    "SPEEDRUN": "스피드런",
    "Demo": "데모",
    "Made with FMOD Studio by Firelight Technologies Pty Ltd.": "Firelight Technologies Pty Ltd의 FMOD Studio로 제작되었습니다.",
    "Follow on Twitter! @AxiomVerge": "트위터(@AxiomVerge)를 팔로우하세요!",
    "Follow on Facebook! fb.com/AxiomVerge": "페이스북(fb.com/AxiomVerge)을 팔로우하세요!",
    "LOAD GAME": "게임 불러오기",
    "COPY FROM?": "어디에서 복사할까요?",
    "COPY TO?": "어디로 복사할까요?",
    "DELETE WHICH?": "어떤 것을 삭제할까요?",
    "ITEM": "아이템",
    "MAP": "지도",
    "New Game": "새 게임",
    "COPY": "복사",
    "DELETE": "삭제",
    "EXIT": "종료",
    "Press UP to save.": "위 버튼을 눌러 저장하세요.",
    "Game saved.": "게임이 저장되었습니다.",
    "Save failed.": "저장에 실패했습니다.",
    "Are you sure you want to permanently delete this save?": "이 저장 데이터를 영구적으로 삭제하시겠습니까?",
    "Are you sure you want to overwrite this save?": "이 저장 데이터를 덮어쓰시겠습니까?",
    "Choose Difficulty": "난이도 선택",
    "Normal": "보통",
    "Hard": "어려움",
    "GAMEPAD CONFIGURATION": "게임패드 설정",
    "KEYBOARD CONFIGURATION": "키보드 설정",
    "WEAPON SELECT UI STYLE": "무기 선택 UI 스타일",
    "Linear": "선형",
    "Ring": "링형",
    "LANGUAGE": "언어",
    "English": "영어",
    "Français": "프랑스어",
    "Deutsch": "독일어",
    "Italiano": "이탈리아어",
    "Português": "포르투갈어",
    "Español": "스페인어",
    "Русский": "러시아어",
    "日本語": "한국어",
    "简体中文": "중국어(간체)",
    "SPEEDRUN MODE": "스피드런 모드",
    "START NEW": "새로 시작",
    "CONTINUE": "계속하기",
    "DIFFICULTY": "난이도"
}

def translate_csv(input_path, output_path):
    with open(input_path, 'r', encoding='utf-8-sig') as f:
        reader = csv.reader(f)
        header = next(reader)
        rows = [header]
        for row in reader:
            if len(row) > 8:
                english_text = row[1]
                # Simple lookup for now, will expand to AI translation for more complex rows
                translated = translations.get(english_text, english_text)
                row[8] = translated # Replace Japanese column
            rows.append(row)
            
    with open(output_path, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.writer(f)
        writer.writerows(rows)

translate_csv('C:/Users/parkj/Documents/superpowers/specs/Content/Text/UI.csv', 'C:/Users/parkj/Documents/superpowers/specs/Content/Text/UI_Korean.csv')
print("Successfully translated UI.csv to UI_Korean.csv")
