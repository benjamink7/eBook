class Validators {
  static String? text(String? value, String label, {int min = 2, required int max}) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return '$label je obavezan.';
    }

    if (text.length < min || text.length > max) {
      return '$label mora imati između $min i $max znakova.';
    }

    return null;
  }

  static String? optionalText(String? value, String label, {required int max}) {
    final text = value?.trim() ?? '';

    return text.length > max ? '$label može imati najviše $max znakova.' : null;
  }

  static String? requiredField(String? value, String message) =>
      (value == null || value.trim().isEmpty) ? message : null;
}
