# Rdzeń postaci (poziom 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Kreator i model danych postaci Pathfinder 2e (Remaster) na poziomie 1, ograniczony do
czterech klas startowych (Wojownik, Czarodziej, Kapłan, Zbir), z poprawnymi wyliczeniami HP/AC/
Class DC/saves/percepcji/umiejętności na bazie cech i rang biegłości.

**Architecture:** Czysta, testowalna logika C# (`Tracefinder.Character` namespace, bez zależności od
GodotSharp) liczy wszystkie wyliczane statystyki na podstawie prostych struktur danych. Definicje
klas żyją jako dane — Godot `Resource` (`.tres`), edytowalne w Inspectorze — z metodą-mostkiem
przekładającą je na czystą, testowalną strukturę `ClassStats`, dokładnie tym samym wzorcem co
`CharacterSheet.ToCombatStats()` sprawdzony wcześniej w tym projekcie. UI kreatora to nowa scena
Godota + kontroler C#.

**Tech Stack:** Godot 4.7.2 (.NET/C#), xUnit (nowy projekt testowy `tests/Tracefinder.Tests.csproj`).

**Spec:** `docs/superpowers/specs/2026-09-09-rdzen-postaci-design.md`

## Global Constraints

- Cała logika wyliczeniowa musi być czystym C# bez `using Godot` — testowana xUnit-em. Po KAŻDYM
  zadaniu dotykającym `scripts/Character/*.cs` uruchom ZARÓWNO `dotnet build Tracefinder.csproj` JAK
  I `dotnet test tests/Tracefinder.Tests.csproj`. Projekt testowy używa WYŁĄCZNIE jawnych,
  pojedynczych wpisów `<Compile Include>` (nie wildcardów) — to celowa decyzja zapobiegająca
  dokładnie tej klasie błędu, która wcześniej w tym repo (inny branch) spowodowała, że wildcard
  wciągnął plik zależny od Godota do projektu testowego i zepsuł `dotnet test` na 5 kolejnych zadań
  bez wykrycia.
- `ClassDefinition.cs` (jedyny plik Godot-zależny w tym planie, Task 7) NIE wchodzi do projektu
  testowego — nie ma go na liście `<Compile Include>` w `tests/Tracefinder.Tests.csproj`.
- Postać jest zawsze poziomu 1 (`CharacterCore.Level = 1`, stała), ale wzory przyjmują `level` jako
  parametr — nie zaszywaj "1" na sztywno w formułach `ProficiencyMath`.
- Dane czterech klas startowych (poniżej, w Task 8) pochodzą z Pathfinder 2e Player Core (2023,
  Remaster) — zweryfikowane przy pisaniu tego planu, nie zgadywane.
- Poza zakresem (nie implementuj): ancestry, background, zaklęcia, feat-y jako działająca mechanika,
  ekwipunek, integracja z rozgrywką, leveling. Patrz spec, sekcja "Poza zakresem".
- **Korekta względem specu:** spec opisuje `BonusSkillsFromIntModifier` jako `bool`. To nieścisłość
  wykryta przy pisaniu tego planu — każda z czterech klas startowych ma bazową liczbę dodatkowych
  umiejętności treningowych RÓŻNĄ od zera (3 dla Wojownika, 2 dla Czarodzieja/Kapłana, 7 dla Zbira),
  do której dolicza się modyfikator INT — nie jest to przełącznik "włącz/wyłącz mod INT". Ten plan
  używa `int BonusSkillCount` (wartość bazowa, np. 3) zamiast `bool` — finalna liczba dodatkowych
  umiejętności to `BonusSkillCount + modyfikator INT`.

---

## Task 1: Projekt testowy (szkielet xUnit)

**Files:**
- Create: `tests/Tracefinder.Tests.csproj`
- Modify: `Tracefinder.csproj`

**Interfaces:**
- Produces: działający, pusty projekt testowy gotowy na dołączanie plików w kolejnych zadaniach.

- [ ] **Step 1: Napisz `tests/Tracefinder.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>disable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

</Project>
```

(Celowo bez `<ItemGroup>` z `<Compile Include>` na razie — pliki dochodzą jeden po drugim w kolejnych
zadaniach, jawnie, nigdy przez wildcard.)

- [ ] **Step 2: Wyklucz katalog `tests/` z głównego projektu Godota**

W `Tracefinder.csproj` dodaj nowy `<ItemGroup>`:

```xml
  <ItemGroup>
    <Compile Remove="tests/**/*.cs" />
  </ItemGroup>
```

Pełna oczekiwana zawartość pliku po zmianie:

```xml
<Project Sdk="Godot.NET.Sdk/4.7.2">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <TargetFramework Condition=" '$(GodotTargetPlatform)' == 'android' ">net9.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="tests/**/*.cs" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Zbuduj oba projekty**

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: "Nie znaleziono testów" / "No test is available" (pusty projekt, zero testów, zero błędów
kompilacji) — to oczekiwany wynik na tym etapie, nie porażka.

- [ ] **Step 4: Commit**

```bash
git add tests/Tracefinder.Tests.csproj Tracefinder.csproj
git commit -m "Dodaj szkielet projektu testowego xUnit"
```

---

## Task 2: Enumy i podstawowa matematyka biegłości (ProficiencyMath)

**Files:**
- Create: `scripts/Character/AbilityScore.cs`
- Create: `scripts/Character/ProficiencyRank.cs`
- Create: `scripts/Character/ProficiencyMath.cs`
- Test: `tests/Character/ProficiencyMathTests.cs`
- Modify: `tests/Tracefinder.Tests.csproj`

**Interfaces:**
- Produces: `enum AbilityScore { Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma }`;
  `enum ProficiencyRank { Untrained, Trained, Expert, Master, Legendary }`;
  `static class ProficiencyMath` z `static int Bonus(ProficiencyRank rank, int level)` i
  `static int AbilityModifier(int abilityScore)`.

- [ ] **Step 1: Napisz test `ProficiencyMathTests.cs`**

```csharp
using Tracefinder.Character;
using Xunit;

public class ProficiencyMathTests
{
    [Theory]
    [InlineData(ProficiencyRank.Untrained, 1, 0)]
    [InlineData(ProficiencyRank.Trained, 1, 3)]
    [InlineData(ProficiencyRank.Expert, 1, 5)]
    [InlineData(ProficiencyRank.Master, 1, 7)]
    [InlineData(ProficiencyRank.Legendary, 1, 9)]
    [InlineData(ProficiencyRank.Untrained, 5, 0)]
    [InlineData(ProficiencyRank.Trained, 5, 7)]
    public void Bonus_ReturnsCorrectValue(ProficiencyRank rank, int level, int expected)
    {
        Assert.Equal(expected, ProficiencyMath.Bonus(rank, level));
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(18, 4)]
    [InlineData(4, -3)]
    public void AbilityModifier_ReturnsCorrectValue(int score, int expected)
    {
        Assert.Equal(expected, ProficiencyMath.AbilityModifier(score));
    }
}
```

- [ ] **Step 2: Dodaj wpisy do `tests/Tracefinder.Tests.csproj`**

Dodaj nowy `<ItemGroup>` (przed zamykającym `</Project>`):

```xml
  <ItemGroup>
    <Compile Include="../scripts/Character/AbilityScore.cs" Link="Character/AbilityScore.cs" />
    <Compile Include="../scripts/Character/ProficiencyRank.cs" Link="Character/ProficiencyRank.cs" />
    <Compile Include="../scripts/Character/ProficiencyMath.cs" Link="Character/ProficiencyMath.cs" />
  </ItemGroup>
```

- [ ] **Step 3: Uruchom testy i zweryfikuj, że nie przechodzą (pliki źródłowe jeszcze nie istnieją)**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: FAIL (błąd kompilacji — `AbilityScore`/`ProficiencyRank`/`ProficiencyMath` nie istnieją).

- [ ] **Step 4: Napisz `scripts/Character/AbilityScore.cs`**

```csharp
namespace Tracefinder.Character;

public enum AbilityScore
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma
}
```

- [ ] **Step 5: Napisz `scripts/Character/ProficiencyRank.cs`**

```csharp
namespace Tracefinder.Character;

