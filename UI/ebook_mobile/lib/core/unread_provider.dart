import 'dart:async';

import 'package:flutter/foundation.dart';

import 'api_client.dart';
import 'api_exception.dart';

class UnreadProvider extends ChangeNotifier {
  UnreadProvider(this._api);

  static const Duration _interval = Duration(seconds: 30);

  final ApiClient _api;
  Timer? _timer;
  int count = 0;

  void start() {
    _timer?.cancel();
    refresh();
    _timer = Timer.periodic(_interval, (_) => refresh());
  }

  Future<void> refresh() async {
    try {
      final json = await _api.get('/api/Notifications/unread-count') as Map<String, dynamic>;
      final value = json['count'] as int;

      if (value != count) {
        count = value;
        notifyListeners();
      }
    } on ApiException {
      // Sljedeci ciklus osvjezavanja ponovo pokusava dohvatiti broj.
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}
