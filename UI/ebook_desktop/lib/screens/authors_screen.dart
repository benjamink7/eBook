import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/paged_table.dart';
import '../widgets/save_dialog.dart';

class AuthorsScreen extends StatefulWidget {
  const AuthorsScreen({super.key});

  @override
  State<AuthorsScreen> createState() => _AuthorsScreenState();
}

class _AuthorsScreenState extends State<AuthorsScreen> {
  final _tableKey = GlobalKey<PagedTableState<Author>>();

  Future<void> _edit([Author? author]) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _AuthorDialog(author: author));

    if (saved == true) {
      _tableKey.currentState?.reload();
    }
  }

  Future<void> _delete(Author author) async {
    final confirmed = await confirmAction(
      context,
      title: 'Brisanje autora',
      message: 'Da li ste sigurni da želite obrisati autora "${author.fullName}"?',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/Authors/${author.id}');
      showMessage('Autor je obrisan.');
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
        Text('Autori', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<Author>(
            key: _tableKey,
            path: '/api/Authors',
            fromJson: Author.fromJson,
            searchHint: 'Pretraga po imenu',
            actions: [FilledButton.icon(onPressed: _edit, icon: const Icon(Icons.add), label: const Text('Dodaj autora'))],
            columns: const [
              DataColumn(label: Text('Ime i prezime')),
              DataColumn(label: Text('Godina rođenja'), numeric: true),
              DataColumn(label: Text('Broj knjiga'), numeric: true),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (author) => [
              DataCell(Text(author.fullName)),
              DataCell(Text(author.birthYear?.toString() ?? '-')),
              DataCell(Text('${author.bookCount}')),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.edit_outlined), tooltip: 'Uredi', onPressed: () => _edit(author)),
                  IconButton(icon: const Icon(Icons.delete_outline), tooltip: 'Obriši', onPressed: () => _delete(author)),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}

class _AuthorDialog extends StatefulWidget {
  const _AuthorDialog({this.author});

  final Author? author;

  @override
  State<_AuthorDialog> createState() => _AuthorDialogState();
}

class _AuthorDialogState extends State<_AuthorDialog> with SaveDialogMixin {
  late final TextEditingController _firstName = TextEditingController(text: widget.author?.firstName);
  late final TextEditingController _lastName = TextEditingController(text: widget.author?.lastName);
  late final TextEditingController _birthYear = TextEditingController(text: widget.author?.birthYear?.toString());
  late final TextEditingController _biography = TextEditingController(text: widget.author?.biography);

  @override
  void dispose() {
    for (final c in [_firstName, _lastName, _birthYear, _biography]) {
      c.dispose();
    }
    super.dispose();
  }

  String? _validateYear(String? value) {
    final text = (value ?? '').trim();

    if (text.isEmpty) {
      return null;
    }

    final year = int.tryParse(text);

    return year == null || year < 1000 || year > 2100 ? 'Godina rođenja mora biti između 1000 i 2100.' : null;
  }

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return buildDialog(
      title: widget.author == null ? 'Novi autor' : 'Uređivanje autora',
      children: [
        dialogField(_firstName, 'Ime', autofocus: true, validator: (v) => Validators.text(v, 'Ime', max: 100)),
        dialogField(_lastName, 'Prezime', validator: (v) => Validators.text(v, 'Prezime', max: 100)),
        dialogField(_birthYear, 'Godina rođenja (opcionalno)', type: TextInputType.number, validator: _validateYear),
        dialogField(_biography, 'Biografija (opcionalno)', maxLines: 4, validator: (v) => Validators.optionalText(v, 'Biografija', max: 4000)),
      ],
      onSave: () => submit(() async {
        final biography = _biography.text.trim();
        final year = _birthYear.text.trim();
        final body = {
          'firstName': _firstName.text.trim(),
          'lastName': _lastName.text.trim(),
          'biography': biography.isEmpty ? null : biography,
          'birthYear': year.isEmpty ? null : int.parse(year),
        };

        if (widget.author == null) {
          await api.post('/api/Authors', body: body);
        } else {
          await api.put('/api/Authors/${widget.author!.id}', body: body);
        }
      }, 'Autor je sačuvan.'),
    );
  }
}
