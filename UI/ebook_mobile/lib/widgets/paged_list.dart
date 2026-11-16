import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../models/models.dart';

class PagedList<T> extends StatefulWidget {
  const PagedList({
    super.key,
    required this.path,
    required this.fromJson,
    required this.itemBuilder,
    required this.emptyText,
    this.query,
    this.reloadToken = 0,
  });

  final String path;
  final Map<String, dynamic>? query;
  final T Function(Map<String, dynamic>) fromJson;
  final Widget Function(BuildContext context, T item, VoidCallback reload) itemBuilder;
  final String emptyText;
  final int reloadToken;

  @override
  State<PagedList<T>> createState() => _PagedListState<T>();
}

class _PagedListState<T> extends State<PagedList<T>> {
  static const int _pageSize = 20;

  final _scroll = ScrollController();
  final List<T> _items = [];
  int _page = 1;
  int _total = 0;
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
    _load(reset: true);
  }

  @override
  void didUpdateWidget(PagedList<T> oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (oldWidget.reloadToken != widget.reloadToken) {
      _load(reset: true);
    }
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 200 &&
        !_loading &&
        _items.length < _total) {
      _page++;
      _load();
    }
  }

  Future<void> _load({bool reset = false}) async {
    if (reset) {
      _page = 1;
    }

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final json = await context.read<AuthProvider>().api.get(widget.path, query: {
        ...?widget.query,
        'page': _page,
        'pageSize': _pageSize,
      });
      final result = PagedResult.fromJson(json, widget.fromJson);

      if (!mounted) {
        return;
      }

      setState(() {
        if (reset) {
          _items.clear();
        }

        _items.addAll(result.items);
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
    if (_error != null && _items.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!),
            TextButton(onPressed: () => _load(reset: true), child: const Text('Pokušaj ponovo')),
          ],
        ),
      );
    }

    if (_items.isEmpty && !_loading) {
      return RefreshIndicator(
        onRefresh: () => _load(reset: true),
        child: ListView(
          children: [
            const SizedBox(height: 120),
            Center(child: Text(widget.emptyText)),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => _load(reset: true),
      child: ListView.builder(
        controller: _scroll,
        physics: const AlwaysScrollableScrollPhysics(),
        itemCount: _items.length + (_loading ? 1 : 0),
        itemBuilder: (context, index) {
          if (index >= _items.length) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }

          return widget.itemBuilder(context, _items[index], () => _load(reset: true));
        },
      ),
    );
  }
}
