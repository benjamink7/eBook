import 'package:flutter/material.dart';

final GlobalKey<ScaffoldMessengerState> messengerKey = GlobalKey<ScaffoldMessengerState>();

void showMessage(String message) {
  messengerKey.currentState
    ?..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));
}

Future<bool> confirmAction(BuildContext context, {required String title, required String message}) async {
  final result = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Odustani')),
        FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Potvrdi')),
      ],
    ),
  );

  return result ?? false;
}

String formatMoney(num amount, String currency) => '${amount.toStringAsFixed(2)} $currency';
