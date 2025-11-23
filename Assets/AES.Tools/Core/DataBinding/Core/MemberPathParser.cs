using System;
using System.Collections.Generic;
using System.Text;

namespace AES.Tools
{
    public static class MemberPathParser
    {
        public static List<PathToken> Parse(string path)
        {
            var tokens = new List<PathToken>();

            int i = 0;
            while (i < path.Length)
            {
                // 1) 멤버 이름 먼저 읽음
                var name = ReadIdentifier(path, ref i);
                if (!string.IsNullOrEmpty(name))
                    tokens.Add(new MemberToken { Name = name });

                // 2) 인덱서/키 형태 반복 처리
                while (i < path.Length && path[i] == '[')
                {
                    i++; // '[' 건너뛰기
                    SkipSpaces(path, ref i);

                    if (path[i] == '"' || path[i] == '\'')
                    {
                        // 키 접근: Stats["HP"]
                        var key = ReadStringLiteral(path, ref i);
                        tokens.Add(new KeyToken { Key = key });
                    }
                    else
                    {
                        // 숫자 인덱스: Items[12]
                        var index = ReadInt(path, ref i);
                        tokens.Add(new IndexToken { Index = index });
                    }

                    SkipSpaces(path, ref i);

                    if (i < path.Length && path[i] == ']')
                        i++;
                }

                // '.' 처리 후 다음 루프
                if (i < path.Length && path[i] == '.')
                    i++;
            }

            return tokens;
        }

        static string ReadIdentifier(string s, ref int i)
        {
            var sb = new StringBuilder();
            while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_'))
            {
                sb.Append(s[i]);
                i++;
            }
            return sb.ToString();
        }

        static int ReadInt(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && char.IsDigit(s[i]))
                i++;

            var numStr = s.Substring(start, i - start);
            return int.Parse(numStr);
        }

        static string ReadStringLiteral(string s, ref int i)
        {
            char quote = s[i];
            i++; // skip quote

            var sb = new StringBuilder();
            while (i < s.Length && s[i] != quote)
            {
                sb.Append(s[i]);
                i++;
            }
            i++; // closing quote

            return sb.ToString();
        }

        static void SkipSpaces(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }
    }
}