public enum ProficiencyRank
{
    Untrained,
    Trained,
    Expert,
    Master,
    Legendary
}
```

- [ ] **Step 6: Napisz `scripts/Character/ProficiencyMath.cs`**

```csharp
using System;

namespace Tracefinder.Character;

public static class ProficiencyMath
{
    public static int Bonus(ProficiencyRank rank, int level)
    {
        return rank switch
        {
            ProficiencyRank.Untrained => 0,
            ProficiencyRank.Trained => level + 2,
            ProficiencyRank.Expert => level + 4,
            ProficiencyRank.Master => level + 6,
            ProficiencyRank.Legendary => level + 8,
            _ => 0
        };
    }

    public static int AbilityModifier(int abilityScore)
    {
        return (int)Math.Floor((abilityScore - 10) / 2.0);
    }
}
```

- [ ] **Step 7: Uruchom testy i zweryfikuj, że wszystkie przechodzą**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: PASS (14 testów: 7 `[InlineData]` na `Bonus_ReturnsCorrectValue` + 7 na
`AbilityModifier_ReturnsCorrectValue`).

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 8: Commit**

```bash
git add scripts/Character/AbilityScore.cs scripts/Character/ProficiencyRank.cs scripts/Character/ProficiencyMath.cs tests/Character/ProficiencyMathTests.cs tests/Tracefinder.Tests.csproj
git commit -m "Dodaj enumy AbilityScore/ProficiencyRank i ProficiencyMath"
```

---

## Task 3: Katalog umiejętności (SkillName, SkillCatalog)

**Files:**
- Create: `scripts/Character/SkillName.cs`
- Create: `scripts/Character/SkillCatalog.cs`
- Test: `tests/Character/SkillCatalogTests.cs`
- Modify: `tests/Tracefinder.Tests.csproj`

**Interfaces:**
- Consumes: `AbilityScore` (Task 2).
- Produces: `enum SkillName` (16 wartości: Acrobatics, Arcana, Athletics, Crafting, Deception,
  Diplomacy, Intimidation, Medicine, Nature, Occultism, Performance, Religion, Society, Stealth,
  Survival, Thievery); `static class SkillCatalog` z
  `static AbilityScore KeyAbilityFor(SkillName skill)` i `static readonly IReadOnlyList<SkillName> All`.

- [ ] **Step 1: Napisz test `SkillCatalogTests.cs`**

```csharp
using System.Linq;
using Tracefinder.Character;
using Xunit;

public class SkillCatalogTests
{
    [Theory]
    [InlineData(SkillName.Acrobatics, AbilityScore.Dexterity)]
    [InlineData(SkillName.Arcana, AbilityScore.Intelligence)]
    [InlineData(SkillName.Athletics, AbilityScore.Strength)]
    [InlineData(SkillName.Crafting, AbilityScore.Intelligence)]
    [InlineData(SkillName.Deception, AbilityScore.Charisma)]
    [InlineData(SkillName.Diplomacy, AbilityScore.Charisma)]
    [InlineData(SkillName.Intimidation, AbilityScore.Charisma)]
    [InlineData(SkillName.Medicine, AbilityScore.Wisdom)]
    [InlineData(SkillName.Nature, AbilityScore.Wisdom)]
    [InlineData(SkillName.Occultism, AbilityScore.Intelligence)]
    [InlineData(SkillName.Performance, AbilityScore.Charisma)]
    [InlineData(SkillName.Religion, AbilityScore.Wisdom)]
    [InlineData(SkillName.Society, AbilityScore.Intelligence)]
    [InlineData(SkillName.Stealth, AbilityScore.Dexterity)]
    [InlineData(SkillName.Survival, AbilityScore.Wisdom)]
    [InlineData(SkillName.Thievery, AbilityScore.Dexterity)]
    public void KeyAbilityFor_ReturnsCorrectMapping(SkillName skill, AbilityScore expected)
    {
        Assert.Equal(expected, SkillCatalog.KeyAbilityFor(skill));
    }

    [Fact]
    public void All_Contains16Skills()
    {
        Assert.Equal(16, SkillCatalog.All.Count);
    }

    [Fact]
    public void All_HasNoDuplicates()
    {
        Assert.Equal(SkillCatalog.All.Count, SkillCatalog.All.Distinct().Count());
    }
}
```

- [ ] **Step 2: Dodaj wpisy do `tests/Tracefinder.Tests.csproj`**

Dodaj do istniejącego `<ItemGroup>` z Task 2 (nie twórz nowego):

```xml
    <Compile Include="../scripts/Character/SkillName.cs" Link="Character/SkillName.cs" />
    <Compile Include="../scripts/Character/SkillCatalog.cs" Link="Character/SkillCatalog.cs" />
```

- [ ] **Step 3: Uruchom testy, zweryfikuj FAIL (kompilacja)**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: FAIL — `SkillName`/`SkillCatalog` nie istnieją.

- [ ] **Step 4: Napisz `scripts/Character/SkillName.cs`**

```csharp
namespace Tracefinder.Character;

public enum SkillName
{
    Acrobatics,
    Arcana,
    Athletics,
    Crafting,
    Deception,
    Diplomacy,
    Intimidation,
    Medicine,
    Nature,
    Occultism,
    Performance,
    Religion,
    Society,
    Stealth,
    Survival,
    Thievery
}
```

- [ ] **Step 5: Napisz `scripts/Character/SkillCatalog.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace Tracefinder.Character;

public static class SkillCatalog
{
    public static readonly IReadOnlyList<SkillName> All = (SkillName[])Enum.GetValues(typeof(SkillName));

