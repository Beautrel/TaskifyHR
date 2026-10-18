# Taskify HR

Application mobile légère de **pointage géolocalisé** et de **gestion des congés** pour les PME.
Projet d'examen pratique – Atelier de développement mobile (Niv 5 Génie Logiciel, 2026/2027).

> **État du projet :** front end complet (9 écrans) fonctionnant avec des **données fictives en mémoire**.
> Il n'y a pas encore de serveur : les services sont remplacés par des « mocks » (voir *Architecture*).

## Fonctionnalités

| Fonctionnalité | Écran(s) |
|---|---|
| Connexion, création de compte (validation en direct) | Connexion, Inscription |
| Pointage géolocalisé (position demandée uniquement au clic) | Accueil |
| Demande de congé (jours ouvrés, solde, anti-doublon) | Demande de congé |
| Solde de congés et historique des demandes | Solde |
| Planning mensuel (en attente en orange, approuvé en vert) | Planning |
| Validation hiérarchique (refus avec motif obligatoire) | Validation manager |
| Profil (seul le téléphone est modifiable), déconnexion confirmée | Profil |
| Export mensuel en PDF ou CSV, avec partage | Export mensuel |

Chaque écran gère ses états : chargement, erreur, vide, succès.

## Technologies

- .NET MAUI (Android), C#, XAML
- Architecture MVVM avec `CommunityToolkit.Mvvm` (seul package ajouté)
- Aucune autre bibliothèque : le PDF est généré par du code maison (`Services/SimplePdf.cs`)

## Lancer le projet

Prérequis : SDK .NET avec la charge de travail MAUI (`dotnet workload install maui`) et un téléphone Android en débogage USB.

```powershell
dotnet restore
adb devices
dotnet build -t:Run -f net10.0-android
```

Remplacer `net10.0` par la version affichée dans `TaskifyHR.csproj` si elle est différente.

## Comptes de démonstration

Mot de passe commun : `Taskify2026!`

| Compte | Rôle |
|---|---|
| `lea.martin@taskify.fr` | Employée |
| `sophie.bernard@taskify.fr` | Manager (valide les demandes, exporte) |

Un compte créé depuis l'écran Inscription fonctionne aussi tant que l'application n'est pas fermée.

## Architecture

```
TaskifyHR/
├─ Models/        données (utilisateur, demande de congé, solde, rapport...)
├─ Services/      interfaces + implémentations fictives (MockServices.cs),
│                 géolocalisation, génération CSV/PDF, partage
├─ ViewModels/    la logique de chaque écran (règles métier, validation, états)
├─ Views/         les pages XAML (affichage uniquement)
├─ Controls/      composants réutilisables : StateView, StatusBadge
├─ Helpers/       jours ouvrés, formats de dates en français
└─ Resources/Styles/  Colors.xaml et Styles.xaml : la charte graphique
```

- **MVVM** : une page XAML affiche, un ViewModel décide. Les liaisons (`{Binding ...}`) les relient.
- **Services par interfaces** : pour brancher une vraie API, il suffit de remplacer les classes `Mock...` dans `MauiProgram.cs`. Les écrans ne changent pas.
- **Règles métier dans le service** (solde vérifié à l'approbation, refus sans motif rejeté, anti-doublon) et pas seulement dans l'interface.

## Choix d'éco-conception

- Géolocalisation demandée uniquement au clic de pointage, jamais en suivi continu.
- Police système (Roboto), aucune image décorative, aucune bibliothèque externe.
- Un seul jeu de composants et de styles réutilisés sur tous les écrans.
- Données chargées à l'affichage de chaque écran, listes courtes, calendrier mois par mois.

## Tester les états d'erreur

Les services fictifs ont des interrupteurs à passer temporairement à `true` dans `Services/MockServices.cs` et `Services/FileExportService.cs` :

| Interrupteur | Effet |
|---|---|
| `MockAttendanceService.FailNext` | erreur réseau au pointage |
| `MockLeaveService.FailNext` | erreur de chargement, d'envoi ou de décision |
| `MockLeaveService.SimulateZeroBalance` | solde à zéro |
| `MockLeaveService.SimulateEmptyList` / `SimulateNoPending` | listes vides |
| `MockAuthService.FailNext` | erreur à l'inscription et à l'enregistrement du profil |
| `FileExportService.FailNext` | échec de l'export |
| `DeviceLocationService.DemoMode` | `false` pour activer la vérification de zone |

## Limites connues

- Données fictives : rien n'est conservé quand l'application est fermée.
- Les mots de passe sont gardés en clair **en mémoire** pour la démonstration ; une vraie API ne stocke qu'un hash.
- L'export ne contient pas les pointages (l'historique n'existe pas encore).
- Le planning n'affiche que les congés de l'utilisateur, pas ceux de l'équipe.
- Pas de thème sombre ; pas de tests automatisés ; Android uniquement.

## Évolutions prévues

API réelle (authentification, congés, pointages), historique des pointages dans l'export, planning d'équipe, thème sombre, tests unitaires des règles métier.
