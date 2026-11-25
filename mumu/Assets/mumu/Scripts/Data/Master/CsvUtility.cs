using System.Collections.Generic;
using System.Text;

namespace mumu
{
    /// <summary>
    /// CSV の読み書き（ダブルクォート・カンマ・改行を含む値に対応）
    /// </summary>
    public static class CsvUtility
    {
        /// <summary>
        /// CSV を行ごとの値に分割（空行は除く）
        /// </summary>
        /// <param name="_text"></param>
        public static List<string[]> Parse(string _text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < _text.Length; i++)
            {
                char c = _text[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < _text.Length && _text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        endRow(rows, row, field);
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }
            endRow(rows, row, field);
            return rows;
        }

        /// <summary>
        /// 1行分の CSV 文字列
        /// </summary>
        /// <param name="_values"></param>
        public static string ToLine(params object[] _values)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < _values.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }
                builder.Append(escape(_values[i] == null ? "" : _values[i].ToString()));
            }
            return builder.ToString();
        }

        /// <summary>
        /// 行を確定（空行は捨てる）
        /// </summary>
        /// <param name="_rows"></param>
        /// <param name="_row"></param>
        /// <param name="_field"></param>
        private static void endRow(List<string[]> _rows, List<string> _row, StringBuilder _field)
        {
            _row.Add(_field.ToString());
            _field.Clear();
            if (_row.Count > 1 || _row[0].Trim().Length > 0)
            {
                _rows.Add(_row.ToArray());
            }
            _row.Clear();
        }

        /// <summary>
        /// 必要ならダブルクォートで囲む
        /// </summary>
        /// <param name="_value"></param>
        private static string escape(string _value)
        {
            if (_value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            {
                return _value;
            }
            return "\"" + _value.Replace("\"", "\"\"") + "\"";
        }
    }
}
