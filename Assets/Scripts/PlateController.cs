using Cookverse.Assets.Scripts;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlateController : IngredientSlotBehaviour, IDualAnchorIngredientSlot
{
    [Header("Recipe")]
    [SerializeField] public Recipe recipe;
    private List<Ingredient> requiredIngredients;

    [Header("Placement")]
    [Tooltip("Where the protein ingredient snaps to (create an empty child and assign it).")]
    public Transform proteinAnchor;
    [Tooltip("Where the vegetable ingredient snaps to (create an empty child and assign it).")]
    public Transform vegetableAnchor;
    [Tooltip("How close an ingredient must be (to the anchor) to snap.")]
    public float snapRange = 0.8f;

    private KitchenIngredientController proteinIngredient;
    private KitchenIngredientController vegetableIngredient;

    [Header("Recipe Completion TMP")]
    [SerializeField] public TextMeshProUGUI completionText;


    public Transform ProteinAnchor => proteinAnchor;
    public Transform VegetableAnchor => vegetableAnchor;
    public override float SnapRange => snapRange;

    public Transform GetProteinAnchor() => proteinAnchor != null ? proteinAnchor : transform;
    public Transform GetVegetableAnchor() => vegetableAnchor != null ? vegetableAnchor : transform;

    public void Start()
    {
        requiredIngredients = recipe != null ? Recipes.RecipeIngredients[recipe] : new List<Ingredient>();
    }
    public override bool CanAcceptIngredient(KitchenIngredientController ingredient)
    {
        if (ingredient == null) return false;
        if (!ingredient.IsCooked()) return false;

        bool isProtein = ingredient.IsProteinIngredient;
        bool isVegetable = ingredient.IsVegetableIngredient;

        if (isProtein && !HasProteinIngredient()) return true;
        if (isVegetable && !HasVegetableIngredient()) return true;

        return false;
    }

    private bool HasProteinIngredient()
    {
        return proteinIngredient != null;
    }

    private bool HasVegetableIngredient()
    {
        return vegetableIngredient != null;
    }

    public override float DistanceToAnchor(Vector3 worldPos, KitchenIngredientController ingredient)
    {
        if (ingredient == null) return float.PositiveInfinity;
        Transform anchor = ingredient.IsProteinIngredient ? GetProteinAnchor() : GetVegetableAnchor();
        return Vector3.Distance(worldPos, anchor.position);
    }

    public override bool IsWithinSnapRange(Vector3 ingredientWorldPos)
    {
        float proteinDistance = Vector3.Distance(ingredientWorldPos, GetProteinAnchor().position);
        float vegetableDistance = Vector3.Distance(ingredientWorldPos, GetVegetableAnchor().position);
        return proteinDistance <= snapRange || vegetableDistance <= snapRange;
    }

    public override bool TryPlaceIngredient(KitchenIngredientController ingredient)
    {
        if (ingredient == null) return false;
        if (!ingredient.IsCooked()) return false;

        bool isProtein = ingredient.IsProteinIngredient;
        bool isVegetable = ingredient.IsVegetableIngredient;

        if (isProtein && !HasProteinIngredient())
        {
            proteinIngredient = ingredient;
            ingredient.SnapInto(GetProteinAnchor());
            IsRecipeComplete();
            return true;
        }
        else if (isVegetable && !HasVegetableIngredient())
        {
            vegetableIngredient = ingredient;
            ingredient.SnapInto(GetVegetableAnchor());
            IsRecipeComplete();
            return true;
        }

        return false;
    }

    public override bool CanRemoveIngredient()
    {
        // Allow removing from plate (useful while testing).
        return true;
    }

    public override bool RemoveIngredient(KitchenIngredientController ingredient)
    {
        if (ingredient == null) return false;
        if (!CanRemoveIngredient()) return false;

        if (proteinIngredient == ingredient)
        {
            proteinIngredient = null;
            ingredient.OnRemovedFromSlot();
            return true;
        }

        if (vegetableIngredient == ingredient)
        {
            vegetableIngredient = null;
            ingredient.OnRemovedFromSlot();
            return true;
        }

        return false;
    }

    public bool IsRecipeComplete()
    {
        if (recipe == null || proteinIngredient == null || vegetableIngredient == null) return false;
        bool hasRequiredProtein = requiredIngredients.Exists(ing => proteinIngredient != null && ing == proteinIngredient.IngredientType);
        bool hasRequiredVegetable = requiredIngredients.Exists(ing => vegetableIngredient != null && ing == vegetableIngredient.IngredientType);
        if (hasRequiredProtein && hasRequiredVegetable)
        {
            Debug.Log("Recipe complete: " + recipe);
            completionText.text = "You unlocked the " + recipe.ToString() + " recipe!";
        }
        return hasRequiredProtein && hasRequiredVegetable;
    }

}