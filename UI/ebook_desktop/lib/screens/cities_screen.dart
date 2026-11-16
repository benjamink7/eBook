import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/paged_table.dart';
import '../widgets/save_dialog.dart';

Future<List<Country>> loadCountries(BuildContext context) async {
  final json = await context.read<AuthProvider>().api.get('/api/Countries', query: {'pageSize': 100});
  return PagedResult.fromJson(json, Country.fromJson).items;
}

class CitiesScreen extends StatefulWidget {
  const CitiesScreen({super.key});

  @override
  State<CitiesScreen> createState() => _CitiesScreenState();
}

class _CitiesScreenState extends State<CitiesScreen> {
  final _tableKey = GlobalKey<PagedTableState<City>>();
  List<Country> _countries = const [];
  int? _countryId;

  @override
  void initState() {
    super.initState();
    _loadCountries();
  }

  Future<void> _loadCountries() async {
    try {
      final countries = await loadCountries(context);

      if (mounted) {
        setState(() => _countries = countries);
      }
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  Future<void> _edit([City? city]) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _CityDialog(city: city, countries: _countries));

    if (saved == true) {
      _tableKey.currentState?.reload();
    }
  }

  Future<void> _delete(City city) async {
    final confirmed = await confirmAction(
      context,
      title: 'Brisanje grada',
      message: 'Da li ste sigurni da želite obrisati grad "${city.name}"?',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/Cities/${city.id}');
      showMessage('Grad je obrisan.');
      _tableKey.currentState?.reload();
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Gradovi', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<City>(
            key: _tableKey,
            path: '/api/Cities',
            query: {'countryId': _countryId},
            fromJson: City.fromJson,
            searchHint: 'Pretraga po nazivu',
            actions: [
              SizedBox(
                width: 220,
                child: DropdownButtonFormField<int?>(
                  initialValue: _countryId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Država', isDense: true, border: OutlineInputBorder()),
                  items: [
                    const DropdownMenuItem<int?>(value: null, child: Text('Sve države')),
                    for (final country in _countries) DropdownMenuItem<int?>(value: country.id, child: Text(country.name)),
                  ],
                  onChanged: (value) => setState(() => _countryId = value),
                ),
              ),
              const SizedBox(width: 12),
              FilledButton.icon(onPressed: _edit, icon: const Icon(Icons.add), label: const Text('Dodaj grad')),
            ],
            columns: const [
              DataColumn(label: Text('Naziv')),
              DataColumn(label: Text('Država')),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (city) => [
              DataCell(Text(city.name)),
              DataCell(Text(city.countryName)),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.edit_outlined), tooltip: 'Uredi', onPressed: () => _edit(city)),
                  IconButton(icon: const Icon(Icons.delete_outline), tooltip: 'Obriši', onPressed: () => _delete(city)),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}

class _CityDialog extends StatefulWidget {
  const _CityDialog({this.city, required this.countries});

  final City? city;
  final List<Country> countries;

  @override
  State<_CityDialog> createState() => _CityDialogState();
}

class _CityDialogState extends State<_CityDialog> with SaveDialogMixin {
  late final TextEditingController _name = TextEditingController(text: widget.city?.name);
  late int? _countryId = widget.city?.countryId;

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return buildDialog(
      title: widget.city == null ? 'Novi grad' : 'Uređivanje grada',
      children: [
        dialogField(_name, 'Naziv', autofocus: true, validator: (v) => Validators.text(v, 'Naziv grada', max: 100)),
        Padding(
          padding: const EdgeInsets.only(top: 12),
          child: DropdownButtonFormField<int>(
            initialValue: _countryId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Država', border: OutlineInputBorder()),
            items: [for (final country in widget.countries) DropdownMenuItem(value: country.id, child: Text(country.name))],
            onChanged: (value) => _countryId = value,
            validator: (v) => v == null ? 'Odaberite državu.' : null,
          ),
        ),
      ],
      onSave: () => submit(() async {
        final body = {'name': _name.text.trim(), 'countryId': _countryId};

        if (widget.city == null) {
          await api.post('/api/Cities', body: body);
        } else {
          await api.put('/api/Cities/${widget.city!.id}', body: body);
        }
      }, 'Grad je sačuvan.'),
    );
  }
}
