import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart' show FailureCode, SetupPaymentSheetParameters, Stripe, StripeException;
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../models/models.dart';

class SubscriptionScreen extends StatefulWidget {
  const SubscriptionScreen({super.key});

  @override
  State<SubscriptionScreen> createState() => _SubscriptionScreenState();
}

class _SubscriptionScreenState extends State<SubscriptionScreen> {
  List<SubscriptionPlan> _plans = const [];
  Subscription? _current;
  bool _loading = true;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final api = context.read<AuthProvider>().api;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final plans = await api.get('/api/Subscriptions/plans') as List;
      final current = await api.get('/api/Subscriptions/current');

      if (!mounted) {
        return;
      }

      setState(() {
        _plans = plans.map((p) => SubscriptionPlan.fromJson(p as Map<String, dynamic>)).toList();
        _current = current == null ? null : Subscription.fromJson(current as Map<String, dynamic>);
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

  Future<void> _buy(SubscriptionPlan plan) async {
    final api = context.read<AuthProvider>().api;

    setState(() => _busy = true);

    try {
      final checkout = await api.post('/api/Subscriptions/checkout', body: {'planId': plan.id}) as Map<String, dynamic>;

      Stripe.publishableKey = checkout['publishableKey'] as String;
      await Stripe.instance.applySettings();
      await Stripe.instance.initPaymentSheet(
        paymentSheetParameters: SetupPaymentSheetParameters(
          paymentIntentClientSecret: checkout['paymentIntentClientSecret'] as String,
          merchantDisplayName: 'eBook',
        ),
      );
      await Stripe.instance.presentPaymentSheet();

      final confirmed =
          await api.post('/api/Subscriptions/${checkout['subscriptionId']}/confirm') as Map<String, dynamic>;

      showMessage(confirmed['status'] == 'Active'
          ? 'Plaćanje je uspjelo. Premium pretplata je aktivna.'
          : 'Plaćanje se obrađuje. Status će se uskoro ažurirati.');
      await _load();
    } on StripeException catch (e) {
      if (e.error.code != FailureCode.Canceled) {
        showMessage(e.error.localizedMessage ?? 'Plaćanje nije uspjelo.');
      }
    } on ApiException catch (e) {
      showMessage(e.message);
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  Future<void> _cancel(Subscription subscription) async {
    final reason = await showDialog<String>(
      context: context,
      builder: (context) => const _CancelDialog(),
    );

    if (reason == null || !mounted) {
      return;
    }

    setState(() => _busy = true);

    try {
      await context
          .read<AuthProvider>()
          .api
          .post('/api/Subscriptions/${subscription.id}/cancel', body: {'reason': reason});
      showMessage('Pretplata je otkazana.');
      await _load();
    } on ApiException catch (e) {
      showMessage(e.message);
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Pretplata')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!),
                      TextButton(onPressed: _load, child: const Text('Pokušaj ponovo')),
                    ],
                  ),
                )
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    if (_current != null && _current!.isActive) _currentCard(_current!),
                    if (_current == null || !_current!.isActive) ...[
                      Text('Odaberite plan', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 8),
                      for (final plan in _plans) _planCard(plan),
                    ],
                  ],
                ),
    );
  }

  Widget _currentCard(Subscription subscription) {
    final format = DateFormat('dd.MM.yyyy.');

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.workspace_premium, color: Colors.amber.shade800),
                const SizedBox(width: 8),
                Text('Premium je aktivan', style: Theme.of(context).textTheme.titleMedium),
              ],
            ),
            const SizedBox(height: 8),
            Text('Plan: ${subscription.planName}'),
            if (subscription.startDate != null) Text('Početak: ${format.format(subscription.startDate!)}'),
            if (subscription.endDate != null) Text('Ističe: ${format.format(subscription.endDate!)}'),
            const SizedBox(height: 12),
            OutlinedButton(
              onPressed: _busy ? null : () => _cancel(subscription),
              child: const Text('Otkaži pretplatu'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _planCard(SubscriptionPlan plan) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(plan.name, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 4),
            Text('${plan.price.toStringAsFixed(2)} ${plan.currency}', style: Theme.of(context).textTheme.titleMedium),
            if (plan.description != null) ...[
              const SizedBox(height: 4),
              Text(plan.description!),
            ],
            const SizedBox(height: 12),
            FilledButton(
              onPressed: _busy ? null : () => _buy(plan),
              child: _busy
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Kupi'),
            ),
          ],
        ),
      ),
    );
  }
}

class _CancelDialog extends StatefulWidget {
  const _CancelDialog();

  @override
  State<_CancelDialog> createState() => _CancelDialogState();
}

class _CancelDialogState extends State<_CancelDialog> {
  final _formKey = GlobalKey<FormState>();
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Otkazivanje pretplate'),
      content: Form(
        key: _formKey,
        child: TextFormField(
          controller: _reason,
          maxLines: 3,
          decoration: const InputDecoration(labelText: 'Razlog otkazivanja'),
          validator: (value) {
            final text = value?.trim() ?? '';

            if (text.length < 3) {
              return 'Razlog mora imati najmanje 3 znaka.';
            }

            return text.length > 500 ? 'Razlog može imati najviše 500 znakova.' : null;
          },
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Odustani')),
        FilledButton(
          onPressed: () {
            if (_formKey.currentState!.validate()) {
              Navigator.pop(context, _reason.text.trim());
            }
          },
          child: const Text('Otkaži pretplatu'),
        ),
      ],
    );
  }
}