    public static AbilityScore KeyAbilityFor(SkillName skill)
    {
        return skill switch
        {
            SkillName.Acrobatics => AbilityScore.Dexterity,
            SkillName.Arcana => AbilityScore.Intelligence,
            SkillName.Athletics => AbilityScore.Strength,
            SkillName.Crafting => AbilityScore.Intelligence,
            SkillName.Deception => AbilityScore.Charisma,
            SkillName.Diplomacy => AbilityScore.Charisma,
            SkillName.Intimidation => AbilityScore.Charisma,
            SkillName.Medicine => AbilityScore.Wisdom,
            SkillName.Nature => AbilityScore.Wisdom,
            SkillName.Occultism => AbilityScore.Intelligence,
            SkillName.Performance => AbilityScore.Charisma,
            SkillName.Religion => AbilityScore.Wisdom,
            SkillName.Society => AbilityScore.Intelligence,
            SkillName.Stealth => AbilityScore.Dexterity,
            SkillName.Survival => AbilityScore.Wisdom,
            SkillName.Thievery => AbilityScore.Dexterity,
            _ => throw new ArgumentOutOfRangeException(nameof(skill))
        };
    }
}
```

- [ ] **Step 6: Uruchom testy, zweryfikuj PASS**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: PASS (14 poprzednich + 18 nowych = 32 testy: 16 `KeyAbilityFor` + `All_Contains16Skills` +
`All_HasNoDuplicates`).

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 7: Commit**

```bash
git add scripts/Character/SkillName.cs scripts/Character/SkillCatalog.cs tests/Character/SkillCatalogTests.cs tests/Tracefinder.Tests.csproj
git commit -m "Dodaj SkillName i SkillCatalog (mapowanie umiejętność→cecha)"
```

---

## Task 4: ClassStats — czysta struktura danych klasy

**Files:**
- Create: `scripts/Character/ClassStats.cs`
- Test: `tests/Character/ClassStatsTests.cs`
- Modify: `tests/Tracefinder.Tests.csproj`

**Interfaces:**
- Consumes: `AbilityScore`, `ProficiencyRank`, `SkillName` (Task 2, 3).
- Produces: `sealed class ClassStats` z właściwościami (get/set): `string Name`,
  `List<AbilityScore> KeyAbilityOptions`, `int HitPointsPerLevel`, `List<SkillName> TrainedSkills`,
  `int BonusSkillCount`, `ProficiencyRank PerceptionProficiency`, `FortitudeProficiency`,
  `ReflexProficiency`, `WillProficiency`, `ClassDcProficiency`, `UnarmoredProficiency`,
  `List<string> Level1Features`.

- [ ] **Step 1: Napisz test `ClassStatsTests.cs`**

```csharp
using System.Collections.Generic;
using Tracefinder.Character;
using Xunit;

public class ClassStatsTests
{
    [Fact]
    public void NewInstance_HasEmptyDefaultCollections()
    {
        var stats = new ClassStats();

        Assert.Empty(stats.KeyAbilityOptions);
        Assert.Empty(stats.TrainedSkills);
        Assert.Empty(stats.Level1Features);
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var stats = new ClassStats
        {
            Name = "Wojownik",
            KeyAbilityOptions = new List<AbilityScore> { AbilityScore.Strength, AbilityScore.Dexterity },
            HitPointsPerLevel = 10,
            TrainedSkills = new List<SkillName> { SkillName.Athletics },
            BonusSkillCount = 3,
            PerceptionProficiency = ProficiencyRank.Expert,
            FortitudeProficiency = ProficiencyRank.Expert,
            ReflexProficiency = ProficiencyRank.Expert,
            WillProficiency = ProficiencyRank.Trained,
            ClassDcProficiency = ProficiencyRank.Trained,
            UnarmoredProficiency = ProficiencyRank.Trained,
            Level1Features = new List<string> { "Attack of Opportunity" }
        };

        Assert.Equal("Wojownik", stats.Name);
        Assert.Equal(2, stats.KeyAbilityOptions.Count);
        Assert.Equal(10, stats.HitPointsPerLevel);
        Assert.Contains(SkillName.Athletics, stats.TrainedSkills);
        Assert.Equal(3, stats.BonusSkillCount);
        Assert.Equal(ProficiencyRank.Expert, stats.PerceptionProficiency);
        Assert.Single(stats.Level1Features);
    }
}
```

- [ ] **Step 2: Dodaj wpis do `tests/Tracefinder.Tests.csproj`**

```xml
    <Compile Include="../scripts/Character/ClassStats.cs" Link="Character/ClassStats.cs" />
```

- [ ] **Step 3: Uruchom testy, zweryfikuj FAIL**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: FAIL — `ClassStats` nie istnieje.

- [ ] **Step 4: Napisz `scripts/Character/ClassStats.cs`**

```csharp
using System.Collections.Generic;

namespace Tracefinder.Character;

public sealed class ClassStats
{
    public string Name { get; set; } = "";
    public List<AbilityScore> KeyAbilityOptions { get; set; } = new();
    public int HitPointsPerLevel { get; set; }
    public List<SkillName> TrainedSkills { get; set; } = new();
    public int BonusSkillCount { get; set; }
    public ProficiencyRank PerceptionProficiency { get; set; }
    public ProficiencyRank FortitudeProficiency { get; set; }
    public ProficiencyRank ReflexProficiency { get; set; }
    public ProficiencyRank WillProficiency { get; set; }
    public ProficiencyRank ClassDcProficiency { get; set; }
    public ProficiencyRank UnarmoredProficiency { get; set; }
    public List<string> Level1Features { get; set; } = new();
}
```

- [ ] **Step 5: Uruchom testy, zweryfikuj PASS**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: PASS (32 poprzednie + 2 nowe = 34).

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 6: Commit**

```bash
git add scripts/Character/ClassStats.cs tests/Character/ClassStatsTests.cs tests/Tracefinder.Tests.csproj
git commit -m "Dodaj ClassStats: czystą strukturę danych definicji klasy"
```

---

## Task 5: CharacterCore — wyliczenia postaci

**Files:**
- Create: `scripts/Character/CharacterCore.cs`
- Test: `tests/Character/CharacterCoreTests.cs`
- Modify: `tests/Tracefinder.Tests.csproj`

**Interfaces:**
- Consumes: `AbilityScore`, `ProficiencyRank`, `SkillName` (Task 2, 3), `ProficiencyMath.Bonus`/
  `AbilityModifier` (Task 2), `SkillCatalog.KeyAbilityFor` (Task 3), `ClassStats` (Task 4).
- Produces: `sealed class CharacterCore` z: `const int Level = 1`; właściwościami `Name`, `Strength`,
  `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma` (int), `ClassId` (string),
  `SelectedKeyAbility` (AbilityScore), `ExtraTrainedSkills` (`List<SkillName>`); metodami
  `int GetScore(AbilityScore ability)`, `int AbilityModifier(AbilityScore ability)`,
  `int ComputeHp(ClassStats classStats)`, `int ComputeAc(ClassStats classStats)`,
  `int ComputeClassDc(ClassStats classStats)`, `int ComputeFortitude(ClassStats classStats)`,
  `int ComputeReflex(ClassStats classStats)`, `int ComputeWill(ClassStats classStats)`,
  `int ComputePerception(ClassStats classStats)`,
  `ProficiencyRank GetSkillRank(SkillName skill, ClassStats classStats)`,
  `int ComputeSkillBonus(SkillName skill, ClassStats classStats)`.

- [ ] **Step 1: Napisz test `CharacterCoreTests.cs`**

```csharp
using System.Collections.Generic;
using Tracefinder.Character;
using Xunit;

public class CharacterCoreTests
{
    private static ClassStats MakeFighterStats() => new()
    {
        Name = "Wojownik",
        KeyAbilityOptions = new List<AbilityScore> { AbilityScore.Strength, AbilityScore.Dexterity },
        HitPointsPerLevel = 10,
        TrainedSkills = new List<SkillName> { SkillName.Athletics },
        BonusSkillCount = 3,
        PerceptionProficiency = ProficiencyRank.Expert,
        FortitudeProficiency = ProficiencyRank.Expert,
        ReflexProficiency = ProficiencyRank.Expert,
        WillProficiency = ProficiencyRank.Trained,
        ClassDcProficiency = ProficiencyRank.Trained,
        UnarmoredProficiency = ProficiencyRank.Trained
    };

    private static CharacterCore MakeFighter() => new()
    {
        Name = "Amber",
        Strength = 18,
        Dexterity = 14,
        Constitution = 16,
        Intelligence = 10,
        Wisdom = 12,
        Charisma = 8,
        ClassId = "fighter",
        SelectedKeyAbility = AbilityScore.Strength
    };

