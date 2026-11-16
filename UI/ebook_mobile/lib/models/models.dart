class PagedResult<T> {
  PagedResult({required this.items, required this.totalCount, required this.page, required this.pageSize});

  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;

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
  Genre({required this.id, required this.name});

  final int id;
  final String name;

  factory Genre.fromJson(Map<String, dynamic> json) =>
      Genre(id: json['id'] as int, name: json['name'] as String);
}

class City {
  City({required this.id, required this.name, required this.countryName});

  final int id;
  final String name;
  final String countryName;

  factory City.fromJson(Map<String, dynamic> json) => City(
        id: json['id'] as int,
        name: json['name'] as String,
        countryName: json['countryName'] as String,
      );
}

class BookListItem {
  BookListItem({
    required this.id,
    required this.title,
    required this.authorName,
    required this.publishedYear,
    required this.isPremium,
    required this.genres,
    required this.averageRating,
    required this.reviewCount,
    required this.coverUrl,
  });

  final int id;
  final String title;
  final String authorName;
  final int publishedYear;
  final bool isPremium;
  final List<String> genres;
  final double? averageRating;
  final int reviewCount;
  final String? coverUrl;

  factory BookListItem.fromJson(Map<String, dynamic> json) => BookListItem(
        id: json['id'] as int,
        title: json['title'] as String,
        authorName: json['authorName'] as String,
        publishedYear: json['publishedYear'] as int,
        isPremium: json['accessType'] == 'Premium',
        genres: List<String>.from(json['genres'] as List),
        averageRating: (json['averageRating'] as num?)?.toDouble(),
        reviewCount: json['reviewCount'] as int,
        coverUrl: json['coverUrl'] as String?,
      );
}

class BookDetails {
  BookDetails({
    required this.id,
    required this.title,
    required this.description,
    required this.publishedYear,
    required this.pageCount,
    required this.isPremium,
    required this.authorName,
    required this.genres,
    required this.averageRating,
    required this.reviewCount,
    required this.canRead,
    required this.coverUrl,
  });

  final int id;
  final String title;
  final String description;
  final int publishedYear;
  final int pageCount;
  final bool isPremium;
  final String authorName;
  final List<String> genres;
  final double? averageRating;
  final int reviewCount;
  final bool canRead;
  final String? coverUrl;

  factory BookDetails.fromJson(Map<String, dynamic> json) => BookDetails(
        id: json['id'] as int,
        title: json['title'] as String,
        description: json['description'] as String,
        publishedYear: json['publishedYear'] as int,
        pageCount: json['pageCount'] as int,
        isPremium: json['accessType'] == 'Premium',
        authorName: json['authorName'] as String,
        genres: (json['genres'] as List).map((g) => (g as Map<String, dynamic>)['name'] as String).toList(),
        averageRating: (json['averageRating'] as num?)?.toDouble(),
        reviewCount: json['reviewCount'] as int,
        canRead: json['canRead'] as bool,
        coverUrl: json['coverUrl'] as String?,
      );
}

class Review {
  Review({
    required this.id,
    required this.username,
    required this.rating,
    required this.comment,
    required this.createdAt,
    required this.isMine,
  });

  final int id;
  final String username;
  final int rating;
  final String? comment;
  final DateTime createdAt;
  final bool isMine;

  factory Review.fromJson(Map<String, dynamic> json) => Review(
        id: json['id'] as int,
        username: json['username'] as String,
        rating: json['rating'] as int,
        comment: json['comment'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
        isMine: json['isMine'] as bool,
      );
}

class BookPage {
  BookPage({required this.pageNumber, required this.totalPages, required this.content});

  final int pageNumber;
  final int totalPages;
  final String content;

  factory BookPage.fromJson(Map<String, dynamic> json) => BookPage(
        pageNumber: json['pageNumber'] as int,
        totalPages: json['totalPages'] as int,
        content: json['content'] as String,
      );
}

class SubscriptionPlan {
  SubscriptionPlan({
    required this.id,
    required this.name,
    required this.description,
    required this.price,
    required this.currency,
    required this.durationInMonths,
  });

