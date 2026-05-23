using System.Text.Json.Serialization;

namespace AV2Patcher.Modern;

/// <summary>
/// BMFont 텍스트 형식(.fnt)의 개별 문자 데이터
/// </summary>
public class BmChar
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int XAdvance { get; set; }
}

/// <summary>
/// BMFont .fnt 파일에서 파싱된 폰트 메타데이터
/// </summary>
public class FontMetadata
{
    public int LineHeight { get; set; }
    public int Base { get; set; }
    public int ScaleW { get; set; }
    public int ScaleH { get; set; }

    /// <summary>
    /// 파싱된 문자 데이터 (유니코드 코드포인트 → BmChar)
    /// </summary>
    public Dictionary<int, BmChar> ParsedChars { get; set; } = new();

    /// <summary>
    /// 마지막으로 지정된 게임 EXE 경로 (패치 시 임시 사용)
    /// </summary>
    public string? LastGamePath { get; set; }
}

public class CustomFont
{
    /// <summary>"원본 유지" 항목의 고정 이름 (sentinel value)</summary>
    public const string KeepOriginalName = "(원본 유지)";

    /// <summary>이 항목이 "원본 유지" sentinel인지 여부</summary>
    [JsonIgnore]
    public bool IsKeepOriginal => Name == KeepOriginalName;

    /// <summary>"원본 유지" CustomFont 인스턴스 생성</summary>
    public static CustomFont CreateKeepOriginal() => new() { Name = KeepOriginalName };

    public string Name { get; set; } = "";
    public string FntPath { get; set; } = "";
    public string PngPath { get; set; } = "";
    public FontMetadata Metadata { get; set; } = new();
    public int CharCount => Metadata.ParsedChars.Count;
    public int TextureWidth { get; set; }
    public int TextureHeight { get; set; }

    public override string ToString() => $"{Name} ({CharCount} chars)";

    [JsonIgnore]
    public string DisplayInfo => IsKeepOriginal ? "" : $"{CharCount} chars, {TextureWidth}x{TextureHeight}";
}

public class FontMapping : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new(name));

    public string TargetXnb { get; set; } = "";
    public string Description { get; set; } = "";

    private string _selectedFontName = "";
    public string SelectedFontName
    {
        get => _selectedFontName;
        set { _selectedFontName = value; OnPropertyChanged(nameof(SelectedFontName)); }
    }

    private string _buildMode = "Append";
    public string BuildMode
    {
        get => _buildMode;
        set { _buildMode = value; OnPropertyChanged(nameof(BuildMode)); }
    }

    private int _yOffsetAdjust;
    /// <summary>수직 위치 조정 (px). 양수 = 아래로, 음수 = 위로.</summary>
    public int YOffsetAdjust { get => _yOffsetAdjust; set { _yOffsetAdjust = value; OnPropertyChanged(nameof(YOffsetAdjust)); } }

    private int _xOffsetAdjust;
    /// <summary>수평 위치 조정 (px). 양수 = 오른쪽으로, 음수 = 왼쪽으로.</summary>
    public int XOffsetAdjust { get => _xOffsetAdjust; set { _xOffsetAdjust = value; OnPropertyChanged(nameof(XOffsetAdjust)); } }

    private int _xAdvanceAdjust;
    /// <summary>글자 간격/너비 조정 (px). 양수 = 더 넓게, 음수 = 더 좁게.</summary>
    public int XAdvanceAdjust { get => _xAdvanceAdjust; set { _xAdvanceAdjust = value; OnPropertyChanged(nameof(XAdvanceAdjust)); } }

    private int _originalTextureWidth;
    /// <summary>추출된 원본 XNB 텍스처 가로 크기 (px). config.json에 저장하지 않음.</summary>
    [JsonIgnore]
    public int OriginalTextureWidth
    {
        get => _originalTextureWidth;
        set { _originalTextureWidth = value; OnPropertyChanged(nameof(OriginalTextureWidth)); OnPropertyChanged(nameof(OrigTextureSizeInfo)); }
    }

    private int _originalTextureHeight;
    /// <summary>추출된 원본 XNB 텍스처 세로 크기 (px). config.json에 저장하지 않음.</summary>
    [JsonIgnore]
    public int OriginalTextureHeight
    {
        get => _originalTextureHeight;
        set { _originalTextureHeight = value; OnPropertyChanged(nameof(OriginalTextureHeight)); OnPropertyChanged(nameof(OrigTextureSizeInfo)); }
    }

    /// <summary>UI 표시용 원본 텍스처 크기 문자열.</summary>
    [JsonIgnore]
    public string OrigTextureSizeInfo =>
        OriginalTextureWidth > 0
            ? $"원본: {OriginalTextureWidth}×{OriginalTextureHeight}px"
            : "원본 미추출";

    private int _builtTextureWidth;
    /// <summary>빌드된 한글 XNB 텍스처 가로 크기 (px). config.json에 저장하지 않음.</summary>
    [JsonIgnore]
    public int BuiltTextureWidth
    {
        get => _builtTextureWidth;
        set { _builtTextureWidth = value; OnPropertyChanged(nameof(BuiltTextureWidth)); OnPropertyChanged(nameof(BuiltTextureSizeInfo)); }
    }

    private int _builtTextureHeight;
    /// <summary>빌드된 한글 XNB 텍스처 세로 크기 (px). config.json에 저장하지 않음.</summary>
    [JsonIgnore]
    public int BuiltTextureHeight
    {
        get => _builtTextureHeight;
        set { _builtTextureHeight = value; OnPropertyChanged(nameof(BuiltTextureHeight)); OnPropertyChanged(nameof(BuiltTextureSizeInfo)); }
    }

    /// <summary>UI 표시용 빌드된 텍스처 크기 문자열.</summary>
    [JsonIgnore]
    public string BuiltTextureSizeInfo =>
        BuiltTextureWidth > 0
            ? $"빌드본: {BuiltTextureWidth}×{BuiltTextureHeight}px"
            : "";
}