    [Fact]
    public void GetScore_ReturnsCorrectAbilityValue()
    {
        CharacterCore character = MakeFighter();

        Assert.Equal(18, character.GetScore(AbilityScore.Strength));
        Assert.Equal(14, character.GetScore(AbilityScore.Dexterity));
        Assert.Equal(8, character.GetScore(AbilityScore.Charisma));
    }

    [Fact]
    public void AbilityModifier_UsesProficiencyMathFormula()
    {
        CharacterCore character = MakeFighter();

        // STR 18 -> mod +4
        Assert.Equal(4, character.AbilityModifier(AbilityScore.Strength));
        // CHA 8 -> mod -1
        Assert.Equal(-1, character.AbilityModifier(AbilityScore.Charisma));
    }

    [Fact]
    public void ComputeHp_AddsClassHitPointsAndConstitutionModifier()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // 10 (klasa) + mod CON 16 (+3) = 13
        Assert.Equal(13, character.ComputeHp(fighter));
    }

    [Fact]
    public void ComputeAc_UnarmoredFormula()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // 10 + mod DEX 14 (+2) + bonus(Trained, poziom 1)=3 = 15
        Assert.Equal(15, character.ComputeAc(fighter));
    }

    [Fact]
    public void ComputeClassDc_UsesSelectedKeyAbility()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // 10 + bonus(Trained, 1)=3 + mod STR 18 (+4) = 17
        Assert.Equal(17, character.ComputeClassDc(fighter));
    }

    [Fact]
    public void ComputeFortitude_ExpertProficiency()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // bonus(Expert, 1)=5 + mod CON 16 (+3) = 8
        Assert.Equal(8, character.ComputeFortitude(fighter));
    }

    [Fact]
    public void ComputeWill_TrainedProficiency()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // bonus(Trained, 1)=3 + mod WIS 12 (+1) = 4
        Assert.Equal(4, character.ComputeWill(fighter));
    }

    [Fact]
    public void ComputePerception_ExpertProficiency()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // bonus(Expert, 1)=5 + mod WIS 12 (+1) = 6
        Assert.Equal(6, character.ComputePerception(fighter));
    }

    [Fact]
    public void GetSkillRank_ClassTrainedSkill_ReturnsTrained()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        Assert.Equal(ProficiencyRank.Trained, character.GetSkillRank(SkillName.Athletics, fighter));
    }

    [Fact]
    public void GetSkillRank_UntrainedSkill_ReturnsUntrained()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        Assert.Equal(ProficiencyRank.Untrained, character.GetSkillRank(SkillName.Arcana, fighter));
    }

    [Fact]
    public void GetSkillRank_ExtraTrainedSkill_ReturnsTrained()
    {
        CharacterCore character = MakeFighter();
        character.ExtraTrainedSkills.Add(SkillName.Intimidation);
        ClassStats fighter = MakeFighterStats();

        Assert.Equal(ProficiencyRank.Trained, character.GetSkillRank(SkillName.Intimidation, fighter));
    }

    [Fact]
    public void ComputeSkillBonus_TrainedSkill_AddsAbilityModifier()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // Athletics (Trained) -> bonus(Trained,1)=3 + mod STR 18 (+4) = 7
        Assert.Equal(7, character.ComputeSkillBonus(SkillName.Athletics, fighter));
    }

    [Fact]
    public void ComputeSkillBonus_UntrainedSkill_OnlyAbilityModifier()
    {
        CharacterCore character = MakeFighter();
        ClassStats fighter = MakeFighterStats();

        // Stealth (Untrained) -> bonus(Untrained,1)=0 + mod DEX 14 (+2) = 2
        Assert.Equal(2, character.ComputeSkillBonus(SkillName.Stealth, fighter));
    }
}
```

- [ ] **Step 2: Dodaj wpis do `tests/Tracefinder.Tests.csproj`**

```xml
    <Compile Include="../scripts/Character/CharacterCore.cs" Link="Character/CharacterCore.cs" />
```

- [ ] **Step 3: Uruchom testy, zweryfikuj FAIL**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: FAIL — `CharacterCore` nie istnieje.

- [ ] **Step 4: Napisz `scripts/Character/CharacterCore.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tracefinder.Character;

public sealed class CharacterCore
{
    public const int Level = 1;

    public string Name { get; set; } = "";
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }
    public string ClassId { get; set; } = "";
    public AbilityScore SelectedKeyAbility { get; set; }
    public List<SkillName> ExtraTrainedSkills { get; set; } = new();

    public int GetScore(AbilityScore ability)
    {
        return ability switch
        {
            AbilityScore.Strength => Strength,
            AbilityScore.Dexterity => Dexterity,
            AbilityScore.Constitution => Constitution,
            AbilityScore.Intelligence => Intelligence,
            AbilityScore.Wisdom => Wisdom,
            AbilityScore.Charisma => Charisma,
            _ => throw new ArgumentOutOfRangeException(nameof(ability))
        };
    }

    public int AbilityModifier(AbilityScore ability) => ProficiencyMath.AbilityModifier(GetScore(ability));

    public int ComputeHp(ClassStats classStats) =>
        classStats.HitPointsPerLevel + AbilityModifier(AbilityScore.Constitution);

    public int ComputeAc(ClassStats classStats) =>
        10 + AbilityModifier(AbilityScore.Dexterity) + ProficiencyMath.Bonus(classStats.UnarmoredProficiency, Level);

    public int ComputeClassDc(ClassStats classStats) =>
        10 + ProficiencyMath.Bonus(classStats.ClassDcProficiency, Level) + AbilityModifier(SelectedKeyAbility);

    public int ComputeFortitude(ClassStats classStats) =>
        ProficiencyMath.Bonus(classStats.FortitudeProficiency, Level) + AbilityModifier(AbilityScore.Constitution);

    public int ComputeReflex(ClassStats classStats) =>
        ProficiencyMath.Bonus(classStats.ReflexProficiency, Level) + AbilityModifier(AbilityScore.Dexterity);

    public int ComputeWill(ClassStats classStats) =>
        ProficiencyMath.Bonus(classStats.WillProficiency, Level) + AbilityModifier(AbilityScore.Wisdom);

    public int ComputePerception(ClassStats classStats) =>
        ProficiencyMath.Bonus(classStats.PerceptionProficiency, Level) + AbilityModifier(AbilityScore.Wisdom);

    public ProficiencyRank GetSkillRank(SkillName skill, ClassStats classStats)
    {
        bool trained = classStats.TrainedSkills.Contains(skill) || ExtraTrainedSkills.Contains(skill);
        return trained ? ProficiencyRank.Trained : ProficiencyRank.Untrained;
    }

    public int ComputeSkillBonus(SkillName skill, ClassStats classStats)
    {
        ProficiencyRank rank = GetSkillRank(skill, classStats);
        return ProficiencyMath.Bonus(rank, Level) + AbilityModifier(SkillCatalog.KeyAbilityFor(skill));
    }
}
```

- [ ] **Step 5: Uruchom testy, zweryfikuj PASS**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: PASS (34 poprzednie + 13 nowych = 47).

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 6: Commit**

```bash
git add scripts/Character/CharacterCore.cs tests/Character/CharacterCoreTests.cs tests/Tracefinder.Tests.csproj
git commit -m "Dodaj CharacterCore: wyliczenia HP/AC/Class DC/saves/percepcji/umiejętności"
```

---

## Task 6: Zapis i odczyt postaci (CharacterSheetRepository)

**Files:**
- Create: `scripts/Character/CharacterSheetRepository.cs`
- Test: `tests/Character/CharacterSheetRepositoryTests.cs`
- Modify: `tests/Tracefinder.Tests.csproj`

**Interfaces:**
- Consumes: `AbilityScore`, `SkillName` (Task 2, 3), `CharacterCore` (Task 5).
- Produces: `sealed class CharacterSheetRepository` z konstruktorem `(string directory)`,
  `IReadOnlyList<string> ListCharacterNames()`, `CharacterCore Load(string name)`,
  `void Save(CharacterCore character)`.

- [ ] **Step 1: Napisz test `CharacterSheetRepositoryTests.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Tracefinder.Character;
using Xunit;

