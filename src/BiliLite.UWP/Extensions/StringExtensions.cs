﻿using Newtonsoft.Json;
using System.Text;
using System;
using BiliLite.Helpers;
using BiliLite.Models.Common;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;

namespace BiliLite.Extensions
{
    /// <summary>
    /// cc转为SRT
    /// </summary>
    /// <param name="json">CC字幕</param>
    /// <param name="toSimplified">转为简体</param>
    /// <returns></returns>
    public static class StringExtensions
    {
        public static string CcConvertToSrt(this string json, bool toSimplified = false)
        {
            var subtitle = JsonConvert.DeserializeObject<Subtitle>(json);
            var stringBuilder = new StringBuilder();
            var i = 1;
            foreach (var item in subtitle.body)
            {
                var start = TimeSpan.FromSeconds(item.from);
                var end = TimeSpan.FromSeconds(item.to);
                stringBuilder.AppendLine(i.ToString());
                stringBuilder.AppendLine($"{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}");
                var content = item.content;
                if (toSimplified)
                {
                    content = Utils.ToSimplifiedChinese(content);
                }

                stringBuilder.AppendLine(content);
                stringBuilder.AppendLine();
                i++;
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 简体转繁体
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string SimplifiedToTraditional(this string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var key in input)
            {
                stringBuilder.Append(ChineseDictionary.chsDictionary.ContainsKey(key)
                    ? ChineseDictionary.chsDictionary[key]
                    : key);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 繁体转简体
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string TraditionalToSimplified(this string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var key in input)
            {
                stringBuilder.Append(ChineseDictionary.chtDictionary.ContainsKey(key)
                    ? ChineseDictionary.chtDictionary[key]
                    : key);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 文本转富文本控件
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="emote"></param>
        /// <returns></returns>
        public static RichTextBlock ToRichTextBlock(this string txt, JObject emote)
        {
            string input = txt;
            try
            {
                if (txt != null)
                {

                    //处理特殊字符
                    input = input.Replace("&", "&amp;");
                    input = input.Replace("<", "&lt;");
                    input = input.Replace(">", "&gt;");
                    input = input.Replace("\r\n", "<LineBreak/>");
                    input = input.Replace("\n", "<LineBreak/>");
                    //处理链接
                    input = HandelUrl(input);

                    //处理表情
                    input = HandelEmoji(input, emote);

                    //处理av号
                    input = HandelVideoID(input);



                    //生成xaml
                    var xaml = string.Format(@"<RichTextBlock HorizontalAlignment=""Stretch"" TextWrapping=""Wrap""  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
    xmlns:mc = ""http://schemas.openxmlformats.org/markup-compatibility/2006"" LineHeight=""20"">
                                          <Paragraph>{0}</Paragraph>
                                      </RichTextBlock>", input);
                    var p = (RichTextBlock)XamlReader.Load(xaml);
                    return p;
                }
                else
                {
                    var tx = new RichTextBlock();
                    Paragraph paragraph = new Paragraph();
                    Run run = new Run() { Text = txt };
                    paragraph.Inlines.Add(run);
                    tx.Blocks.Add(paragraph);
                    return tx;
                }
            }
            catch (Exception)
            {
                var tx = new RichTextBlock();
                Paragraph paragraph = new Paragraph();
                Run run = new Run() { Text = txt };
                paragraph.Inlines.Add(run);
                tx.Blocks.Add(paragraph);
                return tx;

            }
        }

        public static string ProtectValues(this string url, params string[] keys)
        {
            foreach (string key in keys)
            {
                string pattern = $@"({key}=)([^&]*)";
                string replacement = $"$1{{hasValue}}";
                url = Regex.Replace(url, pattern, replacement);
            }
            return url;
        }

        #region Private methods

        /// <summary>
        /// 处理表情
        /// </summary>
        private static string HandelEmoji(string input, JObject emote)
        {
            if (emote == null) return input;
            //替换表情
            MatchCollection mc = Regex.Matches(input, @"\[.*?\]");
            foreach (Match item in mc)
            {
                if (emote != null && emote.ContainsKey(item.Groups[0].Value))
                {
                    var emoji = emote[item.Groups[0].Value];
                    input = input.Replace(item.Groups[0].Value,
                        string.Format(
                            @"<InlineUIContainer><Border  Margin=""0 -4 4 -4""><Image Source=""{0}"" Width=""{1}"" Height=""{1}"" /></Border></InlineUIContainer>",
                            emoji["url"].ToString(), emoji["meta"]["size"].ToInt32() == 1 ? "20" : "36"));
                }
            }

            return input;
        }

        /// <summary>
        /// 处理视频AVID,BVID,CVID
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string HandelVideoID(string input)
        {
            //处理AV号
            List<string> keyword = new List<string>();
            //如果是链接就不处理了
            if (!Regex.IsMatch(input, @"/[aAbBcC][vV]([a-zA-Z0-9]+)"))
            {
                //处理AV号
                MatchCollection av = Regex.Matches(input, @"[aA][vV](\d+)");
                foreach (Match item in av)
                {
                    if (keyword.Contains(item.Groups[0].Value))
                    {
                        continue;
                    }

                    keyword.Add(item.Groups[0].Value);
                    var data =
                        @"<InlineUIContainer><HyperlinkButton Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " +
                        string.Format(
                            @" CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>",
                            item.Groups[0].Value, "bilibili://video/" + item.Groups[0].Value);
                    input = input.Replace(item.Groups[0].Value, data);
                }

                //处理AV号
                MatchCollection bv = Regex.Matches(input, @"[bB][vV]([a-zA-Z0-9]{8,})");
                foreach (Match item in bv)
                {
                    if (keyword.Contains(item.Groups[0].Value))
                    {
                        continue;
                    }

                    keyword.Add(item.Groups[0].Value);
                    var data =
                        @"<InlineUIContainer><HyperlinkButton Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " +
                        string.Format(
                            @" CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>",
                            item.Groups[0].Value, "bilibili://video/" + item.Groups[0].Value);
                    input = input.Replace(item.Groups[0].Value, data);
                }

                //处理CV号

                MatchCollection cv = Regex.Matches(input, @"[cC][vV](\d+)");
                foreach (Match item in cv)
                {
                    if (keyword.Contains(item.Groups[0].Value))
                    {
                        continue;
                    }

                    keyword.Add(item.Groups[0].Value);
                    var data =
                        @"<InlineUIContainer><HyperlinkButton Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " +
                        string.Format(
                            @" CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>",
                            item.Groups[0].Value, "bilibili://article/" + item.Groups[1].Value);
                    input = input.Replace(item.Groups[0].Value, data);
                }
            }

            keyword.Clear();
            keyword = null;
            return input;
        }

        /// <summary>
        /// 处理URL链接
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string HandelUrl(string input)
        {
            //处理AV号
            List<string> keyword = new List<string>();
            MatchCollection url = Regex.Matches(input,
                @"(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
            foreach (Match item in url)
            {
                if (keyword.Contains(item.Groups[0].Value))
                {
                    continue;
                }

                keyword.Add(item.Groups[0].Value);
                var data =
                    @"<InlineUIContainer><HyperlinkButton x:Name=""btn"" Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " +
                    string.Format(
                        @" CommandParameter=""{0}"" ><TextBlock>🔗网页链接</TextBlock></HyperlinkButton></InlineUIContainer>",
                        item.Groups[0].Value);
                input = input.Replace(item.Groups[0].Value, data);
            }


            return input;

            //MatchCollection url = Regex.Matches(input, @"(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
            //foreach (Match item in url)
            //{
            //    var data = @"<InlineUIContainer><HyperlinkButton x:Name=""btn"" Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " + string.Format(@" Tag=""{0}""  CommandParameter=""{0}"" >{0}</HyperlinkButton></InlineUIContainer>", item.Groups[0].Value);
            //    input = input.Replace(item.Groups[0].Value, data);
            //}


            //return input;
        }

        #endregion
    }
}
