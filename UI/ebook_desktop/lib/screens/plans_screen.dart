import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/validators.dart';
import '../models/models.dart';
import '../widgets/paged_table.dart';
import '../widgets/save_dialog.dart';

class PlansScreen extends StatefulWidget {
  const PlansScreen({super.key});

  @override
  State<PlansScreen> createState() => _PlansScreenState();
}

class _PlansScreenState extends State<PlansScreen> {
  final _tableKey = GlobalKey<PagedTableState<Plan>>();

  Future<void> _edit([Plan? plan]) async {
    final saved = await showDialog<bool>(context: context, builder: (_) => _PlanDialog(plan: plan));

    if (saved == true) {
      _tableKey.currentState?.reload();
    }
  }

  Future<void> _delete(Plan plan) async {
    final confirmed = await confirmAction(
      context,
      title: 'Brisanje plana',
      message: 'Da li ste sigurni da želite obrisati plan "${plan.name}"?',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.delete('/api/SubscriptionPlans/${plan.id}');
      showMessage('Plan je obrisan.');
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
        Text('Planovi pretplate', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<Plan>(
            key: _tableKey,
            path: '/api/SubscriptionPlans',
            fromJson: Plan.fromJson,
            searchHint: 'Pretraga po nazivu',
            actions: [FilledButton.icon(onPressed: _edit, icon: const Icon(Icons.add), label: const Text('Dodaj plan'))],
            columns: const [
              DataColumn(label: Text('Naziv')),
              DataColumn(label: Text('Cijena'), numeric: true),
              DataColumn(label: Text('Trajanje (mjeseci)'), numeric: true),
              DataColumn(label: Text('Pretplata'), numeric: true),
              DataColumn(label: Text('Aktivan')),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (plan) => [
              DataCell(Text(plan.name)),
              DataCell(Text(formatMoney(plan.price, plan.currency))),
              DataCell(Text('${plan.durationInMonths}')),
              DataCell(Text('${plan.subscriptionCount}')),
              DataCell(Icon(plan.isActive ? Icons.check_circle : Icons.cancel_outlined)),
              DataCell(Row(
                children: [
                  IconButton(icon: const Icon(Icons.edit_outlined), tooltip: 'Uredi', onPressed: () => _edit(plan)),
                  IconButton(icon: const Icon(Icons.delete_outline), tooltip: 'Obriši', onPressed: () => _delete(plan)),
                ],
              )),
            ],
          ),
        ),
      ],
    );
  }
}

class _PlanDialog extends StatefulWidget {
  const _PlanDialog({this.plan});

  final Plan? plan;

  @override
  State<_PlanDialog> createState() => _PlanDialogState();
}

class _PlanDialogState extends State<_PlanDialog> with SaveDialogMixin {
  late final TextEditingController _name = TextEditingController(text: widget.plan?.name);
  late final TextEditingController _description = TextEditingController(text: widget.plan?.description);
  late final TextEditingController _price = TextEditingController(text: widget.plan?.price.toStringAsFixed(2));
  late final TextEditingController _currency = TextEditingController(text: widget.plan?.currency ?? 'EUR');
  late final TextEditingController _months = TextEditingController(text: widget.plan?.durationInMonths.toString());
  late bool _isActive = widget.plan?.isActive ?? true;

  @override
  void dispose() {
    for (final c in [_name, _description, _price, _currency, _months]) {
      c.dispose();
    }
    super.dispose();
  }

  String? _validatePrice(String? value) {
    final price = double.tryParse((value ?? '').trim().replaceAll(',', '.'));

    if (price == null) {
      return 'Unesite cijenu kao broj, npr. 4.99.';
    }

    return price < 0.5 || price > 10000 ? 'Cijena mora biti između 0.50 i 10000.' : null;
  }

  String? _validateMonths(String? value) {
    final months = int.tryParse((value ?? '').trim());

    if (months == null) {
      return 'Unesite trajanje kao cijeli broj mjeseci.';
    }

    return months < 1 || months > 60 ? 'Trajanje mora biti između 1 i 60 mjeseci.' : null;
  }

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return buildDialog(
      title: widget.plan == null ? 'Novi plan' : 'Uređivanje plana',
      children: [
        dialogField(_name, 'Naziv', autofocus: true, validator: (v) => Validators.text(v, 'Naziv plana', max: 100)),
        dialogField(_description, 'Opis (opcionalno)', maxLines: 2, validator: (v) => Validators.optionalText(v, 'Opis', max: 500)),
        dialogField(_price, 'Cijena', type: TextInputType.number, validator: _validatePrice),
        dialogField(_currency, 'Valuta (npr. EUR)', validator: (v) {
          return RegExp(r'^[a-zA-Z]{3}$').hasMatch((v ?? '').trim()) ? null : 'Valuta mora imati 3 slova, npr. EUR.';
        }),
        dialogField(_months, 'Trajanje (mjeseci)', type: TextInputType.number, validator: _validateMonths),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Aktivan'),
          value: _isActive,
          onChanged: (value) => setState(() => _isActive = value),
        ),
      ],
      onSave: () => submit(() async {
        final description = _description.text.trim();
        final body = {
          'name': _name.text.trim(),
          'description': description.isEmpty ? null : description,
          'price': double.parse(_price.text.trim().replaceAll(',', '.')),
          'currency': _currency.text.trim(),
          'durationInMonths': int.parse(_months.text.trim()),
          'isActive': _isActive,
        };

        if (widget.plan == null) {
          await api.post('/api/SubscriptionPlans', body: body);
        } else {
          await api.put('/api/SubscriptionPlans/${widget.plan!.id}', body: body);
        }
      }, 'Plan je sačuvan.'),
    );
  }
}