public class CharacterSheetRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CharacterSheetRepository _repository;

    public CharacterSheetRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tracefinder-character-test-" + Guid.NewGuid());
        _repository = new CharacterSheetRepository(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ListCharacterNames_EmptyDirectory_ReturnsEmpty()
    {
        Assert.Empty(_repository.ListCharacterNames());
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var character = new CharacterCore
        {
            Name = "Amber",
            Strength = 18,
            Dexterity = 14,
            Constitution = 16,
            Intelligence = 10,
            Wisdom = 12,
            Charisma = 8,
            ClassId = "fighter",
            SelectedKeyAbility = AbilityScore.Strength,
            ExtraTrainedSkills = new List<SkillName> { SkillName.Intimidation, SkillName.Medicine }
        };

        _repository.Save(character);
        CharacterCore loaded = _repository.Load("Amber");

        Assert.Equal("Amber", loaded.Name);
        Assert.Equal(18, loaded.Strength);
        Assert.Equal(14, loaded.Dexterity);
        Assert.Equal(16, loaded.Constitution);
        Assert.Equal(10, loaded.Intelligence);
        Assert.Equal(12, loaded.Wisdom);
        Assert.Equal(8, loaded.Charisma);
        Assert.Equal("fighter", loaded.ClassId);
        Assert.Equal(AbilityScore.Strength, loaded.SelectedKeyAbility);
        Assert.Contains(SkillName.Intimidation, loaded.ExtraTrainedSkills);
        Assert.Contains(SkillName.Medicine, loaded.ExtraTrainedSkills);
    }

    [Fact]
    public void Save_AddsNameToList()
    {
        _repository.Save(new CharacterCore { Name = "Amber", ClassId = "fighter" });
        _repository.Save(new CharacterCore { Name = "Boris", ClassId = "wizard" });

        var names = _repository.ListCharacterNames();

        Assert.Contains("Amber", names);
        Assert.Contains("Boris", names);
    }
}
```

- [ ] **Step 2: Dodaj wpis do `tests/Tracefinder.Tests.csproj`**

```xml
    <Compile Include="../scripts/Character/CharacterSheetRepository.cs" Link="Character/CharacterSheetRepository.cs" />
```

- [ ] **Step 3: Uruchom testy, zweryfikuj FAIL**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: FAIL — `CharacterSheetRepository` nie istnieje.

- [ ] **Step 4: Napisz `scripts/Character/CharacterSheetRepository.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Tracefinder.Character;

public sealed class CharacterSheetRepository
{
    private readonly string _directory;

    public CharacterSheetRepository(string directory)
    {
        _directory = directory;
    }

    public IReadOnlyList<string> ListCharacterNames()
    {
        if (!Directory.Exists(_directory))
            return Array.Empty<string>();

        return Directory.GetFiles(_directory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name => name)
            .ToList();
    }

    public CharacterCore Load(string name)
    {
        string json = File.ReadAllText(GetPath(name));
        return JsonSerializer.Deserialize<CharacterCore>(json);
    }

    public void Save(CharacterCore character)
    {
        Directory.CreateDirectory(_directory);
        string json = JsonSerializer.Serialize(character, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(GetPath(character.Name), json);
    }

    private string GetPath(string name) => Path.Combine(_directory, $"{name}.json");
}
```

- [ ] **Step 5: Uruchom testy, zweryfikuj PASS**

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: PASS (47 poprzednich + 3 nowe = 50).

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 6: Commit**

```bash
git add scripts/Character/CharacterSheetRepository.cs tests/Character/CharacterSheetRepositoryTests.cs tests/Tracefinder.Tests.csproj
git commit -m "Dodaj CharacterSheetRepository: zapis/odczyt postaci jako JSON"
```

---

## Task 7: ClassDefinition — most Godot Resource ↔ ClassStats

**Files:**
- Create: `scripts/Character/ClassDefinition.cs`

**Interfaces:**
- Consumes: `AbilityScore`, `SkillName`, `ProficiencyRank` (Task 2, 3), `ClassStats` (Task 4).
- Produces: `partial class ClassDefinition : Resource` (Godot) z `[Export]` polami odpowiadającymi
  `ClassStats` i metodą `ClassStats ToClassStats()`.

To zadanie dotyka wyłącznie kodu zależnego od GodotSharp — uruchom TYLKO `dotnet build
Tracefinder.csproj`. NIE dodawaj tego pliku do `tests/Tracefinder.Tests.csproj` — to jedyny plik w
tym planie zależny od Godota, celowo pominięty na liście `<Compile Include>` (patrz Global
Constraints).

- [ ] **Step 1: Napisz `scripts/Character/ClassDefinition.cs`**

```csharp
using System.Linq;
using Godot;
using Tracefinder.Character;

[GlobalClass]
public partial class ClassDefinition : Resource
{
    [Export] public string ClassName = "";
    [Export] public Godot.Collections.Array<AbilityScore> KeyAbilityOptions = new();
    [Export] public int HitPointsPerLevel;
    [Export] public Godot.Collections.Array<SkillName> TrainedSkills = new();
    [Export] public int BonusSkillCount;
    [Export] public ProficiencyRank PerceptionProficiency;
    [Export] public ProficiencyRank FortitudeProficiency;
    [Export] public ProficiencyRank ReflexProficiency;
    [Export] public ProficiencyRank WillProficiency;
    [Export] public ProficiencyRank ClassDcProficiency;
    [Export] public ProficiencyRank UnarmoredProficiency;
    [Export] public Godot.Collections.Array<string> Level1Features = new();

