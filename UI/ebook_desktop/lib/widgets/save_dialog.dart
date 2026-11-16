import 'package:flutter/material.dart';

import '../core/api_exception.dart';
import '../core/navigation.dart';

// Zajednicka logika dijaloga za unos: validacija, prikaz greske servera i zatvaranje nakon uspjeha.
mixin SaveDialogMixin<T extends StatefulWidget> on State<T> {
  final formKey = GlobalKey<FormState>();
  bool busy = false;
  String? error;

  Future<void> submit(Future<void> Function() action, String successMessage) async {
    if (!formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      busy = true;
      error = null;
    });

    try {
      await action();

      if (mounted) {
        Navigator.pop(context, true);
        showMessage(successMessage);
      }
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => error = e.message);
      }
    } finally {
      if (mounted) {
        setState(() => busy = false);
      }
    }
  }

  Widget buildDialog({
    required String title,
    required List<Widget> children,
    required VoidCallback onSave,
    double width = 440,
  }) {
    return AlertDialog(
      title: Text(title),
      content: SizedBox(
        width: width,
        child: Form(
          key: formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                ...children,
                if (error != null) ...[
                  const SizedBox(height: 12),
                  Text(error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                ],
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: busy ? null : () => Navigator.pop(context), child: const Text('Odustani')),
        FilledButton(onPressed: busy ? null : onSave, child: const Text('Sačuvaj')),
      ],
    );
  }
}

Widget dialogField(TextEditingController controller, String label,
    {String? Function(String?)? validator, int maxLines = 1, TextInputType? type, bool autofocus = false}) {
  return Padding(
    padding: const EdgeInsets.only(top: 12),
    child: TextFormField(
      controller: controller,
      maxLines: maxLines,
      keyboardType: type,
      autofocus: autofocus,
      decoration: InputDecoration(labelText: label, border: const OutlineInputBorder()),
      validator: validator,
    ),
  );
}
