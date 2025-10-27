using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {
        // HTML void elements (no closing tag)
        private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "area","base","br","col","embed","hr","img","input","link","meta",
            "param","source","track","wbr"
        };

        // Tags where we should not count/alter inner text (keep raw)
        private static readonly HashSet<string> NoCountTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "script","style"
        };

        // Tags where we should not collapse whitespace
        private static readonly HashSet<string> NoCollapseTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "pre","code","textarea"
        };

        /// <summary>
        /// Minifies the provided HTML string while preserving structure (no trimming).
        /// - Collapses inter-element whitespace (except inside pre/code/textarea).
        /// - Leaves script/style content untouched.
        /// - Avoids generating invalid closing tags for void elements.
        /// - Lightly minifies attribute spacing.
        /// </summary>
        public static string TrimAndMinifyHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var output = new StringBuilder();
            var tagStack = new Stack<string>();

            // Order matters: comments/doctype first, then regular tags
            var tagRegex = new Regex(
                @"<!--.*?-->|<!DOCTYPE[^>]*>|</?([a-zA-Z][a-zA-Z0-9:\-]*)(\s[^>]*?)?>",
                RegexOptions.Compiled | RegexOptions.Singleline);

            int lastIndex = 0;

            foreach (Match match in tagRegex.Matches(html))
            {
                int tagStart = match.Index;
                int tagEnd = tagStart + match.Length;

                // Text between previous tag and this one
                string textBetween = html.Substring(lastIndex, tagStart - lastIndex);

                bool insideNoCount = ContainsAny(tagStack, NoCountTags);
                bool insideNoCollapse = ContainsAny(tagStack, NoCollapseTags);

                if (insideNoCount)
                {
                    // Inside script/style: pass through exactly
                    output.Append(textBetween);
                }
                else
                {
                    // Outside script/style: collapse unless in pre/code/textarea
                    output.Append(insideNoCollapse ? textBetween : CollapseWhitespace(textBetween));
                }

                string fullTag = match.Value;
                string tagName = match.Groups[1].Success ? match.Groups[1].Value : string.Empty;

                // Comments or DOCTYPE: output as-is
                if (fullTag.StartsWith("<!--") || fullTag.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    output.Append(fullTag);
                    lastIndex = tagEnd;
                    continue;
                }

                bool isClose = fullTag.Length > 1 && fullTag[1] == '/';
                bool isSelfClosingSyntax = fullTag.EndsWith("/>", StringComparison.Ordinal);
                bool isVoid = !string.IsNullOrEmpty(tagName) && VoidTags.Contains(tagName);

                if (isClose)
                {
                    // Pop only if it matches the current open tag
                    if (tagStack.Count > 0 && string.Equals(tagStack.Peek(), tagName, StringComparison.OrdinalIgnoreCase))
                        tagStack.Pop();

                    // Output original closing tag
                    output.Append(fullTag);
                }
                else
                {
                    // Opening tag: lightly minify attributes/spacing
                    string tagMin = MinifyTag(fullTag);

                    // If it's void or explicitly self-closing, don't push to the stack
                    if (!(isVoid || isSelfClosingSyntax))
                        tagStack.Push(tagName);

                    output.Append(tagMin);
                }

                lastIndex = tagEnd;
            }

            // Remaining tail text after the last tag
            if (lastIndex < html.Length)
            {
                string tail = html.Substring(lastIndex);
                bool insideNoCountTail = ContainsAny(tagStack, NoCountTags);
                bool insideNoCollapseTail = ContainsAny(tagStack, NoCollapseTags);

                if (insideNoCountTail)
                {
                    output.Append(tail);
                }
                else
                {
                    output.Append(insideNoCollapseTail ? tail : CollapseWhitespace(tail));
                }
            }

            // Close any remaining non-void open tags to keep structure valid
            while (tagStack.Count > 0)
            {
                var open = tagStack.Pop();
                if (!VoidTags.Contains(open))
                    output.Append("</").Append(open).Append('>');
            }

            return output.ToString();
        }

        private static bool ContainsAny(Stack<string> stack, HashSet<string> names)
        {
            foreach (var t in stack)
                if (names.Contains(t)) return true;
            return false;
        }

        // Collapse runs of whitespace to a single space.
        // Keeps leading/trailing spaces only if they are meaningful between text nodes.
        private static string CollapseWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Replace sequences of whitespace with a single space
            string collapsed = Regex.Replace(input, @"\s+", " ");

            // Remove space directly inside tags boundaries: > <  (avoid introducing text shifts)
            collapsed = Regex.Replace(collapsed, @">\s+<", "><");

            return collapsed;
        }

        // Light tag minification: tighten around '=', collapse extra spaces, trim before '>'.
        // Keeps attribute values and quoting intact (we do not reorder or drop attributes).
        private static string MinifyTag(string tag)
        {
            // Don’t touch comments/doctype
            if (tag.StartsWith("<!--") || tag.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                return tag;

            string result = tag;

            // Tighten spaces around '='
            result = Regex.Replace(result, @"\s*=\s*", "=");

            // Collapse 2+ spaces to one (safe enough for tags)
            result = Regex.Replace(result, @"\s{2,}", " ");

            // Remove spaces just before '>'
            result = Regex.Replace(result, @"\s+>", ">");

            return result;
        }
    }
}
