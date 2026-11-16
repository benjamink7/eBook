import 'package:ebook_desktop/core/validators.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('naziv zanra mora imati izmedju 2 i 100 znakova', () {
    expect(Validators.text('Roman', 'Naziv', max: 100), isNull);
    expect(Validators.text('R', 'Naziv', max: 100), isNotNull);
  });
}
