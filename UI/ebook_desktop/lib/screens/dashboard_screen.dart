import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../models/models.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  Statistics? _stats;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final json = await context.read<AuthProvider>().api.get('/api/Statistics') as Map<String, dynamic>;

      if (mounted) {
        setState(() => _stats = Statistics.fromJson(json));
      }
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

  String _money(List<CurrencyAmount> amounts) =>
      amounts.isEmpty ? '0.00' : amounts.map((a) => formatMoney(a.amount, a.currency)).join(' / ');

  @override
  Widget build(BuildContext context) {
    final stats = _stats;

    if (_loading && stats == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (stats == null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [Text(_error ?? ''), TextButton(onPressed: _load, child: const Text('Pokušaj ponovo'))],
        ),
      );
    }

    final theme = Theme.of(context);
    final maxSubscriptions = stats.months.map((m) => m.newSubscriptions).fold<int>(1, (a, b) => a > b ? a : b);

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text('Pregled sistema', style: theme.textTheme.headlineSmall),
              const Spacer(),
              IconButton(onPressed: _load, icon: const Icon(Icons.refresh), tooltip: 'Osvježi'),
            ],
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 16,
            runSpacing: 16,
            children: [
              _StatCard(label: 'Korisnici', value: '${stats.totalUsers}', icon: Icons.people_outline),
              _StatCard(
                  label: 'Aktivne pretplate', value: '${stats.activeSubscriptions}', icon: Icons.workspace_premium),
              _StatCard(
                  label: 'Knjige (Premium)',
                  value: '${stats.totalBooks} (${stats.premiumBooks})',
                  icon: Icons.menu_book_outlined),
              _StatCard(label: 'Promet ovog mjeseca', value: _money(stats.revenueThisMonth), icon: Icons.payments_outlined),
              _StatCard(label: 'Ukupan promet', value: _money(stats.totalRevenue), icon: Icons.account_balance_outlined),
            ],
          ),
          const SizedBox(height: 32),
          Text('Nove pretplate po mjesecima', style: theme.textTheme.titleMedium),
          const SizedBox(height: 12),
          SizedBox(
            height: 230,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                for (final month in stats.months)
                  Expanded(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        Text('${month.newSubscriptions}'),
                        const SizedBox(height: 4),
                        Container(
                          height: 120 * (month.newSubscriptions / maxSubscriptions),
                          margin: const EdgeInsets.symmetric(horizontal: 24),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.primary,
                            borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(month.month),
                        Text(_money(month.revenue), style: theme.textTheme.labelSmall),
                      ],
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 32),
          Text('Najčitanije knjige', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: DataTable(
              columns: const [
                DataColumn(label: Text('Naslov')),
                DataColumn(label: Text('Autor')),
                DataColumn(label: Text('Čitaoci'), numeric: true),
                DataColumn(label: Text('Favoriti'), numeric: true),
                DataColumn(label: Text('Ocjena'), numeric: true),
              ],
              rows: [
                for (final book in stats.topBooks)
                  DataRow(cells: [
                    DataCell(Text(book.title)),
                    DataCell(Text(book.authorName)),
                    DataCell(Text('${book.readers}')),
                    DataCell(Text('${book.favorites}')),
                    DataCell(Text(book.averageRating?.toStringAsFixed(1) ?? '-')),
                  ]),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({required this.label, required this.value, required this.icon});

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return SizedBox(
      width: 230,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Icon(icon, size: 36, color: theme.colorScheme.primary),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(label, style: theme.textTheme.labelMedium),
                    const SizedBox(height: 4),
                    Text(value, style: theme.textTheme.titleLarge),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
