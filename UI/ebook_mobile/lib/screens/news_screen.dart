import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../core/auth_provider.dart';
import '../models/models.dart';
import '../widgets/paged_list.dart';

class NewsScreen extends StatefulWidget {
  const NewsScreen({super.key});

  @override
  State<NewsScreen> createState() => _NewsScreenState();
}

class _NewsScreenState extends State<NewsScreen> {
  final _search = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          child: TextField(
            controller: _search,
            textInputAction: TextInputAction.search,
            onSubmitted: (value) => setState(() => _query = value.trim()),
            decoration: InputDecoration(
              hintText: 'Pretraga novosti',
              prefixIcon: const Icon(Icons.search),
              isDense: true,
              suffixIcon: _query.isEmpty
                  ? null
                  : IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        _search.clear();
                        setState(() => _query = '');
                      },
                    ),
            ),
          ),
        ),
        Expanded(
          child: PagedList<NewsItem>(
            key: ValueKey(_query),
            path: '/api/News',
            query: {'search': _query},
            fromJson: NewsItem.fromJson,
            emptyText: 'Nema novosti.',
            itemBuilder: (context, news, reload) => _NewsCard(news: news),
          ),
        ),
      ],
    );
  }
}

class _NewsCard extends StatelessWidget {
  const _NewsCard({required this.news});

  final NewsItem news;

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => NewsDetailsScreen(news: news))),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Image.network(
              api.uri(news.imageUrl).toString(),
              headers: api.authHeaders,
              height: 150,
              width: double.infinity,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => const SizedBox(height: 0),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(news.title, style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 4),
                  Text(DateFormat('dd.MM.yyyy.').format(news.createdAt), style: Theme.of(context).textTheme.labelSmall),
                  const SizedBox(height: 8),
                  Text(news.content, maxLines: 3, overflow: TextOverflow.ellipsis),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class NewsDetailsScreen extends StatelessWidget {
  const NewsDetailsScreen({super.key, required this.news});

  final NewsItem news;

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return Scaffold(
      appBar: AppBar(title: const Text('Novost')),
      body: ListView(
        children: [
          Image.network(
            api.uri(news.imageUrl).toString(),
            headers: api.authHeaders,
            width: double.infinity,
            fit: BoxFit.cover,
            errorBuilder: (_, __, ___) => const SizedBox(height: 0),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(news.title, style: Theme.of(context).textTheme.headlineSmall),
                const SizedBox(height: 4),
                Text(DateFormat('dd.MM.yyyy. HH:mm').format(news.createdAt)),
                const SizedBox(height: 16),
                Text(news.content),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
