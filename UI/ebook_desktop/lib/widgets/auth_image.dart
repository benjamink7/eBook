import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/auth_provider.dart';

class AuthImage extends StatelessWidget {
  const AuthImage({super.key, required this.url, this.width = 40, this.height = 56, this.version = 0});

  final String? url;
  final double width;
  final double height;
  final int version;

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;
    final placeholder = Container(
      width: width,
      height: height,
      color: Theme.of(context).colorScheme.surfaceContainerHighest,
      child: Icon(Icons.image_outlined, color: Theme.of(context).colorScheme.outline),
    );

    if (url == null) {
      return placeholder;
    }

    return ClipRRect(
      borderRadius: BorderRadius.circular(4),
      child: Image.network(
        api.uri(url!, {'v': version}).toString(),
        headers: api.authHeaders,
        width: width,
        height: height,
        fit: BoxFit.cover,
        errorBuilder: (_, __, ___) => placeholder,
      ),
    );
  }
}
