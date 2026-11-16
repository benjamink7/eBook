import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/paged_table.dart';
import '../widgets/save_dialog.dart';

class CountriesScreen extends StatefulWidget {
  const CountriesScreen({super.key});

  @override
  State<CountriesScreen> createState() => _CountriesScreenState();
}

class _CountriesScreenState extends State<CountriesScreen> {
  final _tableKey = GlobalKey<PagedTableState<Country>>();

  Future<void> _edit([Country? country]) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _CountryDialog(country: country));

    if (saved == true) {
      _tableKey.currentState?.reload();
    }
  }

  Future<void> _delete(Country country) async {
    final confirmed = await confirmAction(
      context,
      title: 'Brisanje države',
      message: 'Da li ste sigurni da želite obrisati državu "${country.name}"?',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/Countries/${country.id}');
      showMessage('Država je obrisana.');
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
        Text('Države', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<Country>(
            key: _tableKey,
            path: '/api/Countries',
            fromJson: Country.fromJson,
            searchHint: 'Pretraga po nazivu',
            actions: [FilledButton.icon(onPressed: _edit, icon: const Icon(Icons.add), label: const Text('Dodaj državu'))],
            columns: const [
              DataColumn(label: Text('Naziv')),
              DataColumn(label: Text('Broj gradova'), numeric: true),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (country) => [
              DataCell(Text(country.name)),
              DataCell(Text('${country.cityCount}')),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.edit_outlined), tooltip: 'Uredi', onPressed: () => _edit(country)),
                  IconButton(icon: const Icon(Icons.delete_outline), tooltip: 'Obriši', onPressed: () => _delete(country)),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}

class _CountryDialog extends StatefulWidget {
  const _CountryDialog({this.country});

  final Country? country;

  @override
  State<_CountryDialog> createState() => _CountryDialogState();
}

class _CountryDialogState extends State<_CountryDialog> with SaveDialogMixin {
  late final TextEditingController _name = TextEditingController(text: widget.country?.name);

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return buildDialog(
      title: widget.country == null ? 'Nova država' : 'Uređivanje države',
      children: [dialogField(_name, 'Naziv', autofocus: true, validator: (v) => Validators.text(v, 'Naziv države', max: 100))],
      onSave: () => submit(() async {
        final body = {'name': _name.text.trim()};

        if (widget.country == null) {
          await api.post('/api/Countries', body: body);
        } else {
          await api.put('/api/Countries/${widget.country!.id}', body: body);
        }
      }, 'Država je sačuvana.'),
    );
  }
}
