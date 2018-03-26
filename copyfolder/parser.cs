using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace gitignore_parser
{

    /*
        规则参考：
        https://git-scm.com/docs/gitignore
        https://jingyan.baidu.com/article/8065f87faeb57e23312498e7.html
        代码参考：
        https://github.com/sabhiram/go-gitignore 
    */

    class GitIgnoreRegex
    {
        public Regex MyRegex;
        public bool Negative;

        public static GitIgnoreRegex Create(string orig_line)
        {
            var line = orig_line.TrimEnd('\r');

            // 以#开头的是注释
            if (line.StartsWith("#"))
            {
                return null;
            }

            line = line.Trim(' ');

            if (line.Length == 0)
            {
                return null;
            }

            bool negative = false;
            if (line.StartsWith("!"))
            {
                negative = true;
            }

            // 以\#或者\!开头的，转化成以#或者!开头
            if (Regex.IsMatch(line, @"^(\\#|\\!)"))
            {
                line = line.Substring(1);
            }

            // 对于foo/*.blah的，改成/foo/*.blah
            if (!line.StartsWith("/") && Regex.IsMatch(line, @"([^\\/]+)/.*\*\."))
            {
                line = "/" + line;
            }

            // 把"."转义成"\."
            line = Regex.Replace(line, @"\.", ".");

            // 随意一个字符
            string magicStar = "#$~";
            if (line.Contains(magicStar))
            {
                throw new Exception("此行不合法：" + orig_line);
            }

            if (line.StartsWith("/**/"))
            {
                line = line.Substring(1);
            }

            // 把gitignore语法替换成C#识别的正则表达式
            line = Regex.Replace(line, @"/\*\*/", @"(/|/.+/)");
            line = Regex.Replace(line, @"\*\*/", @"(|." + magicStar + "/)");
            line = Regex.Replace(line, @"/\*\*", @"(|/." + magicStar + ")");

            line = Regex.Replace(line, @"\\\*", @"\" + magicStar);
            line = Regex.Replace(line, @"\*", "([^/]*)");

            line = line.Replace("?", @"\?");
            line = line.Replace(magicStar, "*");

            // 
            string expr = "";
            if (line.EndsWith("/"))
            {
                expr = line + @"(|.*)$";
            }
            else
            {
                expr = line + @"(|/.*)$";
            }

            if (expr.StartsWith("/"))
            {
                expr = "^(|/)" + expr.Substring(1);
            }
            else
            {
                expr = "^(|.*/)" + expr;
            }

            GitIgnoreRegex ret = new GitIgnoreRegex();

            ret.MyRegex = new Regex(expr);
            ret.Negative = negative;

            return ret;
        }
    }

    class Parser
    {
        List<GitIgnoreRegex> AllRegex;


        public Parser(string filename)
        {
            AllRegex = new List<GitIgnoreRegex>();
            var lines = File.ReadAllLines(filename);
            foreach (var line in lines)
            {
                var reg = GitIgnoreRegex.Create(line);
                if (reg != null)
                {
                    AllRegex.Add(reg);
                }
            }
        }

        // 传入的是相对路径，相对于.gitignore文件的
        public bool IsMatch(string input)
        {
            // 把文件路径中的"\"统统替换成"/"
            input = input.Replace('\\', '/');

            bool matched = false;
            foreach (var reg in AllRegex)
            {
                if (reg.MyRegex.IsMatch(input))
                {
                    if (reg.Negative)
                        matched = false;
                    else
                        matched = true;
                    // 不终止，这样，就能优先后面的匹配规则了。
                }
            }
            return matched;
        }
    }
}
