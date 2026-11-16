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

    public static EmailContent SubscriptionActivated(string firstName, string planName, DateTime endDate)
    {
        var name = WebUtility.HtmlEncode(firstName);
        var plan = WebUtility.HtmlEncode(planName);

        return new EmailContent(
            "eBook – pretplata aktivirana",
            $"<h2>Hvala na kupovini, {name}!</h2>" +
            $"<p>Vaša pretplata <b>{plan}</b> je aktivna do <b>{endDate:dd.MM.yyyy}</b>.</p>" +
            "<p>Sve Premium knjige su vam sada dostupne.</p>");
    }

    public static EmailContent SubscriptionCancelled(string firstName, string planName, string reason)
    {
        var name = WebUtility.HtmlEncode(firstName);
        var plan = WebUtility.HtmlEncode(planName);
        var reasonText = WebUtility.HtmlEncode(reason);

        return new EmailContent(
            "eBook – pretplata otkazana",
            $"<h2>Pretplata otkazana</h2>" +
            $"<p>Poštovani {name}, vaša pretplata <b>{plan}</b> je otkazana.</p>" +
            $"<p>Razlog: {reasonText}</p>");
    }

    public static EmailContent PaymentRefunded(string firstName, decimal amount, string currency)
    {
        var name = WebUtility.HtmlEncode(firstName);

        return new EmailContent(
            "eBook – povrat novca",
            $"<h2>Povrat novca</h2>" +
            $"<p>Poštovani {name}, vraćeno vam je <b>{amount:0.00} {currency.ToUpperInvariant()}</b> na karticu kojom ste platili.</p>" +
            "<p>Povrat može biti vidljiv u roku od nekoliko radnih dana.</p>");
    }
}
