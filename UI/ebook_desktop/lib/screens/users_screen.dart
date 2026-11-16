import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../models/models.dart';
import '../widgets/paged_table.dart';

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key});

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  final _tableKey = GlobalKey<PagedTableState<AdminUser>>();
  bool? _isActive;

  Future<void> _toggle(AdminUser user) async {
    final activate = !user.isActive;
    final confirmed = await confirmAction(
      context,
      title: activate ? 'Aktivacija korisnika' : 'Deaktivacija korisnika',
      message: activate
          ? 'Da li želite aktivirati korisnika "${user.username}"?'
          : 'Da li želite deaktivirati korisnika "${user.username}"? Korisnik će biti odjavljen i neće se moći prijaviti.',
    );

    if (!confirmed || !mounted) {
      return;
    }

    try {
      await context.read<AuthProvider>().api.post('/api/Users/${user.id}/${activate ? 'activate' : 'deactivate'}');
      showMessage(activate ? 'Korisnik je aktiviran.' : 'Korisnik je deaktiviran.');
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
        Text('Korisnici', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Expanded(
          child: PagedTable<AdminUser>(
            key: _tableKey,
            path: '/api/Users',
            query: {'isActive': _isActive},
            fromJson: AdminUser.fromJson,
            searchHint: 'Pretraga po imenu, korisničkom imenu ili e-mailu',
            actions: [
              SizedBox(
                width: 200,
                child: DropdownButtonFormField<bool?>(
                  initialValue: _isActive,
                  decoration: const InputDecoration(labelText: 'Status', isDense: true, border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem<bool?>(value: null, child: Text('Svi')),
                    DropdownMenuItem<bool?>(value: true, child: Text('Aktivni')),
                    DropdownMenuItem<bool?>(value: false, child: Text('Neaktivni')),
                  ],
                  onChanged: (value) => setState(() => _isActive = value),
                ),
              ),
            ],
            columns: const [
              DataColumn(label: Text('Ime i prezime')),
              DataColumn(label: Text('Korisničko ime')),
              DataColumn(label: Text('E-mail')),
              DataColumn(label: Text('Grad')),
              DataColumn(label: Text('Uloga')),
              DataColumn(label: Text('Premium')),
              DataColumn(label: Text('Aktivan')),
              DataColumn(label: Text('Akcije')),
            ],
            cells: (user) => [
              DataCell(Text(user.fullName)),
              DataCell(Text(user.username)),
              DataCell(Text(user.email)),
              DataCell(Text('${user.cityName}, ${user.countryName}')),
              DataCell(Text(user.roles.join(', '))),
              DataCell(Icon(user.hasActiveSubscription ? Icons.workspace_premium : Icons.remove)),
              DataCell(Icon(user.isActive ? Icons.check_circle : Icons.cancel_outlined)),
              DataCell(TextButton(
                onPressed: () => _toggle(user),
                child: Text(user.isActive ? 'Deaktiviraj' : 'Aktiviraj'),
              )),
            ],
          ),
        ),
      ],
    );
  }
}
