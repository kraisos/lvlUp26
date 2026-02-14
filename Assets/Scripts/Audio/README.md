# Systeme Audio — Le Fourneau

## Architecture

```
Scripts/Audio/
  AudioManager.cs        Singleton — musique d'ambiance (crossfade)
  StoryAudioManager.cs   Singleton — narration / dialogues
  StoryEntry.cs          ScriptableObject — donnees d'une ligne narrative
  StoryTrigger.cs        Composant — declencheur spatial (collider)
```

---

## Setup rapide

### 1. Placer les managers dans la scene

Les prefabs sont dans `Prefabs/Audio/`. Glisse-les dans la scene :

- **AudioManager** — gere la musique de fond
- **StoryAudioManager** — gere les voix / narration

> Ils utilisent `DontDestroyOnLoad`, donc place-les dans ta scene de depart uniquement.

### 2. Configurer AudioManager (Inspector)

| Champ              | Description                                      |
|--------------------|--------------------------------------------------|
| Default Music      | Musique jouee au lancement (ex: Oil Lamp in the Ruins) |
| Chase Music        | Musique pendant une poursuite (ex: Relentless Pursuit) |
| Exploration Music  | Musique d'exploration (ex: Iron Cathedral)        |
| Boss Music         | Musique de boss (ex: Corrupted Shift Change)      |
| Music Volume       | Volume global musique (0-1)                       |

Les AudioSources sont crees automatiquement si non assignes.

### 3. Creer des Story Entries

Clic droit dans `Assets/Audio/StoryEntries/` :

```
Create > Audio > Story Entry
```

Remplir les champs :

| Champ         | Exemple                          | Description                          |
|---------------|----------------------------------|--------------------------------------|
| Entry Id      | `01_reveil`                      | Identifiant unique                   |
| Voice Id      | `V1`                             | V1=Joueur, V2=Vasseur, V3=Morel...  |
| Audio Clip    | `01.mp3`                         | Le fichier audio (drag & drop)       |
| Trigger Type  | `FirstSpawn`                     | Quel evenement declenche ce dialogue |
| Play Once     | `true`                           | Ne jouer qu'une seule fois           |
| Priority      | `5`                              | Plus haut = plus important (0-10)    |
| Subtitle Text | `"Ou est-ce que... Non..."`      | Texte pour futurs sous-titres        |

### 4. Assigner les entries au StoryAudioManager

Selectionne le GameObject **StoryAudioManager** dans la scene, puis :
- Dans le champ **Story Entries** (liste), ajoute toutes tes Story Entry assets

---

## Triggers disponibles (StoryTriggerType)

Chaque trigger correspond a un moment du script narratif :

| Trigger               | Quand                                          | Acte |
|-----------------------|------------------------------------------------|------|
| `FirstSpawn`          | Premier spawn du joueur                         | 1    |
| `IdleAtBase`          | Joueur reste 20s a la base sans bouger          | 1    |
| `LookAtFurnace`      | Joueur regarde le fourneau au loin              | 1    |
| `FirstExit`           | Joueur quitte la zone eclairee (1ere fois)      | 2    |
| `EnterBuilding`       | Joueur entre dans un batiment (1ere fois)       | 2    |
| `PickupNote`          | Ramasse une note audio                          | 2-5  |
| `PlaceLamp`           | Pose la premiere lampe                          | 2    |
| `WorldRearrange`      | Geographie change                               | 2    |
| `FirstCreatureSeen`   | Premiere creature apercue                       | 3    |
| `CreatureKilledByLight` | Creature dissoute par la lumiere              | 3    |
| `FirstChase`          | Premiere poursuite                              | 3    |
| `SurviveChase`        | Survie apres premiere chase                     | 3    |
| `FirstDeath`          | Premiere mort                                   | 3    |
| `DeathRespawn`        | Morts suivantes (lignes aleatoires)             | rec. |
| `PickupEnergy`        | Ramasse un noyau d'energie                      | rec. |
| `PickupGas`           | Ramasse du combustible                          | rec. |
| `PickupBlueprint`     | Ramasse un schema                               | rec. |
| `PlaceLampRecurrent`  | Pose une lampe (occasionnel)                    | rec. |
| `LongDarkness`        | Trop longtemps dans le noir                     | rec. |
| `FirstDash`           | Premier dash                                    | rec. |
| `MeetLeFondeur`       | Voit Le Fondeur (1ere fois)                     | 4    |
| `MeetLeRampant`       | Voit Les Rampants (1ere fois)                   | 4    |
| `MeetLaSentinelle`    | Repere par La Sentinelle (1ere fois)            | 4    |
| `ReturnToBase`        | Retour a la base apres longue exploration       | 4    |
| `BossEncounter`       | L'Amalgame apparait                             | 5    |
| `ReachFurnace`        | Arrive au fourneau                              | 6    |
| `ActivateFurnace`     | Active le fourneau                              | 6    |
| `FurnaceLit`          | Le fourneau s'allume (cinematique)              | 6    |
| `SkyClears`           | Le brouillard se dissipe                        | 6    |
| `Epilogue`            | Ecran final / credits                           | 6    |
| `Custom`              | Declenchement manuel via code                   | -    |

