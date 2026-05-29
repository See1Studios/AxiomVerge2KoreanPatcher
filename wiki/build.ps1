# build.ps1
$ErrorActionPreference = "Stop"

function Import-CsvSecure ($Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $content = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($content.Length -gt 0 -and $content[0] -eq [char]0xFEFF) {
        $content = $content.Substring(1)
    }
    return ConvertFrom-Csv -InputObject $content
}

# Use the directory where the script is located
$wikiDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrEmpty($wikiDir)) {
    $wikiDir = Get-Location
}

# Dynamically find the translation guidelines markdown file in the wiki directory
$mdFileItem = Get-ChildItem -Path $wikiDir -Filter "*용어집*.md" | Select-Object -First 1
if (-not $mdFileItem) {
    $mdFileItem = Get-ChildItem -Path $wikiDir -Filter "*.md" | Where-Object { $_.Name -notlike "README.md" } | Select-Object -First 1
}

if (-not $mdFileItem) {
    throw "No guideline markdown files found in $wikiDir"
}
$mdFile = $mdFileItem.FullName
Write-Host "Found Guideline File: $mdFile"

# Establish repo root and find official translations in resources/Translations
$repoRoot = Split-Path -Parent $wikiDir
$translationsDir = Join-Path $repoRoot "resources\Translations"
if (-not (Test-Path $translationsDir)) {
    $translationsDir = $wikiDir
}

$notesCsv = Join-Path $translationsDir "Notes.csv"
$dialogueCsv = Join-Path $translationsDir "Dialogue.csv"
$hacksCsv = Join-Path $translationsDir "Hacks.csv"
$itemsCsv = Join-Path $translationsDir "Items.csv"
$skillsCsv = Join-Path $translationsDir "Skills.csv"
$outputJs = Join-Path $wikiDir "wiki_data.js"

Write-Host "1. Reading terminology md file..."
$bytes = [System.IO.File]::ReadAllBytes($mdFile)
$mdContent = [System.Text.Encoding]::UTF8.GetString($bytes)

$terms = @()
$currentCategory = "General"

# Parse Markdown Tables and Section headers
$lines = $mdContent -split "`r?`n"
foreach ($line in $lines) {
    # Detect Section Heading to assign concise categories
    if ($line -match '^##\s+(.+)$') {
        $sec = $Matches[1].Trim()
        if ($sec -match '지리|공간|우주론') {
            $currentCategory = "지명"
        }
        elseif ($sec -match '사회|종족|체계') {
            $currentCategory = "종족/사회"
        }
        elseif ($sec -match '암스|Arms|생체') {
            $currentCategory = "암스 (Arms)"
        }
        elseif ($sec -match '유물|도구|기술') {
            $currentCategory = "유물/도구"
        }
        elseif ($sec -match '인물|명칭|기록') {
            $currentCategory = "인물/기록"
        }
        Write-Host "Current parsing category: $currentCategory"
    }

    # Match table rows
    if ($line -match '^\|\s*\*\*?([^*|]+)\*\*?\s*\|\s*([^|]+)\s*\|\s*([^|]+)\s*\|\s*\*\*?([^*|]+)\*\*?\s*\|') {
        $eng = $Matches[1].Trim()
        $origin = $Matches[2].Trim()
        $context = $Matches[3].Trim()
        $kor = $Matches[4].Trim()
        
        # Filter headers out
        if ($eng -notmatch '^[A-Za-z0-9\s\-\u2019\&\/\+\(\)\.\,\''\:\?\!\`]+$' -or $eng -match '^(English|Key|Name|Category)$') {
            continue
        }
        
        # Find the single best detail paragraph using a scoring heuristic
        $bestParagraph = ""
        $bestScore = -1
        $paragraphs = $mdContent -split "`r?`n"
        
        foreach ($p in $paragraphs) {
            $pClean = $p.Trim()
            # Skip empty, headers, or table rows
            if ([string]::IsNullOrEmpty($pClean) -or $pClean -match '^#+' -or $pClean -like "*|*|*") {
                continue
            }
            
            # Paragraph must contain the English term to be considered
            if ($pClean -like "*$eng*") {
                $score = 0
                
                # Heuristic 1: Starts with bullet point containing the term
                if ($pClean -match "^\*\s*\*\*.*$eng.*?\*\*" -or $pClean -match "^\*\s*.*$eng.*?:") {
                    $score += 100
                }
                
                # Heuristic 2: Bolded terms anywhere in the line
                if ($pClean -match "\*\*.*$eng.*?\*\*") {
                    $score += 60
                }
                
                # Heuristic 3: Term (English or Korean) starts the line/paragraph (within first 15 chars)
                $idxEng = $pClean.IndexOf($eng, [System.StringComparison]::OrdinalIgnoreCase)
                $idxKor = -1
                if (![string]::IsNullOrEmpty($kor)) {
                    $idxKor = $pClean.IndexOf($kor)
                }
                if (($idxEng -ge 0 -and $idxEng -lt 15) -or ($idxKor -ge 0 -and $idxKor -lt 15)) {
                    $score += 60
                }
                
                # Heuristic 4: Term appears early in the paragraph
                if ($idxEng -ge 0 -and $idxEng -lt 60) {
                    $score += 50
                } elseif ($idxEng -ge 0 -and $idxEng -lt 150) {
                    $score += 20
                }
                
                # Heuristic 5: Korean term appears early in the paragraph
                if ($idxKor -ge 0 -and $idxKor -lt 60) {
                    $score += 30
                }
                
                # Heuristic 6: Contains defining verbs in Korean
                if ($pClean -match '은\s*|는\s*|이다|뜻한다|의미한다|칭한다|지칭한다|유래했다') {
                    $score += 10
                }
                
                if ($score -gt $bestScore) {
                    $bestScore = $score
                    $bestParagraph = $pClean
                }
            }
        }
        
        $details = $bestParagraph
        if ([string]::IsNullOrEmpty($details)) {
            $details = "* **어원**: $origin`n* **맥락**: $context"
        }
        
        if (-not ($terms | Where-Object { $_.english -eq $eng })) {
            $terms += [PSCustomObject]@{
                english = $eng
                origin = $origin
                context = $context
                korean = $kor
                category = $currentCategory  # Added clean category
                details = $details.Trim()
            }
        }
    }
}

