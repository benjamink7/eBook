import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/auth_provider.dart';
import 'cities_screen.dart';
import 'countries_screen.dart';
import 'dashboard_screen.dart';
import 'genres_screen.dart';
import 'plans_screen.dart';
import 'subscriptions_screen.dart';
import 'users_screen.dart';

class _Section {
  const _Section(this.label, this.icon, this.builder);

  final String label;
  final IconData icon;
  final Widget Function() builder;
}

class ShellScreen extends StatefulWidget {
  const ShellScreen({super.key});

  @override
  State<ShellScreen> createState() => _ShellScreenState();
}

class _ShellScreenState extends State<ShellScreen> {
  int _index = 0;

  static final List<_Section> _sections = [
    _Section('Početna', Icons.dashboard_outlined, () => const DashboardScreen()),
    _Section('Korisnici', Icons.people_outline, () => const UsersScreen()),
    _Section('Pretplate', Icons.workspace_premium_outlined, () => const SubscriptionsScreen()),
    _Section('Planovi', Icons.sell_outlined, () => const PlansScreen()),
    _Section('Žanrovi', Icons.category_outlined, () => const GenresScreen()),
    _Section('Države', Icons.public_outlined, () => const CountriesScreen()),
    _Section('Gradovi', Icons.location_city_outlined, () => const CitiesScreen()),
  ];

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final theme = Theme.of(context);

    return Scaffold(
      body: Row(
        children: [
          SizedBox(
            width: 240,
            child: Column(
              children: [
                const Padding(
                  padding: EdgeInsets.symmetric(vertical: 20),
                  child: Text('eBook Admin', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                ),
                Expanded(
                  child: ListView(
                    children: [
                      for (var i = 0; i < _sections.length; i++)
                        ListTile(
                          leading: Icon(_sections[i].icon),
                          title: Text(_sections[i].label),
                          selected: i == _index,
                          selectedTileColor: theme.colorScheme.primaryContainer,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
                          contentPadding: const EdgeInsets.symmetric(horizontal: 20),
                          onTap: () => setState(() => _index = i),
                        ),
                    ],
                  ),
                ),
                const Divider(height: 1),
                Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    children: [
                      Text(auth.username ?? ''),
                      TextButton.icon(onPressed: auth.logout, icon: const Icon(Icons.logout), label: const Text('Odjava')),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const VerticalDivider(width: 1),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: _sections[_index].builder(),
            ),
          ),
        ],
      ),
    );
  }
}
