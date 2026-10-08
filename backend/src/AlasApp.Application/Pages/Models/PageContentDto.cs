namespace AlasApp.Application.Pages.Models;

/// <summary>
/// Editable text of a static page managed in WordPress, as plain-text paragraphs (no HTML).
/// <see cref="Intro"/> is the text before the first H2 heading; each H2 opens a <see cref="Sections"/> entry.
/// </summary>
public sealed record PageContentDto(
    string Slug,
    string Lang,
    string Title,
    IReadOnlyList<string> Intro,
    IReadOnlyList<PageSectionDto> Sections);

public sealed record PageSectionDto(string Title, IReadOnlyList<string> Paragraphs);
