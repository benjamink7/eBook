class Config {
  // Adresa API-ja se zadaje pri pokretanju: --dart-define=API_BASE_URL=http://localhost:5000
  static const String apiBaseUrl =
      String.fromEnvironment('API_BASE_URL', defaultValue: 'http://localhost:5000');
}
