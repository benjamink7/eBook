import 'dart:io';
import 'dart:typed_data';

import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import '../core/labels.dart';
import '../core/navigation.dart';

Future<pw.ThemeData> _theme() async {
  final fonts = '${Platform.environment['WINDIR'] ?? r'C:\Windows'}\\Fonts';
  final regular = pw.Font.ttf(ByteData.sublistView(await File('$fonts\\arial.ttf').readAsBytes()));
  final bold = pw.Font.ttf(ByteData.sublistView(await File('$fonts\\arialbd.ttf').readAsBytes()));

  return pw.ThemeData.withFont(base: regular, bold: bold);
}

pw.Widget _header(String title, List<String> filters) => pw.Column(
      crossAxisAlignment: pw.CrossAxisAlignment.start,
      children: [
        pw.Text('eBook', style: pw.TextStyle(fontSize: 11, color: PdfColors.grey700)),
        pw.Text(title, style: pw.TextStyle(fontSize: 20, fontWeight: pw.FontWeight.bold)),
        pw.SizedBox(height: 4),
        for (final filter in filters) pw.Text(filter, style: const pw.TextStyle(fontSize: 9)),
        pw.Text('Generisano: ${dateTimeFormat.format(DateTime.now())}', style: const pw.TextStyle(fontSize: 9)),
        pw.SizedBox(height: 12),
      ],
    );

pw.Widget _footer(pw.Context context) => pw.Align(
      alignment: pw.Alignment.centerRight,
      child: pw.Text('Stranica ${context.pageNumber} od ${context.pagesCount}', style: const pw.TextStyle(fontSize: 9)),
    );

Future<Uint8List> buildRevenuePdf(Map<String, dynamic> report) async {
  final rows = (report['rows'] as List).cast<Map<String, dynamic>>();
  final totals = (report['totals'] as List).cast<Map<String, dynamic>>();
  final from = DateTime.parse(report['from'] as String).toLocal();
  final to = DateTime.parse(report['to'] as String).toLocal();
  final doc = pw.Document(theme: await _theme());

  doc.addPage(pw.MultiPage(
    pageFormat: PdfPageFormat.a4,
    margin: const pw.EdgeInsets.all(32),
    header: (_) => _header('Izvještaj o prihodima', ['Period: ${formatDate(from)} - ${formatDate(to)}']),
    footer: _footer,
    build: (_) => [
      if (rows.isEmpty)
        pw.Text('Nema uplata u odabranom periodu.')
      else
        pw.TableHelper.fromTextArray(
          headers: ['Datum', 'Korisnik', 'Plan', 'Iznos', 'Status', 'Vraćeno'],
          data: [
            for (final r in rows)
              [
                dateTimeFormat.format(DateTime.parse(r['paidAt'] as String).toLocal()),
                r['username'],
                r['planName'],
                formatMoney(r['amount'] as num, r['currency'] as String),
                paymentStatusLabel(r['status'] as String?),
                formatMoney(r['refundedAmount'] as num, r['currency'] as String),
              ],
          ],
          headerStyle: pw.TextStyle(fontWeight: pw.FontWeight.bold, fontSize: 9),
          cellStyle: const pw.TextStyle(fontSize: 9),
          headerDecoration: const pw.BoxDecoration(color: PdfColors.grey300),
          cellAlignments: {3: pw.Alignment.centerRight, 5: pw.Alignment.centerRight},
        ),
      pw.SizedBox(height: 16),
      pw.Text('Ukupno', style: pw.TextStyle(fontSize: 13, fontWeight: pw.FontWeight.bold)),
      pw.SizedBox(height: 4),
      if (totals.isEmpty)
        pw.Text('0.00')
      else
        for (final t in totals)
          pw.Text(
            '${t['currency']}: ${t['count']} uplata, naplaćeno ${formatMoney(t['paid'] as num, t['currency'] as String)}, '
            'vraćeno ${formatMoney(t['refunded'] as num, t['currency'] as String)}, '
            'neto ${formatMoney(t['net'] as num, t['currency'] as String)}',
          ),
    ],
  ));

  return doc.save();
}

Future<Uint8List> buildPopularBooksPdf(Map<String, dynamic> report, {required String genreName, required int top}) async {
  final rows = (report['rows'] as List).cast<Map<String, dynamic>>();
  final from = DateTime.parse(report['from'] as String).toLocal();
  final to = DateTime.parse(report['to'] as String).toLocal();
  final doc = pw.Document(theme: await _theme());

  doc.addPage(pw.MultiPage(
    pageFormat: PdfPageFormat.a4,
    margin: const pw.EdgeInsets.all(32),
    header: (_) => _header('Izvještaj o najčitanijim knjigama', [
      'Period: ${formatDate(from)} - ${formatDate(to)}',
      'Žanr: $genreName, broj knjiga: $top',
    ]),
    footer: _footer,
    build: (_) => [
      if (rows.isEmpty)
        pw.Text('Nema podataka o čitanju u odabranom periodu.')
      else
        pw.TableHelper.fromTextArray(
          headers: ['#', 'Naslov', 'Autor', 'Pristup', 'Čitaoci', 'Završilo', 'Recenzije', 'Ocjena', 'Favoriti'],
          data: [
            for (var i = 0; i < rows.length; i++)
              [
                '${i + 1}',
                rows[i]['title'],
                rows[i]['authorName'],
                rows[i]['accessType'] == 'Premium' ? 'Premium' : 'Besplatna',
                '${rows[i]['readers']}',
                '${rows[i]['finishedCount']}',
                '${rows[i]['reviewCount']}',
                (rows[i]['averageRating'] as num?)?.toStringAsFixed(1) ?? '-',
                '${rows[i]['favoritesCount']}',
              ],
          ],
          headerStyle: pw.TextStyle(fontWeight: pw.FontWeight.bold, fontSize: 9),
          cellStyle: const pw.TextStyle(fontSize: 9),
          headerDecoration: const pw.BoxDecoration(color: PdfColors.grey300),
          cellAlignments: {for (final i in [0, 4, 5, 6, 7, 8]) i: pw.Alignment.centerRight},
        ),
    ],
  ));

  return doc.save();
}
