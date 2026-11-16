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
