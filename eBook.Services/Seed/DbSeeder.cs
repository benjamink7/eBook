using System.Reflection;
using eBook.Model.Constants;
using eBook.Model.Enums;
using eBook.Services.Database;
using eBook.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBook.Services.Seed;

public static class DbSeeder
{
    private const string DefaultPassword = "test";
    private const int DemoPagesPerBook = 12;

    private sealed record BookSeed(
        string CoverName, string Title, string Description, int Year,
        BookAccessType Access, string AuthorName, string[] Genres);

    private static readonly BookSeed[] Books =
    [
        new("pride", "Ponos i predrasude", "Roman o Elizabeth Bennet i gospodinu Darcyju, o ponosu, društvenim predrasudama i ljubavi u engleskom društvu 19. stoljeća.", 1813, BookAccessType.Free, "Jane Austen", ["Roman", "Klasici"]),
        new("frankenstein", "Frankenstein", "Mladi naučnik Victor Frankenstein stvara živo biće i suočava se s posljedicama svog eksperimenta.", 1818, BookAccessType.Free, "Mary Shelley", ["Roman", "Znanstvena fantastika", "Horor"]),
        new("dracula", "Drakula", "Grupa prijatelja se suprotstavlja grofu Drakuli, vampiru koji iz Transilvanije dolazi u Englesku.", 1897, BookAccessType.Premium, "Bram Stoker", ["Horor", "Klasici"]),
        new("studyinscarlet", "Studija u grimizu", "Prva priča o Sherlocku Holmesu i doktoru Watsonu, u kojoj istražuju tajanstveno ubistvo u Londonu.", 1887, BookAccessType.Free, "Arthur Conan Doyle", ["Krimi"]),
        new("leagues", "Dvadeset hiljada milja pod morem", "Kapetan Nemo i njegova podmornica Nautilus vode profesora Aronnaxa kroz podmorske dubine svijeta.", 1870, BookAccessType.Premium, "Jules Verne", ["Avantura", "Znanstvena fantastika"]),
        new("crime", "Zločin i kazna", "Siromašni student Raskoljnikov počini ubistvo i bori se s krivicom i savješću.", 1866, BookAccessType.Premium, "Fjodor Dostojevski", ["Roman", "Klasici"]),
        new("metamorphosis", "Preobražaj", "Gregor Samsa se jednog jutra budi pretvoren u golemog kukca, a njegova porodica se mora nositi s tom promjenom.", 1915, BookAccessType.Free, "Franz Kafka", ["Klasici", "Roman"]),
        new("timemachine", "Vremeplov", "Naučnik putuje daleko u budućnost i otkriva društvo podijeljeno na dvije vrste bića.", 1895, BookAccessType.Free, "H. G. Wells", ["Znanstvena fantastika", "Avantura"]),
        new("dorian", "Slika Doriana Graya", "Mladić ostaje vječno mlad dok njegov portret nosi tragove svih njegovih grijeha.", 1890, BookAccessType.Premium, "Oscar Wilde", ["Roman", "Klasici"]),
        new("tomsawyer", "Pustolovine Toma Sawyera", "Dječak Tom doživljava pustolovine na obalama Mississippija zajedno s prijateljem Huckleberryjem Finnom.", 1876, BookAccessType.Free, "Mark Twain", ["Avantura", "Roman"]),
    ];

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Roles.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var adminRole = new Role { Name = AppRoles.Admin };
        var userRole = new Role { Name = AppRoles.User };

        var bosnia = new Country { Name = "Bosna i Hercegovina" };
        var croatia = new Country { Name = "Hrvatska" };
        var serbia = new Country { Name = "Srbija" };

        var sarajevo = new City { Name = "Sarajevo", Country = bosnia };
        var mostar = new City { Name = "Mostar", Country = bosnia };
        var cities = new List<City>
        {
            sarajevo, mostar,
            new() { Name = "Tuzla", Country = bosnia },
            new() { Name = "Banja Luka", Country = bosnia },
            new() { Name = "Zagreb", Country = croatia },
            new() { Name = "Split", Country = croatia },
            new() { Name = "Beograd", Country = serbia }
        };

        var genres = new[] { "Roman", "Klasici", "Krimi", "Znanstvena fantastika", "Horor", "Avantura", "Poezija", "Historija" }
            .ToDictionary(name => name, name => new Genre { Name = name });

