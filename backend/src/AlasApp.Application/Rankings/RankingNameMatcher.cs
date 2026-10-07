using System.Globalization;
using System.Text;

namespace AlasApp.Application.Rankings;

/// <summary>
/// Ranking snapshots (SurfScores) only store the competitor's name as free text, e.g.
/// "julian schweizer" or "Viviana Margarita Araya Riquelme". This finds a registered competitor's
/// position ignoring case, accents, word order and extra middle names / second surnames.
/// </summary>
public sealed class RankingNameMatcher
{
    private readonly Dictionary<string, int> _byFullName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _bySortedTokens = new(StringComparer.Ordinal);
    private readonly List<(HashSet<string> Tokens, int Position)> _entries = [];

    public RankingNameMatcher(IEnumerable<(string Name, int Position)> entries)
    {
        foreach (var (name, position) in entries)
        {
            var tokens = Tokenize(name);
            if (tokens.Count == 0)
            {
                continue;
            }

            _byFullName.TryAdd(string.Join(' ', tokens), position);
            _bySortedTokens.TryAdd(string.Join(' ', tokens.Order(StringComparer.Ordinal)), position);
            _entries.Add((tokens.ToHashSet(StringComparer.Ordinal), position));
        }
    }

    public int? FindPosition(string firstName, string lastName)
    {
        var tokens = Tokenize($"{firstName} {lastName}");
        if (tokens.Count == 0)
        {
            return null;
        }

        if (_byFullName.TryGetValue(string.Join(' ', tokens), out var exact))
        {
            return exact;
        }

        if (_bySortedTokens.TryGetValue(string.Join(' ', tokens.Order(StringComparer.Ordinal)), out var reordered))
        {
            return reordered;
        }

        // One name contains the other ("Viviana Araya" ↔ "Viviana Margarita Araya Riquelme").
        // Needs at least two shared words and a single candidate, so homonyms never get mixed up.
        var competitor = tokens.ToHashSet(StringComparer.Ordinal);
        var candidates = _entries
            .Where(e => Math.Min(e.Tokens.Count, competitor.Count) >= 2 &&
                        (e.Tokens.IsSubsetOf(competitor) || competitor.IsSubsetOf(e.Tokens)))
            .Select(e => e.Position)
            .Distinct()
            .Take(2)
            .ToList();

        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static List<string> Tokenize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
