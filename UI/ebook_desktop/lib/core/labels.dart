import 'package:intl/intl.dart';

final DateFormat dateFormat = DateFormat('dd.MM.yyyy.');
final DateFormat dateTimeFormat = DateFormat('dd.MM.yyyy. HH:mm');

String formatDate(DateTime? value) => value == null ? '-' : dateFormat.format(value);

String subscriptionStatusLabel(String status) {
  switch (status) {
    case 'Pending':
      return 'Na čekanju';
    case 'Active':
      return 'Aktivna';
    case 'Cancelled':
      return 'Otkazana';
    case 'Expired':
      return 'Istekla';
    default:
      return status;
  }
}

String paymentStatusLabel(String? status) {
  switch (status) {
    case 'Pending':
      return 'Na čekanju';
    case 'Completed':
      return 'Plaćeno';
    case 'Failed':
      return 'Neuspjelo';
    case 'Refunded':
      return 'Vraćeno';
    default:
      return '-';
  }
}