  final int id;
  final String name;
  final String? description;
  final double price;
  final String currency;
  final int durationInMonths;

  factory SubscriptionPlan.fromJson(Map<String, dynamic> json) => SubscriptionPlan(
        id: json['id'] as int,
        name: json['name'] as String,
        description: json['description'] as String?,
        price: (json['price'] as num).toDouble(),
        currency: json['currency'] as String,
        durationInMonths: json['durationInMonths'] as int,
      );
}

class Subscription {
  Subscription({
    required this.id,
    required this.planName,
    required this.status,
    required this.startDate,
    required this.endDate,
  });

  final int id;
  final String planName;
  final String status;
  final DateTime? startDate;
  final DateTime? endDate;

  bool get isActive => status == 'Active';

  factory Subscription.fromJson(Map<String, dynamic> json) => Subscription(
        id: json['id'] as int,
        planName: json['planName'] as String,
        status: json['status'] as String,
        startDate: json['startDate'] == null ? null : DateTime.parse(json['startDate'] as String).toLocal(),
        endDate: json['endDate'] == null ? null : DateTime.parse(json['endDate'] as String).toLocal(),
      );
}

class AppNotification {
  AppNotification({
    required this.id,
    required this.title,
    required this.message,
    required this.isRead,
    required this.createdAt,
  });

  final int id;
  final String title;
  final String message;
  bool isRead;
  final DateTime createdAt;

  factory AppNotification.fromJson(Map<String, dynamic> json) => AppNotification(
        id: json['id'] as int,
        title: json['title'] as String,
        message: json['message'] as String,
        isRead: json['isRead'] as bool,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
      );
}

class NewsItem {
  NewsItem({
    required this.id,
    required this.title,
    required this.content,
    required this.createdAt,
    required this.imageUrl,
  });

  final int id;
  final String title;
  final String content;
  final DateTime createdAt;
  final String imageUrl;

  factory NewsItem.fromJson(Map<String, dynamic> json) => NewsItem(
        id: json['id'] as int,
        title: json['title'] as String,
        content: json['content'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
        imageUrl: json['imageUrl'] as String,
      );
}

class Recommendation {
  Recommendation({required this.book, required this.reasons});

  final BookListItem book;
  final List<String> reasons;

  factory Recommendation.fromJson(Map<String, dynamic> json) => Recommendation(
        book: BookListItem.fromJson(json['book'] as Map<String, dynamic>),
        reasons: List<String>.from(json['reasons'] as List),
      );
}

class ReadingProgressItem {
  ReadingProgressItem({
    required this.bookId,
    required this.title,
    required this.coverUrl,
    required this.currentPage,
    required this.totalPages,
    required this.isFinished,
  });

  final int bookId;
  final String title;
  final String? coverUrl;
  final int currentPage;
  final int totalPages;
  final bool isFinished;

  factory ReadingProgressItem.fromJson(Map<String, dynamic> json) => ReadingProgressItem(
        bookId: json['bookId'] as int,
        title: json['title'] as String,
        coverUrl: json['coverUrl'] as String?,
        currentPage: json['currentPage'] as int,
        totalPages: json['totalPages'] as int,
        isFinished: json['isFinished'] as bool,
      );
}

class Profile {
  Profile({
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.username,
    required this.phoneNumber,
    required this.cityId,
    required this.cityName,
    required this.countryName,
    required this.pictureUrl,
  });

  final String firstName;
  final String lastName;
  final String email;
  final String username;
  final String? phoneNumber;
  final int cityId;
  final String cityName;
  final String countryName;
  final String? pictureUrl;

  String get fullName => '$firstName $lastName';

  factory Profile.fromJson(Map<String, dynamic> json) => Profile(
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        email: json['email'] as String,
        username: json['username'] as String,
        phoneNumber: json['phoneNumber'] as String?,
        cityId: json['cityId'] as int,
        cityName: json['cityName'] as String,
        countryName: json['countryName'] as String,
        pictureUrl: json['pictureUrl'] as String?,
      );
}
