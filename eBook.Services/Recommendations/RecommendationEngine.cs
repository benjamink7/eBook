namespace eBook.Services.Recommendations;

public record BookInfo(
    int Id,
    string Title,
    string AuthorName,
    int AuthorId,
    IReadOnlyList<int> GenreIds,
    double? AverageRating,
    int ReviewCount,
    int ReaderCount);

public enum SignalKind
{
    Favorite,
    Finished,
    Started,
    HighRating,
    NeutralRating,
    LowRating,
    Search
}

public record UserSignal(int BookId, SignalKind Kind);

public record RecommendationInput(
    IReadOnlyList<BookInfo> Books,
    IReadOnlyDictionary<int, string> GenreNames,
    IReadOnlyList<UserSignal> Signals,
    IReadOnlyDictionary<int, HashSet<int>> OtherUsersPositiveBooks);

public record ScoredBook(int BookId, double Score, double Content, double Collaborative, double Popularity, List<string> Reasons);

public record RecommendationOutcome(bool IsPersonalized, List<ScoredBook> Items);

// Hibridni preporucivac: sadrzajni (zanrovi i autori), kolaborativni (slicni citaoci) i popularnost.
// Cista funkcija bez pristupa bazi, pa je rezultat u potpunosti objasnjiv i ponovljiv.
public static class RecommendationEngine
{
    public const double ContentWeight = 0.5;
    public const double CollaborativeWeight = 0.35;
    public const double PopularityWeight = 0.15;
    public const double AuthorBonus = 0.25;
    public const double PositiveThreshold = 3;
    public const double MinReasonContribution = 0.05;
    public const int MaxReasons = 3;

    public static double WeightOf(SignalKind kind) => kind switch
    {
        SignalKind.Favorite => 3,
        SignalKind.Finished => 3,
        SignalKind.Started => 1,
        SignalKind.HighRating => 4,
        SignalKind.NeutralRating => 1,
        SignalKind.LowRating => -3,
        SignalKind.Search => 0.5,
        _ => 0
    };

    public static RecommendationOutcome Recommend(RecommendationInput input, int count)
    {
        var books = input.Books.ToDictionary(book => book.Id);
        var signals = input.Signals.Where(signal => books.ContainsKey(signal.BookId)).ToList();

        // Knjige s kojima je korisnik vec imao kontakt (osim same pretrage) se ne preporucuju ponovo.
        var excluded = signals.Where(signal => signal.Kind != SignalKind.Search).Select(signal => signal.BookId).ToHashSet();
        var candidates = input.Books.Where(book => !excluded.Contains(book.Id)).ToList();

        var netWeights = signals
            .GroupBy(signal => signal.BookId)
            .ToDictionary(
                group => group.Key,
                group => new BookAffinity(
                    group.Sum(signal => WeightOf(signal.Kind)),
                    group.OrderByDescending(signal => WeightOf(signal.Kind)).First().Kind));

        var isPersonalized = netWeights.Values.Any(affinity => affinity.Net > 0);

        var popularity = ComputePopularity(candidates);

        if (!isPersonalized)
        {
            return new RecommendationOutcome(false, candidates
                .Select(book => new ScoredBook(
                    book.Id, popularity[book.Id], 0, 0, popularity[book.Id], PopularityReasons(book)))
                .Where(scored => scored.Score > 0)
                .OrderByDescending(scored => scored.Score).ThenBy(scored => scored.BookId)
                .Take(count)
                .ToList());
        }

        var genreProfile = BuildProfile(netWeights, books, book => book.GenreIds);
        // Pretraga je slab signal i utice samo na zanrove, ne i na omiljene autore.
        var authorWeights = netWeights
            .Where(pair => pair.Value.Strongest != SignalKind.Search)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var authorProfile = BuildProfile(authorWeights, books, book => new[] { book.AuthorId });
        var genreMax = genreProfile.Values.Select(entry => entry.Score).DefaultIfEmpty(0).Max();
        var authorMax = authorProfile.Values.Select(entry => entry.Score).DefaultIfEmpty(0).Max();

        var userPositive = signals
            .Where(signal => signal.Kind != SignalKind.Search)
            .GroupBy(signal => signal.BookId)
            .Where(group => group.Sum(signal => WeightOf(signal.Kind)) >= PositiveThreshold)
            .Select(group => group.Key)
            .ToHashSet();

        var collaborative = ComputeCollaborative(userPositive, input.OtherUsersPositiveBooks, excluded);

        var results = new List<ScoredBook>();

        foreach (var book in candidates)
        {
            var content = ComputeContent(book, genreProfile, authorProfile, genreMax, authorMax);
            var collaborativeScore = collaborative.Scores.GetValueOrDefault(book.Id);
            var popularityScore = popularity[book.Id];

            var score = ContentWeight * content + CollaborativeWeight * collaborativeScore + PopularityWeight * popularityScore;

            if (score <= 0)
            {
                continue;
            }

            var reasons = BuildReasons(
                book, content, collaborativeScore, popularityScore,
                genreProfile, authorProfile, netWeights, books, input.GenreNames, collaborative.Supporters.GetValueOrDefault(book.Id));

            results.Add(new ScoredBook(book.Id, score, content, collaborativeScore, popularityScore, reasons));
        }

        return new RecommendationOutcome(true, results
            .OrderByDescending(scored => scored.Score).ThenBy(scored => scored.BookId)
            .Take(count)
            .ToList());
    }

