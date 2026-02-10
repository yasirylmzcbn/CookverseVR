using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitchenController : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private SwitchCamera switchCamera;
    [SerializeField] public Recipe activeRecipe;
    [SerializeField] public List<Ingredient> availableIngredients;
    void Start()
    {
        availableIngredients = Recipes.RecipeIngredients[activeRecipe];
    }

    bool IInteractable.Interact()
    {
        if (switchCamera != null)
        {
            switchCamera.SwitchToKitchenCamera(SwitchCamera.KitchenCameras.Stove);
            return true;
        }
        return false;
    }

}
