using System.Collections.Generic;

public enum Ingredient
{
    Potato,
    Tomato,
    Pepper,
    Mushroom,
    DraculaWing,
    MerewolfSteak,
    ManticoreTail
}

public enum Recipe
{
    PepperSteak,
    DragulaCacciotare,
    ManticoreRisotto
}

public static class Recipes
{
    public static readonly Dictionary<Recipe, List<Ingredient>> RecipeIngredients = new()
    {
        { Recipe.PepperSteak, new List<Ingredient> { Ingredient.Pepper, Ingredient.MerewolfSteak } },
        { Recipe.DragulaCacciotare, new List<Ingredient> { Ingredient.Pepper, Ingredient.DraculaWing } },
        { Recipe.ManticoreRisotto, new List<Ingredient> { Ingredient.Pepper, Ingredient.ManticoreTail } }
    };
}