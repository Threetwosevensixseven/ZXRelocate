using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ZXRelocate
{
    class Program
    {
        static bool Interactive;
        static string SymbolFile = "";
        static string SymbolRegEx = "";
        static string BankRegEx = "";
        static string RelocateTableFile = "";
        static string RelocateCountFile = "";
        static int AddressOffset = 0;
        static string DefineWord = "";
        static string Equate = "";
        static string Comment = "";
        static bool IncludeComments;

        static int Main(string[] args)
        {
            try
            {
                Interactive = args.Any(a => a == "-i");
                if (Interactive)
                    Console.WriteLine("Running in Interactive mode.");
                SymbolFile = (ConfigurationManager.AppSettings["SymbolFile"] ?? "").Trim();
                Console.WriteLine("Symbol File: " + SymbolFile);
                RelocateTableFile = (ConfigurationManager.AppSettings["RelocateTableFile"] ?? "").Trim();
                Console.WriteLine("Relocate Table File: " + RelocateTableFile);
                RelocateCountFile = (ConfigurationManager.AppSettings["RelocateCountFile"] ?? "").Trim();
                Console.WriteLine("Relocate Count File: " + RelocateCountFile);
                SymbolRegEx = (ConfigurationManager.AppSettings["SymbolRegEx"] ?? "").Trim();
                Console.WriteLine("Symbol RegEx: " + SymbolRegEx);
                BankRegEx = (ConfigurationManager.AppSettings["BankRegEx"] ?? "").Trim();
                Console.WriteLine("Bank RegEx: " + BankRegEx);
                string val = (ConfigurationManager.AppSettings["AddressOffset"] ?? "").Trim();
                int.TryParse(val, out AddressOffset);
                Console.WriteLine("Address Offset (add to every symbol): " + AddressOffset);
                DefineWord = (ConfigurationManager.AppSettings["DefineWord"] ?? "");
                Console.WriteLine("Define Word: " + DefineWord);
                Comment = (ConfigurationManager.AppSettings["Comment"] ?? "");
                Console.WriteLine("Comment: " + Comment);
                Equate = (ConfigurationManager.AppSettings["Equate"] ?? "");
                Console.WriteLine("Equate: " + Equate);

                IncludeComments = (ConfigurationManager.AppSettings["IncludeComments"] ?? "").Trim().ToLower() == "true";
                Console.WriteLine("Include Comments: " + IncludeComments);
                Console.WriteLine("Reading symbol file...");
                var lines = File.ReadAllLines(SymbolFile);
                Console.WriteLine("Read " + lines.Length + " line(s).");
                var r_RL = new Regex(SymbolRegEx, RegexOptions.Singleline);
                var r_BA = new Regex(BankRegEx, RegexOptions.Singleline);
                var RL_matches = new Dictionary<string, string>();
                var BA_matches = new Dictionary<string, string>();
                foreach (string line in lines)
                {
                    var m = r_RL.Match(line);
                    if (m.Groups["Address"].Success)
                    {
                        string oldval = m.Groups["Address"].Value;
                        string newval = oldval;
                        if (AddressOffset != 0)
                        {
                            if (oldval.StartsWith("$"))
                            {
                                string v = "0x" + oldval.Substring(1);
                                int nv = Convert.ToInt32(v, 16);
                                nv += AddressOffset;
                                newval = "$" + nv.ToString("X4");
                            }
                            else if (oldval.StartsWith("#"))
                            {
                                string v = "0x" + oldval.Substring(1);
                                int nv = Convert.ToInt32(v, 16);
                                nv += AddressOffset;
                                newval = "#" + nv.ToString("X4");
                            }
                            else if (oldval.StartsWith("0x"))
                            {
                                int nv = Convert.ToInt32(oldval, 16);
                                nv += AddressOffset;
                                newval = "0x" + nv.ToString("X4");
                            }
                            else
                            {
                                int nv = Convert.ToInt32(oldval);
                                nv += AddressOffset;
                                newval = nv.ToString();
                            }
                        }
                        RL_matches.Add(newval, line);
                    }
                    m = r_BA.Match(line);
                    if (m.Groups["Address"].Success)
                    {
                        string newval = m.Groups["Address"].Value;
                        BA_matches.Add(newval, line);
                    }
                }
                Console.WriteLine("Matched " + RL_matches.Count + " relocation line(s).");
                Console.WriteLine("Matched " + BA_matches.Count + " bank line(s).");
                Console.WriteLine("Writing relocate table file...");
                var sb = new StringBuilder();
                if (IncludeComments)
                {
                    sb.Append(Comment);
                    sb.AppendLine(Path.GetFileName(RelocateTableFile));
                    sb.Append(Comment);
                    sb.AppendLine("Generated automatically by Relocate.exe");
                    sb.AppendLine();
                }
                foreach (var line in RL_matches)
                {
                    sb.Append(DefineWord);
                    sb.Append(line.Key);
                    if (IncludeComments)
                    {
                        sb.Append(" ");
                        sb.Append(Comment);
                        sb.Append(line.Value);
                    }
                    sb.AppendLine();
                }
                File.WriteAllText(RelocateTableFile, sb.ToString());
                Console.WriteLine("Writing relocate count file...");
                sb = new StringBuilder();
                if (IncludeComments)
                {
                    sb.Append(Comment);
                    sb.AppendLine(Path.GetFileName(RelocateCountFile));
                    sb.Append(Comment);
                    sb.AppendLine("Generated automatically by Relocate.exe");
                    sb.AppendLine();
                }
                sb.Append("RelocateCount" + Equate);
                sb.Append(RL_matches.Count.ToString());
                if (IncludeComments)
                {
                    sb.Append(" ");
                    sb.Append(Comment);
                    sb.Append("Relocation table is " + (RL_matches.Count * 2) + " byte(s) long");
                }
                sb.AppendLine();
                foreach (var line in BA_matches)
                {
                    sb.Append(line.Value);
                    if (IncludeComments)
                    {
                        sb.Append(" ");
                        sb.Append(Comment);
                        sb.Append("A bank ID will get patched here");
                    }
                    sb.AppendLine();
                }
                File.WriteAllText(RelocateCountFile, sb.ToString());
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (Interactive)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
            return 1;
        }
    }
}
