import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../models/models.dart';
import '../widgets/book_cover.dart';
import 'book_details_screen.dart';

class RecommendationsScreen extends StatefulWidget {
  const RecommendationsScreen({super.key});

  @override
  State<RecommendationsScreen> createState() => _RecommendationsScreenState();
}

class _RecommendationsScreenState extends State<RecommendationsScreen> {
  List<Recommendation> _items = const [];
  String _strategy = '';
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final json = await context.read<AuthProvider>().api.get('/api/Recommendations', query: {'count': 10})
          as Map<String, dynamic>;

      if (!mounted) {
        return;
      }

      setState(() {
        _strategy = json['strategy'] as String;
        _items = (json['items'] as List).map((i) => Recommendation.fromJson(i as Map<String, dynamic>)).toList();
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

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [Text(_error!), TextButton(onPressed: _load, child: const Text('Pokušaj ponovo'))],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            _strategy == 'Personalized' ? 'Preporučeno za vas' : 'Popularno među čitaocima',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 8),
          if (_items.isEmpty) const Text('Trenutno nema preporuka.'),
          for (final item in _items)
            Card(
              child: InkWell(
                onTap: () => Navigator.of(context)
                    .push(MaterialPageRoute(builder: (_) => BookDetailsScreen(bookId: item.book.id))),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      BookCover(coverUrl: item.book.coverUrl, width: 64, height: 92),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(item.book.title, style: Theme.of(context).textTheme.titleMedium),
                            Text(item.book.authorName),
                            const SizedBox(height: 6),
                            for (final reason in item.reasons)
                              Padding(
                                padding: const EdgeInsets.only(bottom: 2),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Icon(Icons.lightbulb_outline, size: 16),
                                    const SizedBox(width: 6),
                                    Expanded(
                                      child: Text(reason, style: Theme.of(context).textTheme.bodySmall),
                                    ),
                                  ],
                                ),
                              ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
