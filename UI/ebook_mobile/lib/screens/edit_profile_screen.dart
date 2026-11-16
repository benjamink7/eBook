import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/profile_avatar.dart';

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key, required this.profile});

  final Profile profile;

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _firstName = TextEditingController(text: widget.profile.firstName);
  late final TextEditingController _lastName = TextEditingController(text: widget.profile.lastName);
  late final TextEditingController _email = TextEditingController(text: widget.profile.email);
  late final TextEditingController _phone = TextEditingController(text: widget.profile.phoneNumber ?? '');

  List<City> _cities = const [];
  late int? _cityId = widget.profile.cityId;
  late String? _pictureUrl = widget.profile.pictureUrl;
  int _pictureVersion = 0;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadCities();
  }

  @override
  void dispose() {
    for (final c in [_firstName, _lastName, _email, _phone]) {
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

  Future<void> _pickPicture() async {
    final api = context.read<AuthProvider>().api;
    final file = await ImagePicker().pickImage(source: ImageSource.gallery, maxWidth: 800, imageQuality: 85);

    if (file == null) {
      return;
    }

    setState(() => _busy = true);

    try {
      final json = await api.putFile(
        '/api/Profile/picture',
        field: 'image',
        bytes: await file.readAsBytes(),
        filename: file.name,
      ) as Map<String, dynamic>;

      if (mounted) {
        setState(() {
          _pictureUrl = json['pictureUrl'] as String?;
          _pictureVersion++;
        });
      }
    } on ApiException catch (e) {
      showMessage(e.message);
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    final phone = _phone.text.trim();

    try {
      await context.read<AuthProvider>().api.put('/api/Profile', body: {
        'firstName': _firstName.text.trim(),
        'lastName': _lastName.text.trim(),
        'email': _email.text.trim(),
        'phoneNumber': phone.isEmpty ? null : phone,
        'cityId': _cityId,
      });

      if (!mounted) {
        return;
      }

      Navigator.of(context).pop(true);
      showMessage('Profil je sačuvan.');
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
      appBar: AppBar(title: const Text('Uredi profil')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Center(child: ProfileAvatar(pictureUrl: _pictureUrl, radius: 52, version: _pictureVersion)),
                TextButton.icon(
                  onPressed: _busy ? null : _pickPicture,
                  icon: const Icon(Icons.photo_camera),
                  label: const Text('Promijeni sliku'),
                ),
                const SizedBox(height: 8),
                _field(_firstName, 'Ime', validator: (v) => Validators.name(v, 'Ime')),
                _field(_lastName, 'Prezime', validator: (v) => Validators.name(v, 'Prezime')),
                _field(_email, 'E-mail', type: TextInputType.emailAddress, validator: Validators.email),
                _field(_phone, 'Telefon (opcionalno)', type: TextInputType.phone, validator: Validators.phone),
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: DropdownButtonFormField<int>(
                    initialValue: _cities.any((c) => c.id == _cityId) ? _cityId : null,
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
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                  ),
                FilledButton(
                  onPressed: _busy ? null : _save,
                  child: _busy
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Sačuvaj'),
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
    TextInputType? type,
    String? Function(String?)? validator,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextFormField(
        controller: controller,
        keyboardType: type,
        decoration: InputDecoration(labelText: label),
        validator: validator,
      ),
    );
  }
}
