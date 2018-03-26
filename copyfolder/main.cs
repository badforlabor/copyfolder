using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gitignore_parser;
using System.IO;

namespace copyfolder
{
    class main
    {
        static bool ForceOverride = false;  // 是否强制覆盖。

        static void ParseString(string src, string token, ref string value)
        {
            if (src.StartsWith(token))
            {
                value = src.Substring(token.Length);
            }
        }
        static void ParseBool(string src, string token, ref bool value)
        {
            if (src.StartsWith(token))
            {
                value = int.Parse(src.Substring(token.Length)) > 0;
            }
        }
        static string TrimDir(string src)
        {
            src = src.Replace('\\', '/');

            src = src.Trim('/');

            return src;
        }
        static bool EqualDir(string src, string dst)
        {
            src = src.Replace('\\', '/');
            dst = dst.Replace('\\', '/');

            src = src.Trim('/');
            dst = dst.Trim('/');

            return src == dst;
        }

        static void Main(string[] args)
        {
            string src = "./";
            string dst = "../copyed";
            src = @"F:\github6\gitignore-parser\";

            var cmds = new List<string>(args);
            foreach (var cmd in cmds)
            {
                ParseString(cmd, "-src=", ref src);
                ParseString(cmd, "-dst=", ref dst);
                ParseBool(cmd, "-override=", ref ForceOverride);
            }

            // 检查下，如果src和dst的目录是一样的，抛异常
            {
                var srcinfo = new DirectoryInfo(src);
                var dstinfo = new DirectoryInfo(dst);
                if (EqualDir(srcinfo.FullName, dstinfo.FullName))
                {
                    throw new Exception("目录结构一样了.");
                }
                Console.WriteLine(TrimDir(srcinfo.FullName));
                Console.WriteLine(TrimDir(dstinfo.FullName));
            }

            {
                var srcinfo = Directory.GetParent(src);
                var dstinfo = Directory.GetParent(dst);
                if (EqualDir(srcinfo.FullName, dstinfo.FullName))
                {
                    throw new Exception("目录结构一样了");
                }
                Console.WriteLine(TrimDir(srcinfo.FullName));
                Console.WriteLine(TrimDir(dstinfo.FullName));
            }

            // 是否强制删除目标文件夹
            if (ForceOverride)
            {
                if (Directory.Exists(dst))
                {
                    Directory.Delete(dst, true);
                }
            }
            else
            {
                System.Random r = new System.Random();
                while (Directory.Exists(dst))
                {
                    dst += r.Next(0, 9);
                }
                Directory.CreateDirectory(dst);
            }

            Parser p = new Parser(src + @".gitignore");

            LoopCheck(p, src, src, dst);

            Console.WriteLine("拷贝成功，目标目录为：" + new DirectoryInfo(dst).FullName);
            Console.WriteLine("按任意键结束...");
            Console.ReadKey();
        }
        static void LoopCheck(Parser p, string loopdir, string baseroot, string dst)
        {
            var all = Directory.GetFileSystemEntries(loopdir);
            List<string> Folders = new List<string>();

            foreach (var one in all)
            {
                string relative = one.Substring(baseroot.Length);

                bool folder = Directory.Exists(one);
                if (folder)
                {
                    relative = relative + "/";
                }

                if (!p.IsMatch(relative))
                {
                    Console.WriteLine("匹配通过：" + relative);
                    if (folder)
                    {
                        Folders.Add(one);
                    }
                    else
                    {
                        CopyFile(one, dst + "/" + relative);
                    }
                }
            }

            foreach (var folder in Folders)
            {
                LoopCheck(p, folder, baseroot, dst);
            }
        }
        static void CopyFile(string src, string dst)
        {
            src = src.Replace('\\', '/');
            dst = dst.Replace('\\', '/');

            string folder = dst.Substring(0, dst.LastIndexOf("/"));
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.Copy(src, dst);
        }
    }
}
