import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import 'api_exception.dart';
import 'config.dart';

class ApiClient {
  ApiClient({this.onUnauthorized});

  String? token;
  final void Function()? onUnauthorized;

  static const Duration _timeout = Duration(seconds: 20);

  Map<String, String> get authHeaders => {if (token != null) 'Authorization': 'Bearer $token'};

  Uri uri(String path, [Map<String, dynamic>? query]) {
    final base = Uri.parse(Config.apiBaseUrl);
    final params = <String, String>{};
    query?.forEach((key, value) {
      if (value != null && value.toString().isNotEmpty) {
        params[key] = value.toString();
      }
    });

    return base.replace(path: path, queryParameters: params.isEmpty ? null : params);
  }

  Future<dynamic> get(String path, {Map<String, dynamic>? query}) =>
      _send('GET', path, query: query);

  Future<dynamic> post(String path, {Object? body}) => _send('POST', path, body: body);

  Future<dynamic> put(String path, {Object? body}) => _send('PUT', path, body: body);

  Future<dynamic> delete(String path) => _send('DELETE', path);

  Future<dynamic> _send(
    String method,
    String path, {
    Map<String, dynamic>? query,
    Object? body,
  }) async {
    final request = http.Request(method, uri(path, query));
    request.headers.addAll({
      'Accept': 'application/json',
      ...authHeaders,
      if (body != null) 'Content-Type': 'application/json',
    });

    if (body != null) {
      request.body = jsonEncode(body);
    }

    return _execute(request, path);
  }

  Future<dynamic> putFile(
    String path, {
    required String field,
    required List<int> bytes,
    required String filename,
  }) {
    final request = http.MultipartRequest('PUT', uri(path))
      ..headers.addAll({'Accept': 'application/json', ...authHeaders})
      ..files.add(http.MultipartFile.fromBytes(field, bytes, filename: filename));

    return _execute(request, path);
  }

  Future<dynamic> _execute(http.BaseRequest request, String path) async {
    http.Response response;

    try {
      final streamed = await request.send().timeout(_timeout);
      response = await http.Response.fromStream(streamed);
    } on TimeoutException {
      throw ApiException('Server ne odgovara. Pokušajte ponovo.');
    } on SocketException {
      throw ApiException('Nema veze sa serverom. Provjerite internet vezu.');
    } on http.ClientException {
      throw ApiException('Nema veze sa serverom. Provjerite internet vezu.');
    }

    final isAuthEndpoint = path.startsWith('/api/Auth/login') || path.startsWith('/api/Auth/register');

    if (response.statusCode == 401 && !isAuthEndpoint) {
      onUnauthorized?.call();
      throw ApiException('Sesija je istekla. Prijavite se ponovo.', statusCode: 401);
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) {
        return null;
      }

      return jsonDecode(utf8.decode(response.bodyBytes));
    }

    throw ApiException(_errorMessage(response), statusCode: response.statusCode);
  }

  String _errorMessage(http.Response response) {
    try {
      final json = jsonDecode(utf8.decode(response.bodyBytes));

      if (json is Map<String, dynamic>) {
        if (json['message'] is String) {
          return json['message'] as String;
        }

        final errors = json['errors'];

        if (errors is Map<String, dynamic> && errors.isNotEmpty) {
          final first = errors.values.first;

          if (first is List && first.isNotEmpty) {
            return first.first.toString();
          }
        }
      }
    } catch (_) {
      // Tijelo odgovora nije JSON, koristi se opca poruka ispod.
    }

    if (response.statusCode == 403) {
      return 'Nemate pravo pristupa.';
    }

    if (response.statusCode == 429) {
      return 'Previše pokušaja. Sačekajte trenutak i pokušajte ponovo.';
    }

    return 'Došlo je do greške. Pokušajte ponovo.';
  }
}
