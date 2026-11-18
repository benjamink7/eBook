# eBook – e-biblioteka s Free i Premium pretplatom

Seminarski rad iz predmeta Razvoj softvera II (FIT). Aplikacija omogućava online čitanje knjiga: besplatne knjige su dostupne svima, a Premium knjige korisnicima s aktivnom pretplatom (plaćanje preko Stripe sandboxa).

## Sastav rješenja

| Dio | Tehnologija | Folder |
|---|---|---|
| REST API | .NET 10, EF Core, SQL Server, JWT | `eBook.API`, `eBook.Services`, `eBook.Model` |
| Worker (mikroservis) | .NET 10 Worker, RabbitMQ, SMTP | `eBook.Worker` |
| Desktop (administrator) | Flutter, Windows | `UI/ebook_desktop` |
| Mobilna aplikacija (korisnik) | Flutter, Android | `UI/ebook_mobile` |

Worker obavlja dva posla: šalje e-mailove iz reda `ebook.email` (RabbitMQ) i svake minute obrađuje pretplate (istekle prebacuje u status `Expired`, a korisnicima šalje podsjetnik 3 dana prije isteka).

Dokumentacija preporučivača: [recommender-dokumentacija.md](recommender-dokumentacija.md).

## Pokretanje backenda

Potrebni su Docker Desktop i `.env` fajl u korijenu repozitorija. `.env` nije na GitHubu; nalazi se u arhivi `.env-tajne.zip` (lozinka: `fit`) uz release; GitHub je prikazuje pod nazivom `default.env-tajne.zip`. Raspakujte ga u korijen repozitorija (pored `docker-compose.yml`). Ako arhive nema, kopirajte `.env.example` u `.env` i unesite vlastite vrijednosti.

```powershell
docker compose up -d --build
```

Pokreće se SQL Server, RabbitMQ, API i Worker. Baza se kreira i popunjava početnim podacima pri prvom startu.

- API: http://localhost:5000 (dokumentacija: http://localhost:5000/scalar)
- RabbitMQ konzola: http://localhost:15672

Potpuni reset baze: `docker compose down -v`, pa ponovo `docker compose up -d --build`.

## Stripe (sandbox)

Plaćanje koristi Stripe test mod. Test kartica: `4242 4242 4242 4242`, bilo koji budući datum isteka, bilo koji CVC.

Za lokalno primanje webhooka (potvrda plaćanja i povrat novca) pokrenite Stripe CLI i proslijedite događaje API-ju:

```powershell
docker run --rm -it stripe/stripe-cli listen --api-key <STRIPE_SECRET_KEY> --forward-to http://host.docker.internal:5000/api/stripe/webhook
```

Ispisani `whsec_...` potpis upišite u `STRIPE_WEBHOOK_SECRET` u `.env`, pa ponovo pokrenite API (`docker compose up -d api`).

## Desktop aplikacija (Windows)

```powershell
cd UI\ebook_desktop
flutter pub get
flutter run -d windows
```

Adresa API-ja se može promijeniti s `--dart-define=API_BASE_URL=http://localhost:5000`. U release arhivi je gotova verzija (`ebook_desktop.exe` u folderu `Release`).

## Mobilna aplikacija (Android)

Pokrenite emulator, pa:

```powershell
cd UI\ebook_mobile
flutter pub get
flutter run
```

Emulator API vidi na `http://10.0.2.2:5000` (zadano). Za fizički uređaj: `--dart-define=API_BASE_URL=http://<IP računara>:5000`. U release arhivi je gotov `app-release.apk`.

## Test kredencijali

| Aplikacija | Korisničko ime | Lozinka | Uloga |
|---|---|---|---|
| Desktop | `desktop` | `test` | Administrator |
| Mobilna | `mobile` | `test` | Korisnik |

Na mobilnoj aplikaciji moguća je i registracija novog korisnika. Reset lozinke šalje kod na e-mail korisnika preko Workera.

## Funkcionalnosti

**Desktop (administrator):** statistika na početnoj stranici; CRUD knjiga (s naslovnicom i sadržajem), autora, žanrova, novosti, planova pretplate, država i gradova; pregled korisnika (aktivacija/deaktivacija); pregled pretplata s historijom statusa i povratom novca; dva PDF izvještaja (prihodi po periodu, najčitanije knjige po periodu, žanru i broju knjiga).

**Mobilna (korisnik):** katalog s pretragom i filterima; detalji knjige i recenzije; čitač s pamćenjem stranice; favoriti i historija čitanja; preporuke s objašnjenjem; kupovina Premium pretplate (Stripe PaymentSheet) i otkazivanje; novosti; notifikacije s automatskim osvježavanjem; profil, promjena lozinke i reset lozinke.
