import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../core/api_exception.dart';
import '../core/auth_provider.dart';
import '../core/navigation.dart';
import '../core/unread_provider.dart';
import '../models/models.dart';
import '../widgets/paged_list.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  int _reloadToken = 0;
  int _lastUnread = 0;

  Future<void> _markAllRead() async {
    final unread = context.read<UnreadProvider>();

    try {
      await context.read<AuthProvider>().api.post('/api/Notifications/read-all');
      await unread.refresh();
      setState(() => _reloadToken++);
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  Future<void> _markRead(AppNotification notification) async {
    if (notification.isRead) {
      return;
    }

    final unread = context.read<UnreadProvider>();

    try {
      await context.read<AuthProvider>().api.post('/api/Notifications/${notification.id}/read');
      await unread.refresh();
      setState(() => _reloadToken++);
    } on ApiException catch (e) {
      showMessage(e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    final unread = context.watch<UnreadProvider>().count;

    // Novi broj neprocitanih sa servera znaci da su stigle nove obavijesti pa se lista osvjezava.
    if (unread > _lastUnread) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          setState(() => _reloadToken++);
        }
      });
    }

    _lastUnread = unread;

    final format = DateFormat('dd.MM.yyyy. HH:mm');

    return Column(
      children: [
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: unread == 0 ? null : _markAllRead,
            icon: const Icon(Icons.done_all),
            label: const Text('Označi sve kao pročitano'),
          ),
        ),
        Expanded(
          child: PagedList<AppNotification>(
            path: '/api/Notifications',
            reloadToken: _reloadToken,
            fromJson: AppNotification.fromJson,
            emptyText: 'Nemate obavijesti.',
            itemBuilder: (context, item, reload) => ListTile(
              leading: Icon(
                item.isRead ? Icons.notifications_none : Icons.notifications_active,
                color: item.isRead ? null : Theme.of(context).colorScheme.primary,
              ),
              title: Text(item.title, style: TextStyle(fontWeight: item.isRead ? FontWeight.normal : FontWeight.bold)),
              subtitle: Text('${item.message}\n${format.format(item.createdAt)}'),
              isThreeLine: true,
              onTap: () => _markRead(item),
            ),
          ),
        ),
      ],
    );
  }
}
