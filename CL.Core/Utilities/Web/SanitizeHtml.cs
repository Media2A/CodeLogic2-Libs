using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {
        // Default allowed tags and attributes
        private static readonly List<string> DefaultAllowedTags = new List<string>
        {
            "b", "i", "u", "a", "p", "br", "ul", "ol", "li", "strong", "em", "span", "div", "img"
        };

        private static readonly Dictionary<string, List<string>> DefaultAllowedAttributes =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "a", new List<string> { "href", "title", "target" } },
                { "img", new List<string> { "src", "alt", "title", "width", "height" } }
            };

        // Attribute regex: captures name and value (without the leading '=')
        private static readonly Regex AttributeRegex = new Regex(
            @"(?<name>[A-Za-z0-9:-]+)(?:\s*=\s*(?:'(?<val1>[^']*)'|""(?<val2>[^""]*)""|(?<val3>[^\s>]+)))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        // Allowed absolute URI schemes
        private static readonly HashSet<string> AllowedUriSchemes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "http", "https", "mailto", "tel"
            };

        // HTML void elements to self-close
        private static readonly HashSet<string> VoidElements =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "area","base","br","col","embed","hr","img","input","link","meta","param","source","track","wbr"
            };

        /// <summary>
        /// Validates the input HTML to ensure it contains only allowed tags (simple nesting check).
        /// </summary>
        public static bool ValidateHtml(string input, List<string> allowedTags = null, Dictionary<string, List<string>> allowedAttributes = null)
        {
            if (string.IsNullOrEmpty(input))
                return true; // Empty or plain text input is valid

            allowedTags ??= DefaultAllowedTags;

            // Regex to extract HTML tags
            var tagRegex = new Regex(@"<\s*(\/?)(\w+)", RegexOptions.IgnoreCase);
            var matches = tagRegex.Matches(input);

            // If no HTML tags are found, treat as plain text (valid)
            if (matches.Count == 0)
                return true;

            var tagStack = new Stack<string>();

            foreach (Match match in matches)
            {
                var isClosingTag = match.Groups[1].Value == "/";
                var tagName = match.Groups[2].Value.ToLower();

                // Check if tag is allowed
                if (!allowedTags.Contains(tagName))
                {
                    Console.WriteLine($"Invalid tag detected: <{tagName}>");
                    return false; // Found an invalid tag
                }

                if (!isClosingTag)
                {
                    // Opening tag - push to stack
                    tagStack.Push(tagName);
                }
                else
                {
                    // Closing tag - check stack
                    if (tagStack.Count == 0 || tagStack.Pop() != tagName)
                    {
                        Console.WriteLine($"Tag nesting error detected: <{tagName}>");
                        return false; // Mismatched or extra closing tag
                    }
                }
            }

            // Ensure no unclosed tags remain in the stack
            if (tagStack.Count > 0)
            {
                Console.WriteLine($"Unclosed tags detected: {string.Join(", ", tagStack)}");
                return false;
            }

            return true; // All tags are properly nested and closed
        }

        /// <summary>
        /// Sanitizes the input HTML by removing invalid tags and attributes.
        /// Fixes:
        ///  - Walks every tag (not just outer pairs), so nested content is sanitized.
        ///  - Drops disallowed tags/attrs and event handlers (on*).
        ///  - Blocks javascript:, vbscript:, and non-image data: URIs.
        ///  - Normalizes void elements (<img>, <br>, etc.) to self-closing.
        /// </summary>
        public static string SanitizeHtml(string input, List<string> allowedTags = null, Dictionary<string, List<string>> allowedAttributes = null)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            allowedTags ??= DefaultAllowedTags;
            allowedAttributes ??= DefaultAllowedAttributes;

            // 1) Remove dangerous paired tags entirely (content included)
            var dangerousTagsPattern = @"<(?<tag>script|iframe|style|object|embed)[^>]*>.*?</\k<tag>>";
            input = Regex.Replace(input, dangerousTagsPattern, string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Also remove standalone/self-closing versions of these tags
            var dangerousOpenPattern = @"</?(script|iframe|style|object|embed)\b[^>]*?/?>";
            input = Regex.Replace(input, dangerousOpenPattern, string.Empty,
                RegexOptions.IgnoreCase);

            // 2) Walk every remaining tag and rebuild allowed ones with cleaned attributes
            var tagRegex = new Regex(
                @"<(?<closing>/)?(?<tag>\w+)(?<attrs>[^>]*)>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
            );

            string result = tagRegex.Replace(input, m =>
            {
                bool isClosing = m.Groups["closing"].Success;
                string tagName = m.Groups["tag"].Value.ToLowerInvariant();

                // Drop tags that are not allowed (remove entirely)
                if (!allowedTags.Contains(tagName))
                    return string.Empty;

                if (isClosing)
                {
                    // Keep closing tag for allowed non-void tags; drop closing for voids
                    return !VoidElements.Contains(tagName) ? $"</{tagName}>" : string.Empty;
                }

                string attrs = m.Groups["attrs"].Value;
                var cleanAttributes = new List<string>();

                if (allowedAttributes.TryGetValue(tagName, out var allowedForTag))
                {
                    foreach (Match am in AttributeRegex.Matches(attrs))
                    {
                        string name = am.Groups["name"].Value;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        // Drop any event handler attributes (onload, onerror, onclick, ...)
                        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!allowedForTag.Contains(name, StringComparer.OrdinalIgnoreCase))
                            continue;

                        // Extract unquoted value
                        string rawVal =
                            am.Groups["val1"].Success ? am.Groups["val1"].Value :
                            am.Groups["val2"].Success ? am.Groups["val2"].Value :
                            am.Groups["val3"].Success ? am.Groups["val3"].Value :
                            string.Empty;

                        // Special handling for href/src
                        if (name.Equals("href", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("src", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!IsSafeUri(rawVal, tagName, name))
                                continue;
                        }

                        string encoded = System.Net.WebUtility.HtmlEncode(rawVal ?? string.Empty);
                        cleanAttributes.Add($"{name}=\"{encoded}\"");
                    }
                }

                string attrsOut = cleanAttributes.Count > 0 ? " " + string.Join(" ", cleanAttributes) : string.Empty;

                // Normalize void elements to self-closing
                if (VoidElements.Contains(tagName))
                    return $"<{tagName}{attrsOut} />";

                // Non-void opening tag
                return $"<{tagName}{attrsOut}>";
            });

            return result;
        }

        // Helper: validate URIs against a safe scheme allowlist and basic data: filtering
        private static bool IsSafeUri(string value, string tagName, string attrName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string v = System.Net.WebUtility.HtmlDecode(value).Trim();

            // Block script/vbscript
            if (v.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return false;
            if (v.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase)) return false;

            // Allow data: only for images in <img src="data:image/...">
            if (v.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                if (tagName.Equals("img", StringComparison.OrdinalIgnoreCase) &&
                    attrName.Equals("src", StringComparison.OrdinalIgnoreCase) &&
                    v.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            }

            // Relative URLs are OK; absolute URLs must have an allowed scheme
            if (Uri.TryCreate(v, UriKind.RelativeOrAbsolute, out var uri))
            {
                if (!uri.IsAbsoluteUri)
                    return true;

                return AllowedUriSchemes.Contains(uri.Scheme);
            }

            return false;
        }
    }
}
