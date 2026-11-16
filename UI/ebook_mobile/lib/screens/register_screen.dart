import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/validators.dart';
import '../models/models.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _email = TextEditingController();
  final _username = TextEditingController();
  final _phone = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  List<City> _cities = const [];
  int? _cityId;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadCities();
  }

  @override
  void dispose() {
    for (final c in [_firstName, _lastName, _email, _username, _phone, _password, _confirm]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _loadCities() async {
    try {
      final json = await context.read<AuthProvider>().api.get('/api/Cities', query: {'pageSize': 100});
      final result = PagedResult.fromJson(json, City.fromJson);

      if (mounted) {
        setState(() => _cities = result.items);
      }
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    final phone = _phone.text.trim();

    try {
      await context.read<AuthProvider>().register({
        'firstName': _firstName.text.trim(),
        'lastName': _lastName.text.trim(),
        'email': _email.text.trim(),
        'username': _username.text.trim(),
        'password': _password.text,
        'confirmPassword': _confirm.text,
        'phoneNumber': phone.isEmpty ? null : phone,
        'cityId': _cityId,
      });

      navigatorKey.currentState?.popUntil((route) => route.isFirst);
      showMessage('Račun je uspješno kreiran.');
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registracija')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _field(_firstName, 'Ime', validator: (v) => Validators.name(v, 'Ime')),
                _field(_lastName, 'Prezime', validator: (v) => Validators.name(v, 'Prezime')),
                _field(_email, 'E-mail', type: TextInputType.emailAddress, validator: Validators.email),
                _field(_username, 'Korisničko ime', validator: Validators.username),
                _field(_phone, 'Telefon (opcionalno)', type: TextInputType.phone, validator: Validators.phone),
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: DropdownButtonFormField<int>(
                    initialValue: _cityId,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Grad'),
                    items: [
                      for (final city in _cities)
                        DropdownMenuItem(value: city.id, child: Text('${city.name}, ${city.countryName}')),
                    ],
                    onChanged: (value) => setState(() => _cityId = value),
                    validator: (v) => v == null ? 'Odaberite grad.' : null,
                  ),
                ),
                _field(_password, 'Lozinka', obscure: true, validator: Validators.password),
                _field(
                  _confirm,
                  'Potvrda lozinke',
                  obscure: true,
                  validator: (v) => Validators.confirmPassword(v, _password.text),
                ),
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                  ),
                FilledButton(
                  onPressed: _busy ? null : _submit,
                  child: _busy
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Registruj se'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _field(
    TextEditingController controller,
    String label, {
    bool obscure = false,
    TextInputType? type,
    String? Function(String?)? validator,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextFormField(
        controller: controller,
        obscureText: obscure,
        keyboardType: type,
        decoration: InputDecoration(labelText: label),
        validator: validator,
      ),
    );
  }
}