    public ClassStats ToClassStats()
    {
        return new ClassStats
        {
            Name = ClassName,
            KeyAbilityOptions = KeyAbilityOptions.ToList(),
            HitPointsPerLevel = HitPointsPerLevel,
            TrainedSkills = TrainedSkills.ToList(),
            BonusSkillCount = BonusSkillCount,
            PerceptionProficiency = PerceptionProficiency,
            FortitudeProficiency = FortitudeProficiency,
            ReflexProficiency = ReflexProficiency,
            WillProficiency = WillProficiency,
            ClassDcProficiency = ClassDcProficiency,
            UnarmoredProficiency = UnarmoredProficiency,
            Level1Features = Level1Features.ToList()
        };
    }
}
```

- [ ] **Step 2: Zbuduj projekt**

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

Run: `dotnet test tests/Tracefinder.Tests.csproj`
Expected: PASS, dokładnie 50 testów (bez zmian — ten plik nie wchodzi do projektu testowego,
uruchomienie tu jest tylko potwierdzeniem, że wykluczenie działa i nic się nie zepsuło).

- [ ] **Step 3: Commit**

```bash
git add scripts/Character/ClassDefinition.cs
git commit -m "Dodaj ClassDefinition: most Godot Resource -> ClassStats"
```

---

## Task 8: Dane czterech klas startowych (.tres)

**Files:**
- Create: `resources/classes/Warrior.tres`
- Create: `resources/classes/Wizard.tres`
- Create: `resources/classes/Cleric.tres`
- Create: `resources/classes/Rogue.tres`
- Delete: `resourses/Warrior.tres` (pusty, wcześniejszy eksperyment użytkownika — literówka w
  nazwie katalogu, zastępowany właściwym plikiem wyżej)

**Interfaces:**
- Consumes: `ClassDefinition` (Task 7).

Dane poniżej pochodzą z Pathfinder 2e Player Core (2023, Remaster), poziom 1. Enumy w plikach
`.tres` serializują się jako liczby całkowite odpowiadające pozycji w enumie (licząc od 0) —
`AbilityScore`: Strength=0, Dexterity=1, Constitution=2, Intelligence=3, Wisdom=4, Charisma=5.
`ProficiencyRank`: Untrained=0, Trained=1, Expert=2, Master=3, Legendary=4. `SkillName`:
Acrobatics=0, Arcana=1, Athletics=2, Crafting=3, Deception=4, Diplomacy=5, Intimidation=6,
Medicine=7, Nature=8, Occultism=9, Performance=10, Religion=11, Society=12, Stealth=13,
Survival=14, Thievery=15.

**Uwaga o ryzyku:** dokładny format tekstowy `.tres` dla typowanych tablic customowych enumów C#
(`Godot.Collections.Array<AbilityScore>` itd.) nie był weryfikowany w żywym edytorze Godota (Godot
niedostępny w tym środowisku wykonawczym). Poniższy format to najlepsze możliwe odwzorowanie
standardowego zapisu Godota 4 dla typowanych tablic (`Array[int](...)` dla enumów, `Array[String]`
dla stringów). Jeśli po otwarciu w Godot Editorze pola nie wczytają się poprawnie (błąd parsowania
zasobu, puste pola w Inspectorze), popraw ręcznie w Inspectorze i zapisz ponownie — to
udokumentowane ryzyko, nie oznacza błędu w tym zadaniu.

- [ ] **Step 1: Usuń pusty, wcześniejszy plik eksperymentalny**

```bash
rm -f resourses/Warrior.tres
rmdir resourses 2>/dev/null || true
```

- [ ] **Step 2: Stwórz katalog i napisz `resources/classes/Warrior.tres` (Wojownik)**

```
[gd_resource type="Resource" script_class="ClassDefinition" load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/Character/ClassDefinition.cs" id="1"]

[resource]
script = ExtResource("1")
ClassName = "Wojownik"
KeyAbilityOptions = Array[int]([0, 1])
HitPointsPerLevel = 10
TrainedSkills = Array[int]([2])
BonusSkillCount = 3
PerceptionProficiency = 2
FortitudeProficiency = 2
ReflexProficiency = 2
WillProficiency = 1
ClassDcProficiency = 1
UnarmoredProficiency = 1
Level1Features = Array[String](["Attack of Opportunity", "Shield Block"])
```

(Uproszczenie udokumentowane w planie: PF2e RAW pozwala wybrać Acrobatics LUB Athletics jako
umiejętność treningową Wojownika — tu na stałe Athletics (`TrainedSkills = [2]`), żeby nie budować
osobnego mechanizmu wyboru umiejętności analogicznego do `KeyAbilityOptions` w tym pierwszym
sub-projekcie.)

- [ ] **Step 3: Napisz `resources/classes/Wizard.tres` (Czarodziej)**

```
[gd_resource type="Resource" script_class="ClassDefinition" load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/Character/ClassDefinition.cs" id="1"]

[resource]
script = ExtResource("1")
ClassName = "Czarodziej"
KeyAbilityOptions = Array[int]([3])
HitPointsPerLevel = 6
TrainedSkills = Array[int]([1])
BonusSkillCount = 2
PerceptionProficiency = 1
FortitudeProficiency = 1
ReflexProficiency = 1
WillProficiency = 2
ClassDcProficiency = 1
UnarmoredProficiency = 1
Level1Features = Array[String](["Arcane Spellcasting", "Arcane School", "Spellbook"])
```

- [ ] **Step 4: Napisz `resources/classes/Cleric.tres` (Kapłan)**

```
[gd_resource type="Resource" script_class="ClassDefinition" load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/Character/ClassDefinition.cs" id="1"]

[resource]
script = ExtResource("1")
ClassName = "Kapłan"
KeyAbilityOptions = Array[int]([4])
HitPointsPerLevel = 8
TrainedSkills = Array[int]([11])
BonusSkillCount = 2
PerceptionProficiency = 1
FortitudeProficiency = 1
ReflexProficiency = 1
WillProficiency = 2
ClassDcProficiency = 1
UnarmoredProficiency = 1
Level1Features = Array[String](["Divine Spellcasting", "Divine Font", "Doctrine"])
```

- [ ] **Step 5: Napisz `resources/classes/Rogue.tres` (Zbir)**

```
[gd_resource type="Resource" script_class="ClassDefinition" load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/Character/ClassDefinition.cs" id="1"]

[resource]
script = ExtResource("1")
ClassName = "Zbir"
KeyAbilityOptions = Array[int]([1])
HitPointsPerLevel = 8
TrainedSkills = Array[int]([13])
BonusSkillCount = 7
PerceptionProficiency = 2
FortitudeProficiency = 1
ReflexProficiency = 2
WillProficiency = 2
ClassDcProficiency = 1
UnarmoredProficiency = 1
Level1Features = Array[String](["Rogue's Racket (Thief)", "Sneak Attack", "Surprise Attack"])
```

(Uproszczenie udokumentowane w planie: RAW klucz cechy Zbira zależy od wybranego "Racket" —
mechaniki wyboru poza zakresem tego sub-projektu, patrz spec. Tu na stałe Dexterity (`[1]`),
odpowiadające najpopularniejszemu Racketowi "Thief".)

- [ ] **Step 6: Zbuduj projekt (weryfikacja C#, NIE weryfikuje poprawności `.tres` — patrz uwaga o ryzyku wyżej)**

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 7: Commit**

```bash
git add resources/classes/ resourses/
git commit -m "Dodaj dane czterech klas startowych (Wojownik/Czarodziej/Kapłan/Zbir)"
```

---

## Task 9: Kreator postaci — UI

**Files:**
- Create: `scenes/CharacterCreator.tscn`
- Create: `scripts/UI/CharacterCreatorControl.cs`
- Modify: `scenes/MainMenu.tscn`
- Modify: `scripts/MainMenu.cs`

**Interfaces:**
- Consumes: `CharacterCore`, `CharacterSheetRepository` (Task 5, 6), `ClassDefinition` (Task 7),
  `AbilityScore`, `SkillName` (Task 2, 3).

To zadanie jest Godot-zależne (UI) — tylko `dotnet build Tracefinder.csproj`, bez `dotnet test`.
Manualna weryfikacja w Task 10 (Godot niedostępny w tym środowisku wykonawczym).

- [ ] **Step 1: Napisz `scenes/CharacterCreator.tscn`**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/UI/CharacterCreatorControl.cs" id="1"]

[node name="CharacterCreator" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")

[node name="CharacterNames" type="VBoxContainer" parent="."]
layout_mode = 1
anchor_left = 0.05
anchor_top = 0.1
anchor_right = 0.3
anchor_bottom = 0.9
grow_horizontal = 2
grow_vertical = 2

[node name="FormContainer" type="VBoxContainer" parent="."]
layout_mode = 1
anchor_left = 0.35
anchor_top = 0.1
anchor_right = 0.7
anchor_bottom = 0.9
grow_horizontal = 2
grow_vertical = 2

[node name="NameInput" type="LineEdit" parent="FormContainer"]
layout_mode = 2
placeholder_text = "Nazwa postaci"

[node name="StrengthInput" type="LineEdit" parent="FormContainer"]
layout_mode = 2
placeholder_text = "Siła (8-18)"
text = "10"

[node name="DexterityInput" type="LineEdit" parent="FormContainer"]
layout_mode = 2
placeholder_text = "Zręczność (8-18)"
text = "10"

[node name="ConstitutionInput" type="LineEdit" parent="FormContainer"]
layout_mode = 2
placeholder_text = "Kondycja (8-18)"
text = "10"

[node name="IntelligenceInput" type="LineEdit" parent="FormContainer"]
layout_mode = 2
placeholder_text = "Inteligencja (8-18)"
text = "10"

[node name="WisdomInput" type="LineEdit" parent="FormContainer"]
layout_mode = 2
placeholder_text = "Mądrość (8-18)"
text = "10"

[node name="CharismaInput" type="LineEdit" parent="FormContainer"]
layout_mode = 2
placeholder_text = "Charyzma (8-18)"
text = "10"

[node name="ClassOption" type="OptionButton" parent="FormContainer"]
layout_mode = 2

[node name="KeyAbilityOption" type="OptionButton" parent="FormContainer"]
layout_mode = 2

[node name="ErrorLabel" type="Label" parent="FormContainer"]
layout_mode = 2
text = ""
visible = false

[node name="SaveButton" type="Button" parent="FormContainer"]
layout_mode = 2
text = "Zapisz"

[node name="PreviewLabel" type="Label" parent="."]
layout_mode = 1
anchor_left = 0.75
anchor_top = 0.1
anchor_right = 0.98
anchor_bottom = 0.9
grow_horizontal = 2
grow_vertical = 2
text = ""
autowrap_mode = 2

[node name="BackButton" type="Button" parent="."]
layout_mode = 1
anchor_left = 0.05
anchor_top = 0.92
anchor_right = 0.3
anchor_bottom = 0.98
text = "Wróć"
```

- [ ] **Step 2: Napisz `scripts/UI/CharacterCreatorControl.cs`**

```csharp
using System.Collections.Generic;
using System.Linq;
using Godot;
using Tracefinder.Character;

public partial class CharacterCreatorControl : Control
{
    private VBoxContainer _characterNames;
    private LineEdit _nameInput;
    private LineEdit _strengthInput;
    private LineEdit _dexterityInput;
    private LineEdit _constitutionInput;
    private LineEdit _intelligenceInput;
    private LineEdit _wisdomInput;
    private LineEdit _charismaInput;
    private OptionButton _classOption;
    private OptionButton _keyAbilityOption;
    private Label _errorLabel;
    private Button _saveButton;
    private Label _previewLabel;
    private Button _backButton;

    private CharacterSheetRepository _repository;
    private readonly List<ClassDefinition> _classes = new();

    public override void _Ready()
    {
        _characterNames = GetNode<VBoxContainer>("CharacterNames");
        _nameInput = GetNode<LineEdit>("FormContainer/NameInput");
        _strengthInput = GetNode<LineEdit>("FormContainer/StrengthInput");
        _dexterityInput = GetNode<LineEdit>("FormContainer/DexterityInput");
        _constitutionInput = GetNode<LineEdit>("FormContainer/ConstitutionInput");
        _intelligenceInput = GetNode<LineEdit>("FormContainer/IntelligenceInput");
        _wisdomInput = GetNode<LineEdit>("FormContainer/WisdomInput");
        _charismaInput = GetNode<LineEdit>("FormContainer/CharismaInput");
        _classOption = GetNode<OptionButton>("FormContainer/ClassOption");
        _keyAbilityOption = GetNode<OptionButton>("FormContainer/KeyAbilityOption");
        _errorLabel = GetNode<Label>("FormContainer/ErrorLabel");
        _saveButton = GetNode<Button>("FormContainer/SaveButton");
        _previewLabel = GetNode<Label>("PreviewLabel");
        _backButton = GetNode<Button>("BackButton");

        LoadClassDefinitions();
        foreach (ClassDefinition classDef in _classes)
            _classOption.AddItem(classDef.ClassName);

        _repository = new CharacterSheetRepository(ProjectSettings.GlobalizePath("user://characters"));

        _classOption.ItemSelected += _ => RefreshKeyAbilityOptions();
        _saveButton.Pressed += OnSavePressed;
        _backButton.Pressed += OnBackPressed;

        RefreshKeyAbilityOptions();
        RefreshCharacterList();
    }

    private void LoadClassDefinitions()
    {
        string[] paths =
        {
            "res://resources/classes/Warrior.tres",
            "res://resources/classes/Wizard.tres",
            "res://resources/classes/Cleric.tres",
            "res://resources/classes/Rogue.tres"
        };

        foreach (string path in paths)
        {
            var classDef = GD.Load<ClassDefinition>(path);
            if (classDef != null)
                _classes.Add(classDef);
        }
    }

    private void RefreshKeyAbilityOptions()
    {
        _keyAbilityOption.Clear();
        if (_classOption.Selected < 0 || _classOption.Selected >= _classes.Count)
            return;

        ClassDefinition selected = _classes[_classOption.Selected];
        foreach (AbilityScore ability in selected.KeyAbilityOptions)
            _keyAbilityOption.AddItem(ability.ToString());
    }

    private void RefreshCharacterList()
    {
        foreach (Node child in _characterNames.GetChildren())
        {
            _characterNames.RemoveChild(child);
            child.QueueFree();
        }

        foreach (string name in _repository.ListCharacterNames())
        {
            var button = new Button { Text = name };
            button.Pressed += () => LoadIntoForm(name);
            _characterNames.AddChild(button);
        }
    }

    private void LoadIntoForm(string name)
    {
        CharacterCore character = _repository.Load(name);
        _nameInput.Text = character.Name;
        _strengthInput.Text = character.Strength.ToString();
        _dexterityInput.Text = character.Dexterity.ToString();
        _constitutionInput.Text = character.Constitution.ToString();
        _intelligenceInput.Text = character.Intelligence.ToString();
        _wisdomInput.Text = character.Wisdom.ToString();
        _charismaInput.Text = character.Charisma.ToString();

        int classIndex = _classes.FindIndex(c => ClassIdFor(c) == character.ClassId);
        if (classIndex >= 0)
        {
            _classOption.Selected = classIndex;
            RefreshKeyAbilityOptions();
        }

        UpdatePreview(character);
        _errorLabel.Visible = false;
    }

    private void OnSavePressed()
    {
        if (string.IsNullOrWhiteSpace(_nameInput.Text))
        {
            ShowError("Nazwa postaci nie może być pusta.");
            return;
        }

        if (!TryParseAbility(_strengthInput.Text, out int strength) ||
            !TryParseAbility(_dexterityInput.Text, out int dexterity) ||
            !TryParseAbility(_constitutionInput.Text, out int constitution) ||
            !TryParseAbility(_intelligenceInput.Text, out int intelligence) ||
            !TryParseAbility(_wisdomInput.Text, out int wisdom) ||
            !TryParseAbility(_charismaInput.Text, out int charisma))
        {
            ShowError("Każda cecha musi być liczbą w zakresie 8-18.");
            return;
        }

        if (_classOption.Selected < 0 || _classOption.Selected >= _classes.Count)
        {
            ShowError("Wybierz klasę.");
            return;
        }

        ClassDefinition selectedClass = _classes[_classOption.Selected];
        int keyAbilityIndex = System.Math.Max(_keyAbilityOption.Selected, 0);
        AbilityScore selectedKeyAbility = selectedClass.KeyAbilityOptions.Count > keyAbilityIndex
            ? selectedClass.KeyAbilityOptions[keyAbilityIndex]
            : selectedClass.KeyAbilityOptions.FirstOrDefault();

        var character = new CharacterCore
        {
            Name = _nameInput.Text,
            Strength = strength,
            Dexterity = dexterity,
            Constitution = constitution,
            Intelligence = intelligence,
            Wisdom = wisdom,
            Charisma = charisma,
            ClassId = ClassIdFor(selectedClass),
            SelectedKeyAbility = selectedKeyAbility
        };

        _repository.Save(character);
        _errorLabel.Visible = false;
        UpdatePreview(character);
        RefreshCharacterList();
    }

    private void UpdatePreview(CharacterCore character)
    {
        if (_classOption.Selected < 0 || _classOption.Selected >= _classes.Count)
            return;

        ClassStats classStats = _classes[_classOption.Selected].ToClassStats();

        _previewLabel.Text =
            $"HP: {character.ComputeHp(classStats)}\n" +
            $"AC: {character.ComputeAc(classStats)}\n" +
            $"Class DC: {character.ComputeClassDc(classStats)}\n" +
            $"Fortitude: {character.ComputeFortitude(classStats)}\n" +
            $"Reflex: {character.ComputeReflex(classStats)}\n" +
            $"Will: {character.ComputeWill(classStats)}\n" +
            $"Percepcja: {character.ComputePerception(classStats)}";
    }

    private static bool TryParseAbility(string text, out int value)
    {
        return int.TryParse(text, out value) && value >= 8 && value <= 18;
    }

    private static string ClassIdFor(ClassDefinition classDef) => classDef.ClassName.ToLowerInvariant();

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}
```

- [ ] **Step 3: Dodaj przycisk "Postacie" do `MainMenu.tscn`**

W `scenes/MainMenu.tscn`, w węźle `ButtonManager`, dodaj nowy `Button` obok istniejących
`Play`/`Settings`/`Exit`:

```
[node name="Characters" type="Button" parent="ButtonManager"]
layout_mode = 0
offset_left = 760.0
offset_top = 720.0
offset_right = 1136.0
offset_bottom = 768.0
text = "Postacie"
```

- [ ] **Step 4: Podepnij przycisk w `scripts/MainMenu.cs`**

Dodaj nowe pole i podpięcie w `_Ready()`, analogicznie do istniejących `_PlayButton` itd. (dostosuj
dokładne miejsce wstawienia do aktualnej zawartości pliku — dołącz `[Export] private Button
_CharactersButton;` obok pozostałych `[Export]` pól, `_CharactersButton.Pressed +=
OnCharactersButtonPressed;` w `_Ready()`, i nową metodę):

```csharp
private void OnCharactersButtonPressed()
{
    GetTree().ChangeSceneToFile("res://scenes/CharacterCreator.tscn");
}
```

- [ ] **Step 5: Zbuduj projekt**

Run: `dotnet build Tracefinder.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 6: Commit**

```bash
git add scenes/CharacterCreator.tscn scripts/UI/CharacterCreatorControl.cs scenes/MainMenu.tscn scripts/MainMenu.cs
git commit -m "Dodaj kreator postaci (UI) i przycisk Postacie w MainMenu"
```

---

## Task 10: Manualna weryfikacja i dokumentacja

**Files:**
- Modify: `README.md`

Godot Engine może być niedostępny w środowisku wykonawczym (jak we wcześniejszych sub-projektach
tego repo) — jeśli `which godot4`/`which godot`/`which Godot` niczego nie zwraca, implementer
wykonuje tylko Krok 2 (README) i Krok 3 (commit); Krok 1 (manualny test w Godot Editorze) pozostaje
do wykonania przez użytkownika.

- [ ] **Step 1: Manualny scenariusz testowy (do wykonania w Godot Editorze, jeśli dostępny)**

1. Uruchom grę (F5), w MainMenu kliknij "Postacie".
2. Stwórz nową postać: wpisz nazwę, cechy (np. STR 18, DEX 14, CON 16, INT 10, WIS 12, CHA 8),
   wybierz klasę "Wojownik", sprawdź że pole wyboru kluczowej cechy pokazuje "Strength"/"Dexterity"
   do wyboru, wybierz "Strength", kliknij "Zapisz".
3. Sprawdź podgląd statystyk po prawej: HP 13, AC 15, Class DC 17, Fortitude 8, Reflex ?, Will 4,
   Percepcja 6 (dokładnie zgodne z wartościami z testów `CharacterCoreTests`).
4. Sprawdź, że nazwa nowej postaci pojawia się na liście po lewej.
5. Kliknij nazwę postaci na liście — sprawdź, że formularz i podgląd wypełniają się poprawnie
   wczytanymi danymi.
6. Powtórz tworzenie postaci dla każdej z pozostałych 3 klas (Czarodziej, Kapłan, Zbir) — sprawdź,
   że pole wyboru kluczowej cechy pokazuje TYLKO jedną opcję dla tych klas (brak wyboru, w
   odróżnieniu od Wojownika).
7. Kliknij "Wróć" — sprawdź powrót do MainMenu.

- [ ] **Step 2: Zaktualizuj `README.md`**

Dodaj sekcję opisującą kreator postaci, dopasowując się do dotychczasowej struktury/formatu/języka
pliku (przeczytaj obecną treść README najpierw). Uwzględnij: model danych postaci (cechy, klasa,
wyliczane HP/AC/Class DC/saves/percepcja/umiejętności), cztery klasy startowe, definicje klas jako
zasoby `.tres` (łatwe dodawanie kolejnych), świadome uproszczenia (bez ancestry/background/zaklęć/
featów/ekwipunku — patrz spec).

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "Udokumentuj kreator postaci (rdzeń poziomu 1)"
```

---

## Self-Review (przeprowadzony przed zapisaniem planu)

**Pokrycie specu:** model danych (`ClassStats`/`CharacterCore`, Task 4-5) ✓, obliczenia (Task 2, 5)
✓, tworzenie postaci UI (Task 9) ✓, zapis (Task 6) ✓, cztery klasy startowe z poprawnymi danymi z
Player Core (Task 8) ✓, testowanie (Task 1-6) ✓, manualna weryfikacja + dokumentacja (Task 10) ✓.
Wszystkie sekcje specu poza "Poza zakresem" mają odpowiadające zadanie.

**Skan placeholderów:** brak "TBD"/"TODO" — każdy krok zawiera pełny, gotowy do wklejenia kod.
Jedyne świadomie odłożone niepewności (dokładny format `.tres` dla typowanych tablic enumów w
Task 8) są jawnie nazwane jako ryzyko z konkretną instrukcją naprawy, nie ukryte "zrobisz to jakoś".

**Spójność typów:** `ClassStats` (Task 4) używane identycznie w Task 5 (`CharacterCore.Compute*`),
Task 7 (`ClassDefinition.ToClassStats()`), Task 9 (UI). `AbilityScore`/`ProficiencyRank`/`SkillName`
(Task 2-3) używane spójnie we wszystkich kolejnych zadaniach z tymi samymi nazwami. Sygnatury metod
`CharacterCore.Compute*(ClassStats)` zgodne między definicją (Task 5) a użyciem (Task 9).
