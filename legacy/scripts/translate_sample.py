import csv
import io

def translate_dialogue_sample(input_path, output_path):
    # We use a custom parser to handle multi-line CSV correctly
    with open(input_path, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        rows = list(reader)

    header = rows[0]
    # Find Japanese column index (usually 8)
    jp_idx = header.index("Japanese")

    # Sample Translation: Intro Scroll
    for row in rows:
        if row[0] == "DLG_TITLESCROLL_0":
            row[jp_idx] = "2007년, 젊은 과학자 엘리자베스 하먼드 박사가 인류 최초의 초광속 통신 장치인 '앤서블(Ansible)'을 발명하며 세상은 혁명적인 변화를 맞이했습니다.\n\n지연 시간 없는 컴퓨팅은 곧 표준이 되었고, 하먼드 코퍼레이션은 세계적인 기업으로 자리 잡았습니다."
        elif row[0] == "DLG_TITLESCROLL_1":
            row[jp_idx] = "하지만 박사는 안주하지 않았습니다. 그녀는 앤서블의 원리를 응용해 훨씬 더 거대한 무언가를 찾기 시작했습니다."

    with open(output_path, 'w', encoding='utf-8-sig') as f:
        writer = csv.writer(f, quoting=csv.QUOTE_ALL)
        writer.writerows(rows)
    print(f"Sample Dialogue Translated: {output_path}")

csv_in = r"D:\SteamLibrary\steamapps\common\Axiom Verge 2\.gemini\superpowers\specs\Content\Text\Dialogue.csv"
csv_out = r"C:\Users\parkj\.gemini\tmp\axiom-verge-2\Dialogue_Sample.csv"

translate_dialogue_sample(csv_in, csv_out)
