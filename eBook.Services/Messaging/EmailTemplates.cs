using System.Net;

namespace eBook.Services.Messaging;

public record EmailContent(string Subject, string Body);

public static class EmailTemplates
{
    public static EmailContent Welcome(string firstName)
    {
        var name = WebUtility.HtmlEncode(firstName);

        return new EmailContent(
            "Dobrodošli u eBook",
            $"<h2>Dobrodošli, {name}!</h2>" +
            "<p>Vaš eBook račun je uspješno kreiran. Pregledajte katalog, čitajte besplatne knjige " +
            "ili odaberite Premium pretplatu za cijelu biblioteku.</p>" +
            "<p>Ugodno čitanje!</p>");
    }

    public static EmailContent PasswordReset(string firstName, string code, int validityMinutes)
    {
        var name = WebUtility.HtmlEncode(firstName);

        return new EmailContent(
            "eBook – kod za reset lozinke",
            $"<h2>Reset lozinke</h2>" +
            $"<p>Poštovani {name}, vaš kod za reset lozinke je:</p>" +
            $"<h1 style=\"letter-spacing:4px\">{code}</h1>" +
            $"<p>Kod važi {validityMinutes} minuta. Ako niste tražili reset lozinke, slobodno zanemarite ovu poruku.</p>");
    }
}
