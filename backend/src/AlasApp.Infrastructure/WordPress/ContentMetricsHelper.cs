using System.Text.RegularExpressions;

namespace AlasApp.Infrastructure.WordPress;

public static partial class ContentMetricsHelper
{
    [GeneratedRegex("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex HtmlTagsRegex();

    public static int CalculateReadTime(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return 1;
        }

        var textOnly = HtmlTagsRegex().Replace(htmlContent, string.Empty);
        var wordCount = textOnly.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = (int)Math.Ceiling(wordCount / 200.0);

        return minutes == 0 ? 1 : minutes;
    }
}
