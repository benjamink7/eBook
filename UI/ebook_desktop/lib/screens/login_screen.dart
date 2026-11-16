import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/validators.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _username = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _username.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      await context.read<AuthProvider>().login(_username.text.trim(), _password.text);
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
    final theme = Theme.of(context);

    return Scaffold(
      body: Center(
        child: SizedBox(
          width: 400,
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(32),
              child: Form(
                key: _formKey,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Icon(Icons.auto_stories, size: 64, color: theme.colorScheme.primary),
                    const SizedBox(height: 8),
                    Text('eBook Admin', textAlign: TextAlign.center, style: theme.textTheme.headlineMedium),
                    const SizedBox(height: 24),
                    TextFormField(
                      controller: _username,
                      autofocus: true,
                      decoration: const InputDecoration(
                          labelText: 'Korisničko ime', border: OutlineInputBorder(), prefixIcon: Icon(Icons.person)),
                      textInputAction: TextInputAction.next,
                      validator: (v) => Validators.requiredField(v, 'Korisničko ime je obavezno.'),
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: _password,
                      obscureText: true,
                      decoration: const InputDecoration(
                          labelText: 'Lozinka', border: OutlineInputBorder(), prefixIcon: Icon(Icons.lock)),
                      onFieldSubmitted: (_) => _submit(),
                      validator: (v) => Validators.requiredField(v, 'Lozinka je obavezna.'),
                    ),
                    if (_error != null) ...[
                      const SizedBox(height: 12),
                      Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                    ],
                    const SizedBox(height: 20),
                    FilledButton(
                      onPressed: _busy ? null : _submit,
                      child: _busy
                          ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Text('Prijava'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
