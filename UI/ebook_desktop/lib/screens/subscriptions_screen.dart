import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/labels.dart';
import '../core/navigation.dart';
import '../models/models.dart';
import '../widgets/paged_table.dart';
import '../widgets/reason_dialog.dart';

class SubscriptionsScreen extends StatefulWidget {
  const SubscriptionsScreen({super.key});

  @override
  State<SubscriptionsScreen> createState() => _SubscriptionsScreenState();
}

class _SubscriptionsScreenState extends State<SubscriptionsScreen> {
  final _tableKey = GlobalKey<PagedTableState<AdminSubscription>>();
  String? _status;

  Future<void> _refund(AdminSubscription subscription) async {
    final reason = await askReason(context, title: 'Povrat novca za korisnika ${subscription.username}', confirmLabel: 'Vrati novac');

    if (reason == null || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.post('/api/Subscriptions/${subscription.id}/refund', body: {'reason': reason});
      showMessage('Povrat novca je izvršen.');
      _tableKey.currentState?.reload();
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  Future<void> _showHistory(AdminSubscription subscription) async {
    final api = context.read<AuthProvider>().api;

    try {
      final json = await api.get('/api/Subscriptions/${subscription.id}/history') as List;
      final entries = json.map((e) => StatusHistoryEntry.fromJson(e as Map<String, dynamic>)).toList();

      if (!mounted) {
        return;
      }

      await showDialog<void>(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Historija statusa • ${subscription.username}'),
          content: SizedBox(
            width: 560,
            child: SingleChildScrollView(
              child: DataTable(
                columns: const [
                  DataColumn(label: Text('Vrijeme')),
                  DataColumn(label: Text('Promjena')),
                  DataColumn(label: Text('Izvršio')),
                  DataColumn(label: Text('Napomena')),
                ],
                rows: [
                  for (final entry in entries)
                    DataRow(cells: [
                      DataCell(Text(dateTimeFormat.format(entry.changedAt))),
                      DataCell(Text(
                          '${entry.fromStatus == null ? '-' : subscriptionStatusLabel(entry.fromStatus!)} → ${subscriptionStatusLabel(entry.toStatus)}')),
                      DataCell(Text(entry.changedBy)),
                      DataCell(Text(entry.note ?? '')),
                    ]),
                ],
              ),
            ),
          ),
          actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Zatvori'))],
        ),
      );
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Pretplate', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<AdminSubscription>(
            key: _tableKey,
            path: '/api/Subscriptions',
            query: {'status': _status},
            fromJson: AdminSubscription.fromJson,
            searchHint: 'Pretraga po korisniku ili planu',
            actions: [
              SizedBox(
                width: 200,
                child: DropdownButtonFormField<String?>(
                  initialValue: _status,
                  decoration: const InputDecoration(labelText: 'Status', isDense: true, border: OutlineInputBorder()),
                  items: [
                    const DropdownMenuItem<String?>(value: null, child: Text('Svi')),
                    for (final status in ['Pending', 'Active', 'Cancelled', 'Expired'])
                      DropdownMenuItem<String?>(value: status, child: Text(subscriptionStatusLabel(status))),
                  ],
                  onChanged: (value) => setState(() => _status = value),
                ),
              ),
            ],
            columns: const [
              DataColumn(label: Text('Korisnik')),
              DataColumn(label: Text('Plan')),
              DataColumn(label: Text('Status')),
              DataColumn(label: Text('Početak')),
              DataColumn(label: Text('Ističe')),
              DataColumn(label: Text('Iznos'), numeric: true),
              DataColumn(label: Text('Plaćanje')),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (s) => [
              DataCell(Text(s.username)),
              DataCell(Text(s.planName)),
              DataCell(Text(subscriptionStatusLabel(s.status))),
              DataCell(Text(formatDate(s.startDate))),
              DataCell(Text(formatDate(s.endDate))),
              DataCell(Text(s.amount == null ? '-' : formatMoney(s.amount!, (s.currency ?? '').toUpperCase()))),
              DataCell(Text(paymentStatusLabel(s.paymentStatus))),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.history), tooltip: 'Historija statusa', onPressed: () => _showHistory(s)),
                  if (s.paymentStatus == 'Completed')
                    TextButton(onPressed: () => _refund(s), child: const Text('Povrat novca')),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}
