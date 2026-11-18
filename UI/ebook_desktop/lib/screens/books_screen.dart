import 'dart:io';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/uploads.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/auth_image.dart';
import '../widgets/paged_table.dart';
import '../widgets/save_dialog.dart';

class BooksScreen extends StatefulWidget {
  const BooksScreen({super.key});

  @override
  State<BooksScreen> createState() => _BooksScreenState();
}

class _BooksScreenState extends State<BooksScreen> {
  final _tableKey = GlobalKey<PagedTableState<BookRow>>();
  List<Genre> _genres = const [];
  int? _genreId;
  String? _accessType;
  int _coverVersion = 0;

  @override
  void initState() {
    super.initState();
    _loadGenres();
  }

  Future<void> _loadGenres() async {
    try {
      final json = await context.read<AuthProvider>().api.get('/api/Genres', query: {'pageSize': 100});

      if (mounted) {
        setState(() => _genres = PagedResult.fromJson(json, Genre.fromJson).items);
      }
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  Future<void> _edit([BookRow? book]) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _BookDialog(book: book));

    if (saved == true) {
      setState(() => _coverVersion++);
      _tableKey.currentState?.reload();
    }
  }

  Future<void> _delete(BookRow book) async {
    final confirmed = await confirmAction(
      context,
      title: 'Brisanje knjige',
      message: 'Da li ste sigurni da želite obrisati knjigu "${book.title}"?',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/Books/${book.id}');
      showMessage('Knjiga je obrisana.');
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
        Text('Knjige', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<BookRow>(
            key: _tableKey,
            path: '/api/Books',
            query: {'genreId': _genreId, 'accessType': _accessType},
            fromJson: BookRow.fromJson,
            searchHint: 'Pretraga po naslovu ili autoru',
            actions: [
              SizedBox(
                width: 180,
                child: DropdownButtonFormField<int?>(
                  initialValue: _genreId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Žanr', isDense: true, border: OutlineInputBorder()),
                  items: [
                    const DropdownMenuItem<int?>(value: null, child: Text('Svi žanrovi')),
                    for (final genre in _genres) DropdownMenuItem<int?>(value: genre.id, child: Text(genre.name)),
                  ],
                  onChanged: (value) => setState(() => _genreId = value),
                ),
              ),
              const SizedBox(width: 12),
              SizedBox(
                width: 160,
                child: DropdownButtonFormField<String?>(
                  initialValue: _accessType,
                  decoration: const InputDecoration(labelText: 'Pristup', isDense: true, border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem<String?>(value: null, child: Text('Svi')),
                    DropdownMenuItem<String?>(value: 'Free', child: Text('Besplatne')),
                    DropdownMenuItem<String?>(value: 'Premium', child: Text('Premium')),
                  ],
                  onChanged: (value) => setState(() => _accessType = value),
                ),
              ),
              const SizedBox(width: 12),
              FilledButton.icon(onPressed: _edit, icon: const Icon(Icons.add), label: const Text('Dodaj knjigu')),
            ],
            columns: const [
              DataColumn(label: Text('')),
              DataColumn(label: Text('Naslov')),
              DataColumn(label: Text('Autor')),
              DataColumn(label: Text('Godina'), numeric: true),
              DataColumn(label: Text('Pristup')),
              DataColumn(label: Text('Žanrovi')),
              DataColumn(label: Text('Ocjena'), numeric: true),
              DataColumn(label: Text('Aktivna')),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (book) => [
              DataCell(Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: AuthImage(url: book.coverUrl, version: _coverVersion),
              )),
              DataCell(Text(book.title)),
              DataCell(Text(book.authorName)),
              DataCell(Text('${book.publishedYear}')),
              DataCell(Text(book.isPremium ? 'Premium' : 'Besplatna')),
              DataCell(Text(book.genres.join(', '))),
              DataCell(Text(book.averageRating?.toStringAsFixed(1) ?? '-')),
              DataCell(Icon(book.isActive ? Icons.check_circle : Icons.cancel_outlined)),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.edit_outlined), tooltip: 'Uredi', onPressed: () => _edit(book)),
                  IconButton(icon: const Icon(Icons.delete_outline), tooltip: 'Obriši', onPressed: () => _delete(book)),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}

class _BookDialog extends StatefulWidget {
  const _BookDialog({this.book});

  final BookRow? book;

  @override
  State<_BookDialog> createState() => _BookDialogState();
}

class _BookDialogState extends State<_BookDialog> with SaveDialogMixin {
  final _title = TextEditingController();
  final _description = TextEditingController();
  final _year = TextEditingController();
  final _content = TextEditingController();

  List<Author> _authors = const [];
  List<Genre> _genres = const [];
  final Set<int> _genreIds = {};
  int? _authorId;
  bool _isPremium = false;
  bool _isActive = true;
  String? _coverPath;
  String? _coverError;
  bool _loading = true;

  bool get _isEdit => widget.book != null;

  @override
  void initState() {
    super.initState();
    _init();
  }

  @override
  void dispose() {
    for (final c in [_title, _description, _year, _content]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _init() async {
    final api = context.read<AuthProvider>().api;

    try {
      final authors = await api.get('/api/Authors', query: {'pageSize': 100});
      final genres = await api.get('/api/Genres', query: {'pageSize': 100});
      BookDetails? details;

      if (_isEdit) {
        details = BookDetails.fromJson(await api.get('/api/Books/${widget.book!.id}') as Map<String, dynamic>);
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _authors = PagedResult.fromJson(authors, Author.fromJson).items;
        _genres = PagedResult.fromJson(genres, Genre.fromJson).items;

        if (details != null) {
          _title.text = details.title;
          _description.text = details.description;
          _year.text = details.publishedYear.toString();
          _authorId = details.authorId;
          _genreIds.addAll(details.genreIds);
          _isPremium = details.isPremium;
          _isActive = details.isActive;
        }

        _loading = false;
      });
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          error = e.message;
          _loading = false;
        });
      }
    }
  }

  Future<void> _pickCover() async {
    final path = await pickFilePath(extensions: ['png', 'jpg', 'jpeg']);

    if (path == null) {
      return;
    }

    setState(() {
      _coverError = validateImageFile(path);
      _coverPath = _coverError == null ? path : null;
    });
  }

  Future<void> _loadContent() async {
    final path = await pickFilePath(extensions: ['txt']);

    if (path == null) {
      return;
    }

    try {
      final text = await File(path).readAsString();
      setState(() => _content.text = text);
    } on FileSystemException {
      setState(() => error = 'Datoteku nije moguće pročitati.');
    } on FormatException {
      setState(() => error = 'Datoteka mora biti tekst u UTF-8 formatu.');
    }
  }

  String? _validateYear(String? value) {
    final year = int.tryParse((value ?? '').trim());

    return year == null || year < 1000 || year > 2100 ? 'Godina izdanja mora biti između 1000 i 2100.' : null;
  }

  String? _validateContent(String? value) {
    final text = value ?? '';

    if (!_isEdit && text.trim().isEmpty) {
      return 'Sadržaj knjige je obavezan.';
    }

    return text.length > 5000000 ? 'Sadržaj knjige može imati najviše 5 miliona znakova.' : null;
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const AlertDialog(content: SizedBox(width: 120, height: 80, child: Center(child: CircularProgressIndicator())));
    }

    final api = context.read<AuthProvider>().api;

    return buildDialog(
      title: _isEdit ? 'Uređivanje knjige' : 'Nova knjiga',
      width: 680,
      children: [
        dialogField(_title, 'Naslov', autofocus: true, validator: (v) => Validators.text(v, 'Naslov', min: 1, max: 200)),
        dialogField(_description, 'Opis', maxLines: 3, validator: (v) => Validators.text(v, 'Opis', min: 1, max: 4000)),
        Padding(
          padding: const EdgeInsets.only(top: 12),
          child: Row(
            children: [
              Expanded(
                child: TextFormField(
                  controller: _year,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Godina izdanja', border: OutlineInputBorder()),
                  validator: _validateYear,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: DropdownButtonFormField<bool>(
                  initialValue: _isPremium,
                  decoration: const InputDecoration(labelText: 'Pristup', border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem(value: false, child: Text('Besplatna')),
                    DropdownMenuItem(value: true, child: Text('Premium')),
                  ],
                  onChanged: (value) => _isPremium = value ?? false,
                ),
              ),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.only(top: 12),
          child: DropdownButtonFormField<int>(
            initialValue: _authorId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Autor', border: OutlineInputBorder()),
            items: [for (final author in _authors) DropdownMenuItem(value: author.id, child: Text(author.fullName))],
            onChanged: (value) => _authorId = value,
            validator: (v) => v == null ? 'Odaberite autora.' : null,
          ),
        ),
        Padding(
          padding: const EdgeInsets.only(top: 12),
          child: FormField<Set<int>>(
            initialValue: _genreIds,
            validator: (value) => (value == null || value.isEmpty) ? 'Odaberite barem jedan žanr.' : null,
            builder: (state) => Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Žanrovi'),
                const SizedBox(height: 4),
                Wrap(
                  spacing: 8,
                  runSpacing: 4,
                  children: [
                    for (final genre in _genres)
                      FilterChip(
                        label: Text(genre.name),
                        selected: _genreIds.contains(genre.id),
                        onSelected: (selected) {
                          setState(() => selected ? _genreIds.add(genre.id) : _genreIds.remove(genre.id));
                          state.didChange(_genreIds);
                        },
                      ),
                  ],
                ),
                if (state.hasError)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(state.errorText!, style: TextStyle(color: Theme.of(context).colorScheme.error, fontSize: 12)),
                  ),
              ],
            ),
          ),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Aktivna'),
          value: _isActive,
          onChanged: (value) => setState(() => _isActive = value),
        ),
        Row(
          children: [
            OutlinedButton.icon(onPressed: _pickCover, icon: const Icon(Icons.image_outlined), label: const Text('Odaberi naslovnicu')),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                _coverError ?? (_coverPath == null ? 'PNG ili JPEG, najviše 2 MB' : fileNameOf(_coverPath!)),
                style: _coverError == null ? null : TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            OutlinedButton.icon(onPressed: _loadContent, icon: const Icon(Icons.upload_file), label: const Text('Učitaj sadržaj iz .txt')),
            const SizedBox(width: 12),
            Expanded(child: Text(_isEdit ? 'Ostavite prazno da se zadrži postojeći sadržaj.' : 'Sadržaj je obavezan.')),
          ],
        ),
        dialogField(_content, 'Sadržaj knjige', maxLines: 6, validator: _validateContent),
      ],
      onSave: () => submit(() async {
        final content = _content.text.trim();
        final body = {
          'title': _title.text.trim(),
          'description': _description.text.trim(),
          'publishedYear': int.parse(_year.text.trim()),
          'accessType': _isPremium ? 'Premium' : 'Free',
          'authorId': _authorId,
          'genreIds': _genreIds.toList(),
          'isActive': _isActive,
          'content': content.isEmpty ? null : _content.text,
        };

        final result = _isEdit
            ? await api.put('/api/Books/${widget.book!.id}', body: body)
            : await api.post('/api/Books', body: body);

        if (_coverPath != null) {
          final id = (result as Map<String, dynamic>)['id'] as int;
          await api.multipart('PUT', '/api/Books/$id/cover', files: [await imageMultipart('file', _coverPath!)]);
        }
      }, 'Knjiga je sačuvana.'),
    );
  }
}
