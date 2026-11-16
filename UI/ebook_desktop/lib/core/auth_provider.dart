import 'package:flutter/foundation.dart';

import 'api_client.dart';
import 'api_exception.dart';

class AuthProvider extends ChangeNotifier {
  AuthProvider() {
    api = ApiClient(onUnauthorized: _handleUnauthorized);
  }

  late final ApiClient api;

  String? username;

  bool get isAuthenticated => api.token != null;

  Future<void> login(String username, String password) async {
    final json = await api.post('/api/Auth/login', body: {'username': username, 'password': password})
        as Map<String, dynamic>;
    final roles = List<String>.from(json['roles'] as List);

    if (!roles.contains('Admin')) {
      throw ApiException('Ova aplikacija je namijenjena samo administratorima.');
    }

    api.token = json['token'] as String;
    this.username = json['username'] as String;
    notifyListeners();
  }

  Future<void> logout() async {
    try {
      await api.post('/api/Auth/logout');
    } on ApiException {
      // Lokalna sesija se zavrsava bez obzira na odgovor servera.
    }

    _clear();
  }

  void _handleUnauthorized() => _clear();

  void _clear() {
    api.token = null;
    username = null;
    notifyListeners();
  }
}
