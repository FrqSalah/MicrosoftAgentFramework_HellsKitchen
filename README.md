# 🍳 Agent Framework Quick Start - Battle Culinaire

Une démonstration interactive de **multi-agent AI orchestration** avec .NET et OpenAI, mettant en scène une bataille culinaire épique entre deux chefs IA !

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-Latest-239120?logo=c-sharp)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4-412991?logo=openai)
![Spectre.Console](https://img.shields.io/badge/Spectre.Console-0.47.0-5C2D91)

## 🎯 Concept

Ce projet démontre l'orchestration de **4 agents IA distincts** dans un scénario ludique :

- **👨‍🍳 Samy** : Chef créatif et audacieux, spécialiste des recettes orientales
- **👩‍🍳 Marie** : Cuisinière perfectionniste, adepte des recettes classiques raffinées  
- **🤬 Gordon** : Chef validateur exigeant avec accès aux outils (vérification du frigo)
- **👔 Manager** : Décideur final qui prononce promotions et licenciements

## ✨ Fonctionnalités

### Mode Battle Interactif
- 🥊 **Propositions parallèles** : Les deux chefs créent simultanément leurs recettes
- ✅ **Validation stricte** : Gordon vérifie les ingrédients via l'outil `CheckIngredient`
- 🗳️ **Vote public** : Vous choisissez le gagnant (touche `S` pour Samy, `M` pour Marie)
- 🎭 **Décision finale** : Le manager annonce promotions et licenciements style Gordon Ramsay

### Interface Rich Console
- 📊 **Tableaux de bord en temps réel** avec graphiques et statistiques
- 🎨 **Panels colorés** pour chaque agent (cyan pour Samy, magenta pour Marie)
- 📋 **Historique des tentatives** avec suivi des validations
- ⏱️ **Métriques de performance** (durée, taux de succès, nombre de propositions)

### Logique Robuste
- 🔄 **Boucle d'essais** : Maximum 5 tentatives avec compteur visuel
- 💡 **Hints adaptatifs** : Indices automatiques sur la dernière tentative
- 🛠️ **Tool Integration** : Gordon utilise `FridgeTool` pour vérifier le stock

## 🚀 Installation

### Prérequis
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Une clé API OpenAI ([obtenir une clé](https://platform.openai.com/api-keys))

### Configuration

1. **Cloner le repository**
```bash
git clone https://github.com/FrqSalah/AgentFrameworkQuickStart.git
cd AgentFrameworkQuickStart
```

2. **Configurer la clé API**

Créez ou modifiez `appsettings.json` :
```json
{
  "OpenAI": {
    "ApiKey": "VOTRE_CLE_API_ICI"
  }
}
```

3. **Restaurer les packages**
```bash
dotnet restore
```

4. **Lancer l'application**
```bash
dotnet run
```

## 📦 Dépendances

| Package | Version | Usage |
|---------|---------|-------|
| `Azure.AI.OpenAI` | 2.5.0-beta.1 | Client OpenAI pour Azure |
| `Microsoft.Agents.AI.OpenAI` | 1.0.0-preview | Framework multi-agents |
| `Microsoft.Extensions.Configuration` | 9.0.0 | Gestion de la configuration |
| `Spectre.Console` | 0.47.0 | Interface console enrichie |

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     ORCHESTRATEUR                            │
│                      (Program.cs)                            │
└────────┬────────────────────────────────────────────────────┘
         │
         ├──► 👨‍🍳 Agent Samy (Creative)
         │         └─► Génère recettes orientales
         │
         ├──► 👩‍🍳 Agent Marie (Classic)  
         │         └─► Génère recettes classiques
         │
         ├──► 🤬 Agent Gordon (Validator)
         │         └─► Vérifie via FridgeTool
         │              └─► CheckIngredient()
         │
         └──► 👔 Agent Manager (Decision Maker)
                   └─► Promotions / Licenciements
```

## 🎮 Exemple d'Utilisation

```
⛔ BATTLE CULINAIRE - SAMY vs MARIE ⛔
Frigo: Pâtes, Beurre, Ketchup, Pain, Fromage, Poulet

Tentative 1/5
┌─────────────────────────────────┐ ┌─────────────────────────────────┐
│ 👨‍🍳 SAMY                        │ │ 👩‍🍳 MARIE                       │
│ Tajine de Poulet aux Épices...  │ │ Poulet Rôti aux Herbes...       │
└─────────────────────────────────┘ └─────────────────────────────────┘

✓ Samy validé par Gordon !
✓ Marie validée par Gordon !

🏆 Vote Public - Samy vs Marie
Appuyez sur S pour 'SAMY' ou sur M pour 'MARIE'

📊 DASHBOARD DE SESSION
┌──────────────────────────────┬──────────┐
│ ⏱️  Durée totale             │    12.3s │
│ 🔄 Tentatives                │      1/5 │
│ ✅ Samy - Taux succès        │     100% │
│ ✅ Marie - Taux succès       │     100% │
│ 🏆 Gagnant                   │     SAMY │
└──────────────────────────────┴──────────┘
```

## 🧩 Structure du Code

```
AgentFrameworkQuickStart/
│
├── Program.cs              # Orchestration principale & UI
├── FridgeTool.cs          # Outil de vérification du stock
├── appsettings.json       # Configuration (API Key)
├── AgentFrameworkQuickStart.csproj
└── README.md
```

### Concepts Clés

**Multi-Agent Pattern**
```csharp
var samy = client.GetChatClient(modelId).CreateAIAgent(
    instructions: "Tu es Samy, un cuisinier CRÉATIF..."
);

var gordon = client.GetChatClient(modelId).CreateAIAgent(
    instructions: "Tu es le Chef Gordon...",
    tools: [AIFunctionFactory.Create(new FridgeTool().CheckIngredient)]
);
```

**Parallel Execution**
```csharp
Task<AgentRunResponse>? samyTask = samy.RunAsync(context);
Task<AgentRunResponse>? marieTask = marie.RunAsync(context);
await Task.WhenAll(samyTask, marieTask);
```

**Tool Integration**
```csharp
[Description("Vérifie si un ingrédient est disponible")]
public string CheckIngredient([Description("Nom de l'ingrédient")] string ingredient)
{
    var stock = new List<string> { "pâtes", "poulet", "fromage" };
    return stock.Contains(ingredient.ToLower()) ? "DISPONIBLE" : "RUPTURE DE STOCK";
}
```

## 🎯 Cas d'Usage

Ce projet illustre :
- ✅ **Agent Orchestration** : Coordination de multiples IA avec rôles distincts
- ✅ **Tool Calling** : Intégration d'outils externes (FridgeTool)
- ✅ **Human-in-the-Loop** : Interaction utilisateur dans le workflow IA
- ✅ **Parallel Processing** : Exécution simultanée de tâches IA
- ✅ **State Management** : Suivi de l'état entre itérations
- ✅ **Rich UX** : Interface console moderne et interactive

## 🔮 Améliorations Possibles

- [ ] **Streaming responses** : Affichage en temps réel des réponses
- [ ] **Cost tracking** : Suivi du coût des appels API
- [ ] **Retry logic** : Gestion automatique des échecs
- [ ] **Multiple rounds** : Plusieurs manches de battle
- [ ] **Persistent history** : Sauvegarde des sessions
- [ ] **Custom judges** : Ajout d'autres agents validateurs

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :
1. Forker le projet
2. Créer une branche (`git checkout -b feature/AmazingFeature`)
3. Commiter vos changements (`git commit -m 'Add AmazingFeature'`)
4. Pusher vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

## 📝 License

Ce projet est sous licence MIT - voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 👤 Auteur

**Salah** - [@FrqSalah](https://github.com/FrqSalah)

## 🙏 Remerciements

- Microsoft Agents Framework Team
- OpenAI pour l'API GPT
- Spectre.Console pour l'UI enrichie
- La communauté .NET

---

⭐ Si ce projet vous a aidé, n'hésitez pas à lui donner une étoile !
