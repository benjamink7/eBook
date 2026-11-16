import 'package:flutter/material.dart';

Future<String?> askReason(BuildContext context, {required String title, required String confirmLabel}) {
  return showDialog<String>(
    context: context,
    builder: (context) => _ReasonDialog(title: title, confirmLabel: confirmLabel),
  );
}

class _ReasonDialog extends StatefulWidget {
  const _ReasonDialog({required this.title, required this.confirmLabel});

  final String title;
  final String confirmLabel;

  @override
  State<_ReasonDialog> createState() => _ReasonDialogState();
}

class _ReasonDialogState extends State<_ReasonDialog> {
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
      title: Text(widget.title),
      content: SizedBox(
        width: 420,
        child: Form(
          key: _formKey,
          child: TextFormField(
            controller: _reason,
            autofocus: true,
            maxLines: 3,
            decoration: const InputDecoration(labelText: 'Razlog', border: OutlineInputBorder()),
            validator: (value) {
              final text = value?.trim() ?? '';

              if (text.length < 3) {
                return 'Razlog mora imati najmanje 3 znaka.';
              }

              return text.length > 500 ? 'Razlog može imati najviše 500 znakova.' : null;
            },
          ),
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
          child: Text(widget.confirmLabel),
        ),
      ],
    );
  }
}
