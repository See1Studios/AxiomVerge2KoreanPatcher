using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AV2Patcher.Tool
{
    public partial class TranslationEditorWindow : Window
    {
        public class TranslationItem : System.ComponentModel.INotifyPropertyChanged
        {
            public string Key { get; set; } = "";
            public string English { get; set; } = "";
            public string Original { get; set; } = ""; // Japanese

            private string _translation = ""; // Korean
            public string Translation
            {
                get => _translation;
                set
                {
                    if (_translation != value)
                    {
                        _translation = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }

        public ObservableCollection<TranslationItem> TranslationItems { get; } = new();
        private string? _loadedCsvFileName;

        private static readonly string[] CsvFilesList = new[] {
            "Dialogue.csv", "Hacks.csv", "Items.csv", "Messages.csv", "Notes.csv", "NPCs.csv", "Skills.csv", "UI.csv"
        };

        public TranslationEditorWindow()
        {
            InitializeComponent();
            cbCsvFiles.ItemsSource = CsvFilesList;
            dgTranslations.ItemsSource = TranslationItems;
        }

        private void OnCsvFileSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCsvFiles.SelectedItem is string selectedFile)
            {
                LoadCsvTranslations(selectedFile);
            }
        }

        private void LoadCsvTranslations(string fileName)
        {
            try
            {
                string translationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations");
                string originalsDir = Path.Combine(translationsDir, "Originals");
                string originalFilePath = Path.Combine(originalsDir, fileName);
                string workingFilePath = Path.Combine(translationsDir, fileName);

                if (!File.Exists(originalFilePath))
                {
                    System.Windows.MessageBox.Show($"원본 파일 {fileName}이 없습니다. 먼저 'Extract Originals'를 실행하세요.", "파일 없음", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 작업용 파일이 없으면 복사
                if (!File.Exists(workingFilePath))
                {
                    File.Copy(originalFilePath, workingFilePath);
                }

                string originalText = File.ReadAllText(originalFilePath, System.Text.Encoding.UTF8);
                string workingText = File.ReadAllText(workingFilePath, System.Text.Encoding.UTF8);

                var originalRecords = Core.SimpleCsvHelper.ParseCsv(originalText);
                var workingRecords = Core.SimpleCsvHelper.ParseCsv(workingText);

                if (originalRecords.Count == 0 || workingRecords.Count == 0)
                {
                    System.Windows.MessageBox.Show("CSV 파싱에 실패했거나 빈 파일입니다.", "파싱 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 헤더 분석 (English / Japanese 컬럼 위치 찾기)
                var headers = originalRecords[0];
                int japaneseIdx = headers.IndexOf("Japanese");
                if (japaneseIdx == -1) japaneseIdx = 8;
                int englishIdx = headers.IndexOf("English");
                if (englishIdx == -1) englishIdx = 1;

                // 원본 맵 구성
                var originalMap = new Dictionary<string, (string English, string Japanese)>();
                for (int i = 1; i < originalRecords.Count; i++)
                {
                    var row = originalRecords[i];
                    if (row.Count > 0)
                    {
                        string key = row[0];
                        string english = row.Count > englishIdx ? row[englishIdx] : "";
                        string japanese = row.Count > japaneseIdx ? row[japaneseIdx] : "";
                        if (!string.IsNullOrEmpty(key) && key != "Key")
                        {
                            originalMap[key] = (english, japanese);
                        }
                    }
                }

                // 작업용 맵 구성
                var workingMap = new Dictionary<string, string>();
                for (int i = 1; i < workingRecords.Count; i++)
                {
                    var row = workingRecords[i];
                    if (row.Count > 0)
                    {
                        string key = row[0];
                        string value = row.Count > japaneseIdx ? row[japaneseIdx] : "";
                        if (!string.IsNullOrEmpty(key) && key != "Key")
                        {
                            workingMap[key] = value;
                        }
                    }
                }

                TranslationItems.Clear();
                foreach (var kvp in originalMap)
                {
                    workingMap.TryGetValue(kvp.Key, out string? workingVal);
                    TranslationItems.Add(new TranslationItem
                    {
                        Key = kvp.Key,
                        English = kvp.Value.English,
                        Original = kvp.Value.Japanese,
                        Translation = workingVal ?? kvp.Value.Japanese
                    });
                }

                _loadedCsvFileName = fileName;
                tbEditorStatus.Text = $"{fileName} loaded. ({TranslationItems.Count} items)";
                btnSaveTranslations.IsEnabled = true;
                btnOpenCsv.IsEnabled = true;
                btnOpenCsvFolder.IsEnabled = true;
                btnRefreshCsv.IsEnabled = true;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"번역 로드 중 오류 발생: {ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnFilterOptionChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(TranslationItems);
            if (view == null) return;

            string searchText = txtSearch.Text.Trim();
            bool untranslatedOnly = chkUntranslatedOnly.IsChecked == true;

            view.Filter = (obj) =>
            {
                if (obj is TranslationItem item)
                {
                    // 검색어 매칭 검사 (Key, English, Original, Translation 모두 검사)
                    bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                         item.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                         item.English.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                         item.Original.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                         item.Translation.Contains(searchText, StringComparison.OrdinalIgnoreCase);

                    if (!matchesSearch) return false;

                    // 미번역 항목 검사
                    if (untranslatedOnly)
                    {
                        return string.IsNullOrEmpty(item.Translation) || item.Translation == item.Original;
                    }

                    return true;
                }
                return false;
            };
        }

        private async void OnSaveTranslationsClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_loadedCsvFileName)) return;

            btnSaveTranslations.IsEnabled = false;
            tbEditorStatus.Text = "Saving translations...";

            try
            {
                string fileName = _loadedCsvFileName;
                await Task.Run(() => SaveTranslations(fileName));
                tbEditorStatus.Text = $"저장 완료: {fileName} ({DateTime.Now:HH:mm:ss})";
                
                // 메인 창의 로그 출력을 돕기 위해 메인 윈도우의 Log 메서드 호출
                if (Owner is MainWindow mainWin)
                {
                    mainWin.Log($"Saved translated CSV: {fileName}");
                }

                System.Windows.MessageBox.Show($"{fileName} 번역이 정상적으로 저장되었습니다.\n\nPatcher & Fonts 탭에서 'Apply Patch'를 실행하면 게임에 반영됩니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                tbEditorStatus.Text = "Error saving translations.";
                System.Windows.MessageBox.Show($"저장 실패: {ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSaveTranslations.IsEnabled = true;
            }
        }

        private void SaveTranslations(string fileName)
        {
            string translationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations");
            string workingFilePath = Path.Combine(translationsDir, fileName);

            if (!File.Exists(workingFilePath))
            {
                throw new FileNotFoundException("작업 대상 파일을 찾을 수 없습니다.", workingFilePath);
            }

            string workingText = File.ReadAllText(workingFilePath, System.Text.Encoding.UTF8);
            var records = Core.SimpleCsvHelper.ParseCsv(workingText);

            if (records.Count == 0)
            {
                throw new Exception("수정 대상 CSV 파일이 비어 있습니다.");
            }

            var headers = records[0];
            int japaneseIdx = headers.IndexOf("Japanese");
            if (japaneseIdx == -1) japaneseIdx = 8;

            var translationMap = new Dictionary<string, string>();
            Dispatcher.Invoke(() =>
            {
                foreach (var item in TranslationItems)
                {
                    translationMap[item.Key] = item.Translation;
                }
            });

            for (int i = 1; i < records.Count; i++)
            {
                var row = records[i];
                if (row.Count > 0)
                {
                    string key = row[0];
                    if (translationMap.TryGetValue(key, out string? newTranslation))
                    {
                        while (row.Count <= japaneseIdx)
                        {
                            row.Add("");
                        }
                        row[japaneseIdx] = newTranslation;
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (var row in records)
            {
                sb.AppendLine(Core.SimpleCsvHelper.FormatCsvRow(row));
            }

            File.WriteAllText(workingFilePath, sb.ToString(), System.Text.Encoding.UTF8);
        }

        private static readonly System.Net.Http.HttpClient _httpClient = CreateHttpClient();

        private static System.Net.Http.HttpClient CreateHttpClient()
        {
            var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            return client;
        }

        private async Task<string?> TranslateTextGoogleAsync(string text, string sourceLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl=ko&dt=t&q={Uri.EscapeDataString(text)}";
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var segments = root[0];
                    if (segments.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        foreach (var segment in segments.EnumerateArray())
                        {
                            if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0)
                            {
                                var translatedText = segment[0];
                                if (translatedText.ValueKind == JsonValueKind.String)
                                {
                                    sb.Append(translatedText.GetString());
                                }
                            }
                        }
                        return sb.ToString();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                if (Owner is MainWindow mainWin)
                {
                    mainWin.Log($"구글 번역 오류 ({sourceLanguage}): {ex.Message}");
                }
                return null;
            }
        }

        private async void OnRowTranslateJpClicked(object sender, RoutedEventArgs e)
        {
            await PerformRowTranslationAsync(sender, "ja");
        }

        private async void OnRowTranslateEnClicked(object sender, RoutedEventArgs e)
        {
            await PerformRowTranslationAsync(sender, "en");
        }

        private async Task PerformRowTranslationAsync(object sender, string sourceLanguage)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            if (btn.DataContext is not TranslationItem item) return;

            string sourceText = sourceLanguage == "en" ? item.English : item.Original;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                item.Translation = "";
                return;
            }

            btn.IsEnabled = false;
            var prevStatus = tbEditorStatus.Text;
            tbEditorStatus.Text = "번역 중...";

            try
            {
                string? translated = await TranslateTextGoogleAsync(sourceText, sourceLanguage);
                if (translated != null)
                {
                    item.Translation = translated;
                    tbEditorStatus.Text = "번역 완료";
                }
                else
                {
                    tbEditorStatus.Text = "번역 실패";
                }
            }
            catch (Exception ex)
            {
                tbEditorStatus.Text = $"번역 중 에러: {ex.Message}";
            }
            finally
            {
                btn.IsEnabled = true;
                await Task.Delay(2000);
                if (tbEditorStatus.Text == "번역 완료" || tbEditorStatus.Text == "번역 실패" || tbEditorStatus.Text == "번역 중...")
                {
                    tbEditorStatus.Text = prevStatus;
                }
            }
        }

        private void OnOpenCsvClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_loadedCsvFileName)) return;
            try
            {
                string translationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations");
                string workingFilePath = Path.Combine(translationsDir, _loadedCsvFileName);
                if (File.Exists(workingFilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = workingFilePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show("대상이 되는 CSV 파일을 찾을 수 없습니다.", "파일 없음", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"파일을 여는 중 에러 발생: {ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenCsvFolderClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string translationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations");
                if (Directory.Exists(translationsDir))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = translationsDir,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show("Translations 폴더가 존재하지 않습니다.", "폴더 없음", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"폴더를 여는 중 에러 발생: {ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRefreshCsvClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_loadedCsvFileName)) return;

            var result = System.Windows.MessageBox.Show(
                "현재 번역을 디스크로부터 다시 불러오시겠습니까? 저장하지 않은 변경 사항은 삭제됩니다.",
                "새로 고침 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoadCsvTranslations(_loadedCsvFileName);
            }
        }
    }
}
