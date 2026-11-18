import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:printing/printing.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/labels.dart';
import '../core/navigation.dart';
import '../models/models.dart';
import '../reports/report_pdf.dart';

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  late DateTime _from = DateTime(DateTime.now().year, DateTime.now().month, 1);
  DateTime _to = DateTime.now();
  List<Genre> _genres = const [];
  int? _genreId;
  final _top = TextEditingController(text: '20');
  final _topKey = GlobalKey<FormFieldState<String>>();
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _loadGenres();
  }

  @override
  void dispose() {
    _top.dispose();
    super.dispose();
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

  Future<void> _pickDate({required bool isFrom}) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: isFrom ? _from : _to,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );

    if (picked != null) {
      setState(() => isFrom ? _from = picked : _to = picked);
    }
  }

  Map<String, dynamic> get _period => {
        'from': DateTime(_from.year, _from.month, _from.day).toIso8601String(),
        'to': DateTime(_to.year, _to.month, _to.day, 23, 59, 59).toIso8601String(),
      };

  bool get _periodValid => !_to.isBefore(_from);

  Future<void> _generate(String name, Future<List<int>> Function(AuthProvider auth) build) async {
    if (!_periodValid) {
      showMessage('Datum završetka ne može biti prije datuma početka.');
      return;
    }

    setState(() => _busy = true);

    try {
      final bytes = await build(context.read<AuthProvider>());

      if (!mounted) {
        return;
      }

      await Printing.layoutPdf(name: name, onLayout: (_) async => Uint8List.fromList(bytes));
    } on ApiException catch (e) {
      showMessage(e.message);
    } catch (_) {
      showMessage('Izvještaj nije moguće generisati.');
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  Future<void> _revenue() => _generate('izvjestaj-prihodi', (auth) async {
        final json = await auth.api.get('/api/Reports/revenue', query: _period) as Map<String, dynamic>;

        return buildRevenuePdf(json);
      });

  Future<void> _popular() async {
    if (!(_topKey.currentState?.validate() ?? false)) {
      return;
    }

    final top = int.parse(_top.text.trim());
    final genreName = _genreId == null ? 'Svi' : _genres.firstWhere((g) => g.id == _genreId).name;

    await _generate('izvjestaj-najcitanije', (auth) async {
      final json = await auth.api.get(
        '/api/Reports/popular-books',
        query: {..._period, 'genreId': _genreId, 'top': top},
      ) as Map<String, dynamic>;

      return buildPopularBooksPdf(json, genreName: genreName, top: top);
    });
  }

  String? _validateTop(String? value) {
    final top = int.tryParse((value ?? '').trim());

    return top == null || top < 1 || top > 100 ? 'Broj knjiga mora biti između 1 i 100.' : null;
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Izvještaji', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Wrap(
          spacing: 12,
          runSpacing: 12,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            OutlinedButton.icon(
              onPressed: () => _pickDate(isFrom: true),
              icon: const Icon(Icons.event),
              label: Text('Od: ${formatDate(_from)}'),
            ),
            OutlinedButton.icon(
              onPressed: () => _pickDate(isFrom: false),
              icon: const Icon(Icons.event),
              label: Text('Do: ${formatDate(_to)}'),
            ),
            if (!_periodValid)
              Text('Datum završetka ne može biti prije datuma početka.', style: TextStyle(color: Theme.of(context).colorScheme.error)),
          ],
        ),
        const SizedBox(height: 24),
        _ReportCard(
          title: 'Izvještaj o prihodima',
          description: 'Sve uplate u odabranom periodu s ukupnim iznosima po valuti.',
          busy: _busy,
          onGenerate: _revenue,
          children: const [],
        ),
        const SizedBox(height: 16),
        _ReportCard(
          title: 'Izvještaj o najčitanijim knjigama',
          description: 'Rang lista knjiga po broju čitalaca u odabranom periodu.',
          busy: _busy,
          onGenerate: _popular,
          children: [
            SizedBox(
              width: 200,
              child: DropdownButtonFormField<int?>(
                initialValue: _genreId,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Žanr', isDense: true, border: OutlineInputBorder()),
                items: [
                  const DropdownMenuItem<int?>(value: null, child: Text('Svi žanrovi')),
                  for (final genre in _genres) DropdownMenuItem<int?>(value: genre.id, child: Text(genre.name)),
                ],
                onChanged: (value) => _genreId = value,
              ),
            ),
            SizedBox(
              width: 160,
              child: TextFormField(
                key: _topKey,
                controller: _top,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Broj knjiga', isDense: true, border: OutlineInputBorder()),
                validator: _validateTop,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _ReportCard extends StatelessWidget {
  const _ReportCard({
    required this.title,
    required this.description,
    required this.busy,
    required this.onGenerate,
    required this.children,
  });

  final String title;
  final String description;
  final bool busy;
  final VoidCallback onGenerate;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text(description),
            const SizedBox(height: 16),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                ...children,
                FilledButton.icon(
                  onPressed: busy ? null : onGenerate,
                  icon: const Icon(Icons.picture_as_pdf_outlined),
                  label: const Text('Generiši PDF'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
