import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../models/models.dart';
import '../widgets/profile_avatar.dart';
import 'change_password_screen.dart';
import 'edit_profile_screen.dart';
import 'favorites_screen.dart';
import 'reading_history_screen.dart';
import 'subscription_screen.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  Profile? _profile;
  int _version = 0;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final json = await context.read<AuthProvider>().api.get('/api/Profile') as Map<String, dynamic>;

      if (mounted) {
        setState(() {
          _profile = Profile.fromJson(json);
          _version++;
          _error = null;
        });
      }
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    }
  }

  Future<void> _edit() async {
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => EditProfileScreen(profile: _profile!)));

    if (mounted) {
      await _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.read<AuthProvider>();
    final profile = _profile;

    void open(Widget screen) => Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));

    return ListView(
      children: [
        const SizedBox(height: 24),
        Center(child: ProfileAvatar(pictureUrl: profile?.pictureUrl, version: _version)),
        const SizedBox(height: 12),
        if (profile != null) ...[
          Center(child: Text(profile.fullName, style: Theme.of(context).textTheme.titleLarge)),
          Center(child: Text('@${profile.username} • ${profile.cityName}, ${profile.countryName}')),
        ] else if (_error != null)
          Center(
            child: Column(
              children: [Text(_error!), TextButton(onPressed: _load, child: const Text('Pokušaj ponovo'))],
            ),
          ),
        const SizedBox(height: 16),
        if (profile != null) _tile(Icons.edit_outlined, 'Uredi profil', _edit),
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
