using System.ComponentModel;

// --- 1. L'Outil MCP : "Le Frigo" ---
// Seul le Chef a le droit d'ouvrir le frigo.
public class FridgeTool
{
    [Description("Vérifie si un ingrédient est disponible dans le frigo.")]
    public string CheckIngredient([Description("Nom de l'ingrédient")] string ingredient)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[TOOL - FRIGO] Vérification du stock pour : {ingredient}...");
        Console.ResetColor();

        // Simulation du stock
        var stock = new List<string> { "oeufs", "lait", "farine", "chocolat", "tomate", "pâtes" };
        
        if (stock.Contains(ingredient.ToLower()))
            return "DISPONIBLE";
        
        return "RUPTURE DE STOCK";
    }
}