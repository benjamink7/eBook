import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/labels.dart';
import '../core/navigation.dart';
import '../core/uploads.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/auth_image.dart';
import '../widgets/paged_table.dart';
import '../widgets/save_dialog.dart';

class NewsScreen extends StatefulWidget {
  const NewsScreen({super.key});

  @override
  State<NewsScreen> createState() => _NewsScreenState();
}

class _NewsScreenState extends State<NewsScreen> {
  final _tableKey = GlobalKey<PagedTableState<NewsRow>>();
  int _imageVersion = 0;

  Future<void> _edit([NewsRow? news]) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _NewsDialog(news: news));

    if (saved == true) {
      setState(() => _imageVersion++);
      _tableKey.currentState?.reload();
    }
  }

  Future<void> _delete(NewsRow news) async {
    final confirmed = await confirmAction(
      context,
      title: 'Brisanje novosti',
      message: 'Da li ste sigurni da želite obrisati novost "${news.title}"?',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/News/${news.id}');
      showMessage('Novost je obrisana.');
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
        Text('Novosti', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<NewsRow>(
            key: _tableKey,
            path: '/api/News',
            fromJson: NewsRow.fromJson,
            searchHint: 'Pretraga po naslovu ili tekstu',
            actions: [FilledButton.icon(onPressed: _edit, icon: const Icon(Icons.add), label: const Text('Dodaj novost'))],
            columns: const [
              DataColumn(label: Text('')),
              DataColumn(label: Text('Naslov')),
              DataColumn(label: Text('Tekst')),
              DataColumn(label: Text('Autor')),
              DataColumn(label: Text('Datum')),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (news) => [
              DataCell(Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: AuthImage(url: news.imageUrl, width: 72, height: 44, version: _imageVersion),
              )),
              DataCell(Text(news.title)),
              DataCell(SizedBox(
                width: 360,
                child: Text(news.content, maxLines: 2, overflow: TextOverflow.ellipsis),
              )),
              DataCell(Text(news.createdBy)),
              DataCell(Text(formatDate(news.createdAt))),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.edit_outlined), tooltip: 'Uredi', onPressed: () => _edit(news)),
                  IconButton(icon: const Icon(Icons.delete_outline), tooltip: 'Obriši', onPressed: () => _delete(news)),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}

class _NewsDialog extends StatefulWidget {
  const _NewsDialog({this.news});

  final NewsRow? news;

  @override
  State<_NewsDialog> createState() => _NewsDialogState();
}

class _NewsDialogState extends State<_NewsDialog> with SaveDialogMixin {
  late final TextEditingController _title = TextEditingController(text: widget.news?.title);
  late final TextEditingController _content = TextEditingController(text: widget.news?.content);
  String? _imagePath;
  String? _imageError;

  bool get _isEdit => widget.news != null;

  @override
  void dispose() {
    _title.dispose();
    _content.dispose();
    super.dispose();
  }

  Future<void> _pickImage() async {
    final path = await pickFilePath(extensions: ['png', 'jpg', 'jpeg']);

    if (path == null) {
      return;
    }

    setState(() {
      _imageError = validateImageFile(path);
      _imagePath = _imageError == null ? path : null;
    });
  }

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return buildDialog(
      title: _isEdit ? 'Uređivanje novosti' : 'Nova novost',
      width: 560,
      children: [
        dialogField(_title, 'Naslov', autofocus: true, validator: (v) => Validators.text(v, 'Naslov', min: 3, max: 200)),
        dialogField(_content, 'Tekst', maxLines: 6, validator: (v) => Validators.text(v, 'Tekst', min: 10, max: 4000)),
        const SizedBox(height: 12),
        Row(
          children: [
            OutlinedButton.icon(onPressed: _pickImage, icon: const Icon(Icons.image_outlined), label: const Text('Odaberi sliku')),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                _imageError ??
                    (_imagePath == null
                        ? (_isEdit ? 'Ostavite prazno da se zadrži postojeća slika.' : 'Slika je obavezna (PNG ili JPEG, do 2 MB).')
                        : fileNameOf(_imagePath!)),
                style: _imageError == null ? null : TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ),
          ],
        ),
      ],
      onSave: () => submit(() async {
        if (!_isEdit && _imagePath == null) {
          throw ApiException('Slika novosti je obavezna.');
        }

        await api.multipart(
          _isEdit ? 'PUT' : 'POST',
          _isEdit ? '/api/News/${widget.news!.id}' : '/api/News',
          fields: {'Title': _title.text.trim(), 'Content': _content.text.trim()},
          files: [if (_imagePath != null) await imageMultipart('image', _imagePath!)],
        );
      }, 'Novost je sačuvana.'),
    );
  }
}
