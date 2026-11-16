class Validators {
  static final RegExp _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  static final RegExp _username = RegExp(r'^[a-zA-Z0-9._]{3,50}$');
  static final RegExp _phone = RegExp(r'^\+?[0-9 ]{6,20}$');
  static final RegExp _code = RegExp(r'^\d{6}$');

  static String? required(String? value, String message) =>
      (value == null || value.trim().isEmpty) ? message : null;

  static String? name(String? value, String label) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return '$label je obavezno.';
    }

    if (text.length < 2 || text.length > 50) {
      return '$label mora imati između 2 i 50 znakova.';
    }

    return null;
  }

  static String? email(String? value) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return 'E-mail adresa je obavezna.';
    }

    return _email.hasMatch(text) ? null : 'Unesite validnu e-mail adresu u formatu: ime@domena.ba';
  }

  static String? username(String? value) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return 'Korisničko ime je obavezno.';
    }

    return _username.hasMatch(text)
        ? null
        : 'Korisničko ime mora imati 3 do 50 znakova (slova, brojevi, tačka i donja crta).';
  }

  static String? password(String? value) {
    final text = value ?? '';

    if (text.isEmpty) {
      return 'Lozinka je obavezna.';
    }

    final valid = text.length >= 8 &&
        text.length <= 100 &&
        RegExp(r'[a-z]').hasMatch(text) &&
        RegExp(r'[A-Z]').hasMatch(text) &&
        RegExp(r'\d').hasMatch(text);

    return valid
        ? null
        : 'Lozinka mora imati 8 do 100 znakova, uključujući veliko slovo, malo slovo i broj.';
  }

  static String? confirmPassword(String? value, String original) {
    if (value == null || value.isEmpty) {
      return 'Potvrda lozinke je obavezna.';
    }

    return value == original ? null : 'Lozinke se ne podudaraju.';
  }

  static String? phone(String? value) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return null;
    }

    return _phone.hasMatch(text) ? null : 'Unesite telefon u formatu: +387 61 123 456';
  }

  static String? code(String? value) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return 'Kod je obavezan.';
    }

    return _code.hasMatch(text) ? null : 'Kod mora imati tačno 6 cifara, npr. 123456.';
  }
}
