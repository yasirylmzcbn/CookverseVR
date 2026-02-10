using UnityEngine;

namespace Cookverse.Assets.Scripts
{
    public interface IIngredientSlot
    {
        float SnapRange { get; }
        bool CanAcceptIngredient(KitchenIngredientController ingredient);
        bool CanRemoveIngredient();
        bool IsWithinSnapRange(Vector3 ingredientWorldPos);
        float DistanceToAnchor(Vector3 worldPos, KitchenIngredientController ingredient);
        bool TryPlaceIngredient(KitchenIngredientController ingredient);
        bool RemoveIngredient(KitchenIngredientController ingredient);
    }

    public interface ISingleAnchorIngredientSlot : IIngredientSlot
    {
        Transform IngredientAnchor { get; }
    }

    public interface IDualAnchorIngredientSlot : IIngredientSlot
    {
        Transform ProteinAnchor { get; }
        Transform VegetableAnchor { get; }
    }

}