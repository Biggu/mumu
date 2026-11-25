using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace mumu
{
    /// <summary>
    /// 文字列テーブルのキーから MumuText_AutoGenerate.cs を作る
    /// </summary>
    public static class GenerateLocalizeText
    {
        /// <summary>出力先</summary>
        private const string OutputPath = "Assets/mumu/Scripts/Localization/MumuText_AutoGenerate.cs";
        /// <summary>{0} や {name} の中身を取る（:N0 などの書式は無視）</summary>
        private static readonly Regex PlaceholderRegex = new Regex(@"\{(\w+)[^{}]*\}");
        /// <summary>C# のメソッド名として使えるキーか</summary>
        private static readonly Regex IdentifierRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$");
        /// <summary>末尾が番号のキー（A_1 の A と 1 に分ける）</summary>
        private static readonly Regex NumberedKeyRegex = new Regex(@"^(.+)_(\d+)$");

        /// <summary>キーと言語ごとの文字</summary>
        private class KeyText
        {
            public string Key;
            public List<string> Values = new List<string>();
            /// <summary>番号の引数の数（{0} {1} なら 2）</summary>
            public int ArgCount;
            /// <summary>名前の引数（{name} {bone} なら name, bone）</summary>
            public List<string> Names = new List<string>();
        }

        /// <summary>
        /// テーブルを読んで MumuText_AutoGenerate.cs を書き出す
        /// </summary>
        [MenuItem("Tool/GenerateLocalizeText")]
        public static void Generate()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationSetup.TableName);
            if (collection == null)
            {
                Debug.LogError($"[GenerateLocalizeText] 文字列テーブル '{LocalizationSetup.TableName}' がありません（mumu > Setup Localization を先に実行）");
                return;
            }

            var tables = new List<StringTable>();
            foreach (string code in LocalizationSetup.LocaleCodes)
            {
                Locale locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
                var table = locale != null ? collection.GetTable(locale.Identifier) as StringTable : null;
                if (table == null)
                {
                    Debug.LogError($"[GenerateLocalizeText] 言語 '{code}' のテーブルがありません");
                    return;
                }
                tables.Add(table);
            }

            var keyTexts = new List<KeyText>();
            foreach (var entry in collection.SharedData.Entries.OrderBy(_entry => _entry.Key, System.StringComparer.Ordinal))
            {
                if (!IdentifierRegex.IsMatch(entry.Key))
                {
                    Debug.LogWarning($"[GenerateLocalizeText] メソッド名に使えないキーなので飛ばしました: {entry.Key}");
                    continue;
                }

                var item = new KeyText { Key = entry.Key };
                foreach (StringTable table in tables)
                {
                    StringTableEntry tableEntry = table.GetEntry(entry.Id);
                    item.Values.Add(tableEntry != null ? tableEntry.Value : "");
                }
                item.ArgCount = countArgs(item.Values);
                item.Names = collectNames(item.Values);
                if (item.ArgCount > 0 && item.Names.Count > 0)
                {
                    Debug.LogWarning($"[GenerateLocalizeText] {item.Key} は {{0}} と {{name}} が混ざっているので飛ばしました（どちらかに統一してください）");
                    continue;
                }
                keyTexts.Add(item);
            }

            // A_1, A_2 … のように同じ名前の番号付きキーが2つ以上あるものは、int 1つのメソッドにまとめる（名前の引数があるものは対象外）
            var keyNames = new HashSet<string>(keyTexts.Select(_item => _item.Key));
            var numberedMembers = new Dictionary<string, List<KeyText>>();
            foreach (KeyText item in keyTexts)
            {
                Match match = NumberedKeyRegex.Match(item.Key);
                if (!match.Success || item.Names.Count > 0)
                {
                    continue;
                }
                string baseName = match.Groups[1].Value;
                if (!numberedMembers.ContainsKey(baseName))
                {
                    numberedMembers[baseName] = new List<KeyText>();
                }
                numberedMembers[baseName].Add(item);
            }

            var groupArgCount = new Dictionary<string, int>();
            var groupMaxIndex = new Dictionary<string, int>();
            var groupedKeys = new HashSet<string>();
            foreach (var pair in numberedMembers)
            {
                if (pair.Value.Count < 2)
                {
                    continue;
                }
                if (keyNames.Contains(pair.Key))
                {
                    Debug.LogWarning($"[GenerateLocalizeText] {pair.Key} と同じ名前のキーがあるので、番号のまとめは作りません");
                    continue;
                }
                groupArgCount[pair.Key] = pair.Value.Max(_member => _member.ArgCount);
                groupMaxIndex[pair.Key] = pair.Value.Max(_member => int.Parse(NumberedKeyRegex.Match(_member.Key).Groups[2].Value));
                foreach (KeyText member in pair.Value)
                {
                    groupedKeys.Add(member.Key);
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine("namespace mumu");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 文字列テーブルのキーから作るテキスト（自動生成。直接編集しない）");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class MumuText");
            builder.AppendLine("    {");

            int written = 0;
            var writtenGroups = new HashSet<string>();
            foreach (KeyText item in keyTexts)
            {
                if (groupedKeys.Contains(item.Key))
                {
                    string baseName = NumberedKeyRegex.Match(item.Key).Groups[1].Value;
                    if (writtenGroups.Add(baseName))
                    {
                        writeGroupMethod(builder, baseName, groupArgCount[baseName], groupMaxIndex[baseName]);
                        written++;
                    }
                    continue;
                }

                writeMethod(builder, item);
                written++;
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            File.WriteAllText(OutputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(OutputPath);
            Debug.Log($"[GenerateLocalizeText] {written} 件のメソッドを書き出しました: {OutputPath}");
        }

        /// <summary>
        /// 言語の文字から最大の {n} の数（番号の引数の数）を数える
        /// </summary>
        /// <param name="_values"></param>
        private static int countArgs(List<string> _values)
        {
            int argCount = 0;
            foreach (string value in _values)
            {
                foreach (Match match in PlaceholderRegex.Matches(value))
                {
                    string token = match.Groups[1].Value;
                    if (token.All(char.IsDigit))
                    {
                        argCount = Mathf.Max(argCount, int.Parse(token) + 1);
                    }
                }
            }
            return argCount;
        }

        /// <summary>
        /// 言語の文字から名前の引数（{name} など）を、出てきた順に集める
        /// </summary>
        /// <param name="_values"></param>
        private static List<string> collectNames(List<string> _values)
        {
            var names = new List<string>();
            foreach (string value in _values)
            {
                foreach (Match match in PlaceholderRegex.Matches(value))
                {
                    string token = match.Groups[1].Value;
                    if (!token.All(char.IsDigit) && !names.Contains(token))
                    {
                        names.Add(token);
                    }
                }
            }
            return names;
        }

        /// <summary>
        /// キーのメソッドを1つ書く（{0} は番号の引数、{name} は名前の引数）
        /// </summary>
        /// <param name="_builder"></param>
        /// <param name="_item"></param>
        private static void writeMethod(StringBuilder _builder, KeyText _item)
        {
            string summary = string.Join(" | ", _item.Values.Select(_value => SecurityElement.Escape(_value.Replace("\r", " ").Replace("\n", " "))));
            List<string> paramNames = _item.Names.Count > 0
                ? _item.Names.Select(_name => $"_{_name}").ToList()
                : Enumerable.Range(0, _item.ArgCount).Select(_i => $"_arg{_i}").ToList();

            _builder.AppendLine("        /// <summary>");
            _builder.AppendLine($"        /// {summary}");
            _builder.AppendLine("        /// </summary>");
            foreach (string paramName in paramNames)
            {
                _builder.AppendLine($"        /// <param name=\"{paramName}\"></param>");
            }
            _builder.AppendLine($"        public static string {_item.Key}({string.Join(", ", paramNames.Select(_p => $"object {_p}"))})");
            _builder.AppendLine("        {");
            if (_item.Names.Count > 0)
            {
                string entries = string.Join(", ", _item.Names.Select(_name => $"{{ \"{_name}\", _{_name} }}"));
                _builder.AppendLine($"            return TextLocalizer.GetNamed(\"{_item.Key}\", new Dictionary<string, object> {{ {entries} }});");
            }
            else
            {
                string args = string.Concat(paramNames.Select(_p => $", {_p}"));
                _builder.AppendLine($"            return TextLocalizer.Get(\"{_item.Key}\"{args});");
            }
            _builder.AppendLine("        }");
            _builder.AppendLine();
        }

        /// <summary>
        /// A_1, A_2 … をまとめた1つのメソッドを書く（_index で番号を選ぶ）
        /// </summary>
        /// <param name="_builder"></param>
        /// <param name="_baseName"></param>
        /// <param name="_argCount"></param>
        /// <param name="_maxIndex"></param>
        private static void writeGroupMethod(StringBuilder _builder, string _baseName, int _argCount, int _maxIndex)
        {
            var parameters = new List<string> { "int _index" };
            for (int i = 0; i < _argCount; i++)
            {
                parameters.Add($"object _arg{i}");
            }

            // 番号ごとのキーを先に作っておく（呼ぶたびに文字列を作らない）
            string keyArray = string.Join(", ", Enumerable.Range(0, _maxIndex + 1).Select(_i => $"\"{_baseName}_{_i}\""));
            _builder.AppendLine($"        /// <summary>{_baseName}_N のキー（N は番号）</summary>");
            _builder.AppendLine($"        private static readonly string[] keys_{_baseName} = {{ {keyArray} }};");
            _builder.AppendLine();
            _builder.AppendLine("        /// <summary>");
            _builder.AppendLine($"        /// {_baseName}_N の文字（N は _index）");
            _builder.AppendLine("        /// </summary>");
            _builder.AppendLine("        /// <param name=\"_index\"></param>");
            for (int i = 0; i < _argCount; i++)
            {
                _builder.AppendLine($"        /// <param name=\"_arg{i}\"></param>");
            }
            _builder.AppendLine($"        public static string {_baseName}({string.Join(", ", parameters)})");
            _builder.AppendLine("        {");
            string args = string.Concat(Enumerable.Range(0, _argCount).Select(_i => $", _arg{_i}"));
            _builder.AppendLine($"            return TextLocalizer.Get(_index >= 0 && _index < keys_{_baseName}.Length ? keys_{_baseName}[_index] : \"{_baseName}_\" + _index{args});");
            _builder.AppendLine("        }");
            _builder.AppendLine();
        }
    }
}
