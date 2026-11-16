import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../models/models.dart';

class ReaderScreen extends StatefulWidget {
  const ReaderScreen({super.key, required this.bookId, required this.title});

  final int bookId;
  final String title;

  @override
  State<ReaderScreen> createState() => _ReaderScreenState();
}

class _ReaderScreenState extends State<ReaderScreen> {
  BookPage? _page;
  int _pageNumber = 1;
  double _fontSize = 18;
  bool _dark = false;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _start();
  }

  Future<void> _start() async {
    final api = context.read<AuthProvider>().api;

    try {
      final prefs = await SharedPreferences.getInstance();
      _fontSize = prefs.getDouble('reader_font_size') ?? 18;
      _dark = prefs.getBool('reader_dark') ?? false;

      final progress = await api.get('/api/ReadingProgress/${widget.bookId}');

      if (progress is Map<String, dynamic>) {
        _pageNumber = progress['isFinished'] == true ? 1 : progress['currentPage'] as int;
      }

      await _loadPage(_pageNumber, save: false);
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _loading = false;
        });
      }
    }
  }

  Future<void> _loadPage(int number, {bool save = true}) async {
    final api = context.read<AuthProvider>().api;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final json = await api.get('/api/Books/${widget.bookId}/pages/$number');
      final page = BookPage.fromJson(json as Map<String, dynamic>);

      if (save) {
        await api.put('/api/ReadingProgress', body: {'bookId': widget.bookId, 'currentPage': page.pageNumber});
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _page = page;
        _pageNumber = page.pageNumber;
      });
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _savePreferences() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setDouble('reader_font_size', _fontSize);
    await prefs.setBool('reader_dark', _dark);
  }

  Future<void> _goTo(int number) async {
    final total = _page?.totalPages ?? 1;

    if (number < 1 || number > total) {
      return;
    }

    await _loadPage(number);
  }

  @override
  Widget build(BuildContext context) {
    final background = _dark ? const Color(0xFF121212) : const Color(0xFFFFFBF0);
    final foreground = _dark ? const Color(0xFFE0E0E0) : const Color(0xFF212121);
    final page = _page;

    return Scaffold(
      backgroundColor: background,
      appBar: AppBar(
        title: Text(widget.title, overflow: TextOverflow.ellipsis),
        actions: [
          IconButton(
            tooltip: _dark ? 'Svijetla tema' : 'Tamna tema',
            icon: Icon(_dark ? Icons.light_mode : Icons.dark_mode),
            onPressed: () {
              setState(() => _dark = !_dark);
              _savePreferences();
            },
          ),
        ],
      ),
      body: _error != null && page == null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(_error!, style: TextStyle(color: foreground), textAlign: TextAlign.center),
              ),
            )
          : Column(
              children: [
                if (_loading) const LinearProgressIndicator(),
                Expanded(
                  child: page == null
                      ? const SizedBox.shrink()
                      : SingleChildScrollView(
                          key: ValueKey(page.pageNumber),
                          padding: const EdgeInsets.all(20),
                          child: Text(
                            page.content,
                            style: TextStyle(fontSize: _fontSize, height: 1.5, color: foreground),
                          ),
                        ),
                ),
                Container(
                  color: Theme.of(context).colorScheme.surfaceContainer,
                  padding: const EdgeInsets.symmetric(horizontal: 8),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.text_fields, size: 18),
                          Expanded(
                            child: Slider(
                              min: 14,
                              max: 28,
                              divisions: 7,
                              value: _fontSize,
                              label: _fontSize.round().toString(),
                              onChanged: (value) => setState(() => _fontSize = value),
                              onChangeEnd: (_) => _savePreferences(),
                            ),
                          ),
                        ],
                      ),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          IconButton(
                            onPressed: _loading || _pageNumber <= 1 ? null : () => _goTo(_pageNumber - 1),
                            icon: const Icon(Icons.chevron_left),
                          ),
                          Text('Stranica $_pageNumber / ${page?.totalPages ?? '-'}'),
                          IconButton(
                            onPressed: _loading || page == null || _pageNumber >= page.totalPages
                                ? null
                                : () => _goTo(_pageNumber + 1),
                            icon: const Icon(Icons.chevron_right),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
    );
  }
}
