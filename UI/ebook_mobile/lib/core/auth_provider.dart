import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'api_client.dart';
import 'api_exception.dart';
import 'navigation.dart';

class AuthProvider extends ChangeNotifier {
  AuthProvider() {
    api = ApiClient(onUnauthorized: _handleUnauthorized);
  }

  static const _storage = FlutterSecureStorage();

  late final ApiClient api;

  String? username;
  int? userId;
  List<String> roles = const [];

  bool get isAuthenticated => api.token != null;

  Future<void> init() async {
    final token = await _storage.read(key: 'token');
    final expiresAt = DateTime.tryParse(await _storage.read(key: 'expiresAt') ?? '');

    if (token == null || expiresAt == null || expiresAt.isBefore(DateTime.now().toUtc())) {
      await _clear();
      return;
    }

    api.token = token;
    userId = int.tryParse(await _storage.read(key: 'userId') ?? '');
    username = await _storage.read(key: 'username');
    roles = List<String>.from(jsonDecode(await _storage.read(key: 'roles') ?? '[]') as List);
  }

  Future<void> login(String username, String password) async {
    final json = await api.post('/api/Auth/login', body: {'username': username, 'password': password});
    await _saveSession(json as Map<String, dynamic>);
  }

  Future<void> register(Map<String, dynamic> body) async {
    final json = await api.post('/api/Auth/register', body: body);
    await _saveSession(json as Map<String, dynamic>);
  }

  Future<String> forgotPassword(String email) async {
    final json = await api.post('/api/Auth/forgot-password', body: {'email': email});
    return (json as Map<String, dynamic>)['message'] as String;
  }

  Future<void> resetPassword(Map<String, dynamic> body) async {
    await api.post('/api/Auth/reset-password', body: body);
  }

  Future<void> changePassword(Map<String, dynamic> body) async {
    final json = await api.post('/api/Auth/change-password', body: body);
    await _saveSession(json as Map<String, dynamic>);
  }

  Future<void> logout() async {
    try {
      await api.post('/api/Auth/logout');
    } on ApiException {
      // Lokalna sesija se brise bez obzira na odgovor servera.
    }

    await _endSession();
  }

  Future<void> _saveSession(Map<String, dynamic> json) async {
    api.token = json['token'] as String;
    userId = json['userId'] as int;
    username = json['username'] as String;
    roles = List<String>.from(json['roles'] as List);

    await _storage.write(key: 'token', value: api.token);
    await _storage.write(key: 'expiresAt', value: json['expiresAt'] as String);
    await _storage.write(key: 'userId', value: userId.toString());
    await _storage.write(key: 'username', value: username);
    await _storage.write(key: 'roles', value: jsonEncode(roles));

    notifyListeners();
  }

  void _handleUnauthorized() {
    _endSession();
  }

  Future<void> _endSession() async {
    await _clear();
    navigatorKey.currentState?.popUntil((route) => route.isFirst);
    notifyListeners();
  }

  Future<void> _clear() async {
    api.token = null;
    userId = null;
    username = null;
    roles = const [];
    await _storage.deleteAll();
  }
}
