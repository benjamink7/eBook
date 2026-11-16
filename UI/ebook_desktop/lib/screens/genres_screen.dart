import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/paged_table.dart';

class GenresScreen extends StatefulWidget {
  const GenresScreen({super.key});

  @override
  State<GenresScreen> createState() => _GenresScreenState();
}

class _GenresScreenState extends State<GenresScreen> {
  final _tableKey = GlobalKey<PagedTableState<Genre>>();

  Future<void> _edit([Genre? genre]) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _GenreDialog(genre: genre));

    if (saved == true) {
      _tableKey.currentState?.reload();
    }
  }

  Future<void> _delete(Genre genre) async {
    final confirmed = await confirmAction(
      context,
      title: 'Brisanje žanra',
      message: 'Da li ste sigurni da želite obrisati žanr "${genre.name}"?',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/Genres/${genre.id}');
      showMessage('Žanr je obrisan.');
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
        Text('Žanrovi', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<Genre>(
            key: _tableKey,
            path: '/api/Genres',
            fromJson: Genre.fromJson,
            searchHint: 'Pretraga po nazivu',
            actions: [FilledButton.icon(onPressed: _edit, icon: const Icon(Icons.add), label: const Text('Dodaj žanr'))],
            columns: const [
              DataColumn(label: Text('Naziv')),
              DataColumn(label: Text('Opis')),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (genre) => [
              DataCell(Text(genre.name)),
              DataCell(Text(genre.description ?? '')),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.edit_outlined), tooltip: 'Uredi', onPressed: () => _edit(genre)),
                  IconButton(icon: const Icon(Icons.delete_outline), tooltip: 'Obriši', onPressed: () => _delete(genre)),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}

class _GenreDialog extends StatefulWidget {
  const _GenreDialog({this.genre});

  final Genre? genre;

  @override
  State<_GenreDialog> createState() => _GenreDialogState();
}

class _GenreDialogState extends State<_GenreDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name = TextEditingController(text: widget.genre?.name);
  late final TextEditingController _description = TextEditingController(text: widget.genre?.description);
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _name.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    final api = context.read<AuthProvider>().api;
    final description = _description.text.trim();
    final body = {'name': _name.text.trim(), 'description': description.isEmpty ? null : description};

    try {
      if (widget.genre == null) {
        await api.post('/api/Genres', body: body);
      } else {
        await api.put('/api/Genres/${widget.genre!.id}', body: body);
      }

      if (mounted) {
        Navigator.pop(context, true);
        showMessage('Žanr je sačuvan.');
      }
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
    return AlertDialog(
      title: Text(widget.genre == null ? 'Novi žanr' : 'Uređivanje žanra'),
      content: SizedBox(
        width: 420,
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: _name,
                autofocus: true,
                decoration: const InputDecoration(labelText: 'Naziv', border: OutlineInputBorder()),
                validator: (v) => Validators.text(v, 'Naziv žanra', max: 100),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _description,
                maxLines: 3,
                decoration: const InputDecoration(labelText: 'Opis (opcionalno)', border: OutlineInputBorder()),
                validator: (v) => Validators.optionalText(v, 'Opis', max: 500),
              ),
              if (_error != null) ...[
                const SizedBox(height: 12),
                Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: _busy ? null : () => Navigator.pop(context), child: const Text('Odustani')),
        FilledButton(onPressed: _busy ? null : _save, child: const Text('Sačuvaj')),
      ],
    );
  }
}