        var (adminHash, adminSalt) = PasswordHasher.Hash(DefaultPassword);
        var admin = new User
        {
            FirstName = "Admin", LastName = "eBook", Email = "desktop@ebook.ba", Username = "desktop",
            PasswordHash = adminHash, PasswordSalt = adminSalt, City = mostar, CreatedAt = now,
            UserRoles = { new UserRole { Role = adminRole } }
        };

        var (userHash, userSalt) = PasswordHasher.Hash(DefaultPassword);
        var reader = new User
        {
            FirstName = "Korisnik", LastName = "eBook", Email = "mobile@ebook.ba", Username = "mobile",
            PasswordHash = userHash, PasswordSalt = userSalt, City = sarajevo, CreatedAt = now,
            UserRoles = { new UserRole { Role = userRole } }
        };

        var plans = new List<SubscriptionPlan>
        {
            new() { Name = "Mjesečna pretplata", Description = "Neograničen pristup Premium knjigama mjesec dana.", Price = 4.99m, Currency = "eur", DurationInMonths = 1 },
            new() { Name = "Godišnja pretplata", Description = "Neograničen pristup Premium knjigama godinu dana uz povoljniju cijenu.", Price = 49.99m, Currency = "eur", DurationInMonths = 12 }
        };

        var authors = new Dictionary<string, Author>
        {
        ["Jane Austen"] = new Author { FirstName = "Jane", LastName = "Austen" },
        ["Mary Shelley"] = new Author { FirstName = "Mary", LastName = "Shelley" },
        ["Bram Stoker"] = new Author { FirstName = "Bram", LastName = "Stoker" },
        ["Arthur Conan Doyle"] = new Author { FirstName = "Arthur Conan", LastName = "Doyle" },
        ["Jules Verne"] = new Author { FirstName = "Jules", LastName = "Verne" },
        ["Fjodor Dostojevski"] = new Author { FirstName = "Fjodor", LastName = "Dostojevski" },
        ["Franz Kafka"] = new Author { FirstName = "Franz", LastName = "Kafka" },
        ["H. G. Wells"] = new Author { FirstName = "H. G.", LastName = "Wells" },
        ["Oscar Wilde"] = new Author { FirstName = "Oscar", LastName = "Wilde" },
        ["Mark Twain"] = new Author { FirstName = "Mark", LastName = "Twain" },
        };

        var books = Books.Select(seed => new Book
        {
            Title = seed.Title,
            Description = seed.Description,
            PublishedYear = seed.Year,
            AccessType = seed.Access,
            PageCount = DemoPagesPerBook,
            CoverImage = LoadCover(seed.CoverName),
            CreatedAt = now,
            Author = authors[seed.AuthorName],
            BookGenres = seed.Genres.Select(g => new BookGenre { Genre = genres[g] }).ToList(),
            Pages = BuildDemoPages(seed.Title)
        }).ToList();

        var news = new List<News>
        {
            new()
            {
                Title = "Dobrodošli u eBook",
                Content = "Pročitajte besplatne klasike ili odaberite Premium pretplatu za cijelu biblioteku.",
                Image = LoadCover(Books[0].CoverName), CreatedAt = now, CreatedByUser = admin
            },
            new()
            {
                Title = "Nove knjige u katalogu",
                Content = "Katalog je proširen novim klasicima svjetske književnosti. Pogledajte šta je novo.",
                Image = LoadCover(Books[2].CoverName), CreatedAt = now, CreatedByUser = admin
            }
        };

        db.AddRange(adminRole, userRole, bosnia, croatia, serbia);
        db.AddRange(cities);
        db.AddRange(genres.Values);
        db.AddRange(admin, reader);
        db.AddRange(plans);
        db.AddRange(books);
        db.AddRange(news);

        await db.SaveChangesAsync();

        logger.LogInformation("Početni podaci su uspješno upisani u bazu.");
    }

    private static List<BookPage> BuildDemoPages(string title)
    {
        const string paragraph =
            "Ovo je probni tekst koji služi za demonstraciju čitača. Stranica prikazuje sadržaj knjige, " +
            "a korisnik može mijenjati veličinu fonta, prelaziti na iduću stranicu i nastaviti čitanje tamo gdje je stao.";

        return Enumerable.Range(1, DemoPagesPerBook)
            .Select(number => new BookPage
            {
                PageNumber = number,
                Content = $"{title} — stranica {number}\n\n{paragraph}\n\n{paragraph}\n\n{paragraph}"
            })
            .ToList();
    }

    private static byte[] LoadCover(string coverName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith($".{coverName}.png", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }
}
