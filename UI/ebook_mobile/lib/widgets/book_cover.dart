import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/auth_provider.dart';

class BookCover extends StatelessWidget {
  const BookCover({super.key, required this.coverUrl, this.width = 56, this.height = 80});

  final String? coverUrl;
  final double width;
  final double height;

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;
    final placeholder = Container(
      width: width,
      height: height,
      color: Theme.of(context).colorScheme.surfaceContainerHighest,
      child: Icon(Icons.menu_book, color: Theme.of(context).colorScheme.outline),
    );

    return ClipRRect(
      borderRadius: BorderRadius.circular(6),
      child: coverUrl == null
          ? placeholder
          : Image.network(
              api.uri(coverUrl!).toString(),
              headers: api.authHeaders,
              width: width,
              height: height,
              fit: BoxFit.cover,
              errorBuilder: (_, _, _) => placeholder,
            ),
    );
  }
}
