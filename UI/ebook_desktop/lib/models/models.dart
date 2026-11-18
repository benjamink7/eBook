class PagedResult<T> {
  PagedResult({required this.items, required this.totalCount, required this.page, required this.pageSize});

  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;

  int get totalPages => totalCount == 0 ? 1 : (totalCount / pageSize).ceil();

  factory PagedResult.fromJson(dynamic json, T Function(Map<String, dynamic>) fromItem) {
    final map = json as Map<String, dynamic>;

    return PagedResult(
      items: (map['items'] as List).map((item) => fromItem(item as Map<String, dynamic>)).toList(),
      totalCount: map['totalCount'] as int,
      page: map['page'] as int,
      pageSize: map['pageSize'] as int,
    );
  }
}

class Genre {
  Genre({required this.id, required this.name, required this.description});

  final int id;
  final String name;
  final String? description;

  factory Genre.fromJson(Map<String, dynamic> json) => Genre(
        id: json['id'] as int,
        name: json['name'] as String,
        description: json['description'] as String?,
      );
}

class CurrencyAmount {
  CurrencyAmount({required this.currency, required this.amount});

  final String currency;
  final double amount;

  factory CurrencyAmount.fromJson(Map<String, dynamic> json) =>
      CurrencyAmount(currency: json['currency'] as String, amount: (json['amount'] as num).toDouble());
}

class MonthlyStatistic {
  MonthlyStatistic({required this.month, required this.newSubscriptions, required this.revenue});

  final String month;
  final int newSubscriptions;
  final List<CurrencyAmount> revenue;

  factory MonthlyStatistic.fromJson(Map<String, dynamic> json) => MonthlyStatistic(
        month: json['month'] as String,
        newSubscriptions: json['newSubscriptions'] as int,
        revenue: (json['revenue'] as List).map((e) => CurrencyAmount.fromJson(e as Map<String, dynamic>)).toList(),
      );
}

class TopBook {
  TopBook({
    required this.title,
    required this.authorName,
    required this.readers,
    required this.favorites,
    required this.averageRating,
  });

  final String title;
  final String authorName;
  final int readers;
  final int favorites;
  final double? averageRating;

  factory TopBook.fromJson(Map<String, dynamic> json) => TopBook(
        title: json['title'] as String,
        authorName: json['authorName'] as String,
        readers: json['readers'] as int,
        favorites: json['favorites'] as int,
        averageRating: (json['averageRating'] as num?)?.toDouble(),
      );
}

class Statistics {
  Statistics({
    required this.totalUsers,
    required this.activeSubscriptions,
    required this.totalBooks,
    required this.premiumBooks,
    required this.totalRevenue,
    required this.revenueThisMonth,
    required this.months,
    required this.topBooks,
  });

  final int totalUsers;
  final int activeSubscriptions;
  final int totalBooks;
  final int premiumBooks;
  final List<CurrencyAmount> totalRevenue;
  final List<CurrencyAmount> revenueThisMonth;
  final List<MonthlyStatistic> months;
  final List<TopBook> topBooks;

  factory Statistics.fromJson(Map<String, dynamic> json) {
    List<T> list<T>(String key, T Function(Map<String, dynamic>) from) =>
        (json[key] as List).map((e) => from(e as Map<String, dynamic>)).toList();

    return Statistics(
      totalUsers: json['totalUsers'] as int,
      activeSubscriptions: json['activeSubscriptions'] as int,
      totalBooks: json['totalBooks'] as int,
      premiumBooks: json['premiumBooks'] as int,
      totalRevenue: list('totalRevenue', CurrencyAmount.fromJson),
      revenueThisMonth: list('revenueThisMonth', CurrencyAmount.fromJson),
      months: list('months', MonthlyStatistic.fromJson),
      topBooks: list('topBooks', TopBook.fromJson),
    );
  }
}

class Country {
  Country({required this.id, required this.name, required this.cityCount});

  final int id;
  final String name;
  final int cityCount;

  factory Country.fromJson(Map<String, dynamic> json) =>
      Country(id: json['id'] as int, name: json['name'] as String, cityCount: json['cityCount'] as int);
}

class City {
  City({required this.id, required this.name, required this.countryId, required this.countryName});

  final int id;
  final String name;
  final int countryId;
  final String countryName;

  factory City.fromJson(Map<String, dynamic> json) => City(
        id: json['id'] as int,
        name: json['name'] as String,
        countryId: json['countryId'] as int,
        countryName: json['countryName'] as String,
      );
}

class Plan {
  Plan({
    required this.id,
    required this.name,
    required this.description,
    required this.price,
    required this.currency,
    required this.durationInMonths,
    required this.isActive,
    required this.subscriptionCount,
  });

  final int id;
  final String name;
  final String? description;
  final double price;
  final String currency;
  final int durationInMonths;
  final bool isActive;
  final int subscriptionCount;

  factory Plan.fromJson(Map<String, dynamic> json) => Plan(
        id: json['id'] as int,
        name: json['name'] as String,
        description: json['description'] as String?,
        price: (json['price'] as num).toDouble(),
        currency: json['currency'] as String,
        durationInMonths: json['durationInMonths'] as int,
        isActive: json['isActive'] as bool,
        subscriptionCount: json['subscriptionCount'] as int,
      );
}

class AdminUser {
  AdminUser({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.username,
    required this.cityName,
    required this.countryName,
    required this.isActive,
    required this.hasActiveSubscription,
    required this.roles,
  });

