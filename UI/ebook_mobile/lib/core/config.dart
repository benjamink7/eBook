class Config {
  // Adresa API-ja se zadaje pri pokretanju: --dart-define=API_BASE_URL=http://10.0.2.2:5000
  static const String apiBaseUrl =
      String.fromEnvironment('API_BASE_URL', defaultValue: 'http://10.0.2.2:5000');
}
