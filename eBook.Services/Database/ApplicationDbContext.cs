using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Referentne tabele
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<BookGenre> BookGenres => Set<BookGenre>();

    // Glavne tabele
    public DbSet<User> Users => Set<User>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookPage> BookPages => Set<BookPage>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReadingProgress> ReadingProgresses => Set<ReadingProgress>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<News> News => Set<News>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Referentne tabele ----------
        modelBuilder.Entity<Role>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Country>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<City>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasOne(x => x.Country).WithMany(c => c.Cities).HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CountryId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Genre>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<BookGenre>(e =>
        {
            e.HasKey(x => new { x.BookId, x.GenreId });
            e.HasOne(x => x.Book).WithMany(b => b.BookGenres).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Genre).WithMany(g => g.BookGenres).HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Glavne tabele ----------
        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasMaxLength(256);
            e.Property(x => x.PasswordSalt).HasMaxLength(256);
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.PasswordResetCodeHash).HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Author>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Biography).HasMaxLength(4000);
        });

        modelBuilder.Entity<Book>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.AccessType).HasConversion<int>();
            e.HasOne(x => x.Author).WithMany(a => a.Books).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Title);
        });

        modelBuilder.Entity<BookPage>(e =>
        {
            e.HasOne(x => x.Book).WithMany(b => b.Pages).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.BookId, x.PageNumber }).IsUnique();
        });

        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.StatusChangeNote).HasMaxLength(500);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.HasOne(x => x.User).WithMany(u => u.Subscriptions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SubscriptionPlan).WithMany(p => p.Subscriptions).HasForeignKey(x => x.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.Status });
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.RefundedAmount).HasPrecision(10, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.StripePaymentIntentId).HasMaxLength(100);
            e.Property(x => x.StripeRefundId).HasMaxLength(100);
            e.HasIndex(x => x.StripePaymentIntentId).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.Payments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Subscription).WithMany(s => s.Payments).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReadingProgress>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.ReadingProgresses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Book).WithMany(b => b.ReadingProgresses).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.HasOne(x => x.User).WithMany(u => u.Reviews).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Book).WithMany(b => b.Reviews).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5"));
        });

        modelBuilder.Entity<Favorite>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.Favorites).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Book).WithMany(b => b.Favorites).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
        });

        modelBuilder.Entity<SearchHistory>(e =>
        {
            e.Property(x => x.Query).HasMaxLength(200);
            e.HasOne(x => x.User).WithMany(u => u.SearchHistories).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(150);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.Property(x => x.Type).HasConversion<int>();
            e.HasOne(x => x.User).WithMany(u => u.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.IsRead });
        });

        modelBuilder.Entity<News>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Content).HasMaxLength(4000);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
