using System.Text;

namespace eBook.Services.Books;

public static class BookPaginator
{
    // Dijeli tekst knjige na stranice, po mogucnosti na granicama paragrafa.
    public static List<string> Split(string text, int maxCharactersPerPage)
    {
        var normalized = text.Replace("\r\n", "\n").Trim();
        var pages = new List<string>();
        var current = new StringBuilder();

        var paragraphs = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var paragraph in paragraphs)
        {
            var remaining = paragraph;

            while (remaining.Length > maxCharactersPerPage)
            {
                var cut = remaining.LastIndexOf(' ', maxCharactersPerPage);
                if (cut <= 0)
                {
                    cut = maxCharactersPerPage;
                }

                AppendParagraph(pages, current, remaining[..cut].Trim(), maxCharactersPerPage);
                remaining = remaining[cut..].TrimStart();
            }

            AppendParagraph(pages, current, remaining, maxCharactersPerPage);
        }

        if (current.Length > 0)
        {
            pages.Add(current.ToString());
        }

        return pages;
    }

    private static void AppendParagraph(List<string> pages, StringBuilder current, string paragraph, int maxCharactersPerPage)
    {
        if (paragraph.Length == 0)
        {
            return;
        }

        var separatorLength = current.Length == 0 ? 0 : 2;

        if (current.Length + separatorLength + paragraph.Length > maxCharactersPerPage)
        {
            pages.Add(current.ToString());
            current.Clear();
        }

        if (current.Length > 0)
        {
            current.Append("\n\n");
        }

        current.Append(paragraph);
    }
}
