import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../models/models.dart';

class PagedTable<T> extends StatefulWidget {
  const PagedTable({
    super.key,
    required this.path,
    required this.fromJson,
    required this.columns,
    required this.cells,
    this.query,
    this.searchHint,
    this.actions = const [],
    this.emptyText = 'Nema podataka.',
  });

  final String path;
  final T Function(Map<String, dynamic>) fromJson;
  final List<DataColumn> columns;
  final List<DataCell> Function(T item) cells;
  final Map<String, dynamic>? query;
  final String? searchHint;
  final List<Widget> actions;
  final String emptyText;

  @override
  State<PagedTable<T>> createState() => PagedTableState<T>();
}

class PagedTableState<T> extends State<PagedTable<T>> {
  static const int _pageSize = 15;

  final _search = TextEditingController();
  List<T> _items = const [];
  int _page = 1;
  int _totalPages = 1;
  int _total = 0;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    reload();
  }

  @override
  void didUpdateWidget(PagedTable<T> oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (!mapEquals(oldWidget.query, widget.query)) {
      reload(page: 1);
    }
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> reload({int? page}) async {
    final target = page ?? _page;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final json = await context.read<AuthProvider>().api.get(widget.path, query: {
        ...?widget.query,
        if (widget.searchHint != null) 'search': _search.text.trim(),
        'page': target,
        'pageSize': _pageSize,
      });
      final result = PagedResult.fromJson(json, widget.fromJson);

      // Nakon brisanja zadnjeg zapisa na stranici vraca se na zadnju postojecu stranicu.
      if (result.items.isEmpty && target > 1 && result.totalCount > 0) {
        await reload(page: result.totalPages);
        return;
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _items = result.items;
        _page = result.page;
        _totalPages = result.totalPages;
        _total = result.totalCount;
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

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            if (widget.searchHint != null)
              SizedBox(
                width: 360,
                child: TextField(
                  controller: _search,
                  textInputAction: TextInputAction.search,
                  onSubmitted: (_) => reload(page: 1),
                  decoration: InputDecoration(
                    hintText: widget.searchHint,
                    prefixIcon: const Icon(Icons.search),
                    isDense: true,
                    border: const OutlineInputBorder(),
                    suffixIcon: IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        _search.clear();
                        reload(page: 1);
                      },
                    ),
                  ),
                ),
              ),
            const SizedBox(width: 8),
            if (widget.searchHint != null)
              FilledButton.tonal(onPressed: () => reload(page: 1), child: const Text('Pretraži')),
            const Spacer(),
            ...widget.actions,
          ],
        ),
        const SizedBox(height: 12),
        if (_loading) const LinearProgressIndicator(),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 12),
            child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
          ),
        Expanded(
          child: _items.isEmpty && !_loading
              ? Center(child: Text(widget.emptyText))
              : LayoutBuilder(
                  builder: (context, constraints) => SingleChildScrollView(
                    child: SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: ConstrainedBox(
                        constraints: BoxConstraints(minWidth: constraints.maxWidth),
                        child: DataTable(
                          columns: widget.columns,
                          rows: [for (final item in _items) DataRow(cells: widget.cells(item))],
                        ),
                      ),
                    ),
                  ),
                ),
        ),
        const Divider(height: 1),
        Padding(
          padding: const EdgeInsets.only(top: 8),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('Ukupno: $_total'),
              const SizedBox(width: 16),
              IconButton(
                onPressed: _loading || _page <= 1 ? null : () => reload(page: _page - 1),
                icon: const Icon(Icons.chevron_left),
              ),
              Text('Stranica $_page od $_totalPages'),
              IconButton(
                onPressed: _loading || _page >= _totalPages ? null : () => reload(page: _page + 1),
                icon: const Icon(Icons.chevron_right),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