---

## Utilisation depuis le code

### Changer la musique

```csharp
// Crossfade vers la musique de chase (0.5s)
AudioManager.Instance.PlayChaseMusic();

// Retour a la musique par defaut
AudioManager.Instance.PlayDefaultMusic();

// Musique custom
AudioManager.Instance.PlayMusic(monClip, 2f);

// Couper la musique
AudioManager.Instance.StopMusic(1f);

// Changer le volume
AudioManager.Instance.SetMusicVolume(0.7f);
```

### Declencher une narration

```csharp
// Par trigger type (cherche la bonne entry automatiquement)
StoryAudioManager.Instance.TriggerStory(StoryTriggerType.FirstDeath);

// Verifier si une narration est en cours
if (StoryAudioManager.Instance.IsPlaying) { ... }

// Couper la narration
StoryAudioManager.Instance.StopNarration();

// Reset (nouvelle partie)
StoryAudioManager.Instance.ResetPlayedEntries();
```

### Ecouter les events narration (pour UI sous-titres)

```csharp
void OnEnable()
{
    StoryAudioManager.Instance.OnNarrationStarted += ShowSubtitle;
    StoryAudioManager.Instance.OnNarrationFinished += HideSubtitle;
}

void ShowSubtitle(StoryEntry entry)
{
    subtitleText.text = entry.subtitleText;
}

void HideSubtitle(StoryEntry entry)
{
    subtitleText.text = "";
}
```

### Trigger spatial (zone dans la scene)

1. Cree un GameObject vide
2. Ajoute un **Collider** (Box, Sphere...) avec `Is Trigger = true`
3. Ajoute le composant **StoryTrigger**
4. Choisis le `Trigger Type` dans le dropdown
5. Le joueur entre dans la zone = narration declenchee

---

## Lignes recurrentes (selection aleatoire)

Pour les dialogues qui varient (mort, ramassage...), cree **plusieurs StoryEntry**
avec le meme `Trigger Type` et `playOnce = false` :

```
StoryEntry: "31a_mort"  → TriggerType: DeathRespawn, playOnce: false
StoryEntry: "31b_mort"  → TriggerType: DeathRespawn, playOnce: false
StoryEntry: "31c_mort"  → TriggerType: DeathRespawn, playOnce: false
```

Le systeme en choisira une au hasard a chaque declenchement.

---

## Music Ducking

Quand une narration joue, la musique baisse automatiquement au volume
defini par `Music Duck Volume` (par defaut 0.2) et revient a la normale
quand la narration finit.

---

## Fichiers audio actuels

```
Assets/music/
  Corrupted Shift Change.mp3         → bossMusic
  Iron Cathedral at the Edge of Night.mp3  → explorationMusic
  Oil Lamp in the Ruins.mp3          → defaultMusic
  Relentless Pursuit.mp3             → chaseMusic

Assets/music/story/
  01.mp3 ... 17.mp3                  → narration (17/38 generes)
```

---

## Voix (reference info.md)

| ID | Personnage              | Style                           |
|----|-------------------------|---------------------------------|
| V1 | Le Joueur               | Homme ~35 ans, rauque, murmure  |
| V2 | Contremaitre Vasseur    | Homme ~55 ans, autoritaire      |
| V3 | Ingenieure Morel        | Femme ~40 ans, scientifique     |
| V4 | Ouvrier panique         | Homme ~25 ans, essoufle         |
| V5 | Directeur Arnaud        | Homme ~50 ans, froid            |
| V6 | Dernier Ouvrier         | Homme ~45 ans, voix brisee      |
