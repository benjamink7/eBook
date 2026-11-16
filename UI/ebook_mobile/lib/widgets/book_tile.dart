import 'package:flutter/material.dart';

import '../models/models.dart';
import '../screens/book_details_screen.dart';
import 'book_cover.dart';

class BookTile extends StatelessWidget {
  const BookTile({super.key, required this.book});

  final BookListItem book;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListTile(
      leading: BookCover(coverUrl: book.coverUrl),
      title: Text(book.title, maxLines: 2, overflow: TextOverflow.ellipsis),
      subtitle: Text('${book.authorName} • ${book.publishedYear}\n${book.genres.join(', ')}'),
      isThreeLine: true,
      onTap: () => Navigator.of(context)
          .push(MaterialPageRoute(builder: (_) => BookDetailsScreen(bookId: book.id))),
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