    private record BookAffinity(double Net, SignalKind Strongest);

    private record ProfileEntry(double Score, int SourceBookId, double SourceWeight);

    private record CollaborativeResult(Dictionary<int, double> Scores, Dictionary<int, int> Supporters);

    private static Dictionary<int, ProfileEntry> BuildProfile(
        Dictionary<int, BookAffinity> netWeights,
        Dictionary<int, BookInfo> books,
        Func<BookInfo, IEnumerable<int>> keySelector)
    {
        var profile = new Dictionary<int, ProfileEntry>();

        foreach (var (bookId, affinity) in netWeights)
        {
            foreach (var key in keySelector(books[bookId]))
            {
                var current = profile.GetValueOrDefault(key) ?? new ProfileEntry(0, 0, double.MinValue);
                var isBetterSource = affinity.Net > current.SourceWeight;

                profile[key] = new ProfileEntry(
                    current.Score + affinity.Net,
                    isBetterSource ? bookId : current.SourceBookId,
                    isBetterSource ? affinity.Net : current.SourceWeight);
            }
        }

        return profile;
    }

    private static double ComputeContent(
        BookInfo book,
        Dictionary<int, ProfileEntry> genreProfile,
        Dictionary<int, ProfileEntry> authorProfile,
        double genreMax,
        double authorMax)
    {
        var genreScore = 0.0;

        if (genreMax > 0 && book.GenreIds.Count > 0)
        {
            genreScore = book.GenreIds
                .Select(genreId => genreProfile.TryGetValue(genreId, out var entry) ? entry.Score / genreMax : 0)
                .Average();
        }

        var authorScore = authorMax > 0 && authorProfile.TryGetValue(book.AuthorId, out var author) && author.Score > 0
            ? AuthorBonus * (author.Score / authorMax)
            : 0;

        return Math.Clamp(genreScore + authorScore, 0, 1);
    }

    // Kosinusna slicnost izmedju skupa knjiga koje korisnik voli i skupova drugih citalaca.
    private static CollaborativeResult ComputeCollaborative(
        HashSet<int> userPositive,
        IReadOnlyDictionary<int, HashSet<int>> others,
        HashSet<int> excluded)
    {
        var raw = new Dictionary<int, double>();
        var supporters = new Dictionary<int, int>();

        if (userPositive.Count == 0)
        {
            return new CollaborativeResult(raw, supporters);
        }

        foreach (var otherBooks in others.Values)
        {
            var overlap = otherBooks.Count(userPositive.Contains);

            if (overlap == 0)
            {
                continue;
            }

            var similarity = overlap / Math.Sqrt((double)userPositive.Count * otherBooks.Count);

            foreach (var bookId in otherBooks.Where(id => !excluded.Contains(id)))
            {
                raw[bookId] = raw.GetValueOrDefault(bookId) + similarity;
                supporters[bookId] = supporters.GetValueOrDefault(bookId) + 1;
            }
        }

        var max = raw.Values.DefaultIfEmpty(0).Max();

        var normalized = max > 0
            ? raw.ToDictionary(pair => pair.Key, pair => pair.Value / max)
            : raw;

        return new CollaborativeResult(normalized, supporters);
    }

