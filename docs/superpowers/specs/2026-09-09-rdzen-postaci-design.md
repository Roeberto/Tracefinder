# Rdzeń postaci (poziom 1) — spec

Data: 2026-09-09
Status: zatwierdzony przez użytkownika, gotowy do planu implementacji

## Kontekst

Tracefinder to gra 2D topdown w Godot 4.7 (.NET/C#) inspirowana Pathfinder 2e. Wcześniejsza,
znacznie bardziej rozbudowana implementacja (sieć/sesje, system akcji i zatwierdzania przez GM,
ekwipunek) żyje na branchu `AIslop` — użytkownik świadomie zresetował `main` do czystego stanu
(prototyp świata + własnoręcznie odbudowane, minimalne menu główne) i buduje od nowa, niezależnie od
tamtej implementacji. Ten dokument NIE zakłada żadnej integracji z kodem z `AIslop`.

**Decyzja o zakresie:** użytkownik chce docelowo pełnego systemu postaci Pathfinder 2e (Remaster:
Player Core, Player Core 2, GM Core, Monster Core — bez dodatków) z pełną progresją poziomów 1-20 i
drzewkami featów. To zbyt duży zakres na jeden spec — podczas brainstormingu rozbito to na sub-projekty:

1. **Rdzeń postaci (poziom 1)** — ten dokument.
2. Ancestry/dziedzictwo.
3. Progresja poziomów (1-20, bez jeszcze featów).
4. Drzewka featów (klasowe/ancestry/ogólne/umiejętności).
5. Integracja z systemem walki/akcji.

Każdy dostaje własny cykl spec → plan → implementacja.

## Cel

Kreator i model danych postaci na poziomie 1, ograniczony do czterech klas startowych (Wojownik,
Czarodziej, Kapłan, Zbir), z poprawnymi wyliczeniami HP/AC/Class DC/saves/percepcji na bazie cech i
rang biegłości — wystarczający, żeby stworzyć, zapisać i wczytać spójną, zgodną z regułami PF2e
postać poziomu 1 (w granicach uproszczeń wymienionych niżej). Definicje klas jako dane (Godot
`Resource`), nie kod — dodanie kolejnej z ~17 pozostałych klas Player Core/Player Core 2 ma być
zadaniem czysto danych (nowy plik `.tres`), bez zmian w logice.

## Poza zakresem

- **Ancestry i background** — cechy przypisywane swobodnie przez gracza w walidowanym zakresie
  (8-18), zamiast przez boosty z ancestry/background. HP nie zawiera składnika ancestry (tylko
  HP klasy/poziom + mod CON). Umiejętności treningowe pochodzą wyłącznie z klasy, bez Lore z
  background'u. To świadome uproszczenie, nie błąd względem RAW — do naprawienia w sub-projekcie 2.
- **Zaklęcia** — Czarodziej i Kapłan mają zapisaną klasę/cechy/HP jak każda inna postać, ale bez
  działającego systemu rzucania (brak listy zaklęć, slotów, przygotowywania). Osobny, przyszły
  sub-projekt "Magia".
- **Feat-y** (klasowe, ancestry, ogólne, umiejętności) — poza zakresem, w tym feat umiejętności z
  background'u. Nazwy featurów klasowych poziomu 1 (np. "Sneak Attack", "Attack of Opportunity") są
  zapisane jako czysty tekst/etykieta na arkuszu — bez działającej mechaniki.
- **Ekwipunek** — brak przedmiotów, w tym zbroi. AC liczone wyłącznie wg formuły "bez zbroi"
  (Unarmored Defense).
- **Integracja z rozgrywką** — arkusz postaci nie jest podpięty pod żadną mechanikę walki/akcji.
  Czysty kreator + zapis + odczyt danych postaci.
- **Leveling** — postać jest zawsze poziomu 1 w tym dokumencie. Wzory wyliczeniowe SĄ jednak
  parametryzowane poziomem (nie zaszyte na sztywno jako "poziom 1"), żeby sub-projekt 3 (progresja
  poziomów) mógł je ponownie wykorzystać bez przepisywania.
- **Więcej niż 4 klasy startowe** — pozostałe ~17 klas z Player Core + Player Core 2 dochodzą później
  jako osobne, dodatkowe zadania (nowy plik `.tres` per klasa) — nie są częścią tego planu.

## Model danych

**`ClassDefinition`** (nowy typ, C# `Resource`, `.tres` — edytowalny w Inspectorze Godota, jeden plik
na klasę, np. `resources/classes/Warrior.tres`) — dane KLASY, wspólne dla wszystkich postaci tej
klasy:
- `Name` (string, np. "Wojownik")
- `KeyAbilityOptions` (lista 1-2 cech do wyboru jako kluczowa cecha klasy — np. Wojownik: STR lub
  DEX; Czarodziej: tylko INT)
- `HitPointsPerLevel` (int)
- `TrainedSkills` (lista nazw umiejętności trenowanych na starcie, stałych dla tej klasy)
- `BonusSkillsFromIntModifier` (bool — czy dodatkowa liczba trenowanych umiejętności = mod INT,
  jak u Zbira)
- `PerceptionProficiency`, `FortitudeProficiency`, `ReflexProficiency`, `WillProficiency`,
  `ClassDcProficiency`, `UnarmoredProficiency` (każde: ranga startowa na poziomie 1 — Untrained/
  Trained/Expert/Master/Legendary, jako enum)
- `Level1Features` (lista stringów — czyste etykiety, np. `["Attack of Opportunity"]`)

**`CharacterCore`** (czysty C#, bez zależności od Godota, testowalny xUnit-em) — dane KONKRETNEJ
postaci:
- `Name` (string)
- `Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma` (int, walidowane
  8-18 przy tworzeniu)
- `ClassId` (string, identyfikator wskazujący na `ClassDefinition`)
- `SelectedKeyAbility` (który z `KeyAbilityOptions` gracz wybrał, jeśli klasa dawała wybór)
- właściwości wyliczane (patrz niżej), NIE zapisywane wprost do JSON — przeliczane przy wczytaniu
  z surowych danych + odnalezionej `ClassDefinition`.

## Obliczenia

**Bonus z rangi biegłości** — jedna generyczna funkcja przyjmująca `ProficiencyRank` i `level`:
`Untrained → 0` (bez dodawania poziomu), `Trained → level + 2`, `Expert → level + 4`,
`Master → level + 6`, `Legendary → level + 8`. Parametryzacja poziomem (nie "poziom 1" na sztywno)
jest celowa — patrz sekcja "Poza zakresem" (leveling).

- **Modyfikator cechy** = `(wartość_cechy - 10) / 2`, zaokrąglone w dół (standardowa formuła PF2e).
- **HP** = `ClassDefinition.HitPointsPerLevel + modyfikator KON` (poziom 1, bez składnika ancestry).
- **AC** (bez zbroi) = `10 + modyfikator DEX + bonus(UnarmoredProficiency, poziom)`.
- **Class DC** = `10 + bonus(ClassDcProficiency, poziom) + modyfikator wybranej kluczowej cechy`.
- **Saves (Fortitude/Reflex/Will)** = `bonus(odpowiednia ranga, poziom) + modyfikator odpowiedniej
  cechy` (Fortitude→KON, Reflex→DEX, Will→MDR).
- **Percepcja** = `bonus(PerceptionProficiency, poziom) + modyfikator WDR`.
- **Umiejętności** (16 podstawowych umiejętności PF2e, bez Lore) — każda ma rangę: `Trained`, jeśli
  na liście `ClassDefinition.TrainedSkills` (plus tyle dodatkowych, swobodnie wybranych przez gracza
  z pozostałych, ile wynosi mod INT, jeśli `BonusSkillsFromIntModifier=true`), inaczej `Untrained`.
  Bonus umiejętności = `bonus(ranga, poziom) + modyfikator powiązanej cechy` (mapowanie
  umiejętność→cecha wg standardowej listy PF2e, np. Atletyka→STR, Skrytość→DEX).

## Tworzenie postaci — UI

Nowy ekran kreatora (Godot Control scene + C# skrypt, wzorem istniejącego menu w tym projekcie):
pola liczbowe na 6 cech z walidacją zakresu 8-18 przy zapisie, `OptionButton` z wyborem jednej z 4
zaimplementowanych `ClassDefinition` (wczytywanych z katalogu zasobów), drugi `OptionButton`/wybór
kluczowej cechy jeśli klasa oferuje więcej niż jedną opcję, podgląd wyliczonych HP/AC/Class DC/
saves/percepcji/umiejętności przed zapisem, przycisk zapisu.

## Zapis

Lokalny plik JSON w `user://characters/<nazwa>.json`, zawierający wyłącznie surowe dane wejściowe
(`Name`, 6 cech, `ClassId`, `SelectedKeyAbility`) — wartości wyliczane (HP/AC/itd.) NIE są
zapisywane, tylko przeliczane na nowo przy wczytaniu z `ClassDefinition` odnalezionej po `ClassId`.
Zapobiega to rozjazdowi zapisanych danych z regułami, gdyby `ClassDefinition` się później zmieniła.

## Obsługa błędów i przypadki brzegowe

- Cecha poza zakresem 8-18 przy zapisie → błąd walidacji w UI, zapis zablokowany.
- `ClassId` z pliku JSON nieznaleziony wśród wczytanych `ClassDefinition` (np. plik `.tres` usunięty
  po zapisaniu postaci) → błąd przy wczytaniu, jawny komunikat, postać nie ładuje się jako
  częściowo poprawna.
- Klasa oferująca wybór kluczowej cechy, ale `SelectedKeyAbility` nieustawione/niepoprawne w danych
  wejściowych → domyślnie pierwsza opcja z `KeyAbilityOptions`.

## Testowanie

- **Cała logika wyliczeniowa** (`CharacterCore`, funkcja bonusu z rangi biegłości, modyfikator cechy,
  HP/AC/Class DC/saves/percepcja/umiejętności) — czysty C# bez zależności od GodotSharp, testowana
  xUnit-em. Dokładne startowe rangi biegłości i listy umiejętności treningowych dla Wojownika/
  Czarodzieja/Kapłana/Zbira będą zweryfikowane względem Player Core przy pisaniu planu implementacji
  (nie zgadywane teraz z pamięci) i wpisane wprost jako wartości oczekiwane w testach.
- **`ClassDefinition` (Resource) i UI kreatora** — testy manualne, Godot Engine niedostępny w tym
  środowisku wykonawczym (jak we wcześniejszych sub-projektach tego repo).

## Powiązania z innymi sub-projektami

- Niezależny od brancha `AIslop` — nie zakłada, nie wymaga i nie integruje się z kodem sieciowym/
  walki/ekwipunku zbudowanym tam wcześniej.
- Fundament pod sub-projekty 2-5 z tej samej serii (ancestry, leveling, feat-y, integracja z walką)
  — architektura (generyczna funkcja bonusu z rangi parametryzowana poziomem, `ClassDefinition` jako
  dane) jest celowo zaprojektowana pod ich przyszłe rozszerzenie bez przepisywania tego kodu.
