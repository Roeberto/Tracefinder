Tracefinder

Gra 2D top-down tworzona w Godot Engine 4.7 (.NET / C#).

Projekt na wczesnym etapie prototypu — obecnie zaimplementowany jest świat oparty na kafelkach oraz sterowana postać gracza z animacjami kierunkowymi.

Wymagania
Godot Engine 4.7.2 – wersja .NET (zwykły build bez .NET nie uruchomi tego projektu)
.NET SDK 10.0

Na Fedorze:

sudo dnf install dotnet-sdk-10.0

Godota w wersji .NET pobierz ze strony godotengine.org/download/linux — plik oznaczony jako „Godot Engine – .NET".

Uruchomienie
bash
git clone <adres-repozytorium>
cd tracefinder

Następnie:

Otwórz projekt w Godocie (Import → wskaż project.godot)
Kliknij przycisk Build (ikona młotka w prawym górnym rogu) — projekty C# wymagają kompilacji przed pierwszym uruchomieniem
Uruchom grę klawiszem F5, zaczyna się od menu głównego (scenes/MainMenu.tscn)

Testy

Testy zasad, walki i generatora świata nie wymagają Godota:

dotnet test tests/Tracefinder.Tests