    private static Dictionary<int, double> ComputePopularity(List<BookInfo> candidates)
    {
        var raw = candidates.ToDictionary(
            book => book.Id,
            book => (book.AverageRating ?? 0) / 5.0 * Math.Log(1 + book.ReviewCount + book.ReaderCount));

        var max = raw.Values.DefaultIfEmpty(0).Max();

        return max > 0 ? raw.ToDictionary(pair => pair.Key, pair => pair.Value / max) : raw;
    }

    private static List<string> BuildReasons(
        BookInfo book,
        double content,
        double collaborative,
        double popularity,
        Dictionary<int, ProfileEntry> genreProfile,
        Dictionary<int, ProfileEntry> authorProfile,
        Dictionary<int, BookAffinity> netWeights,
        Dictionary<int, BookInfo> books,
        IReadOnlyDictionary<int, string> genreNames,
        int supporters)
    {
        var reasons = new List<(double Contribution, string Text)>();

        if (ContentWeight * content >= MinReasonContribution)
        {
            var bestGenre = book.GenreIds
                .Where(genreProfile.ContainsKey)
                .OrderByDescending(genreId => genreProfile[genreId].Score)
                .FirstOrDefault();

            if (bestGenre != 0 && genreNames.TryGetValue(bestGenre, out var genreName))
            {
                var source = books[genreProfile[bestGenre].SourceBookId];

                if (source.Id != book.Id)
                {
                    var verb = DescribeSignal(netWeights[source.Id].Strongest);
                    reasons.Add((ContentWeight * content, $"Volite žanr „{genreName}” – {verb} „{source.Title}”."));
                }
            }

            if (authorProfile.TryGetValue(book.AuthorId, out var author) && author.Score > 0)
            {
                var source = books[author.SourceBookId];
                reasons.Add((ContentWeight * content * 0.5, $"Isti autor ({book.AuthorName}) kao „{source.Title}”."));
            }
        }

        if (CollaborativeWeight * collaborative >= MinReasonContribution && supporters > 0)
        {
            reasons.Add((CollaborativeWeight * collaborative,
                $"Čitaoci sličnog ukusa ({supporters}) su visoko ocijenili, sačuvali ili pročitali ovu knjigu."));
        }

        if (PopularityWeight * popularity >= MinReasonContribution)
        {
            reasons.AddRange(PopularityReasons(book).Select(text => (PopularityWeight * popularity, text)));
        }

        return reasons
            .OrderByDescending(reason => reason.Contribution)
            .Select(reason => reason.Text)
            .Take(MaxReasons)
            .ToList();
    }

    private static List<string> PopularityReasons(BookInfo book)
    {
        if (book.AverageRating is not null && book.ReviewCount > 0)
        {
            return new List<string> { $"Popularna knjiga: prosječna ocjena {book.AverageRating:0.0} ({book.ReviewCount} recenzija)." };
        }

        if (book.ReaderCount > 0)
        {
            return new List<string> { $"Popularna knjiga: čita je {book.ReaderCount} čitalaca." };
        }

        return new List<string>();
    }

    private static string DescribeSignal(SignalKind kind) => kind switch
    {
        SignalKind.Favorite => "dodali ste u favorite",
        SignalKind.Finished => "pročitali ste",
        SignalKind.Started => "počeli ste čitati",
        SignalKind.HighRating => "visoko ste ocijenili",
        SignalKind.NeutralRating => "ocijenili ste",
        SignalKind.Search => "tražili ste",
        _ => "čitali ste"
    };
}
