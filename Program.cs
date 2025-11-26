using System;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using Spectre.Console;
using Spectre.Console.Rendering;

// Charger la configuration depuis appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var apiKey = configuration["OpenAI:ApiKey"];
if (string.IsNullOrEmpty(apiKey) || apiKey == "VOTRE_CLE_API_ICI")
{
    Console.WriteLine("Erreur: Veuillez configurer votre clé API dans appsettings.json");
    return;
}

var client = new OpenAIClient(apiKey);
string modelId = "gpt-5-nano";

// Fonction locale pour afficher un message stylé
void RenderMessage(string role, string text, string color)
{
    var safe = text?.Replace("[", "[[").Replace("]", "]]") ?? string.Empty;
    Color MapColor(string name) => name.ToLower() switch
    {
        "red" => Color.Red,
        "green" => Color.Green,
        "yellow" => Color.Yellow,
        // Couleurs approximatives pour cyan/magenta (non prédéfinies)
        "cyan" => new Color(0, 255, 255),
        "magenta" => new Color(255, 0, 255),
        _ => new Color(128, 128, 128),
    };
    var panel = new Panel(new Markup(safe))
        .Header($"[bold]{role}[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(MapColor(color));
    AnsiConsole.Write(panel);
}

// --- AGENTS ---
var samy = client.GetChatClient(modelId).CreateAIAgent(
    instructions: "Tu es Samy, un cuisinier CRÉATIF et audacieux. " +
                    "Tu proposes des recettes orientales. " +
                    "Propose juste le nom de la recette et la liste des ingrédients."
);

var marie = client.GetChatClient(modelId).CreateAIAgent(
    instructions: "Tu es Marie, une cuisinière PERFECTIONNISTE et technique. " +
                    "Tu proposes des recettes classiques et élégantes. " +
                    "Propose juste le nom de la recette et la liste des ingrédients."
);

var gordon = client.GetChatClient(modelId).CreateAIAgent(
    instructions: "Tu es le Chef Gordon. Tu es EXIGEANT. " +
                  "1. Vérifie l'ingrédient principal avec l'outil CheckIngredient. " +
                  "2. Si MANQUANT -> Refuse violemment. " +
                  "3. Si OK -> Dis 'VALIDÉ TECHNIQUEMENT' et demande l'avis du client.",
    tools: [AIFunctionFactory.Create(new FridgeTool().CheckIngredient)]
);

var manager = client.GetChatClient(modelId).CreateAIAgent(
    instructions: "Tu es le Manager du restaurant. Tu as le dernier mot sur l'équipe. " +
                  "Si le gagnant est félicité -> Annonce sa promotion avec enthousiasme. " +
                  "Si le perdant est mentionné -> Vire-le avec une punchline cinglante style Gordon Ramsay. " +
                  "Sois théâtral et mémorable !"
);

AnsiConsole.Write(new Rule("[yellow]⛔ BATTLE CULINAIRE - SAMY vs MARIE ⛔[/]").LeftJustified());
AnsiConsole.MarkupLine("[grey]Frigo:[/] [bold]Pâtes, Beurre, Ketchup, Pain, Fromage, Poulet[/]\n");

// Variables de tracking
int attempts = 0;
int maxAttempts = 5;
bool samyValidated = false;
bool marieValidated = false;
string context = "Invente une recette spéciale DevFest.";
string samyProposal = "";
string marieProposal = "";
var proposalHistory = new List<(string chef, string recipe, bool validated)>();
var startTime = DateTime.Now;

// BOUCLE TECHNIQUE - MODE BATTLE
while ((!samyValidated || !marieValidated) && attempts < maxAttempts)
{
    attempts++;
    
    // Afficher le compteur de tentatives
    var attemptBar = new BarChart()
        .Width(60)
        .Label($"[bold]Tentative {attempts}/{maxAttempts}[/]")
        .CenterLabel()
        .AddItem("Samy Validé", proposalHistory.Count(h => h.chef == "Samy" && h.validated), Color.Cyan1)
        .AddItem("Marie Validée", proposalHistory.Count(h => h.chef == "Marie" && h.validated), Color.Magenta1);
    AnsiConsole.Write(attemptBar);
    AnsiConsole.WriteLine();

    // Si c'est la dernière tentative, donner des indices sur les ingrédients disponibles
    var contextWithHint = context;
    if (attempts == maxAttempts)
    {
        contextWithHint += " ATTENTION : Dernière chance ! Utilise ces ingrédients disponibles : Pâtes, Tomates ou Chocolat.";
        AnsiConsole.MarkupLine("[yellow]⚠️  Dernière tentative - Indices fournis aux chefs ![/]");
    }

    // Les deux cuisiniers proposent en parallèle
    Task<AgentRunResponse>? samyTask = !samyValidated ? samy.RunAsync(contextWithHint) : null;
    Task<AgentRunResponse>? marieTask = !marieValidated ? marie.RunAsync(contextWithHint) : null;
    
    var tasksToWait = new List<Task>();
    if (samyTask != null) tasksToWait.Add(samyTask);
    if (marieTask != null) tasksToWait.Add(marieTask);
    if (tasksToWait.Count > 0) await Task.WhenAll(tasksToWait);
    
    // Afficher les propositions côte-à-côte
    var panels = new List<Panel>();
    
    if (!samyValidated && samyTask != null)
    {
        samyProposal = samyTask.Result.Messages.LastOrDefault()?.Text ?? "";
        var samyPanel = new Panel(new Markup(samyProposal.Replace("[", "[[").Replace("]", "]]")))
            .Header("[bold cyan1]👨‍🍳 SAMY[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(new Color(0, 255, 255));
        panels.Add(samyPanel);
    }
    
    if (!marieValidated && marieTask != null)
    {
        marieProposal = marieTask.Result.Messages.LastOrDefault()?.Text ?? "";
        var mariePanel = new Panel(new Markup(marieProposal.Replace("[", "[[").Replace("]", "]]")))
            .Header("[bold magenta1]👩‍🍳 MARIE[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(new Color(255, 0, 255));
        panels.Add(mariePanel);
    }
    
    if (panels.Count > 0)
    {
        AnsiConsole.Write(new Columns(panels.ToArray()));
    }
    
    // Gordon valide les propositions
    if (!samyValidated && !string.IsNullOrEmpty(samyProposal))
    {
        var samyCritique = await gordon.RunAsync($"Analyse la recette de SAMY : \"{samyProposal}\". Vérifie le stock !");
        var samyCritiqueText = samyCritique.Messages.LastOrDefault()?.Text ?? "";
        
        if (samyCritiqueText.Contains("VALIDÉ"))
        {
            samyValidated = true;
            proposalHistory.Add(("Samy", samyProposal, true));
            AnsiConsole.MarkupLine("[green]✓ Samy validé par Gordon ![/]");
        }
        else
        {
            proposalHistory.Add(("Samy", samyProposal, false));
            AnsiConsole.MarkupLine("[red]✗ Samy refusé par Gordon[/]");
        }
    }
    
    if (!marieValidated && !string.IsNullOrEmpty(marieProposal))
    {
        var marieCritique = await gordon.RunAsync($"Analyse la recette de MARIE : \"{marieProposal}\". Vérifie le stock !");
        var marieCritiqueText = marieCritique.Messages.LastOrDefault()?.Text ?? "";
        
        if (marieCritiqueText.Contains("VALIDÉ"))
        {
            marieValidated = true;
            proposalHistory.Add(("Marie", marieProposal, true));
            AnsiConsole.MarkupLine("[green]✓ Marie validée par Gordon ![/]");
        }
        else
        {
            proposalHistory.Add(("Marie", marieProposal, false));
            AnsiConsole.MarkupLine("[red]✗ Marie refusée par Gordon[/]");
        }
    }
    
    AnsiConsole.WriteLine();
}

// Afficher l'historique des tentatives
if (proposalHistory.Count > 1)
{
    AnsiConsole.WriteLine();
    var historyTable = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey)
        .AddColumn(new TableColumn("[bold]#[/]").Centered())
        .AddColumn(new TableColumn("[bold]Chef[/]").Centered())
        .AddColumn(new TableColumn("[bold]Recette Proposée[/]"))
        .AddColumn(new TableColumn("[bold]Statut[/]").Centered());
    
    for (int i = 0; i < proposalHistory.Count; i++)
    {
        var (chef, recipe, validated) = proposalHistory[i];
        var status = validated ? "[green]✓ Validé[/]" : "[red]✗ Refusé[/]";
        var shortRecipe = recipe.Length > 40 ? recipe.Substring(0, 37) + "..." : recipe;
        var chefColor = chef == "Samy" ? "[cyan1]" : "[magenta1]";
        historyTable.AddRow($"{i + 1}", $"{chefColor}{chef}[/]", shortRecipe, status);
    }
    
    AnsiConsole.Write(new Panel(historyTable)
        .Header("[bold yellow]📋 Historique des Propositions[/]")
        .BorderColor(Color.Yellow));
    AnsiConsole.WriteLine();
}

// --- VOTE PUBLIC : SAMY vs MARIE ---
AnsiConsole.Write(new Rule("[bold yellow]🏆 Vote Public - Samy vs Marie[/]").Centered());
AnsiConsole.MarkupLine("🗣️ [bold]Quelle recette préférez-vous ?[/]");
AnsiConsole.MarkupLine("Appuyez sur [bold cyan1]S[/] pour '[cyan1]SAMY[/]' ou sur [bold magenta1]M[/] pour '[magenta1]MARIE[/]' .");
AnsiConsole.Markup("[grey italic]En attente de votre choix (G ou M)...[/]\n");
string? winner = null;
while (winner is null)
{
    var key = Console.ReadKey(intercept: true);
    if (key.Key == ConsoleKey.S || key.KeyChar == 's' || key.KeyChar == 'S')
    {
        winner = "Samy";
        AnsiConsole.MarkupLine("[cyan1]✓ SAMY sélectionné ![/]");
    }
    else if (key.Key == ConsoleKey.M || key.KeyChar == 'm' || key.KeyChar == 'M')
    {
        winner = "Marie";
        AnsiConsole.MarkupLine("[magenta1]✓ MARIE sélectionnée ![/]");
    }
    else
    {
        AnsiConsole.MarkupLine($"[grey]Touche invalide. Appuyez sur G ou M.[/]");
    }
}

var loser = winner == "Samy" ? "Marie" : "Samy";
var winningRecipe = winner == "Samy" ? samyProposal : marieProposal;
var losingRecipe = winner == "Samy" ? marieProposal : samyProposal;

// Manager annonce le résultat
var managerWinResponse = await manager.RunAsync($"Le public a choisi {winner} avec la recette : \"{winningRecipe}\". Félicite {winner} et annonce sa promotion !");
var managerWinMessage = managerWinResponse.Messages.LastOrDefault()?.Text ?? $"Bravo {winner} !";
RenderMessage($"🏆 MANAGER - GAGNANT", managerWinMessage, "green");

var managerLoseResponse = await manager.RunAsync($"{loser} a perdu avec : \"{losingRecipe}\". Vire {loser} avec style !");
var managerLoseMessage = managerLoseResponse.Messages.LastOrDefault()?.Text ?? $"{loser}, tu es viré !";
RenderMessage($"🚫 MANAGER - PERDANT", managerLoseMessage, "red");

// Dashboard final
var endTime = DateTime.Now;
var duration = (endTime - startTime).TotalSeconds;

AnsiConsole.WriteLine();
var statsTable = new Table()
    .Border(TableBorder.Heavy)
    .BorderColor(Color.Blue)
    .AddColumn(new TableColumn("[bold blue]Métrique[/]"))
    .AddColumn(new TableColumn("[bold blue]Valeur[/]").RightAligned());

statsTable.AddRow("⏱️  Durée totale", $"{duration:F1}s");
statsTable.AddRow("🔄 Tentatives", $"{attempts}/{maxAttempts}");
statsTable.AddRow("✅ Samy - Taux succès", $"{(proposalHistory.Count(h => h.chef == "Samy" && h.validated) * 100.0 / Math.Max(1, proposalHistory.Count(h => h.chef == "Samy"))):F0}%");
statsTable.AddRow("✅ Marie - Taux succès", $"{(proposalHistory.Count(h => h.chef == "Marie" && h.validated) * 100.0 / Math.Max(1, proposalHistory.Count(h => h.chef == "Marie"))):F0}%");
statsTable.AddRow("👨‍🍳 Agent Samy", $"{proposalHistory.Count(h => h.chef == "Samy")} propositions");
statsTable.AddRow("👩‍🍳 Agent Marie", $"{proposalHistory.Count(h => h.chef == "Marie")} propositions");
statsTable.AddRow("🤬 Agent Gordon", $"{proposalHistory.Count} validations");
statsTable.AddRow("👔 Agent Manager", "2 décisions finales");
statsTable.AddRow("🏆 Gagnant", winner == "Samy" ? "[cyan1]SAMY[/]" : "[magenta1]MARIE[/]");

AnsiConsole.Write(new Panel(statsTable)
    .Header("[bold blue]📊 DASHBOARD DE SESSION[/]")
    .BorderColor(Color.Blue));

AnsiConsole.Write(new Rule("[grey]Fin de la démo[/]").LeftJustified());
