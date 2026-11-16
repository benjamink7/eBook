import 'package:ebook_mobile/core/validators.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('lozinka zahtijeva veliko slovo, malo slovo i broj', () {
    expect(Validators.password('Test1234'), isNull);
    expect(Validators.password('test1234'), isNotNull);
  });

  test('kod za reset mora imati 6 cifara', () {
    expect(Validators.code('123456'), isNull);
    expect(Validators.code('12345'), isNotNull);
  });
}