# Extract manual terms (Weapons / Equipment)
$manualTerms = @(
    @{ english="Gishru"; origin="Gishru / 𒈥𒋽 (폭풍 무기)"; context="원시적인 부메랑 형태의 투척 무기"; korean="기슈루"; category="유물/도구"; details="나노머신이 겉면을 감싸고 있는 원시적인 부메랑 형태의 투척 무기. 발음의 연속성을 고려하여 '기슈루'로 표기한다." },
    @{ english="An Gishru"; origin="An Gishru"; context="기슈루 원격 조종 업그레이드 장치"; korean="안 기슈루"; category="유물/도구"; details="'하늘'을 뜻하는 수메르어 'An'이 결합된 원격 조종 무기. 하늘을 가르며 마음대로 비행하는 무기라는 뜻이 직관적으로 담겨 있다." },
    @{ english="Ul Gishru"; origin="Ul Gishru"; context="더 높은 파괴력을 지닌 최종 업그레이드 단계"; korean="울 기슈루"; category="유물/도구"; details="기슈루의 최종 업그레이드 단계로, 강력한 위력을 발휘하는 무기." },
    @{ english="Diviner's Gem"; origin="점술가의 보석"; context="로어 문서나 숨겨진 아이템 감지 시 반응하는 보석"; korean="점술가의 보석"; category="유물/도구"; details="숨겨진 아이템이나 로어 문서가 근처에 있을 때 나침반 중앙에서 빛을 발하며 반응하는 특수 보석 아이템. 미래를 예지하고 숨겨진 진실을 찾는 점성술과 예언의 모티프를 결합시켰다." },
    @{ english="Ensi's Bracelet"; origin="엔시의 팔찌"; context="도끼 강력 내려치기를 가능하게 해주는 장신구"; korean="엔시의 팔찌"; category="유물/도구"; details="'엔시(Ensi)'가 도시의 군주나 지배자를 뜻한다는 점을 고려할 때, 지배자의 압도적인 권력과 억압의 무게를 물리적인 하강 타격력으로 치환한 아이템." }
)

foreach ($mt in $manualTerms) {
    if (-not ($terms | Where-Object { $_.english -eq $mt.english })) {
        $terms += [PSCustomObject]$mt
    }
}

Write-Host "Extracted terms count: $($terms.Count)"

# --- 2. Parse Notes.csv ---
Write-Host "2. Reading Notes.csv..."
$notesCsvData = Import-CsvSecure $notesCsv
$dialogues = [System.Collections.ArrayList]::new()
$notesHash = @{}

foreach ($row in $notesCsvData) {
    $key = $row.Key
    $eng = $row.English
    $kor = $row.Japanese
    
    if ($key -match '^(.+)_(NAME|TITLE)$') {
        $baseKey = $Matches[1]
        if (-not $notesHash.ContainsKey($baseKey)) {
            $notesHash[$baseKey] = @{}
        }
        $notesHash[$baseKey]["title_eng"] = $eng
        $notesHash[$baseKey]["title_kor"] = $kor
    }
    elseif ($key -match '^(.+)_TEXT$') {
        $baseKey = $Matches[1]
        if (-not $notesHash.ContainsKey($baseKey)) {
            $notesHash[$baseKey] = @{}
        }
        $notesHash[$baseKey]["text_eng"] = $eng
        $notesHash[$baseKey]["text_kor"] = $kor
    }
    else {
        [void]$dialogues.Add([PSCustomObject]@{
            key = $key
            type = "dialogue"
            file = "Notes.csv"
            speaker = ""
            title_eng = ""
            title_kor = ""
            text_eng = $eng
            text_kor = $kor
        })
    }
}

