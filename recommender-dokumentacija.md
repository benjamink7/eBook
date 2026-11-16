# Preporučivač knjiga – dokumentacija

Endpoint: `GET /api/Recommendations?count=10` (zahtijeva prijavu). Implementacija: `eBook.Services/Recommendations/RecommendationEngine.cs` (algoritam, bez pristupa bazi) i `eBook.Services/Implementations/RecommendationService.cs` (učitavanje podataka iz baze).

Preporučivač je **hibridni i objašnjiv**: svaka preporuka nosi ukupnu ocjenu, tri komponente ocjene i do tri razloga napisana običnim jezikom.

## 1. Podaci koje koristi

Svi podaci su stvarni, nastaju korištenjem aplikacije:

| Signal | Izvor | Težina |
|---|---|---|
| Knjiga dodana u favorite | `Favorites` | +3 |
| Knjiga pročitana do kraja | `ReadingProgresses.IsFinished` | +3 |
| Knjiga započeta | `ReadingProgresses` | +1 |
| Ocjena 4 ili 5 | `Reviews` | +4 |
| Ocjena 3 | `Reviews` | +1 |
| Ocjena 1 ili 2 | `Reviews` | −3 |
| Knjiga čiji naslov ili autor odgovara nedavnoj pretrazi (zadnjih 20 upita) | `SearchHistories` | +0,5 |

Težine jedne knjige se sabiraju (npr. favorit + ocjena 5 = 7). Težine su konstante u `RecommendationEngine.WeightOf`.

Za kolaborativni dio koriste se i podaci drugih korisnika: knjige koje su visoko ocijenili (4–5), dodali u favorite ili pročitali do kraja.

## 2. Algoritam

Kandidati su sve aktivne knjige s kojima korisnik još nije imao kontakt (favorit, čitanje, recenzija). Pretraga sama ne isključuje knjigu.

Ocjena svakog kandidata je težinski zbir tri komponente, svaka u rasponu 0–1:

```
ocjena = 0,50 · sadržajna + 0,35 · kolaborativna + 0,15 · popularnost
```

### 2.1 Sadržajna komponenta (žanrovi i autori)

1. Iz korisnikovih signala gradi se profil žanrova: za svaku knjigu s ukupnom težinom *w*, svakom njenom žanru se dodaje *w*. Isto se radi za autore.
2. Profil žanrova se normalizira najvećom vrijednošću (najomiljeniji žanr = 1).
3. Sadržajna ocjena kandidata je **prosjek normaliziranih vrijednosti njegovih žanrova**.
4. Ako korisnik voli autora kandidata, dodaje se bonus do 0,25 (srazmjeran omiljenosti autora). Rezultat se ograničava na 0–1.

Žanr koji korisnik izbjegava (negativna težina zbog niskih ocjena) snižava ocjenu knjiga tog žanra.

### 2.2 Kolaborativna komponenta (slični čitaoci)

1. „Knjige koje korisnik voli" su one čija je ukupna težina najmanje 3 (bez pretrage).
2. Za svakog drugog korisnika računa se **kosinusna sličnost** skupova voljenih knjiga: `presjek / sqrt(|moje| · |njegove|)`.
3. Svaka knjiga koju je sličan korisnik volio, a moj korisnik je još nema, dobija sličnost tog korisnika. Sličnosti se sabiraju preko svih korisnika.
4. Rezultat se normalizira najvećom vrijednošću.

### 2.3 Popularnost

```
sirovo = (prosječna ocjena / 5) · ln(1 + broj recenzija + broj čitalaca)
```

Normalizira se najvećom vrijednošću među kandidatima. Logaritam sprječava da jedna knjiga sa stotinama čitalaca potpuno nadjača ostale.

### 2.4 Hladni start

Ako korisnik nema nijedan pozitivan signal (novi korisnik), personalizacija nije moguća. Tada se preporuke računaju samo iz popularnosti, a odgovor ima `strategy = "Popularity"` i uz svaku preporuku razlog „Još nemamo dovoljno podataka o vašem ukusu, pa prikazujemo najpopularnije knjige." Kod ostalih je `strategy = "Personalized"`.

## 3. Objašnjenje preporuke

Svaka stavka odgovora sadrži `reasons` (najviše 3, poredani po doprinosu ocjeni; komponenta ulazi u razloge ako doprinosi najmanje 0,05):

- sadržajni razlog, npr. *Volite žanr „Horor” – dodali ste u favorite „Frankenstein”.* (navodi se žanr kandidata s najvećom težinom i knjiga koja je najviše doprinijela tom žanru)
- razlog autora, npr. *Isti autor (Mary Shelley) kao „Frankenstein”.*
- kolaborativni razlog, npr. *Čitaoci sličnog ukusa (4) su visoko ocijenili, sačuvali ili pročitali ovu knjigu.*
- razlog popularnosti, npr. *Popularna knjiga: prosječna ocjena 4,5 (12 recenzija).*

Uz to se vraćaju `score`, `contentScore`, `collaborativeScore` i `popularityScore`, pa se svaka ocjena može provjeriti.

## 4. Primjer toka

1. Korisnik doda „Frankenstein" u favorite i ocijeni ga sa 5 → težina knjige 3 + 4 = 7; žanrovi Horor, Roman i Znanstvena fantastika dobijaju 7, autor Mary Shelley 7.
2. Kandidat „Drakula" (žanrovi Horor, Klasici): Horor = 1, Klasici = 0 → sadržajna = 0,5. Ako je korisnik prije toga pročitao neku klasiku, i Klasici dobijaju vrijednost i ocjena raste.
3. Ako su dva slična čitaoca također voljela „Frankenstein" i „Drakulu", „Drakula" dobija kolaborativnu ocjenu, a razlog to navodi.
4. Ukupna ocjena i razlozi vraćaju se klijentu.

## 5. Kako testirati

1. Prijavite se kao `mobile`, pozovite `GET /api/Recommendations` – bez signala dobije se strategija `Popularity`.
2. Dodajte favorit (`PUT /api/Favorites/{bookId}`), počnite čitati (`PUT /api/ReadingProgress`) i ocijenite knjigu (`PUT /api/Reviews`).
3. Ponovo pozovite `GET /api/Recommendations` – strategija je `Personalized`, a `reasons` navode koje knjige i žanrove je algoritam uzeo u obzir.

## 6. Ograničenja

- Podaci drugih korisnika se učitavaju u memoriju pri svakom pozivu. To je prihvatljivo za desetke hiljada interakcija; za veći sistem treba keširanje ili prethodno izračunate sličnosti.
- Pretraga se povezuje s knjigama po podudaranju naslova i autora, ne po značenju upita.
- Težine i omjeri komponenti su odabrani ručno i mogu se podešavati u `RecommendationEngine`.
