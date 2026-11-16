import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/auth_provider.dart';
import 'subscription_screen.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return ListView(
      children: [
        const SizedBox(height: 24),
        const Icon(Icons.account_circle, size: 72),
        const SizedBox(height: 8),
        Center(child: Text(auth.username ?? '', style: Theme.of(context).textTheme.titleLarge)),
        const SizedBox(height: 16),
        ListTile(
          leading: const Icon(Icons.workspace_premium),
          title: const Text('Pretplata'),
          trailing: const Icon(Icons.chevron_right),
          onTap: () =>
              Navigator.of(context).push(MaterialPageRoute(builder: (_) => const SubscriptionScreen())),
        ),
        ListTile(
          leading: const Icon(Icons.logout),
          title: const Text('Odjava'),
          onTap: auth.logout,
        ),
      ],
    );
  }
}