$seenKeys = @{}
foreach ($row in $notesCsvData) {
    $key = $row.Key
    if ($key -match '^(.+)_(NAME|TITLE|TEXT)$') {
        $baseKey = $Matches[1]
        if (-not $seenKeys.ContainsKey($baseKey)) {
            $seenKeys[$baseKey] = $true
            $data = $notesHash[$baseKey]
            
            $speaker = ""
            if ($data.text_eng -match '^"?([^,"]+),\s*([^"]+)"') {
                $speaker = $Matches[1].Trim()
            }
            elseif ($data.text_eng -match '(?m)^([A-Za-z\s]+)$' -and $data.text_eng -split "`n" | Select-Object -Last 1) {
                $lastLine = ($data.text_eng -split "`n" | Where-Object { $_.Trim() -ne "" } | Select-Object -Last 1).Trim()
                if ($lastLine.Length -lt 20 -and $lastLine -match '^[A-Za-z\s\-\u2019]+$') {
                    $speaker = $lastLine
                }
            }
            
            [void]$dialogues.Add([PSCustomObject]@{
                key = $baseKey
                type = "note"
                file = "Notes.csv"
                speaker = $speaker
                title_eng = $data.title_eng
                title_kor = $data.title_kor
                text_eng = $data.text_eng
                text_kor = $data.text_kor
            })
        }
    }
}

Write-Host "Extracted Notes count: $($dialogues.Count)"

# --- 3. Parse Dialogue.csv ---
Write-Host "3. Reading Dialogue.csv..."
$dialogueCsvData = Import-CsvSecure $dialogueCsv
$dialogueCount = 0

foreach ($row in $dialogueCsvData) {
    $key = $row.Key
    $eng = $row.English
    $kor = $row.Japanese
    
    $speaker = ""
    if ($eng -match '^([A-Za-z0-9\s]+):\s*(.*)$') {
        $speaker = $Matches[1].Trim()
        $eng = $Matches[2].Trim()
    }
    
    [void]$dialogues.Add([PSCustomObject]@{
        key = $key
        type = "dialogue"
        file = "Dialogue.csv"
        speaker = $speaker
        title_eng = ""
        title_kor = ""
        text_eng = $eng
        text_kor = $kor
    })
    $dialogueCount++
}

Write-Host "Extracted Dialogues count: $dialogueCount"

# --- 4. Parse Hacks.csv ---
Write-Host "4. Reading Hacks.csv..."
$hacksCsvData = Import-CsvSecure $hacksCsv
$hacksCount = 0
foreach ($row in $hacksCsvData) {
    [void]$dialogues.Add([PSCustomObject]@{
        key = $row.Key
        type = "flat"
        file = "Hacks.csv"
        speaker = ""
        title_eng = ""
        title_kor = ""
        text_eng = $row.English
        text_kor = $row.Japanese
    })
    $hacksCount++
}
Write-Host "Extracted Hacks count: $hacksCount"

# --- 5. Parse Items.csv ---
Write-Host "5. Reading Items.csv..."
$itemsCsvData = Import-CsvSecure $itemsCsv
$itemsCount = 0
foreach ($row in $itemsCsvData) {
    [void]$dialogues.Add([PSCustomObject]@{
        key = $row.Key
        type = "flat"
        file = "Items.csv"
        speaker = ""
        title_eng = ""
        title_kor = ""
        text_eng = $row.English
        text_kor = $row.Japanese
    })
    $itemsCount++
}
Write-Host "Extracted Items count: $itemsCount"

# --- 6. Parse Skills.csv ---
Write-Host "6. Reading Skills.csv..."
$skillsCsvData = Import-CsvSecure $skillsCsv
$skillsCount = 0
foreach ($row in $skillsCsvData) {
    [void]$dialogues.Add([PSCustomObject]@{
        key = $row.Key
        type = "flat"
        file = "Skills.csv"
        speaker = ""
        title_eng = ""
        title_kor = ""
        text_eng = $row.English
        text_kor = $row.Japanese
    })
    $skillsCount++
}
Write-Host "Extracted Skills count: $skillsCount"

Write-Host "Total compiled entries: $($dialogues.Count)"

# Convert to JSON and save
$jsonData = @{
    terms = $terms
    dialogues = $dialogues
    csvRaw = @{
        notes = $notesCsvData
        dialogues = $dialogueCsvData
        hacks = $hacksCsvData
        items = $itemsCsvData
        skills = $skillsCsvData
    }
} | ConvertTo-Json -Depth 100

$jsContent = "const WIKI_DATA = $jsonData;"
[System.IO.File]::WriteAllText($outputJs, $jsContent, [System.Text.Encoding]::UTF8)

Write-Host "Build successfully completed! wiki_data.js created."
