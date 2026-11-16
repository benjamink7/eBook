import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../models/models.dart';
import '../widgets/book_cover.dart';

class CatalogScreen extends StatefulWidget {
  const CatalogScreen({super.key});

  @override
  State<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends State<CatalogScreen> {
  static const int _pageSize = 20;

  final _scroll = ScrollController();
  final _searchController = TextEditingController();
  Timer? _debounce;

  final List<BookListItem> _books = [];
  List<Genre> _genres = const [];
  int? _genreId;
  String _sortBy = 'Title';
  int _page = 1;
  int _total = 0;
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
    _loadGenres();
    _load(reset: true);
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _scroll.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 200 &&
        !_loading &&
        _books.length < _total) {
      _page++;
      _load();
    }
  }

  Future<void> _loadGenres() async {
    try {
      final json = await context.read<AuthProvider>().api.get('/api/Genres', query: {'pageSize': 100});
      final result = PagedResult.fromJson(json, Genre.fromJson);

      if (mounted) {
        setState(() => _genres = result.items);
      }
    } on ApiException {
      // Filter po zanru je opcionalan; katalog radi i bez njega.
    }
  }

  Future<void> _load({bool reset = false}) async {
    if (reset) {
      _page = 1;
    }

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final json = await context.read<AuthProvider>().api.get('/api/Books', query: {
        'search': _searchController.text.trim(),
        'genreId': _genreId,
        'sortBy': _sortBy,
        'page': _page,
        'pageSize': _pageSize,
      });
      final result = PagedResult.fromJson(json, BookListItem.fromJson);

      if (!mounted) {
        return;
      }

      setState(() {
        if (reset) {
          _books.clear();
        }

        _books.addAll(result.items);
        _total = result.totalCount;
      });
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  void _onSearchChanged(String _) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () => _load(reset: true));
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          child: TextField(
            controller: _searchController,
            onChanged: _onSearchChanged,
            decoration: const InputDecoration(
              hintText: 'Pretraga po naslovu ili autoru',
              prefixIcon: Icon(Icons.search),
              isDense: true,
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          child: Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<int?>(
                  initialValue: _genreId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Žanr', isDense: true),
                  items: [
                    const DropdownMenuItem<int?>(value: null, child: Text('Svi žanrovi')),
                    for (final genre in _genres) DropdownMenuItem<int?>(value: genre.id, child: Text(genre.name)),
                  ],
                  onChanged: (value) {
                    _genreId = value;
                    _load(reset: true);
                  },
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: DropdownButtonFormField<String>(
                  initialValue: _sortBy,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Sortiranje', isDense: true),
                  items: const [
                    DropdownMenuItem(value: 'Title', child: Text('Naslov')),
                    DropdownMenuItem(value: 'Newest', child: Text('Najnovije')),
                    DropdownMenuItem(value: 'Rating', child: Text('Ocjena')),
                  ],
                  onChanged: (value) {
                    _sortBy = value!;
                    _load(reset: true);
                  },
                ),
              ),
            ],
          ),
        ),
        Expanded(child: _buildList()),
      ],
    );
  }

  Widget _buildList() {
    if (_error != null && _books.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!),
            TextButton(onPressed: () => _load(reset: true), child: const Text('Pokušaj ponovo')),
          ],
        ),
      );
    }

    if (_books.isEmpty && !_loading) {
      return const Center(child: Text('Nema knjiga za zadane kriterije.'));
    }

    return RefreshIndicator(
      onRefresh: () => _load(reset: true),
      child: ListView.builder(
        controller: _scroll,
        physics: const AlwaysScrollableScrollPhysics(),
        itemCount: _books.length + (_loading ? 1 : 0),
        itemBuilder: (context, index) {
          if (index >= _books.length) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }

          return _BookTile(book: _books[index]);
        },
      ),
    );
  }
}

class _BookTile extends StatelessWidget {
  const _BookTile({required this.book});

  final BookListItem book;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListTile(
      leading: BookCover(coverUrl: book.coverUrl),
      title: Text(book.title, maxLines: 2, overflow: TextOverflow.ellipsis),
      subtitle: Text('${book.authorName} • ${book.publishedYear}\n${book.genres.join(', ')}'),
      isThreeLine: true,
      trailing: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(book.isPremium ? Icons.workspace_premium : Icons.lock_open,
              color: book.isPremium ? Colors.amber.shade800 : theme.colorScheme.outline),
          Text(book.isPremium ? 'Premium' : 'Free', style: theme.textTheme.labelSmall),
          if (book.averageRating != null)
            Text('★ ${book.averageRating!.toStringAsFixed(1)}', style: theme.textTheme.labelSmall),
        ],
      ),
    );
  }
}
