import 'package:flutter/material.dart';

import '../models/models.dart';
import '../widgets/book_tile.dart';
import '../widgets/paged_list.dart';

class FavoritesScreen extends StatelessWidget {
  const FavoritesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Favoriti')),
      body: PagedList<BookListItem>(
        path: '/api/Favorites',
        fromJson: BookListItem.fromJson,
        emptyText: 'Nemate favorita.',
        itemBuilder: (context, book, reload) => BookTile(book: book),
      ),
    );
  }
}
