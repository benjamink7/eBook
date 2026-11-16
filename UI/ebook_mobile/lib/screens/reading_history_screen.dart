import 'package:flutter/material.dart';

import '../models/models.dart';
import '../widgets/book_cover.dart';
import '../widgets/paged_list.dart';
import 'book_details_screen.dart';

class ReadingHistoryScreen extends StatelessWidget {
  const ReadingHistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Historija čitanja')),
      body: PagedList<ReadingProgressItem>(
        path: '/api/ReadingProgress',
        fromJson: ReadingProgressItem.fromJson,
        emptyText: 'Još niste čitali nijednu knjigu.',
        itemBuilder: (context, item, reload) => ListTile(
          leading: BookCover(coverUrl: item.coverUrl),
          title: Text(item.title),
          subtitle: Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                LinearProgressIndicator(value: item.currentPage / item.totalPages),
                const SizedBox(height: 4),
                Text(item.isFinished ? 'Završeno' : 'Stranica ${item.currentPage} / ${item.totalPages}'),
              ],
            ),
          ),
          onTap: () => Navigator.of(context)
              .push(MaterialPageRoute(builder: (_) => BookDetailsScreen(bookId: item.bookId)))
              .then((_) => reload()),
        ),
      ),
    );
  }
}
