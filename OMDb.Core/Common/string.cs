﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OMDb.Core
{
    public static class @string
    {
        public static int NthIndexOf(this string target, string value, int n)
        {
            string pattern = "((" + Regex.Escape(value) + ").*?){" + n + "}";
            Match m = Regex.Match(target, pattern);

            if (m.Success)
                return m.Groups[2].Captures[n - 1].Index;
            else
                return -1;
        }


        public static int NthLastIndexOf(this string target, string value, int n)
        {
            var target_charArray = target.Reverse();
            var target_reverse = new string(target_charArray.ToArray());

            string pattern = "((" + Regex.Escape(value) + ").*?){" + n + "}";
            Match m = Regex.Match(target_reverse, pattern);

            if (m.Success)
                return (target.Length - 1) - (m.Groups[2].Captures[n - 1].Index);
            else
                return -1;
        }


        private static int NIndex(this string target, string value, int n, bool isForward)
        {
            if (isForward)
            {
                return target.NthIndexOf(value, n);
            }
            else
            {
                return target.NthLastIndexOf(value, n);
            }
        }


        public static string SubStringByIndex(this string sourceStr, int startIndex, int endIndex)
        {
            return sourceStr.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// 字符串初始位置截取到指定位置
        /// </summary>
        /// <param name="sourceStr">原始字符串</param>
        /// <param name="str_End">终止字符串</param>
        /// <param name="n_End">第几个终止字符串</param>
        /// <param name="isForward_End">终止字符串 是否从前往后数</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string SubString_02B(this string sourceStr, string str_End, int n_End, bool isForward_End = true)
        {
            if (str_End == string.Empty) return sourceStr;
            if (!sourceStr.Contains(str_End)) throw new Exception(string.Format($@"Source string does not exist:[{str_End}]"));
            var index_End = sourceStr.NIndex(str_End, n_End, isForward_End);
            if (index_End == -1) throw new Exception(string.Format($@"{str_End}[{n_End}] out of index!"));
            return sourceStr.SubStringByIndex(0, index_End) + str_End;
        }

        public static string SubString_A21(this string sourceStr, string str_Start, int n_Start, bool isForward_Start = true)
        {
            if (str_Start == string.Empty) return sourceStr;
            if (!sourceStr.Contains(str_Start)) throw new Exception(string.Format($@"Source string does not exist:[{str_Start}]"));
            var index_Start = sourceStr.NIndex(str_Start, n_Start, isForward_Start);
            if (index_Start == -1) throw new Exception(string.Format($@"{str_Start}[{n_Start}] out of index!"));
            return sourceStr.SubStringByIndex(index_Start, sourceStr.Length);
        }

        public static string SubString_A2B(this string sourceStr,
            string str_Start, string str_End, int n_Start, int n_End,
            bool isForward_Start = true, bool isForward_End = true)
        {
            if (str_Start != string.Empty || str_End != string.Empty)
            {
                var index_End = sourceStr.NIndex(str_End, n_End, isForward_End);
                var index_Start = sourceStr.NIndex(str_Start, n_Start, isForward_Start);
                if (index_End == -1 || index_Start == -1)
                {
                    bool isContain_strStart = sourceStr.Contains(str_Start);
                    bool isContain_strEnd = sourceStr.Contains(str_End);
                    if (!isContain_strStart || !isContain_strEnd)
                    {
                        throw new Exception(string.Format("Source string does not exist:{0}{1}{2}",
                            isContain_strStart ? "[" + str_Start + "]" : "",
                            (isContain_strStart && isContain_strEnd) ? " and " : "",
                            isContain_strEnd ? "[" + str_End + "]" : ""));
                    }

                    StringBuilder sb = new StringBuilder();
                    if (index_Start == -1) sb.AppendFormat("[{0}][{1}({2})] not exist!", str_Start, n_Start, isForward_Start ? "Forward" : "Backward");
                    if (index_Start == -1 && index_End == -1) sb.AppendFormat(" and ");
                    if (index_End == -1) sb.AppendFormat("[{0}][{1}({2})] not exist!", str_End, n_End, isForward_End ? "Forward" : "Backward");
                    throw new Exception(sb.ToString());
                }
                if (index_Start >= index_End) throw new Exception(string.Format($@"Index[{str_Start}]({index_Start})>=Index[{str_End}]({index_End})"));
                return sourceStr.SubStringByIndex(index_Start, index_End) + str_End;
            }
            return sourceStr;
        }
    }
}