  final int id;
  final String firstName;
  final String lastName;
  final String email;
  final String username;
  final String cityName;
  final String countryName;
  final bool isActive;
  final bool hasActiveSubscription;
  final List<String> roles;

  String get fullName => '$firstName $lastName';

  factory AdminUser.fromJson(Map<String, dynamic> json) => AdminUser(
        id: json['id'] as int,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        email: json['email'] as String,
        username: json['username'] as String,
        cityName: json['cityName'] as String,
        countryName: json['countryName'] as String,
        isActive: json['isActive'] as bool,
        hasActiveSubscription: json['hasActiveSubscription'] as bool,
        roles: List<String>.from(json['roles'] as List),
      );
}

class AdminSubscription {
  AdminSubscription({
    required this.id,
    required this.username,
    required this.planName,
    required this.status,
    required this.startDate,
    required this.endDate,
    required this.amount,
    required this.currency,
    required this.paymentStatus,
    required this.cancellationReason,
  });

  final int id;
  final String username;
  final String planName;
  final String status;
  final DateTime? startDate;
  final DateTime? endDate;
  final double? amount;
  final String? currency;
  final String? paymentStatus;
  final String? cancellationReason;

  factory AdminSubscription.fromJson(Map<String, dynamic> json) => AdminSubscription(
        id: json['id'] as int,
        username: json['username'] as String,
        planName: json['planName'] as String,
        status: json['status'] as String,
        startDate: json['startDate'] == null ? null : DateTime.parse(json['startDate'] as String).toLocal(),
        endDate: json['endDate'] == null ? null : DateTime.parse(json['endDate'] as String).toLocal(),
        amount: (json['amount'] as num?)?.toDouble(),
        currency: json['currency'] as String?,
        paymentStatus: json['paymentStatus'] as String?,
        cancellationReason: json['cancellationReason'] as String?,
      );
}

class StatusHistoryEntry {
  StatusHistoryEntry({
    required this.fromStatus,
    required this.toStatus,
    required this.note,
    required this.changedAt,
    required this.changedBy,
  });

  final String? fromStatus;
  final String toStatus;
  final String? note;
  final DateTime changedAt;
  final String changedBy;

  factory StatusHistoryEntry.fromJson(Map<String, dynamic> json) => StatusHistoryEntry(
        fromStatus: json['fromStatus'] as String?,
        toStatus: json['toStatus'] as String,
        note: json['note'] as String?,
        changedAt: DateTime.parse(json['changedAt'] as String).toLocal(),
        changedBy: json['changedBy'] as String,
      );
}

class Author {
  Author({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.biography,
    required this.birthYear,
    required this.bookCount,
  });

  final int id;
  final String firstName;
  final String lastName;
  final String? biography;
  final int? birthYear;
  final int bookCount;

  String get fullName => '$firstName $lastName';

  factory Author.fromJson(Map<String, dynamic> json) => Author(
        id: json['id'] as int,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        biography: json['biography'] as String?,
        birthYear: json['birthYear'] as int?,
        bookCount: json['bookCount'] as int,
      );
}

class BookRow {
  BookRow({
    required this.id,
    required this.title,
    required this.authorName,
    required this.publishedYear,
    required this.isPremium,
    required this.isActive,
    required this.genres,
    required this.averageRating,
    required this.coverUrl,
  });

  final int id;
  final String title;
  final String authorName;
  final int publishedYear;
  final bool isPremium;
  final bool isActive;
  final List<String> genres;
  final double? averageRating;
  final String? coverUrl;

  factory BookRow.fromJson(Map<String, dynamic> json) => BookRow(
        id: json['id'] as int,
        title: json['title'] as String,
        authorName: json['authorName'] as String,
        publishedYear: json['publishedYear'] as int,
        isPremium: json['accessType'] == 'Premium',
        isActive: json['isActive'] as bool,
        genres: List<String>.from(json['genres'] as List),
        averageRating: (json['averageRating'] as num?)?.toDouble(),
        coverUrl: json['coverUrl'] as String?,
      );
}

class BookDetails {
  BookDetails({
    required this.id,
    required this.title,
    required this.description,
    required this.publishedYear,
    required this.isPremium,
    required this.isActive,
    required this.authorId,
    required this.genreIds,
    required this.coverUrl,
  });

  final int id;
  final String title;
  final String description;
  final int publishedYear;
  final bool isPremium;
  final bool isActive;
  final int authorId;
  final Set<int> genreIds;
  final String? coverUrl;

  factory BookDetails.fromJson(Map<String, dynamic> json) => BookDetails(
        id: json['id'] as int,
        title: json['title'] as String,
        description: json['description'] as String,
        publishedYear: json['publishedYear'] as int,
        isPremium: json['accessType'] == 'Premium',
        isActive: json['isActive'] as bool,
        authorId: json['authorId'] as int,
        genreIds: (json['genres'] as List).map((g) => (g as Map<String, dynamic>)['id'] as int).toSet(),
        coverUrl: json['coverUrl'] as String?,
      );
}

class NewsRow {
  NewsRow({
    required this.id,
    required this.title,
    required this.content,
    required this.createdAt,
    required this.createdBy,
    required this.imageUrl,
  });

  final int id;
  final String title;
  final String content;
  final DateTime createdAt;
  final String createdBy;
  final String imageUrl;

  factory NewsRow.fromJson(Map<String, dynamic> json) => NewsRow(
        id: json['id'] as int,
        title: json['title'] as String,
        content: json['content'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
        createdBy: json['createdBy'] as String,
        imageUrl: json['imageUrl'] as String,
      );
}