public class Config
{
    public string ExePath { get; set; } = "";
    public string Theme { get; set; } = "System";
    public List<FontMapping> Mappings { get; set; } = new();

    public void InitializeDefaults()
    {
        var slots = new[]
        {
            new { Xnb = "Uni0553_6pt.xnb",                  Desc = "Speedrun Description (6pt)",       DefaultFont = "",                 DefaultY = 0 },
            new { Xnb = "Hooge0655_8pt.xnb",               Desc = "Dialog Small & Standard UI (8pt)", DefaultFont = "Galmuri9",        DefaultY = 0 },
            new { Xnb = "NotoSansMonoCJKJpRegular12pt.xnb", Desc = "Main Dialogue & Hacking (12pt)",   DefaultFont = "Galmuri11 Regular", DefaultY = 3 },
            new { Xnb = "NotoSansMonoCJKJpRegular16pt.xnb", Desc = "Main Large & Menu Title (16pt)",   DefaultFont = "NeoDunggeunmo",    DefaultY = 4 }
        };

        // 패치 대상이 아닌 슬롯 제거 (AV8ptMonogame, Moire16pt 등)
        var validXnbs = slots.Select(s => s.Xnb).ToHashSet();
        Mappings.RemoveAll(m => !validXnbs.Contains(m.TargetXnb));

        foreach (var s in slots)
        {
            var mapping = Mappings.FirstOrDefault(m => m.TargetXnb == s.Xnb);
            if (mapping == null)
            {
                Mappings.Add(new FontMapping
                {
                    TargetXnb = s.Xnb,
                    Description = s.Desc,
                    SelectedFontName = s.DefaultFont,
                    YOffsetAdjust = s.DefaultY,
                    BuildMode = "Append"
                });
            }
            else
            {
                mapping.Description = s.Desc;
                if (string.IsNullOrEmpty(mapping.SelectedFontName))
                {
                    mapping.SelectedFontName = s.DefaultFont;
                    mapping.YOffsetAdjust = s.DefaultY;
                }
                if (string.IsNullOrEmpty(mapping.BuildMode))
                {
                    mapping.BuildMode = "Append";
                }
            }
        }
    }
}
