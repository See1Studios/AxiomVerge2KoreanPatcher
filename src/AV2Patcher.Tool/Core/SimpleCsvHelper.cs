using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AV2Patcher.Tool.Core;

public static class SimpleCsvHelper
{
    /// <summary>
    /// RFC-4180 규격을 충족하여 개행 및 따옴표가 포함된 CSV 텍스트를 파싱합니다.
    /// </summary>
    public static List<List<string>> ParseCsv(string text)
    {
        var records = new List<List<string>>();
        var currentRecord = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    // Escaped double quote ("")
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();

                    if (currentRecord.Count > 0 && !(currentRecord.Count == 1 && string.IsNullOrEmpty(currentRecord[0])))
                    {
                        records.Add(currentRecord);
                    }
                    currentRecord = new List<string>();

                    // Handle CRLF (\r\n)
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // Add remaining field and record if any
        if (currentField.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(currentField.ToString());
            if (currentRecord.Count > 0 && !(currentRecord.Count == 1 && string.IsNullOrEmpty(currentRecord[0])))
            {
                records.Add(currentRecord);
            }
        }

        return records;
    }

    /// <summary>
    /// CSV 필드값을 RFC-4180에 맞게 이중 인용부호 에스케이프 처리합니다.
    /// </summary>
    public static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        bool needsQuotes = field.Contains(",") || field.Contains("\n") || field.Contains("\r") || field.Contains("\"");
        if (needsQuotes)
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }

    /// <summary>
    /// 필드 목록을 쉼표로 결합한 단일 CSV 레코드 문자열을 반환합니다.
    /// </summary>
    public static string FormatCsvRow(IEnumerable<string> fields)
    {
        return string.Join(",", fields.Select(EscapeCsvField));
    }
}
