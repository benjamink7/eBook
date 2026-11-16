import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../models/models.dart';
import '../widgets/book_cover.dart';
import 'reader_screen.dart';

class BookDetailsScreen extends StatefulWidget {
  const BookDetailsScreen({super.key, required this.bookId});

  final int bookId;

  @override
  State<BookDetailsScreen> createState() => _BookDetailsScreenState();
}

class _BookDetailsScreenState extends State<BookDetailsScreen> {
  BookDetails? _book;
  List<Review> _reviews = const [];
  bool _isFavorite = false;
  bool _loading = true;
  String? _error;

  int _rating = 0;
  final _comment = TextEditingController();
  bool _savingReview = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final api = context.read<AuthProvider>().api;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final results = await Future.wait([
        api.get('/api/Books/${widget.bookId}'),
        api.get('/api/Reviews', query: {'bookId': widget.bookId, 'pageSize': 50}),
        api.get('/api/Favorites', query: {'pageSize': 100}),
      ]);

      final reviews = PagedResult.fromJson(results[1], Review.fromJson).items;
      final favorites = PagedResult.fromJson(results[2], BookListItem.fromJson).items;
      final mine = reviews.where((r) => r.isMine).firstOrNull;

      if (!mounted) {
        return;
      }

      setState(() {
        _book = BookDetails.fromJson(results[0] as Map<String, dynamic>);
        _reviews = reviews;
        _isFavorite = favorites.any((b) => b.id == widget.bookId);
        _rating = mine?.rating ?? 0;
        _comment.text = mine?.comment ?? '';
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

  Future<void> _toggleFavorite() async {
    final api = context.read<AuthProvider>().api;

    try {
      if (_isFavorite) {
        await api.delete('/api/Favorites/${widget.bookId}');
      } else {
        await api.put('/api/Favorites/${widget.bookId}');
      }

      if (mounted) {
        setState(() => _isFavorite = !_isFavorite);
      }
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  Future<void> _saveReview() async {
    if (_rating == 0) {
      showMessage('Odaberite ocjenu od 1 do 5.');
      return;
    }

    setState(() => _savingReview = true);

    try {
      final comment = _comment.text.trim();
      await context.read<AuthProvider>().api.put('/api/Reviews', body: {
        'bookId': widget.bookId,
        'rating': _rating,
        'comment': comment.isEmpty ? null : comment,
      });

      showMessage('Recenzija je sačuvana.');
      await _load();
    } on ApiException catch (e) {
      showMessage(e.message);
    } finally {
      if (mounted) {
        setState(() => _savingReview = false);
      }
    }
  }

  Future<void> _deleteReview(Review review) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Brisanje recenzije'),
        content: const Text('Da li ste sigurni da želite obrisati svoju recenziju?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Odustani')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Obriši')),
        ],
      ),
    );

    if (confirmed != true || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/Reviews/${review.id}');
      _rating = 0;
      _comment.clear();
      await _load();
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  void _openReader() {
    Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => ReaderScreen(bookId: widget.bookId, title: _book!.title)));
  }

  @override
  Widget build(BuildContext context) {
    final book = _book;

    return Scaffold(
      appBar: AppBar(
        title: Text(book?.title ?? 'Knjiga'),
        actions: [
          if (book != null)
            IconButton(
              tooltip: _isFavorite ? 'Ukloni iz favorita' : 'Dodaj u favorite',
              icon: Icon(_isFavorite ? Icons.favorite : Icons.favorite_border),
              onPressed: _toggleFavorite,
            ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!),
                      TextButton(onPressed: _load, child: const Text('Pokušaj ponovo')),
                    ],
                  ),
                )
              : _buildContent(book!),
    );
  }

  Widget _buildContent(BookDetails book) {
    final theme = Theme.of(context);
    final dateFormat = DateFormat('dd.MM.yyyy.');

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            BookCover(coverUrl: book.coverUrl, width: 110, height: 160),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(book.title, style: theme.textTheme.titleLarge),
                  const SizedBox(height: 4),
                  Text(book.authorName, style: theme.textTheme.titleSmall),
                  const SizedBox(height: 8),
                  Text('${book.publishedYear} • ${book.pageCount} str.'),
                  Text(book.genres.join(', ')),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      const Icon(Icons.star, size: 18, color: Colors.amber),
                      const SizedBox(width: 4),
                      Text(book.averageRating == null
                          ? 'Nema ocjena'
                          : '${book.averageRating!.toStringAsFixed(1)} (${book.reviewCount})'),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Chip(
                    avatar: Icon(book.isPremium ? Icons.workspace_premium : Icons.lock_open, size: 18),
                    label: Text(book.isPremium ? 'Premium' : 'Free'),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        if (book.canRead)
          FilledButton.icon(onPressed: _openReader, icon: const Icon(Icons.menu_book), label: const Text('Čitaj'))
        else
          Card(
            color: theme.colorScheme.secondaryContainer,
            child: const Padding(
              padding: EdgeInsets.all(12),
              child: Text('Ova knjiga je dostupna samo uz aktivnu Premium pretplatu.'),
            ),
          ),
        const SizedBox(height: 16),
        Text('Opis', style: theme.textTheme.titleMedium),
        const SizedBox(height: 4),
        Text(book.description),
        const Divider(height: 32),
        Text('Vaša recenzija', style: theme.textTheme.titleMedium),
        Row(
          children: [
            for (var i = 1; i <= 5; i++)
              IconButton(
                onPressed: () => setState(() => _rating = i),
                icon: Icon(i <= _rating ? Icons.star : Icons.star_border, color: Colors.amber),
              ),
          ],
        ),
        TextField(
          controller: _comment,
          maxLines: 3,
          maxLength: 2000,
          decoration: const InputDecoration(labelText: 'Komentar (opcionalno)'),
        ),
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerRight,
          child: FilledButton(
            onPressed: _savingReview ? null : _saveReview,
            child: const Text('Sačuvaj recenziju'),
          ),
        ),
        const Divider(height: 32),
        Text('Recenzije (${_reviews.length})', style: theme.textTheme.titleMedium),
        if (_reviews.isEmpty)
          const Padding(padding: EdgeInsets.symmetric(vertical: 12), child: Text('Još nema recenzija.')),
        for (final review in _reviews)
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: Row(
              children: [
                Text(review.username),
                const SizedBox(width: 8),
                const Icon(Icons.star, size: 16, color: Colors.amber),
                Text('${review.rating}'),
              ],
            ),
            subtitle: Text(
              '${review.comment ?? ''}${review.comment == null ? '' : '\n'}${dateFormat.format(review.createdAt)}',
            ),
            trailing: review.isMine
                ? IconButton(icon: const Icon(Icons.delete_outline), onPressed: () => _deleteReview(review))
                : null,
          ),
      ],
    );
  }
}
