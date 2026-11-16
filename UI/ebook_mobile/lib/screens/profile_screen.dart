import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/auth_provider.dart';
import 'change_password_screen.dart';
import 'favorites_screen.dart';
import 'reading_history_screen.dart';
import 'subscription_screen.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    void open(Widget screen) => Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));

    return ListView(
      children: [
        const SizedBox(height: 24),
        const Icon(Icons.account_circle, size: 72),
        const SizedBox(height: 8),
        Center(child: Text(auth.username ?? '', style: Theme.of(context).textTheme.titleLarge)),
        const SizedBox(height: 16),
        _tile(Icons.workspace_premium, 'Pretplata', () => open(const SubscriptionScreen())),
        _tile(Icons.favorite_border, 'Favoriti', () => open(const FavoritesScreen())),
        _tile(Icons.history, 'Historija čitanja', () => open(const ReadingHistoryScreen())),
        _tile(Icons.lock_outline, 'Promjena lozinke', () => open(const ChangePasswordScreen())),
        ListTile(
          leading: const Icon(Icons.logout),
          title: const Text('Odjava'),
          onTap: auth.logout,
        ),
      ],
    );
  }

  Widget _tile(IconData icon, String title, VoidCallback onTap) => ListTile(
        leading: Icon(icon),
        title: Text(title),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      );
}
