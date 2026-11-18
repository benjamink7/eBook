import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

const int maxImageBytes = 2 * 1024 * 1024;

Future<String?> pickFilePath({required List<String> extensions}) async {
  final files = await FilePicker.pickFiles(type: FileType.custom, allowedExtensions: extensions);

  return files.isEmpty ? null : files.first.path;
}

String fileNameOf(String path) => path.split(RegExp(r'[\\/]')).last;

// Vraca poruku greske ako slika nije prihvatljiva, inace null.
String? validateImageFile(String path) {
  if (File(path).lengthSync() > maxImageBytes) {
    return 'Slika može imati najviše 2 MB.';
  }

  return null;
}

Future<http.MultipartFile> imageMultipart(String field, String path) async {
  final isPng = path.toLowerCase().endsWith('.png');

  return http.MultipartFile.fromBytes(
    field,
    await File(path).readAsBytes(),
    filename: fileNameOf(path),
    contentType: MediaType('image', isPng ? 'png' : 'jpeg'),
  );
}
