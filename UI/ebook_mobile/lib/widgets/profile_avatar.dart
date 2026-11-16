import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/auth_provider.dart';

class ProfileAvatar extends StatelessWidget {
  const ProfileAvatar({super.key, required this.pictureUrl, this.radius = 44, this.version = 0});

  final String? pictureUrl;
  final double radius;

  // Mijenja se nakon slanja nove slike da se zaobidje kesirana stara.
  final int version;

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return CircleAvatar(
      radius: radius,
      backgroundColor: Theme.of(context).colorScheme.surfaceContainerHighest,
      backgroundImage: pictureUrl == null
          ? null
          : NetworkImage(api.uri(pictureUrl!, {'v': version}).toString(), headers: api.authHeaders),
      child: pictureUrl == null ? Icon(Icons.person, size: radius) : null,
    );
  }
}
